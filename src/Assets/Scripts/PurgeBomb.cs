using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// "ВЫДОХ" pickup — a rare screen-clearing bomb. Floats into the
    /// corridor like a powerup, but with a much bigger distinctive visual
    /// (glowing ring + the word ВЫДОХ in its centre). On pickup it kills
    /// every regular enemy currently in the scene (bosses are immune),
    /// awards a big score bonus, and plays a cascade of explosion SFX so
    /// it feels like a nuclear strike — the relief of a long breath out
    /// after a day of failed applications.
    ///
    /// Drop rules:
    ///   • Spawned from <see cref="Enemy.Die"/> with a small per-kill chance,
    ///     only while the level isn't in a boss phase (so it can't trivialise
    ///     the boss fight).
    ///   • At most one pickup in flight at a time — see <see cref="InFlight"/>.
    /// </summary>
    public class PurgeBomb : MonoBehaviour
    {
        /// <summary>True while a pickup is currently on-screen.</summary>
        public static bool InFlight { get; private set; }

        public float fallSpeed = 3f;
        public float lifeTime  = 12f;
        private GameManager _manager;
        private float _born;
        private Vector3 _baseScale;

        public static void Spawn(GameManager manager, Vector3 position)
        {
            if (manager == null) return;
            if (InFlight) return;

            var go = new GameObject("PurgeBomb");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.6f;

            var palette = manager.Palette;

            // Outer halo ring — flat sphere, additive yellow glow.
            var ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "Halo";
            ring.transform.SetParent(go.transform, false);
            ring.transform.localScale = new Vector3(1.6f, 0.18f, 1.6f);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<MeshRenderer>().material =
                MaterialFactory.Emissive(new Color(1f, 0.95f, 0.4f),
                                         new Color(1f, 0.85f, 0.1f) * 3.0f, 0f, 1f);

            // Inner core — glowing pink/cyan gem (rotates 3D).
            var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * 0.9f;
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            Object.Destroy(core.GetComponent<Collider>());
            core.GetComponent<MeshRenderer>().material =
                MaterialFactory.Emissive(palette.TorpedoColor,
                                         palette.TorpedoEmissive * 1.5f, 0f, 1f);

            // ВЫДОХ label — 3DText facing camera, visible from a distance.
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            labelGo.transform.localScale = Vector3.one * 0.25f;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "ВЫДОХ";
            tm.fontSize = 120;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.12f;
            tm.color = new Color(1f, 1f, 1f, 1f);
            tm.fontStyle = FontStyle.Bold;

            // Point light so it glows over fog.
            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 4f;
            light.color = new Color(1f, 0.9f, 0.3f);
            light.shadows = LightShadows.None;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 1.2f;
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var bomb = go.AddComponent<PurgeBomb>();
            bomb._manager = manager;
            InFlight = true;

            // One-off pickup announcement so the player notices it.
            HudBinder.FlashBanner("ВЫДОХ! хватай — все сдохнут",
                                   new Color(1f, 0.9f, 0.3f, 1f), 1.6f);
        }

        private void Start()
        {
            _born = Time.time;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            // Slow drift toward player.
            transform.position += Vector3.back * fallSpeed * Time.deltaTime;
            // Big lazy rotation on the core + halo pulsing.
            transform.Rotate(new Vector3(0f, 90f, 0f) * Time.deltaTime, Space.Self);
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.15f;
            transform.localScale = _baseScale * pulse;

            if (Time.time - _born > lifeTime || transform.position.z < -30f)
            {
                InFlight = false;
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            // Kill every non-boss Enemy currently in the scene. Staggered so
            // explosions cascade instead of all firing on the same frame.
            var enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            int staggered = 0;
            foreach (var e in enemies)
            {
                if (e == null || e.IsBoss) continue;
                float delay = staggered++ * 0.03f;
                StartCoroutine(KillDelayed(e, delay));
            }

            // Big score bonus
            if (_manager != null)
            {
                _manager.ScoreKill(500, transform.position, new Color(1f, 0.9f, 0.3f, 1f));
            }
            UmamiTracker.Track("purge_bomb_picked",
                $"{{\"enemies_killed\":{staggered},\"level\":{(_manager != null ? _manager.LevelReached : 1)}}}");
            AudioManager.Play("explode_big", 0.8f, 1.0f);
            AudioManager.Play("level_up",    1.0f, 0.8f);
            CameraShake.Punch(1.0f, 0.6f);
            TimeWarp.Slow(0.35f, 0.3f, 0.45f);
            HudBinder.FlashBanner("ВЫДОХ АКТИВИРОВАН",
                                   new Color(1f, 0.9f, 0.3f, 1f), 1.2f);

            InFlight = false;
            Destroy(gameObject);
        }

        private System.Collections.IEnumerator KillDelayed(Enemy e, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (e == null) yield break;
            // Ram-kill from a null position so player gets no credit — this is
            // a bomb pickup, not a kill streak. Combo stays at its current
            // multiplier (we don't call ScoreKill per-enemy).
            e.OnPlayerRammed();
        }
    }
}
