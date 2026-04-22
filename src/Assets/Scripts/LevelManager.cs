using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Drives level progression: tracks kills per level, triggers the boss
    /// spawn, advances to the next level after the boss dies, and re-applies
    /// the new level's palette to camera / fog / neon grid.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public int CurrentIndex { get; private set; }
        public LevelDefinition CurrentLevel => Catalog.Levels[Mathf.Clamp(CurrentIndex, 0, Catalog.Levels.Length - 1)];

        public int KillsInLevel { get; private set; }
        public bool IsBossPhase { get; private set; }
        public Enemy CurrentBoss { get; private set; }
        public bool Completed { get; private set; }

        public float BannerUntil { get; private set; }    // HUD banner for level title

        private GameManager _manager;
        private EnemySpawner _spawner;
        private MeshRenderer _gridRenderer;
        private Camera _camera;
        private ParallaxLayers _parallax;
        private PlayerController _player;
        private LevelDecor _decor;
        private OfficeScene _office;

        public void Init(GameManager manager, EnemySpawner spawner, MeshRenderer gridRenderer, Camera camera,
                         ParallaxLayers parallax = null, PlayerController player = null,
                         LevelDecor decor = null, OfficeScene office = null)
        {
            _manager = manager;
            _spawner = spawner;
            _gridRenderer = gridRenderer;
            _camera = camera;
            _parallax = parallax;
            _player = player;
            _decor = decor;
            _office = office;
            CurrentIndex = 0;
            KillsInLevel = 0;
            IsBossPhase = false;
            Completed = false;
            ApplyPalette();
            ShowBanner();
            AudioManager.SwitchMusic(CurrentIndex);
            UmamiTracker.Track("level_start",
                $"{{\"level\":{CurrentIndex + 1},\"title\":\"{CurrentLevel.title}\"}}");
        }

        public void OnEnemyKilled()
        {
            if (IsBossPhase || Completed) return;
            KillsInLevel++;
            if (KillsInLevel >= CurrentLevel.killsToBoss)
            {
                TriggerBoss();
            }
        }

        private void TriggerBoss()
        {
            IsBossPhase = true;
            if (_spawner != null)
            {
                // Spawn boss a bit higher + closer so office props can't eclipse it.
                var pos = new Vector3(0f, 1.5f, 20f);
                CurrentBoss = _spawner.SpawnSpecific(CurrentLevel.boss, pos);
            }
            AudioManager.Play("boss_in", 1f, 1.0f);
            CameraShake.Punch(0.6f, 0.8f);
            AudioManager.DuckMusic(0.10f, 0.4f);
            if (CurrentBoss != null) BossIntro.Get().Play(CurrentBoss.transform);
        }

        public void OnBossKilled()
        {
            if (!IsBossPhase) return;
            IsBossPhase = false;
            CurrentBoss = null;

            // Cinematic on boss death — slow-mo + big shake + flash.
            TimeWarp.Slow(0.25f, 0.45f, 0.6f);
            CameraShake.Punch(1.2f, 0.7f);
            AudioManager.Play("explode_big", 0.8f, 1.0f);
            AudioManager.DuckMusic(0.18f, 0.6f);

            if (CurrentIndex >= Catalog.Levels.Length - 1)
            {
                Completed = true;
                if (_manager != null) _manager.OnVictory();
                return;
            }

            CurrentIndex++;
            KillsInLevel = 0;
            ApplyPalette();
            ShowBanner();
            AudioManager.Play("level_up");
            AudioManager.SwitchMusic(CurrentIndex);
            UmamiTracker.Track("level_start",
                $"{{\"level\":{CurrentIndex + 1},\"title\":\"{CurrentLevel.title}\"}}");
        }

        private void ShowBanner()
        {
            BannerUntil = Time.time + 4.5f;
        }

        /// <summary>
        /// How far through the banner we are, 0 = just started, 1 = ending.
        /// Used by HUD to do in/out animations + dim overlay.
        /// </summary>
        public float BannerProgress01()
        {
            float total = 4.5f;
            float left = BannerUntil - Time.time;
            return Mathf.Clamp01(1f - left / total);
        }

        private void ApplyPalette()
        {
            var p = CurrentLevel.palette;
            if (_manager != null) _manager.SetPalette(p);
            if (_camera != null)
            {
                _camera.backgroundColor = p.BackgroundColor;
            }
            RenderSettings.fogColor = p.BackgroundColor;
            if (_gridRenderer != null)
            {
                _gridRenderer.material = OfficeFloor.BuildCarpetMaterial(p);
            }
            if (_parallax != null) _parallax.ApplyPalette(p);
            if (_decor    != null) _decor.Apply(CurrentIndex, p);
            if (_office   != null) _office.ApplyPalette(p);
            if (_player != null)
            {
                var pv = _player.GetComponent<PlayerVisuals>();
                if (pv != null) pv.Apply(p);
            }
            // Swap skybox + post-FX tint for the new level.
            if (_camera != null)
            {
                SkyboxFactory.Apply(_camera, CurrentIndex, p);
                var pfx = _camera.GetComponent<PostFX>();
                if (pfx != null) pfx.SetLevelTint(p);
            }
        }
    }
}
