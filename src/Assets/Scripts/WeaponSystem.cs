using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Owns the player's current weapon tier and spawns torpedoes/bolts when
    /// <see cref="TryFire"/> is called. Upgrades via <see cref="Upgrade"/> which
    /// progresses Single → Double → Spread3 → Laser.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        public WeaponTier Tier { get; private set; } = WeaponTier.Single;
        public WeaponTierDef Current => Catalog.GetWeapon(Tier);

        private GameManager _manager;
        private Transform _muzzle;
        private float _nextFire;

        public void Init(GameManager manager, Transform muzzle)
        {
            _manager = manager;
            _muzzle = muzzle;
            Tier = WeaponTier.Single;
            _nextFire = 0f;
        }

        public void Reset()
        {
            Tier = WeaponTier.Single;
        }

        public void Upgrade()
        {
            Tier = Catalog.NextTier(Tier);
        }

        public bool TryFire()
        {
            if (_manager == null || _muzzle == null) return false;
            if (Time.time < _nextFire) return false;
            var w = Current;
            _nextFire = Time.time + w.cooldown;

            for (int i = 0; i < w.angles.Length; i++)
            {
                SpawnBolt(w, w.angles[i]);
            }

            // Muzzle flash — brief additive point-light + scale pop at the nose.
            SpawnMuzzleFlash();

            // Audio — laser tier gets its own sound.
            string sfx = w.tier == WeaponTier.Laser ? "shot_laser" : "shot";
            float vol = w.tier == WeaponTier.Spread3 ? 0.45f : 0.65f;
            AudioManager.Play(sfx, 1f, vol);
            return true;
        }

        private void SpawnMuzzleFlash()
        {
            var palette = _manager.Palette;
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(_muzzle, false);
            go.transform.localPosition = new Vector3(0f, 0.1f, 1.2f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = palette.TorpedoEmissive;
            light.intensity = 4f;
            light.range = 6f;
            light.shadows = LightShadows.None;

            // Small glowing sphere so you see the flash even in peripheral vision
            var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = Vector3.one * 0.45f;
            Object.Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<MeshRenderer>().material =
                MaterialFactory.Emissive(palette.TorpedoColor, palette.TorpedoEmissive * 1.6f, 0f, 1f);

            go.AddComponent<MuzzleFlashFade>();
            Destroy(go, 0.10f);
        }

        private void SpawnBolt(WeaponTierDef w, float angleDeg)
        {
            var palette = _manager.Palette;
            var go = new GameObject("Torpedo_" + w.tier);
            go.transform.position = _muzzle.position + Vector3.forward * 1.3f;

            // Laser tier: longer, thinner, faster, higher damage
            bool laser = w.tier == WeaponTier.Laser;

            // Non-laser: prefer the Tripo resume-ball projectile; fall back to
            // a tinted capsule. Laser keeps its capsule so it reads as a beam.
            GameObject visual = null;
            if (!laser)
            {
                var ballPrefab = Resources.Load<GameObject>("Player/resume_ball");
                if (ballPrefab != null)
                {
                    visual = Instantiate(ballPrefab, go.transform);
                    visual.name = "Body";
                    NormaliseBall(visual, 0.55f);
                    var spin = visual.AddComponent<Spinner>();
                    spin.rps = new Vector3(280f, 360f, 200f);
                }
            }
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.transform.localScale = laser
                    ? new Vector3(0.18f, 1.4f, 0.18f)
                    : new Vector3(0.25f, 0.7f, 0.25f);
                Object.Destroy(visual.GetComponent<Collider>());
                visual.GetComponent<MeshRenderer>().material =
                    MaterialFactory.Emissive(palette.TorpedoColor, palette.TorpedoEmissive, 0f, 1f);
            }

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.35f;
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var torpedo = go.AddComponent<Torpedo>();
            torpedo.damage = w.damage;
            if (laser) torpedo.speed = 60f;

            // Trail — gives torpedoes a satisfying neon streak.
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = laser ? 0.18f : 0.10f;
            trail.startWidth = laser ? 0.22f : 0.16f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.material = MaterialFactory.UnlitParticle(palette.TorpedoEmissive);
            trail.startColor = palette.TorpedoEmissive;
            var tail = palette.TorpedoColor; tail.a = 0f;
            trail.endColor = tail;

            // Apply angle (slight spread per angleDeg)
            float rad = angleDeg * Mathf.Deg2Rad;
            torpedo.direction = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
            go.transform.rotation = Quaternion.LookRotation(torpedo.direction);
        }

        /// <summary>
        /// Scale-down the resume-ball GLB to occupy <paramref name="targetExtent"/>
        /// in its longest axis. Uses mesh-local bounds so world position is
        /// irrelevant.
        /// </summary>
        private static void NormaliseBall(GameObject glb, float targetExtent)
        {
            var filters = glb.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;
            bool have = false;
            Bounds b = new Bounds();
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var local = mf.sharedMesh.bounds;
                var mat = glb.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var min = local.min; var max = local.max;
                for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                for (int cz = 0; cz < 2; cz++)
                {
                    var p = new Vector3(cx == 0 ? min.x : max.x, cy == 0 ? min.y : max.y, cz == 0 ? min.z : max.z);
                    var wp = mat.MultiplyPoint3x4(p);
                    if (!have) { b = new Bounds(wp, Vector3.zero); have = true; }
                    else b.Encapsulate(wp);
                }
            }
            if (!have) return;
            float maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxExtent < 0.0001f) return;
            float k = targetExtent * 2f / maxExtent;
            glb.transform.localScale = Vector3.one * k;
            glb.transform.localPosition = -b.center * k;
        }
    }

    /// <summary>
    /// Shrinks the muzzle flash over its 100ms life so it "pops" and fades.
    /// </summary>
    public class MuzzleFlashFade : MonoBehaviour
    {
        private float _born;
        private Vector3 _startScale;
        private Light _light;
        private float _baseIntensity;

        private void Start()
        {
            _born = Time.time;
            _startScale = transform.localScale;
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
        }

        private void Update()
        {
            float k = 1f - Mathf.Clamp01((Time.time - _born) / 0.10f);
            transform.localScale = _startScale * (0.5f + k * 0.7f);
            if (_light != null) _light.intensity = _baseIntensity * k;
        }
    }

    /// <summary>
    /// Tiny Update-driven rotator — resume-ball projectiles tumble as they fly.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 rps = new Vector3(180f, 180f, 180f);
        private void Update() => transform.Rotate(rps * Time.deltaTime, Space.Self);
    }
}
