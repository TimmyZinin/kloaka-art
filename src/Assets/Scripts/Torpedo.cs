using UnityEngine;

namespace SpaceShooter
{
    public class Torpedo : MonoBehaviour
    {
        public float speed = 40f;
        public float lifeTime = 3.5f;
        public int damage = 1;
        public Vector3 direction = Vector3.forward;

        private float _born;

        private void Start()
        {
            _born = Time.time;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            direction.Normalize();
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
            if (Time.time - _born > lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Spawn a spark puff at hit point before the torpedo vanishes
                var mgr = Object.FindAnyObjectByType<GameManager>();
                var col = mgr != null ? mgr.Palette.TorpedoEmissive : Color.white;
                ImpactFX.Spawn(transform.position, col);
                enemy.TakeDamage(damage, true);
                Destroy(gameObject);
            }
        }
    }
}
