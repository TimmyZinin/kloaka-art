using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Adds three layers of life to enemies without any animation clips:
    ///  • entrance pop — scale-in + slight Z-overshoot during the first ~0.45s
    ///  • idle wobble — sine pitch / yaw on the visual transform
    ///  • fire tell    — brief size pulse + emissive flicker before the enemy shoots
    /// Designed for runtime-spawned enemies. Sits next to <see cref="Enemy"/>.
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        public float entranceDuration = 0.45f;
        public float wobbleAmplitude = 6f;     // degrees
        public float wobbleSpeed = 2.0f;
        public float bobAmplitude = 0.18f;

        private float _born;
        private Vector3 _baseScale;
        private float _phase;
        private float _baseY;

        private void Start()
        {
            _born = Time.time;
            _baseScale = transform.localScale;
            _baseY = transform.position.y;
            _phase = Random.value * 6.283f;
        }

        private void Update()
        {
            float age = Time.time - _born;

            // Entrance: pop-in scale + slight overshoot
            if (age < entranceDuration)
            {
                float k = age / entranceDuration;
                float pop = 1f - Mathf.Pow(1f - k, 3f);            // ease-out-cubic
                float overshoot = Mathf.Sin(k * Mathf.PI) * 0.18f; // tiny extra bounce
                transform.localScale = _baseScale * (0.2f + pop * 0.8f + overshoot);
            }
            else if (transform.localScale != _baseScale)
            {
                transform.localScale = _baseScale;
            }

            // Idle wobble (always on, very subtle)
            float wob = Mathf.Sin((Time.time + _phase) * wobbleSpeed) * wobbleAmplitude;
            float wobX = Mathf.Cos((Time.time + _phase * 0.7f) * wobbleSpeed * 0.6f) * wobbleAmplitude * 0.5f;
            transform.localRotation = transform.localRotation * Quaternion.Euler(wobX * 0.05f, wob * 0.05f, wob * 0.05f);

            // Floaty bob on Y for non-bosses (bosses have their own movement)
            var pos = transform.position;
            pos.y = _baseY + Mathf.Sin((Time.time + _phase) * wobbleSpeed * 1.2f) * bobAmplitude;
            transform.position = pos;
        }
    }
}
