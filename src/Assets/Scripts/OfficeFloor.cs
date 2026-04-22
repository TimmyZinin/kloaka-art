using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Textured office floor — replaces the neon-grid backdrop. Generates a
    /// carpet-tile texture procedurally (grid of dim speckled squares with
    /// faint seams) and maps it onto a huge horizontal quad that the player
    /// flies over. Tinted per-level via ApplyPalette.
    /// </summary>
    public static class OfficeFloor
    {
        public static MeshRenderer Create(Transform parent, Palette palette)
        {
            var go = new GameObject("OfficeFloor");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, -3f, 80f);
            go.transform.localScale = new Vector3(500f, 1f, 500f);

            // Use a plane instead of a quad — planes have a real normal so
            // Standard shader lights correctly, Quad is a single face.
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(go.transform, false);
            plane.transform.localScale = Vector3.one;

            Object.Destroy(plane.GetComponent<Collider>());
            var mr = plane.GetComponent<MeshRenderer>();
            mr.material = BuildCarpetMaterial(palette);
            mr.receiveShadows = true;
            return mr;
        }

        /// <summary>
        /// Animated scrolling-grid floor — procedural tile pattern that
        /// sweeps toward the camera with glowing seams and accent stripes.
        /// Sells "flying through an office corridor" vs the flat-static PBR
        /// carpet that was reading as murky noise. Falls back to PBR carpet
        /// if the animated shader is stripped (rare).
        /// </summary>
        public static Material BuildCarpetMaterial(Palette palette)
        {
            var animShader = Shader.Find("SpaceShooter/AnimatedFloor");
            if (animShader != null)
            {
                var am = new Material(animShader);
                // Base colour: very dark level-background hue so neon seams pop.
                var baseCol = Color.Lerp(palette.BackgroundColor, Color.black, 0.5f);
                baseCol.a = 1f;
                var seamCol = palette.HorizonColor; seamCol.a = 1f;
                var accentCol = palette.TorpedoEmissive; accentCol.a = 1f;
                am.SetColor("_BaseColor",   baseCol);
                am.SetColor("_SeamColor",   seamCol);
                am.SetColor("_AccentColor", accentCol);
                am.SetFloat("_TileScale",     6.0f);
                am.SetFloat("_SeamWidth",     0.04f);
                am.SetFloat("_ScrollSpeed",   0.55f);
                am.SetFloat("_StripeFreq",    4.0f);
                am.SetFloat("_StripeSpeed",   1.6f);
                am.SetFloat("_StripeStrength",0.45f);
                am.SetFloat("_GlowPower",     3.0f);
                return am;
            }

            // Fallback: the old PBR carpet if the shader isn't available.
            var albedo   = Resources.Load<Texture2D>("Floor/carpet_diff");
            var normal   = Resources.Load<Texture2D>("Floor/carpet_nor_gl");
            var rough    = Resources.Load<Texture2D>("Floor/carpet_rough");
            var ao       = Resources.Load<Texture2D>("Floor/carpet_ao");

            var shader = Shader.Find("Standard");
            if (shader == null || albedo == null)
            {
                return BuildProceduralFallback(palette);
            }

            var m = new Material(shader);
            m.SetTexture("_MainTex", albedo);
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
                // Previous 1.2 made carpet look like a stormy desert under the
                // point-lights. Soften by 4x so it reads as a floor.
                m.SetFloat("_BumpScale", 0.3f);
            }
            if (rough != null)
            {
                var smooth = InvertChannel(rough);
                m.EnableKeyword("_METALLICGLOSSMAP");
                m.SetTexture("_MetallicGlossMap", smooth);
                // Carpet is fully matte now — even subtle gloss under point-lights
                // was causing wet-pavement look.
                m.SetFloat("_GlossMapScale", 0.05f);
            }
            else
            {
                m.SetFloat("_Glossiness", 0.03f);
            }
            m.SetFloat("_Metallic", 0.0f);
            if (ao != null)
            {
                m.EnableKeyword("_OCCLUSIONMAP");
                m.SetTexture("_OcclusionMap", ao);
                m.SetFloat("_OcclusionStrength", 0.85f);
            }

            // Per-level colour tint through albedo _Color
            Color tint = Color.Lerp(Color.white, palette.HorizonColor, 0.25f);
            tint.a = 1f;
            m.SetColor("_Color", tint);

            // Tile size: Plane is 10×10 units, so ~2 carpet-tile repeats/unit
            // gives the right scale for the player speed.
            m.mainTextureScale   = new Vector2(10f, 10f);
            if (m.HasProperty("_BumpMap"))
                m.SetTextureScale("_BumpMap", new Vector2(10f, 10f));
            if (m.HasProperty("_MetallicGlossMap"))
                m.SetTextureScale("_MetallicGlossMap", new Vector2(10f, 10f));
            if (m.HasProperty("_OcclusionMap"))
                m.SetTextureScale("_OcclusionMap", new Vector2(10f, 10f));

            return m;
        }

        /// <summary>
        /// Pixel-by-pixel channel inversion, used to convert a roughness map
        /// into a smoothness map expected by Unity's Standard shader.
        /// </summary>
        private static Texture2D InvertChannel(Texture2D src)
        {
            // Make a readable copy via GetRawTextureData isn't safe for imported
            // textures (non-readable). Use a CPU blit via RenderTexture.
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float r = px[i].r;
                float smooth = 1f - r;
                px[i] = new Color(0f, smooth, 0f, smooth); // smoothness in A channel (Standard)
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Material BuildProceduralFallback(Palette palette)
        {
            Color accent = palette.HorizonColor;
            Color baseColor = Color.Lerp(new Color(0.18f, 0.18f, 0.22f), accent * 0.25f, 0.35f);
            baseColor.a = 1f;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                Color c = baseColor * (0.75f + n * 0.5f);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            var m = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Texture"));
            m.mainTexture = tex;
            m.SetColor("_Color", Color.Lerp(Color.white, accent, 0.2f));
            m.mainTextureScale = new Vector2(10f, 10f);
            return m;
        }
    }
}
