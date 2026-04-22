using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Briefly tints all MeshRenderers on the host (and children) to white,
    /// then restores their materials. Cheap "got hit" visual punctuation.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        public float duration = 0.07f;
        public Color flashColor = new Color(1f, 1f, 1f, 1f);

        private List<MeshRenderer> _renderers;
        private List<Color> _baseColors;
        private List<Color> _baseEmissive;
        private float _until;
        private bool _flashing;

        private void EnsureCache()
        {
            if (_renderers != null) return;
            _renderers = new List<MeshRenderer>(GetComponentsInChildren<MeshRenderer>());
            _baseColors = new List<Color>();
            _baseEmissive = new List<Color>();
            for (int i = 0; i < _renderers.Count; i++)
            {
                var m = _renderers[i].material; // instance per renderer
                _baseColors.Add(m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                _baseEmissive.Add(m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black);
            }
        }

        public void Flash()
        {
            EnsureCache();
            _until = Time.time + duration;
            if (_flashing) return;
            _flashing = true;
            for (int i = 0; i < _renderers.Count; i++)
            {
                var m = _renderers[i].material;
                if (m.HasProperty("_Color")) m.SetColor("_Color", flashColor);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", flashColor * 2f);
            }
        }

        private void Update()
        {
            if (!_flashing) return;
            if (Time.time < _until) return;
            _flashing = false;
            for (int i = 0; i < _renderers.Count; i++)
            {
                var m = _renderers[i].material;
                if (m.HasProperty("_Color")) m.SetColor("_Color", _baseColors[i]);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", _baseEmissive[i]);
            }
        }
    }
}
