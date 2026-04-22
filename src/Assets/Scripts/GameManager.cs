using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceShooter
{
    public class GameManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public float Difficulty { get; private set; } = 1f;
        public int Lives { get; private set; } = 3;

        // Kill streak — drops back to 1 after ~2.0s without a kill or on hit.
        public int Combo { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public int MaxMultiplier { get; private set; } = 1;
        public float ComboDecayAt { get; private set; }

        // Session stats — frozen at the moment IsGameOver / IsVictory flips so
        // the Game Over screen displays the final values instead of counting.
        public float SessionSeconds =>
            _endTime > 0f ? _endTime - _startTime : Time.time - _startTime;
        public int LevelReached =>
            LevelManager != null ? LevelManager.CurrentIndex + 1 : 1;

        public Palette Palette { get; private set; }
        public LevelManager LevelManager { get; set; }

        private PlayerController _player;
        private float _startTime;
        private float _endTime;
        private float _iFramesUntil;

        public const int StartingLives = 3;
        private const float ComboWindow = 2.0f;
        private const string PrefHighScore = "ss.highscore";

        public void Init(Palette palette)
        {
            Palette = palette;
            Score = 0;
            IsGameOver = false;
            IsVictory = false;
            Lives = StartingLives;
            _startTime = Time.time;
            HighScore = PlayerPrefs.GetInt(PrefHighScore, 0);
        }

        public void SetPalette(Palette palette)
        {
            Palette = palette;
        }

        public void RegisterPlayer(PlayerController player)
        {
            _player = player;
            if (_player != null) _player.Bind(this);
        }

        private void Update()
        {
            if (!IsGameOver && !IsVictory)
            {
                Difficulty = 1f + (Time.time - _startTime) / 100f;
            }
            if (Combo > 0 && Time.time > ComboDecayAt)
            {
                Combo = 0;
                Multiplier = 1;
            }
        }

        /// <summary>
        /// Old AddScore kept as a backwards-compat shim — use ScoreKill for
        /// combo-aware scoring in new code paths.
        /// </summary>
        public void AddScore(int amount)
        {
            ScoreKill(amount, transform.position, Color.white);
        }

        public void ScoreKill(int baseAmount, Vector3 worldPos, Color popupColor)
        {
            if (IsGameOver) return;
            int prevMul = Multiplier;
            Combo++;
            Multiplier = Combo switch
            {
                < 5  => 1,
                < 10 => 2,
                < 20 => 3,
                < 35 => 4,
                _    => 5,
            };
            ComboDecayAt = Time.time + ComboWindow;
            if (Multiplier > MaxMultiplier) MaxMultiplier = Multiplier;
            if (Multiplier > prevMul)
            {
                AudioManager.Play("combo_up", 1f + 0.1f * Multiplier, 0.8f);
                MegaComboFlash.Flash(Multiplier);
                CameraShake.Punch(0.25f * Multiplier * 0.25f, 0.2f);
            }

            int gained = baseAmount * Multiplier;
            Score += gained;
            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(PrefHighScore, HighScore);
                PlayerPrefs.Save();
            }
            ScorePopup.Spawn(worldPos, gained, Multiplier, popupColor);
        }

        public void BreakCombo()
        {
            if (Combo >= 5) AudioManager.Play("combo_break");
            Combo = 0;
            Multiplier = 1;
        }

        public void OnPlayerHit(Vector3 position)
        {
            if (IsGameOver || IsVictory) return;
            if (Time.time < _iFramesUntil) return;

            _iFramesUntil = Time.time + 1.2f;
            Lives = Mathf.Max(0, Lives - 1);
            BreakCombo();
            ExplosionFX.Spawn(position, 1.6f, Palette.ExplosionColor);
            AudioManager.Play("player_hit");
            CameraShake.Punch(0.8f, 0.5f);
            TimeWarp.Slow(0.4f, 0.1f, 0.3f);

            if (Lives <= 0)
            {
                IsGameOver = true;
                _endTime = Time.time;
                CameraShake.Punch(1.4f, 0.9f);
                TimeWarp.Slow(0.2f, 0.6f, 0.8f);
                AudioManager.DuckMusic(0.05f, 0.5f);
                UmamiTracker.Track("game_over",
                    $"{{\"score\":{Score},\"time_s\":{(int)SessionSeconds},\"level\":{LevelReached},\"max_combo\":{MaxMultiplier}}}");
                if (_player != null)
                {
                    foreach (var mr in _player.GetComponentsInChildren<MeshRenderer>())
                    {
                        mr.enabled = false;
                    }
                }
            }
        }

        public void OnVictory()
        {
            if (IsGameOver || IsVictory) return;
            IsVictory = true;
            _endTime = Time.time;
            AudioManager.Play("level_up", 0.7f);
            AudioManager.DuckMusic(0.08f, 0.6f);
            UmamiTracker.Track("victory",
                $"{{\"score\":{Score},\"time_s\":{(int)SessionSeconds},\"max_combo\":{MaxMultiplier}}}");
        }

        public void Restart()
        {
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}
