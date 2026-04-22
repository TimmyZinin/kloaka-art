using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Swaps Unity's solid-colour background for a per-level equirectangular
    /// panorama (Poly Haven CC0 HDRIs). On the Built-in RP we use the stock
    /// "Skybox/Panoramic" shader which samples a lat-long texture directly.
    /// Also pipes the skybox into the ambient light source so props get
    /// picked up by image-based lighting for free.
    /// </summary>
    public static class SkyboxFactory
    {
        private static readonly string[] PerLevelPanorama =
        {
            "Skybox/large_corridor",     // barakholka — dim office corridor
            "Skybox/cinema_lobby",       // blue galaxy — bright lobby
            "Skybox/unfinished_office",  // HR swamp — murky half-built office
            "Skybox/entrance_hall",      // final — imposing entrance hall
        };

        public static void Apply(Camera camera, int levelIndex, Palette palette)
        {
            if (camera == null) return;
            string resPath = PerLevelPanorama[Mathf.Clamp(levelIndex, 0, PerLevelPanorama.Length - 1)];
            var tex = Resources.Load<Texture2D>(resPath);
            if (tex == null)
            {
                // Fallback — keep the solid-colour background.
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = palette.BackgroundColor;
                return;
            }

            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                // Shader stripped — can happen in WebGL. Fall back to colour.
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = palette.BackgroundColor;
                return;
            }

            var mat = new Material(shader);
            mat.SetTexture("_MainTex", tex);
            // Tint toward the level palette so each level still reads as a
            // distinct colour even though the panorama is fixed.
            if (mat.HasProperty("_Tint"))
            {
                var tint = Color.Lerp(Color.white, palette.BackgroundColor * 2f, 0.35f);
                tint.a = 1f;
                mat.SetColor("_Tint", tint);
            }
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1.1f);
            if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", 0f);

            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();

            camera.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
