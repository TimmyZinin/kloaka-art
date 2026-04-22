using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Thematic background decoration for each level: floating
    /// "billboards" (text quads) drifting toward camera, level-specific
    /// silhouette landmarks (barakholka towers, blue corporate cubes,
    /// HR-swamp pillars, final-interview chairs). Renders behind enemies
    /// so the field stops reading as empty void.
    ///
    /// Created and re-decorated by LevelManager on every palette change.
    /// </summary>
    public class LevelDecor : MonoBehaviour
    {
        public static LevelDecor Instance { get; private set; }

        private Transform _root;
        private Transform[] _drift;          // forward-drifting billboards / silhouettes
        private float[]    _driftSpeeds;
        private Texture2D[] _signTextures;   // pool of generated text textures

        private static readonly string[][] LevelPhrases =
        {
            // barakholka — trash-vacancy feed
            new[] { "ВАКАНСИЯ", "ОТКЛИК", "RESUME", "JOB", "СЕНЬОР C++", "JUNIOR ALL", "ZARPLATA", "SOFT-SKILLS", "HR-СКРИНИНГ", "300К/МЕС" },
            // blue galaxy — corporate influencer hashtags
            new[] { "#OPENTOWORK", "#HUSTLE", "#GRINDSET", "MENTOR", "GROWTH", "10X DEV", "THOUGHT LEADER", "BOOK A CALL", "AGILE NINJA", "SYNERGY" },
            // HR swamp
            new[] { "ТЕСТ ЗАДАНИЕ", "5 ЭТАПОВ", "ОБРАТНАЯ СВЯЗЬ", "FEEDBACK?", "СОБЕС", "REJECTED", "GHOSTED", "TAKE-HOME", "CASE-STUDY", "STAGE 7/12" },
            // Final
            new[] { "ГДЕ СЕБЯ ВИДИТЕ?", "ВАШИ СЛАБОСТИ?", "ОЖИДАЕМАЯ ЗП?", "ПОЧЕМУ К НАМ?", "STRESS-INTERVIEW", "CULTURE-FIT", "FAMILY?", "OVERTIME?", "WHY US?", "TELL ABOUT YOU" },
        };

        public static LevelDecor Create(Transform parent)
        {
            var go = new GameObject("LevelDecor");
            go.transform.SetParent(parent);
            var d = go.AddComponent<LevelDecor>();
            d._root = go.transform;
            Instance = d;
            return d;
        }

        public void Apply(int levelIndex, Palette palette)
        {
            // Wipe old decor (level swap)
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Destroy(_root.GetChild(i).gameObject);
            }

            var phrases = LevelPhrases[Mathf.Clamp(levelIndex, 0, LevelPhrases.Length - 1)];

            // Billboards — fewer, smaller, further away, so they decorate the
            // background instead of taking over the screen. Also skip spawning
            // anything in the mid-Y range where the play-field lives.
            int billboardCount = 10;
            _drift = new Transform[billboardCount];
            _driftSpeeds = new float[billboardCount];

            for (int i = 0; i < billboardCount; i++)
            {
                string text = phrases[Random.Range(0, phrases.Length)];
                var tex = MakeBillboardTexture(text, palette);
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
                mat.mainTexture = tex;
                mat.color = new Color(1f, 1f, 1f, 0.5f);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Sign_" + text;
                quad.transform.SetParent(_root, false);
                Object.Destroy(quad.GetComponent<Collider>());
                quad.GetComponent<MeshRenderer>().material = mat;

                // Spawn only in the upper half of the sky (y > 12) or far above
                // the grid, never in the action area.
                quad.transform.localPosition = new Vector3(
                    Random.Range(-45f, 45f),
                    Random.Range(14f, 30f),
                    Random.Range(120f, 260f));
                quad.transform.localRotation = Quaternion.Euler(10f, Random.Range(-18f, 18f), Random.Range(-4f, 4f));
                quad.transform.localScale = new Vector3(
                    Random.Range(4f, 7f),
                    Random.Range(1.4f, 2.4f),
                    1f);

                _drift[i] = quad.transform;
                _driftSpeeds[i] = Random.Range(2.0f, 4.0f);
            }

            // Per-level silhouette landmarks far on the horizon — small and
            // low-contrast so they read as distant scenery.
            int silhouettes = 6;
            for (int i = 0; i < silhouettes; i++)
            {
                var s = MakeSilhouette(levelIndex, palette);
                s.transform.SetParent(_root, false);
                s.transform.localPosition = new Vector3(
                    Random.Range(-60f, 60f),
                    -1.5f,
                    Random.Range(200f, 320f));
                s.transform.localRotation = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);
            }
        }

        private GameObject MakeSilhouette(int levelIndex, Palette palette)
        {
            // Different per-level shape, all unlit, dark colour against bg fog.
            GameObject go;
            Color tone = Color.Lerp(palette.BackgroundColor, palette.HorizonColor, 0.55f);
            tone.a = 1f;
            switch (levelIndex)
            {
                case 0: // barakholka — tall ugly office towers
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(Random.Range(3f, 6f),
                                                          Random.Range(20f, 35f),
                                                          Random.Range(3f, 6f));
                    break;
                case 1: // blue galaxy — chunky cyan corporate cubes
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(Random.Range(6f, 10f),
                                                          Random.Range(8f, 14f),
                                                          Random.Range(6f, 10f));
                    tone = palette.HorizonColor * 0.6f;
                    break;
                case 2: // HR swamp — twisted broken pillars
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(Random.Range(2f, 4f),
                                                          Random.Range(8f, 22f),
                                                          Random.Range(2f, 4f));
                    break;
                default: // final — colossal CEO chairs (capsules as backrests)
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.transform.localScale = new Vector3(Random.Range(5f, 8f),
                                                          Random.Range(15f, 28f),
                                                          Random.Range(4f, 6f));
                    tone = new Color(0.4f, 0.05f, 0.1f, 1f);
                    break;
            }
            go.name = "Silhouette";
            Object.Destroy(go.GetComponent<Collider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = MaterialFactory.Emissive(tone, tone * 0.4f, 0.1f, 0.5f);
            return go;
        }

        private static Texture2D MakeBillboardTexture(string text, Palette palette)
        {
            int w = 512, h = 192;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var bg = new Color(0.05f, 0.0f, 0.08f, 0.85f);
            var stripe = palette.HorizonColor * 0.7f; stripe.a = 1f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || x > w - 5 || y < 4 || y > h - 5;
                    bool stripeBand = (y / 8) % 2 == 0 && y < 14;
                    tex.SetPixel(x, y, border ? stripe : (stripeBand ? stripe * 0.5f : bg));
                }
            }

            // Cheap pixel-font writer — block letters via 5×7 dot patterns.
            DrawText(tex, text, palette.HorizonColor);
            tex.Apply();
            return tex;
        }

        private static void DrawText(Texture2D tex, string text, Color color)
        {
            // Compact 5x7 bitmap font — ASCII subset enough for our slogans.
            // Each glyph is 5 cols × 7 rows, encoded as 5 bytes (LSB = top row).
            // Only chars present in our LevelPhrases need to be defined.
            // Missing glyph → simple solid block.
            string up = text.ToUpperInvariant();

            int charW = 5, charH = 7;
            int scale = Mathf.Max(2, (tex.width - 32) / Mathf.Max(1, up.Length * (charW + 1)));
            int totalW = up.Length * (charW + 1) * scale;
            int x0 = (tex.width - totalW) / 2;
            int y0 = (tex.height - charH * scale) / 2;

            int cx = x0;
            for (int i = 0; i < up.Length; i++)
            {
                var glyph = GetGlyph(up[i]);
                for (int gy = 0; gy < charH; gy++)
                {
                    for (int gx = 0; gx < charW; gx++)
                    {
                        bool on = (glyph[gx] & (1 << gy)) != 0;
                        if (!on) continue;
                        for (int sx = 0; sx < scale; sx++)
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int px = cx + gx * scale + sx;
                            int py = y0 + (charH - 1 - gy) * scale + sy;
                            if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                                tex.SetPixel(px, py, color);
                        }
                    }
                }
                cx += (charW + 1) * scale;
            }
        }

        // Minimal 5×7 font (column bitmaps, top bit = row 0).
        // Covers A-Z, 0-9, ?, !, /, -, space, plus Cyrillic ABC subset
        // (we render Cyrillic letters as best-effort lookalikes from the Latin set).
        private static byte[] GetGlyph(char c)
        {
            // Cyrillic → Latin lookalike fallback (visual only, not transliteration).
            switch (c)
            {
                case 'А': c = 'A'; break;
                case 'Б': c = 'B'; break;
                case 'В': c = 'B'; break;
                case 'Г': c = 'r'; break;
                case 'Д': c = 'D'; break;
                case 'Е': case 'Ё': c = 'E'; break;
                case 'Ж': c = 'X'; break;
                case 'З': c = '3'; break;
                case 'И': case 'Й': c = 'N'; break;
                case 'К': c = 'K'; break;
                case 'Л': c = 'L'; break;
                case 'М': c = 'M'; break;
                case 'Н': c = 'H'; break;
                case 'О': c = 'O'; break;
                case 'П': c = 'n'; break;
                case 'Р': c = 'P'; break;
                case 'С': c = 'C'; break;
                case 'Т': c = 'T'; break;
                case 'У': c = 'Y'; break;
                case 'Ф': c = 'O'; break;
                case 'Х': c = 'X'; break;
                case 'Ц': c = 'U'; break;
                case 'Ч': c = '4'; break;
                case 'Ш': case 'Щ': c = 'W'; break;
                case 'Ъ': case 'Ь': c = 'b'; break;
                case 'Ы': c = 'b'; break;
                case 'Э': c = 'E'; break;
                case 'Ю': c = 'l'; break;
                case 'Я': c = 'R'; break;
            }
            switch (c)
            {
                case 'A': return new byte[] {0x7E,0x09,0x09,0x09,0x7E};
                case 'B': return new byte[] {0x7F,0x49,0x49,0x49,0x36};
                case 'C': return new byte[] {0x3E,0x41,0x41,0x41,0x22};
                case 'D': return new byte[] {0x7F,0x41,0x41,0x22,0x1C};
                case 'E': return new byte[] {0x7F,0x49,0x49,0x49,0x41};
                case 'F': return new byte[] {0x7F,0x09,0x09,0x09,0x01};
                case 'G': return new byte[] {0x3E,0x41,0x49,0x49,0x7A};
                case 'H': return new byte[] {0x7F,0x08,0x08,0x08,0x7F};
                case 'I': return new byte[] {0x00,0x41,0x7F,0x41,0x00};
                case 'J': return new byte[] {0x20,0x40,0x41,0x3F,0x01};
                case 'K': return new byte[] {0x7F,0x08,0x14,0x22,0x41};
                case 'L': return new byte[] {0x7F,0x40,0x40,0x40,0x40};
                case 'M': return new byte[] {0x7F,0x02,0x0C,0x02,0x7F};
                case 'N': return new byte[] {0x7F,0x04,0x08,0x10,0x7F};
                case 'O': return new byte[] {0x3E,0x41,0x41,0x41,0x3E};
                case 'P': return new byte[] {0x7F,0x09,0x09,0x09,0x06};
                case 'Q': return new byte[] {0x3E,0x41,0x51,0x21,0x5E};
                case 'R': return new byte[] {0x7F,0x09,0x19,0x29,0x46};
                case 'S': return new byte[] {0x46,0x49,0x49,0x49,0x31};
                case 'T': return new byte[] {0x01,0x01,0x7F,0x01,0x01};
                case 'U': return new byte[] {0x3F,0x40,0x40,0x40,0x3F};
                case 'V': return new byte[] {0x1F,0x20,0x40,0x20,0x1F};
                case 'W': return new byte[] {0x3F,0x40,0x38,0x40,0x3F};
                case 'X': return new byte[] {0x63,0x14,0x08,0x14,0x63};
                case 'Y': return new byte[] {0x07,0x08,0x70,0x08,0x07};
                case 'Z': return new byte[] {0x61,0x51,0x49,0x45,0x43};
                case '0': return new byte[] {0x3E,0x51,0x49,0x45,0x3E};
                case '1': return new byte[] {0x00,0x42,0x7F,0x40,0x00};
                case '2': return new byte[] {0x42,0x61,0x51,0x49,0x46};
                case '3': return new byte[] {0x21,0x41,0x45,0x4B,0x31};
                case '4': return new byte[] {0x18,0x14,0x12,0x7F,0x10};
                case '5': return new byte[] {0x27,0x45,0x45,0x45,0x39};
                case '6': return new byte[] {0x3C,0x4A,0x49,0x49,0x30};
                case '7': return new byte[] {0x01,0x71,0x09,0x05,0x03};
                case '8': return new byte[] {0x36,0x49,0x49,0x49,0x36};
                case '9': return new byte[] {0x06,0x49,0x49,0x29,0x1E};
                case '?': return new byte[] {0x02,0x01,0x51,0x09,0x06};
                case '!': return new byte[] {0x00,0x00,0x5F,0x00,0x00};
                case '/': return new byte[] {0x20,0x10,0x08,0x04,0x02};
                case '-': return new byte[] {0x08,0x08,0x08,0x08,0x08};
                case '#': return new byte[] {0x14,0x7F,0x14,0x7F,0x14};
                case '×': case 'x': return new byte[] {0x22,0x14,0x08,0x14,0x22};
                case ' ': return new byte[] {0x00,0x00,0x00,0x00,0x00};
                case 'r': return new byte[] {0x7C,0x08,0x04,0x04,0x08}; // small r
                case 'n': return new byte[] {0x7C,0x04,0x04,0x04,0x78};
                case 'b': return new byte[] {0x7F,0x44,0x44,0x44,0x38};
                case 'l': return new byte[] {0x00,0x41,0x7F,0x40,0x00};
                default:  return new byte[] {0x7F,0x41,0x41,0x41,0x7F};
            }
        }

        private void Update()
        {
            if (_drift == null) return;
            for (int i = 0; i < _drift.Length; i++)
            {
                var t = _drift[i];
                if (t == null) continue;
                var p = t.localPosition;
                p.z -= _driftSpeeds[i] * Time.deltaTime;
                if (p.z < -25f)
                {
                    p.x = Random.Range(-45f, 45f);
                    p.y = Random.Range(14f, 30f);
                    p.z = Random.Range(200f, 300f);
                    _driftSpeeds[i] = Random.Range(2.0f, 4.0f);
                }
                t.localPosition = p;
            }
        }
    }
}
