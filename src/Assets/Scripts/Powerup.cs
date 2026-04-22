using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Upgrade drop that floats slowly toward the player edge. On contact with
    /// the player, bumps the <see cref="WeaponSystem"/> to the next tier.
    /// </summary>
    public class Powerup : MonoBehaviour
    {
        public float fallSpeed = 3f;
        public float lifeTime = 10f;

        private GameManager _manager;
        private float _born;

        public static void Spawn(GameManager manager, Vector3 position)
        {
            if (manager == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Powerup";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.9f;

            // Distinctive outer wrapper — emissive torus-ish via nested scale
            var inner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inner.name = "PowerupCore";
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            inner.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            Object.Destroy(inner.GetComponent<Collider>());

            var palette = manager.Palette;
            var body = go.GetComponent<MeshRenderer>();
            body.material = MaterialFactory.Emissive(
                new Color(1f, 1f, 1f),
                new Color(1f, 1f, 1f) * 1.6f, 0f, 1f);

            var coreMr = inner.GetComponent<MeshRenderer>();
            coreMr.material = MaterialFactory.Emissive(
                palette.TorpedoColor,
                palette.TorpedoEmissive, 0f, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var p = go.AddComponent<Powerup>();
            p._manager = manager;
        }

        private Vector3 _baseScale;

        private void Start()
        {
            _born = Time.time;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            transform.position += Vector3.back * fallSpeed * Time.deltaTime;
            transform.Rotate(new Vector3(30f, 60f, 45f) * Time.deltaTime, Space.Self);

            // Pulse scale so it visually pops vs background.
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.12f;
            transform.localScale = _baseScale * pulse;

            if (Time.time - _born > lifeTime || transform.position.z < -30f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            var weapon = player.Weapon;
            if (weapon != null)
            {
                weapon.Upgrade();
                ExplosionFX.Spawn(transform.position, 0.6f, _manager.Palette.TorpedoColor);
                if (_manager != null) _manager.ScoreKill(100, transform.position, _manager.Palette.TorpedoEmissive);
                AudioManager.Play("powerup");
                CameraShake.Punch(0.18f, 0.18f);
                // Full-screen pickup banner — displays the new weapon tier name.
                var bannerColor = _manager != null ? _manager.Palette.TorpedoEmissive : Color.yellow;
                bannerColor.a = 1f;
                HudBinder.FlashBanner(weapon.Current.displayName.ToUpper() + "  !", bannerColor, 0.9f);
                UmamiTracker.Track("powerup_picked",
                    $"{{\"weapon\":\"{weapon.Current.displayName}\",\"level\":{(_manager != null ? _manager.LevelReached : 1)}}}");
            }
            Destroy(gameObject);
        }
    }
}
