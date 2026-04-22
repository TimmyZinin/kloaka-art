using System.Collections;
using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Cinematic triggered by LevelManager when the boss spawns: slow-mo,
    /// camera zoom-in toward the boss, red flash overlay, then ease back
    /// to normal gameplay.
    /// </summary>
    public class BossIntro : MonoBehaviour
    {
        public static BossIntro Instance { get; private set; }

        private static Texture2D _flashTex;
        private float _flashFiredAt;

        public static BossIntro Get()
        {
            if (Instance == null)
            {
                var go = new GameObject("BossIntro");
                Instance = go.AddComponent<BossIntro>();
            }
            return Instance;
        }

        public void Play(Transform boss)
        {
            if (boss == null) return;
            StartCoroutine(Run(boss));
        }

        private IEnumerator Run(Transform boss)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            var restPos = cam.transform.position;
            var restRot = cam.transform.rotation;
            float restFov = cam.fieldOfView;

            // Red flash overlay
            _flashFiredAt = Time.unscaledTime;

            // Slow-mo for ~1s real, zoom camera toward boss
            TimeWarp.Slow(0.3f, 0.9f, 0.5f);

            float d = 0f;
            float dur = 1.0f;
            while (d < dur)
            {
                d += Time.unscaledDeltaTime;
                float k = d / dur;
                // Slide camera halfway between rest and boss.
                var targetPos = Vector3.Lerp(restPos, (restPos + boss.position) * 0.5f + Vector3.up * 2f, k);
                cam.transform.position = targetPos;
                cam.transform.rotation = Quaternion.Slerp(restRot,
                    Quaternion.LookRotation(boss.position - cam.transform.position), k);
                cam.fieldOfView = Mathf.Lerp(restFov, 48f, k);
                yield return null;
            }

            // Hold for a short beat
            yield return new WaitForSecondsRealtime(0.35f);

            // Ease back
            d = 0f;
            dur = 0.55f;
            var fromPos = cam.transform.position;
            var fromRot = cam.transform.rotation;
            float fromFov = cam.fieldOfView;
            while (d < dur)
            {
                d += Time.unscaledDeltaTime;
                float k = d / dur;
                cam.transform.position = Vector3.Lerp(fromPos, restPos, k);
                cam.transform.rotation = Quaternion.Slerp(fromRot, restRot, k);
                cam.fieldOfView = Mathf.Lerp(fromFov, restFov, k);
                yield return null;
            }
            cam.transform.position = restPos;
            cam.transform.rotation = restRot;
            cam.fieldOfView = restFov;
        }

        private static void EnsureFlashTex()
        {
            if (_flashTex != null) return;
            _flashTex = new Texture2D(1, 1);
            _flashTex.SetPixel(0, 0, new Color(1f, 0.15f, 0.1f, 1f));
            _flashTex.Apply();
        }

        private void OnGUI()
        {
            if (_flashFiredAt <= 0f) return;
            float t = Time.unscaledTime - _flashFiredAt;
            if (t > 0.45f) return;
            EnsureFlashTex();
            float a = Mathf.Clamp01(1f - t / 0.45f) * 0.55f;
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.2f, 0.1f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _flashTex);
            GUI.color = prev;
        }
    }
}
