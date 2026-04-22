using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Tiny world-space-to-screen-space HP bar above multi-HP non-boss
    /// enemies. Hidden when enemy is at full HP (no visual clutter for
    /// single-shot kills). Bosses already get a big screen-wide bar from HUD.
    /// </summary>
    public class EnemyHpBar : MonoBehaviour
    {
        private static Texture2D _bgTex;
        private static Texture2D _fgTex;

        private Enemy _enemy;

        private void Start()
        {
            _enemy = GetComponent<Enemy>();
            if (_bgTex == null)
            {
                _bgTex = new Texture2D(1, 1); _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f)); _bgTex.Apply();
                _fgTex = new Texture2D(1, 1); _fgTex.SetPixel(0, 0, new Color(1f, 0.85f, 0.25f, 1f)); _fgTex.Apply();
            }
        }

        private void OnGUI()
        {
            if (_enemy == null || _enemy.hp >= _enemy.def.hp) return;   // full or gone
            if (_enemy.def.hp <= 0) return;

            var cam = Camera.main;
            if (cam == null) return;
            var worldPos = transform.position + Vector3.up * (_enemy.radius + 0.4f);
            var sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) return;

            float ratio = Mathf.Clamp01((float)_enemy.hp / Mathf.Max(1, _enemy.def.hp));
            float barW = 60f;
            float barH = 6f;
            float x = sp.x - barW * 0.5f;
            float y = Screen.height - sp.y - barH - 2f;

            GUI.DrawTexture(new Rect(x - 1, y - 1, barW + 2, barH + 2), _bgTex);
            GUI.DrawTexture(new Rect(x, y, barW * ratio, barH), _fgTex);
        }
    }
}
