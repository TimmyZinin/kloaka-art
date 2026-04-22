using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Projectile fired by shooting enemies. Travels in -Z toward the player.
    /// Hits the player; ignores other enemies so friendly fire is not a thing.
    /// </summary>
    public class EnemyBullet : MonoBehaviour
    {
        public float speed = 14f;
        public float lifeTime = 4f;

        private GameManager _manager;
        private float _born;

        public void Configure(GameManager manager, float projectileSpeed)
        {
            _manager = manager;
            speed = projectileSpeed;
        }

        private void Start() => _born = Time.time;

        private void Update()
        {
            transform.position += Vector3.back * speed * Time.deltaTime;
            if (Time.time - _born > lifeTime || transform.position.z < -30f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && _manager != null)
            {
                _manager.OnPlayerHit(transform.position);
                Destroy(gameObject);
            }
        }
    }
}
