using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Cheap fluorescent-tube flicker. Random micro-variance around the base
    /// intensity; occasionally drops to near-zero for a short blink to evoke
    /// a failing office light. Attached to ceiling-panel point lights in
    /// OfficeScene.
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 2.2f;
        public float jitter = 0.15f;           // ±% around base
        public float blinkChance = 0.04f;      // per-second probability of a blink
        public float blinkMinDur = 0.04f;
        public float blinkMaxDur = 0.16f;

        private Light _light;
        private float _blinkEndsAt;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null) baseIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            if (Time.time < _blinkEndsAt)
            {
                _light.intensity = baseIntensity * 0.15f;
                return;
            }
            if (Random.value < blinkChance * Time.deltaTime)
            {
                _blinkEndsAt = Time.time + Random.Range(blinkMinDur, blinkMaxDur);
                return;
            }
            float j = 1f + (Random.value - 0.5f) * jitter;
            _light.intensity = baseIntensity * j;
        }
    }
}
