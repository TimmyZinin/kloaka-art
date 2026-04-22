using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Very small perlin-driven camera drift so the scene doesn't feel frozen
    /// between combat beats. Runs on top of CameraShake's rest pose.
    /// </summary>
    public class CameraIdleBob : MonoBehaviour
    {
        public float ampPos = 0.12f;
        public float ampRot = 0.55f;
        public float speed  = 0.35f;

        private Vector3 _restPos;
        private Quaternion _restRot;
        private float _seed;

        private void Awake()
        {
            _restPos = transform.position;
            _restRot = transform.rotation;
            _seed = Random.Range(0f, 100f);
        }

        private void LateUpdate()
        {
            // Don't fight CameraShake. CameraShake (LateUpdate too) overwrites
            // position/rotation on shake frames; we run AFTER via `LateUpdate`
            // + `DefaultExecutionOrder` not set, so Unity order is arbitrary.
            // Apply additive offset on top of whatever position is currently
            // set.
            float t = (Time.unscaledTime + _seed) * speed;
            float nx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(t * 0.7f, t * 1.3f) - 0.5f) * 2f;

            transform.position += new Vector3(nx, ny, nz * 0.4f) * ampPos;
            transform.rotation = Quaternion.Euler(ny * ampRot, nx * ampRot, 0f) * transform.rotation;
        }
    }
}
