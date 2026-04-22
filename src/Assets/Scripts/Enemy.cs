using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Generic enemy component. Replaces the old Asteroid — "Asteroid" is now
    /// just one flavor (<see cref="EnemyFlavor.Asteroid"/>) in the Catalog.
    /// Configured per-spawn by <see cref="EnemySpawner"/>.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyDefinition def;
        public float fallSpeed;
        public Vector3 spin;
        public float radius = 1f;
        public int hp = 1;

        private GameManager _manager;
        private LevelManager _levelManager;
        private bool _dead;
        private float _nextFire;
        private float _bossPhaseStart;
        private bool _telegraphing;

        public bool IsBoss => def.flavor == EnemyFlavor.BossInterviewer || def.flavor == EnemyFlavor.BossPanel;

        public void Configure(GameManager manager, LevelManager levelManager, EnemyDefinition definition, float speed, float size)
        {
            _manager = manager;
            _levelManager = levelManager;
            def = definition;
            fallSpeed = speed;
            radius = size;
            hp = Mathf.Max(1, definition.hp);
            // Every enemy keeps a fixed pose facing the player so its
            // silhouette and face read clearly at camera distance. Only the
            // abstract "asteroid" noise tumbles — it's meant to be
            // featureless debris, not a readable character.
            spin = definition.flavor == EnemyFlavor.Asteroid
                ? new Vector3(Random.Range(-60f, 60f),
                              Random.Range(-60f, 60f),
                              Random.Range(-60f, 60f))
                : Vector3.zero;
            if (definition.shootsBack && definition.fireInterval > 0f)
            {
                _nextFire = Time.time + definition.fireInterval * Random.Range(0.6f, 1.4f);
            }
        }

        private void Update()
        {
            if (_manager != null && _manager.IsGameOver) return;

            if (IsBoss)
            {
                UpdateBossMovement();
            }
            else
            {
                transform.position += Vector3.back * fallSpeed * Time.deltaTime;
            }

            transform.Rotate(spin * Time.deltaTime, Space.Self);

            // Boss telegraph — pulse red 0.3s before each shot so the player
            // has time to dodge. Regular enemies skip it (would be too noisy).
            if (IsBoss && def.shootsBack && !_dead && !_telegraphing
                && Time.time >= _nextFire - 0.3f && Time.time < _nextFire
                && transform.position.z > -10f)
            {
                _telegraphing = true;
                StartCoroutine(TelegraphPulse());
            }

            // Enemies firing back
            if (def.shootsBack && !_dead && Time.time >= _nextFire && transform.position.z > -10f)
            {
                _nextFire = Time.time + def.fireInterval;
                FireAtPlayer();
            }

            // Clean up off-screen (regular enemies only — bosses stay anchored)
            if (!IsBoss && transform.position.z < -30f)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateBossMovement()
        {
            if (_bossPhaseStart <= 0f) _bossPhaseStart = Time.time;
            float t = Time.time - _bossPhaseStart;

            // Settle at a hover z-line, then sway sideways in a sine pattern.
            float targetZ = 14f;
            var pos = transform.position;
            pos.z = Mathf.MoveTowards(pos.z, targetZ, fallSpeed * Time.deltaTime);
            pos.x = Mathf.Sin(t * 0.9f) * 7f;
            transform.position = pos;
        }

        private void FireAtPlayer()
        {
            if (_manager == null) return;
            var palette = _manager.Palette;

            var go = new GameObject("EnemyBullet");
            go.transform.position = transform.position + Vector3.back * 0.8f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
            Object.Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<MeshRenderer>().material =
                MaterialFactory.Emissive(def.tint, def.emissive, 0f, 1f);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.32f;
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var bullet = go.AddComponent<EnemyBullet>();
            bullet.Configure(_manager, 14f);
        }

        public void TakeDamage(int amount, bool byPlayer)
        {
            if (_dead) return;
            hp -= amount;
            var flash = GetComponent<HitFlash>();
            if (flash != null) flash.Flash();
            AudioManager.Play("hit", IsBoss ? 0.7f : 1.05f, IsBoss ? 0.9f : 0.55f);
            if (IsBoss) CameraShake.Punch(0.18f, 0.12f);

            // Small -dmg popup at the hit point so the player feels progress
            // on multi-HP enemies and bosses.
            if (byPlayer && hp > 0 && def.hp > 1)
            {
                ScorePopup.Spawn(transform.position + Vector3.up * 0.4f,
                    amount, 1, new Color(1f, 0.65f, 0.3f, 1f));
            }

            if (hp <= 0)
            {
                Die(byPlayer);
            }
            else
            {
                // Brief flash — scale bump
                transform.localScale *= 1.05f;
            }
        }

        private void Die(bool byPlayer)
        {
            if (_dead) return;
            _dead = true;

            float fx = Mathf.Max(1f, radius);
            Color deathColor = _manager != null ? _manager.Palette.ExplosionColor : Color.magenta;
            // Per-flavor death theme — poop sprays brown chunks, toilet bursts
            // with water droplets, recruiter bots spark electric blue, etc.
            switch (def.flavor)
            {
                case EnemyFlavor.PooEmoji:
                    deathColor = new Color(0.55f, 0.30f, 0.12f); break;   // brown
                case EnemyFlavor.MiniToilet:
                    deathColor = new Color(0.55f, 0.80f, 1.0f); break;    // splash blue
                case EnemyFlavor.BlueRecruiterBot:
                    deathColor = new Color(0.3f, 0.7f, 1f); break;        // electric blue
                case EnemyFlavor.HrBlob:
                    deathColor = new Color(0.55f, 1f, 0.3f); break;       // slime green
                case EnemyFlavor.BlueGuru:
                    deathColor = new Color(0.2f, 0.6f, 1f); break;
            }
            ExplosionFX.Spawn(transform.position, fx, deathColor);
            AudioManager.Play(IsBoss ? "explode_big" : "explode", IsBoss ? 0.85f : 1f);
            CameraShake.Punch(IsBoss ? 0.9f : 0.18f, IsBoss ? 0.55f : 0.18f);
            // Hit-stop: regular kills briefly freeze time for "meatier" feel.
            // Bosses already trigger a bigger slow-mo in LevelManager.
            if (!IsBoss) TimeWarp.Slow(0.1f, 0.05f, 0.15f);

            if (byPlayer && _manager != null)
            {
                var palette = _manager.Palette;
                var popupColor = IsBoss ? new Color(1f, 0.9f, 0.3f, 1f) : palette.ExplosionColor;
                _manager.ScoreKill(def.baseScore, transform.position, popupColor);

                // Powerup drop
                if (def.powerupDropChance > 0f && Random.value < def.powerupDropChance)
                {
                    Powerup.Spawn(_manager, transform.position);
                }

                // PURGE bomb drop — rare screen-clearing pickup ("ВЫДОХ").
                // Only outside boss phase (otherwise it trivialises the boss
                // fight), never from bosses themselves, and only one in
                // flight at a time.
                bool canDrop = !IsBoss
                    && (_levelManager == null || !_levelManager.IsBossPhase)
                    && !PurgeBomb.InFlight;
                if (canDrop && Random.value < 0.025f)
                {
                    PurgeBomb.Spawn(_manager, transform.position);
                }
            }

            if (IsBoss)
            {
                UmamiTracker.Track("boss_killed", $"{{\"flavor\":\"{def.flavor}\"}}");
            }

            // Always notify LevelManager so progression advances even if the
            // enemy was rammed to death (no score, but the kill still counts).
            if (_levelManager != null)
            {
                if (IsBoss) _levelManager.OnBossKilled();
                else _levelManager.OnEnemyKilled();
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Boss windup: flash the model bright red for 0.3s then reset.
        /// Gives the player a readable tell before the shot.
        /// </summary>
        private System.Collections.IEnumerator TelegraphPulse()
        {
            AudioManager.Play("ui_click", 0.5f, 0.6f);
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            var block = new MaterialPropertyBlock();
            Color pulse = new Color(1.6f, 0.15f, 0.15f, 1f);
            block.SetColor("_EmissionColor", pulse);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(block);
            }
            yield return new WaitForSeconds(0.28f);
            // Reset to the subtle rim emission applied at spawn.
            var reset = new MaterialPropertyBlock();
            reset.SetColor("_EmissionColor", def.emissive * 0.04f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].SetPropertyBlock(reset);
            }
            _telegraphing = false;
        }

        /// <summary>
        /// Called when the player ship collides with this enemy.
        /// </summary>
        public void OnPlayerRammed()
        {
            // Enemy is destroyed by the collision too (except bosses, which just take damage)
            if (IsBoss)
            {
                TakeDamage(3, false);
            }
            else
            {
                Die(false);
            }
        }
    }
}
