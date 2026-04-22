using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Attached to Main Camera. Apply a brief positional jitter on top of
    /// whatever the camera transform already is. Decays over <see cref="duration"/>
    /// using an ease-out curve.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _restPos;
        private Quaternion _restRot;
        private float _intensity;
        private float _duration;
        private float _t;

        public static void Punch(float intensity, float duration)
        {
            if (Instance == null) return;
            // If a stronger shake is already running, keep the larger one.
            if (Instance._t < Instance._duration && Instance._intensity > intensity) return;
            Instance._intensity = intensity;
            Instance._duration = Mathf.Max(0.05f, duration);
            Instance._t = 0f;
        }

        private void Awake()
        {
            Instance = this;
            _restPos = transform.position;
            _restRot = transform.rotation;
        }

        private void LateUpdate()
        {
            if (_t >= _duration)
            {
                transform.position = _restPos;
                transform.rotation = _restRot;
                return;
            }
            _t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(_t / _duration);
            float k2 = k * k;
            var jitter = new Vector3(
                (Random.value - 0.5f) * _intensity * k2,
                (Random.value - 0.5f) * _intensity * k2,
                (Random.value - 0.5f) * _intensity * k2 * 0.4f);
            transform.position = _restPos + jitter;
            transform.rotation = _restRot * Quaternion.Euler(jitter.y * 8f, jitter.x * 8f, jitter.x * 4f);
        }
    }
}
