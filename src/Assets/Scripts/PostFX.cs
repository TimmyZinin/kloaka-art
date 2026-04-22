using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Attach to Main Camera. Pipes the rendered frame through a single
    /// fullscreen pass that stacks bloom, chromatic aberration, vignette and
    /// film grain. Works on Built-in RP in WebGL — no PP v2 or URP required.
    /// Per-level tint is updated through <see cref="SetLevel"/>.
    /// </summary>
    [ExecuteAlways]
    public class PostFX : MonoBehaviour
    {
        public Material material;

        // Calmer defaults — previous values fogged everything into a VHS mess.
        // Bloom and chroma are now a subtle accent, not a filter.
        [Range(0f, 1.5f)]  public float bloom = 0.22f;
        [Range(0f, 0.02f)] public float chroma = 0.0012f;
        [Range(0f, 1f)]    public float vignetteStart = 0.45f;
        [Range(0.5f, 1.5f)]public float vignetteEnd   = 1.1f;
        public Color vignetteColor = new Color(0.02f, 0.0f, 0.05f, 1f);
        [Range(0f, 0.2f)]  public float grain = 0.015f;
        // Cartoon pass — 8 levels of posterisation gives obvious banding
        // without making skies look crunchy. 32 effectively disables.
        [Range(2f, 32f)]   public float posterizeSteps = 8f;
        [Range(0.5f, 2f)]  public float saturation = 1.25f;

        public static PostFX Attach(Camera camera)
        {
            if (camera == null) return null;
            var pfx = camera.GetComponent<PostFX>();
            if (pfx == null) pfx = camera.gameObject.AddComponent<PostFX>();
            pfx.EnsureMaterial();
            return pfx;
        }

        public void SetLevelTint(Palette p)
        {
            vignetteColor = Color.Lerp(Color.black, p.BackgroundColor, 0.75f);
            // Bloom cadence is much softer now; still palette-driven but caps
            // below 0.45 so it never overpowers silhouettes.
            float v = Mathf.Clamp01(p.TorpedoEmissive.maxColorComponent / 3.5f);
            bloom = Mathf.Lerp(0.15f, 0.4f, v);
        }

        private void EnsureMaterial()
        {
            if (material != null) return;
            var sh = Shader.Find("Hidden/SpaceShooter/PostFX");
            if (sh == null) return;
            material = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            EnsureMaterial();
            if (material == null) { Graphics.Blit(src, dst); return; }
            material.SetFloat("_BloomStrength",  bloom);
            material.SetFloat("_ChromaStrength", chroma);
            material.SetFloat("_VignetteStart",  vignetteStart);
            material.SetFloat("_VignetteEnd",    vignetteEnd);
            material.SetColor("_VignetteColor",  vignetteColor);
            material.SetFloat("_GrainStrength",  grain);
            material.SetFloat("_PosterizeSteps", posterizeSteps);
            material.SetFloat("_Saturation",     saturation);
            material.SetFloat("_Time2",          Time.unscaledTime);
            Graphics.Blit(src, dst, material);
        }
    }
}
