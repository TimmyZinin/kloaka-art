using UnityEngine;

namespace SpaceShooter
{
    public class ExplosionFX : MonoBehaviour
    {
        public static void Spawn(Vector3 pos, float scale, Color color)
        {
            var go = new GameObject("Explosion");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * scale, 10f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.45f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(80f * scale));
            emission.SetBursts(new[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(Color.black, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialFactory.UnlitParticle(color);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Add a short-lived point light for the flash
            var lightGo = new GameObject("Flash");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 8f * scale;
            light.intensity = 4f;

            var flash = lightGo.AddComponent<FlashLight>();
            flash.lifetime = 0.35f;

            Object.Destroy(go, 1.6f);
        }
    }

    public class FlashLight : MonoBehaviour
    {
        public float lifetime = 0.3f;
        private Light _light;
        private float _born;
        private float _baseIntensity;

        private void Start()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _born = Time.time;
        }

        private void Update()
        {
            float t = (Time.time - _born) / lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            _light.intensity = Mathf.Lerp(_baseIntensity, 0f, t);
        }
    }
}
