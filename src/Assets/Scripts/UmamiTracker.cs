using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Thin wrapper around the Umami JS client. In WebGL builds calls
    /// through to <c>window.umami.track(name, data)</c> via the jslib
    /// bridge in <c>Assets/Plugins/WebGL/UmamiBridge.jslib</c>. In the
    /// editor and non-WebGL platforms, just logs so you can see what would
    /// be sent.
    ///
    /// Umami website ID is declared in <c>build/WebGL/index.html</c>;
    /// this class is script-only — no config to wire up.
    /// </summary>
    public static class UmamiTracker
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TrackUmami(string name, string json);
#endif

        /// <summary>
        /// Fire a named event with optional JSON payload (must be a valid
        /// JSON object string, e.g. <c>{"level":2}</c>). Safe to call from
        /// anywhere — swallows errors if umami isn't loaded.
        /// </summary>
        public static void Track(string name, string jsonPayload = "{}")
        {
            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(jsonPayload)) jsonPayload = "{}";
#if UNITY_WEBGL && !UNITY_EDITOR
            try { TrackUmami(name, jsonPayload); }
            catch (System.Exception e) { Debug.LogWarning($"[Umami] {e.Message}"); }
#else
            Debug.Log($"[Umami] {name} {jsonPayload}");
#endif
        }
    }
}
