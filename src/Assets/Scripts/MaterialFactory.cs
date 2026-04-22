using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Creates runtime materials without relying on editor-authored assets.
    /// Uses Unity built-in "Standard" shader which is always available in Built-in RP.
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader _standard;
        private static Shader Standard
        {
            get
            {
                if (_standard == null)
                {
                    _standard = Shader.Find("Standard");
                    if (_standard == null) _standard = Shader.Find("Universal Render Pipeline/Lit");
                    if (_standard == null) _standard = Shader.Find("HDRP/Lit");
                    if (_standard == null)
                    {
                        Debug.LogError("MaterialFactory: no usable PBR shader found");
                        _standard = Shader.Find("Unlit/Color");
                    }
                }
                return _standard;
            }
        }

        private static Shader _unlit;
        private static Shader Unlit
        {
            get
            {
                if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
                return _unlit;
            }
        }

        private static Shader _particle;
        private static Shader Particle
        {
            get
            {
                if (_particle == null) _particle = Shader.Find("Sprites/Default");
                if (_particle == null) _particle = Shader.Find("Unlit/Transparent");
                return _particle;
            }
        }

        public static Material Emissive(Color baseColor, Color emissive, float metallic = 0.2f, float smoothness = 0.8f)
        {
            var m = new Material(Standard);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emissive);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        public static Material Simple(Color baseColor, float metallic = 0.2f, float smoothness = 0.6f)
        {
            var m = new Material(Standard);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material UnlitParticle(Color color)
        {
            var m = new Material(Particle);
            m.SetColor("_Color", color);
            return m;
        }

        public static Material NeonGrid(Color lineColor)
        {
            // Generate a grid texture procedurally
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            Color bg = new Color(0.02f, 0f, 0.05f, 1f);
            Color line = lineColor;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = (x % 64 < 2) || (y % 64 < 2);
                    tex.SetPixel(x, y, isLine ? line : bg);
                }
            }
            tex.Apply();

            var m = new Material(Shader.Find("Unlit/Texture"));
            m.mainTexture = tex;
            m.mainTextureScale = new Vector2(40f, 40f);
            return m;
        }
    }
}
