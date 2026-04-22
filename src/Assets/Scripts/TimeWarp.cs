using System.Collections;
using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Cinematic slow-mo: drops Time.timeScale, then ramps it back over
    /// real-time. Used on boss death and on player death frame.
    /// </summary>
    public class TimeWarp : MonoBehaviour
    {
        public static TimeWarp Instance { get; private set; }
        private Coroutine _running;

        private void Awake()
        {
            Instance = this;
        }

        public static void Slow(float toScale, float hold, float ramp)
        {
            if (Instance == null) return;
            if (Instance._running != null) Instance.StopCoroutine(Instance._running);
            Instance._running = Instance.StartCoroutine(Instance.Run(toScale, hold, ramp));
        }

        private IEnumerator Run(float toScale, float hold, float ramp)
        {
            Time.timeScale = toScale;
            Time.fixedDeltaTime = 0.02f * toScale;

            yield return new WaitForSecondsRealtime(hold);

            float t = 0f;
            while (t < ramp)
            {
                t += Time.unscaledDeltaTime;
                float k = t / ramp;
                float s = Mathf.Lerp(toScale, 1f, k);
                Time.timeScale = s;
                Time.fixedDeltaTime = 0.02f * s;
                yield return null;
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _running = null;
        }
    }
}
