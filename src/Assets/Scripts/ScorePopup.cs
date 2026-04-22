using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Floating "+250 ×3" world-space label spawned at the kill location.
    /// Renders via OnGUI projected from camera so we don't need uGUI / TMP.
    /// Auto-destructs after <see cref="lifetime"/> seconds.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        public string text = "+0";
        public Color color = new Color(1f, 1f, 0.4f, 1f);
        public float lifetime = 0.85f;
        public float riseSpeed = 4f;

        private float _born;
        private GUIStyle _style;

        public static void Spawn(Vector3 worldPos, int amount, int multiplier, Color color)
        {
            var go = new GameObject("ScorePopup");
            go.transform.position = worldPos + new Vector3(0f, 1.4f, 0f);
            var p = go.AddComponent<ScorePopup>();
            p.text = multiplier > 1
                ? $"+{amount}  ×{multiplier}"
                : $"+{amount}";
            p.color = color;
        }

        private void Start()
        {
            _born = Time.time;
        }

        private void Update()
        {
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            if (Time.time - _born > lifetime) Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                };
            }
            var cam = Camera.main;
            if (cam == null) return;
            var sp = cam.WorldToScreenPoint(transform.position);
            if (sp.z < 0f) return;
            float age = (Time.time - _born) / lifetime;
            float alpha = 1f - age * age;
            var c = color; c.a = alpha;
            _style.normal.textColor = c;
            float y = Screen.height - sp.y;
            float w = 240f;
            // shadow
            var shadow = _style.normal.textColor;
            _style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);
            GUI.Label(new Rect(sp.x - w * 0.5f + 2, y + 2, w, 60), text, _style);
            _style.normal.textColor = shadow;
            GUI.Label(new Rect(sp.x - w * 0.5f, y, w, 60), text, _style);
        }
    }
}
