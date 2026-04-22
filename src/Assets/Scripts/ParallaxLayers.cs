using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Layers of depth around the play field:
    ///  • Three nebula slabs at different Z distances, slowly drifting + tinted
    ///    by the active palette
    ///  • Floating "office debris" — small primitive shapes that drift forward
    ///    past the camera giving a sense of speed
    /// Both are recreated/retinted via <see cref="ApplyPalette"/> when the level changes.
    /// </summary>
    public class ParallaxLayers : MonoBehaviour
    {
        public static ParallaxLayers Instance { get; private set; }

        private GameObject _root;
        private MeshRenderer[] _nebulas;
        private Transform[] _debris;
        private float[] _debrisSpeeds;
        private Palette _palette;

        public static ParallaxLayers Create(Transform parent, Palette palette)
        {
            var go = new GameObject("Parallax");
            go.transform.SetParent(parent);
            var p = go.AddComponent<ParallaxLayers>();
            p._root = go;
            p.Build(palette);
            Instance = p;
            return p;
        }

        private void Build(Palette palette)
        {
            _palette = palette;
            _nebulas = new MeshRenderer[3];

            float[] zs = { 220f, 320f, 420f };
            float[] scales = { 320f, 460f, 700f };
            float[] alphas = { 0.55f, 0.4f, 0.25f };
            for (int i = 0; i < 3; i++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Nebula_" + i;
                quad.transform.SetParent(_root.transform, false);
                quad.transform.localPosition = new Vector3(
                    Random.Range(-30f, 30f),
                    Random.Range(-15f, 35f),
                    zs[i]);
                quad.transform.localScale = Vector3.one * scales[i];
                quad.transform.localRotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), Random.Range(0f, 360f));
                Object.Destroy(quad.GetComponent<Collider>());
                var mr = quad.GetComponent<MeshRenderer>();
                mr.material = MakeNebulaMaterial(BlendNebulaColor(palette, i), alphas[i]);
                _nebulas[i] = mr;
            }

            // Debris swarm — 24 tiny primitives that drift past the camera
            int n = 24;
            _debris = new Transform[n];
            _debrisSpeeds = new float[n];
            for (int i = 0; i < n; i++)
            {
                var t = SpawnDebris(palette);
                _debris[i] = t.transform;
                _debrisSpeeds[i] = Random.Range(3f, 7f);
            }
        }

        private GameObject SpawnDebris(Palette palette)
        {
            var prim = Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var go = GameObject.CreatePrimitive(prim);
            go.name = "Debris";
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(
                Random.Range(-22f, 22f),
                Random.Range(-6f, 12f),
                Random.Range(40f, 200f));
            float s = Random.Range(0.18f, 0.4f);
            go.transform.localScale = new Vector3(s, s * Random.Range(0.6f, 2.5f), s * 0.4f);
            go.transform.localRotation = Random.rotation;
            Object.Destroy(go.GetComponent<Collider>());
            var c = palette.HorizonColor * Random.Range(0.4f, 1.1f);
            c.a = 1f;
            go.GetComponent<MeshRenderer>().material =
                MaterialFactory.Emissive(c, c * 1.3f, 0.1f, 0.7f);
            return go;
        }

        private static Color BlendNebulaColor(Palette palette, int i)
        {
            switch (i)
            {
                case 0: return Color.Lerp(palette.HorizonColor, palette.TorpedoColor, 0.4f);
                case 1: return Color.Lerp(palette.HorizonColor, palette.AsteroidEmissive, 0.5f);
                default: return Color.Lerp(palette.HorizonColor, palette.ShipEmissive, 0.3f);
            }
        }

        private static Material MakeNebulaMaterial(Color color, float alpha)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * 0.5f) / (size * 0.5f);
                float dy = (y - size * 0.5f) / (size * 0.5f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float n = Mathf.PerlinNoise(x * 0.025f, y * 0.025f);
                float falloff = Mathf.Clamp01(1f - r);
                falloff *= falloff;
                var c = color;
                c.a = alpha * falloff * (0.55f + n * 0.7f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            var m = new Material(shader);
            m.mainTexture = tex;
            return m;
        }

        public void ApplyPalette(Palette palette)
        {
            _palette = palette;
            float[] alphas = { 0.55f, 0.4f, 0.25f };
            for (int i = 0; i < _nebulas.Length; i++)
            {
                if (_nebulas[i] == null) continue;
                _nebulas[i].material = MakeNebulaMaterial(BlendNebulaColor(palette, i), alphas[i]);
            }
            for (int i = 0; i < _debris.Length; i++)
            {
                if (_debris[i] == null) continue;
                var c = palette.HorizonColor * Random.Range(0.4f, 1.1f);
                c.a = 1f;
                _debris[i].GetComponent<MeshRenderer>().material =
                    MaterialFactory.Emissive(c, c * 1.3f, 0.1f, 0.7f);
            }
        }

        private void Update()
        {
            // Slow nebula rotation for life
            for (int i = 0; i < _nebulas.Length; i++)
            {
                if (_nebulas[i] == null) continue;
                _nebulas[i].transform.Rotate(Vector3.forward, (i + 1) * 0.6f * Time.deltaTime);
            }

            // Drift debris forward; recycle when past camera
            for (int i = 0; i < _debris.Length; i++)
            {
                var t = _debris[i];
                if (t == null) continue;
                var p = t.localPosition;
                p.z -= _debrisSpeeds[i] * Time.deltaTime;
                if (p.z < -25f)
                {
                    p.x = Random.Range(-22f, 22f);
                    p.y = Random.Range(-6f, 12f);
                    p.z = Random.Range(160f, 220f);
                    _debrisSpeeds[i] = Random.Range(3f, 7f);
                }
                t.localPosition = p;
                t.Rotate(Random.onUnitSphere, 30f * Time.deltaTime, Space.Self);
            }
        }
    }
}
