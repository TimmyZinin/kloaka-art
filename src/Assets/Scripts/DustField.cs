using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Thin volumetric-looking dust field through the corridor. Pre-emitted
    /// ParticleSystem with tiny additive particles drifting in world space —
    /// sells "light rays through dusty air" especially through the ceiling
    /// panel lights.
    /// </summary>
    public static class DustField
    {
        public static GameObject Create(Transform parent, Palette palette)
        {
            var go = new GameObject("DustField");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, 5f, 60f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 9999f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.8f, 0.35f),
                new Color(1f, 1f, 1f, 0.2f));
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 14f, 260f);
            shape.position = Vector3.zero;

            ps.Emit(500);

            // Very slow ambient drift (Y up, slight Z back toward camera)
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.8f, -1.4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialFactory.UnlitParticle(new Color(1f, 0.95f, 0.85f, 1f));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return go;
        }
    }
}
