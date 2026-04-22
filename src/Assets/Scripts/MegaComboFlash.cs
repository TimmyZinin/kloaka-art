using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Fullscreen flash + centered "×N COMBO" label triggered on multiplier
    /// milestones (×2, ×3, ×4, ×5). Drawn via IMGUI on top of HUD.
    /// </summary>
    public class MegaComboFlash : MonoBehaviour
    {
        private static MegaComboFlash _instance;
        private float _fireAt;
        private int _level;
        private Texture2D _flashTex;
        private GUIStyle _style;

        public static void Flash(int multiplier)
        {
            if (_instance == null)
            {
                var go = new GameObject("MegaComboFlash");
                _instance = go.AddComponent<MegaComboFlash>();
            }
            _instance._fireAt = Time.unscaledTime;
            _instance._level = multiplier;
        }

        private void EnsureStyle()
        {
            if (_style != null) return;
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 220,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            _flashTex = new Texture2D(1, 1);
            _flashTex.SetPixel(0, 0, new Color(1f, 0.85f, 0.4f, 1f));
            _flashTex.Apply();
        }

        private void OnGUI()
        {
            if (_fireAt <= 0f) return;
            float t = Time.unscaledTime - _fireAt;
            if (t > 1.0f) return;

            EnsureStyle();

            // Flash — quick bright pulse, fades in first 0.12s
            float flashA = Mathf.Clamp01(1f - t / 0.25f) * 0.4f;
            var prevColor = GUI.color;
            GUI.color = new Color(1f, 0.95f, 0.6f, flashA);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _flashTex);
            GUI.color = prevColor;

            // Text — pops in, floats up, fades out
            float k = Mathf.Clamp01(t / 1.0f);
            float scale = 0.5f + Mathf.Sin(Mathf.Clamp01(t * 4f) * Mathf.PI) * 0.5f;
            float alpha = 1f - k * k;
            float yOff = -k * 120f;

            string text = $"×{_level}  COMBO";
            Color c = new Color(1f, 0.75f, 0.25f, alpha);

            _style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);
            // Disable wrapping + overflow clipping so long labels never get
            // cut off by the rect. Force a fat rect that's wider than the
            // screen to guarantee room.
            _style.wordWrap = false;
            _style.clipping = TextClipping.Overflow;

            var prev = GUI.matrix;
            GUI.matrix = prev * Matrix4x4.TRS(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.45f + yOff, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));
            // Huge rect centered on transform origin so the label has plenty
            // of horizontal room at any scale.
            var bigRect = new Rect(-1500, -160, 3000, 320);
            GUI.Label(bigRect, text, _style);
            _style.normal.textColor = c;
            bigRect.x -= 4;
            GUI.Label(bigRect, text, _style);
            GUI.matrix = prev;
        }
    }
}
