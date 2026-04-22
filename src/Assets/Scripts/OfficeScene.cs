using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Procedural office corridor: continuously spawns rows of office
    /// furniture (desks, chairs, filing cabinets, printers, coolers, cubicle
    /// walls, server racks, paper piles) on both sides of the play field plus
    /// ceiling panel lights overhead. Every prop drifts toward the camera so
    /// the player sees the office flowing past. Falls back to tinted primitives
    /// if the Tripo GLBs aren't present yet.
    /// </summary>
    public class OfficeScene : MonoBehaviour
    {
        public static OfficeScene Instance { get; private set; }

        // Props on the left/right of the corridor — only kinds we actually
        // generated (server_rack/meeting_table/paper_pile hit Tripo quota,
        // omit them instead of rendering 'ModelWrapper' fallback cubes).
        private static readonly string[] SideProps =
        {
            "Office/office_desk",     "Office/office_desk",     // weight desks heavier
            "Office/office_chair",    "Office/office_chair",
            "Office/filing_cabinet",
            "Office/printer_copier",
            "Office/water_cooler",
            "Office/cubicle_wall",    "Office/cubicle_wall",
            "Office/coffee_machine",
        };
        private static readonly string CeilingProp = "Office/ceiling_panel_light";

        private readonly Dictionary<string, GameObject> _cache = new();
        private readonly List<Prop> _props = new();
        private float _nextSideZ = 40f;
        private float _nextCeilingZ = 40f;
        private const float SideSpacing = 8f;
        private const float CeilingSpacing = 14f;
        private const float DespawnZ = -25f;
        private const float SpawnFarZ = 160f;
        private const float DriftSpeed = 7.5f;

        private Palette _palette;

        private class Prop
        {
            public Transform t;
            public bool ceiling;
        }

        public static OfficeScene Create(Transform parent, Palette palette)
        {
            var go = new GameObject("OfficeScene");
            go.transform.SetParent(parent);
            var s = go.AddComponent<OfficeScene>();
            s._palette = palette;
            s.PrimeCorridor();
            Instance = s;
            return s;
        }

        public void ApplyPalette(Palette p)
        {
            _palette = p;
            // Re-tint existing props so level transitions aren't jarring.
            foreach (var pr in _props)
            {
                if (pr == null || pr.t == null) continue;
                TintProp(pr.t.gameObject, pr.ceiling);
            }
        }

        private void PrimeCorridor()
        {
            // Pre-populate so the corridor isn't empty on level start.
            for (float z = 10f; z < SpawnFarZ; z += SideSpacing)
            {
                SpawnSidePair(z);
            }
            for (float z = 14f; z < SpawnFarZ; z += CeilingSpacing)
            {
                SpawnCeiling(z);
            }
            _nextSideZ = SpawnFarZ;
            _nextCeilingZ = SpawnFarZ;
        }

        private void SpawnSidePair(float z)
        {
            string propL = SideProps[Random.Range(0, SideProps.Length)];
            string propR = SideProps[Random.Range(0, SideProps.Length)];
            // Push props further out from the play area so enemies never
            // disappear behind a desk. Side X bumped from ±16 to ±20.
            SpawnSide(propL, -20f + Random.Range(-1.5f, 1.5f), z, facingRight: true);
            SpawnSide(propR,  20f + Random.Range(-1.5f, 1.5f), z, facingRight: false);
        }

        private void SpawnSide(string resPath, float x, float z, bool facingRight)
        {
            var go = MakePropInstance(resPath, ceiling: false);
            if (go == null) return;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, -2.8f, z);
            // Face the middle of the corridor; desks/chairs/etc. look right.
            go.transform.localRotation = Quaternion.Euler(0f, facingRight ? 90f : -90f, 0f);
            _props.Add(new Prop { t = go.transform, ceiling = false });
        }

        private void SpawnCeiling(float z)
        {
            var go = MakePropInstance(CeilingProp, ceiling: true);
            if (go == null) return;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Random.Range(-6f, 6f), 10f, z);
            go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Real point-light on each ceiling panel — paints the floor with
            // per-panel spotlights as the corridor streams past. Keep range
            // small so lighting cost stays sane on WebGL.
            var lightGo = new GameObject("PanelLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 2.2f;
            light.color = Color.Lerp(Color.white, _palette.HorizonColor, 0.35f);
            light.shadows = LightShadows.None;
            // Random subset of ceiling lights gets a flicker component —
            // makes the corridor feel lived-in / malfunctioning.
            if (Random.value < 0.25f) lightGo.AddComponent<LightFlicker>();
            _props.Add(new Prop { t = go.transform, ceiling = true });
        }

        private GameObject MakePropInstance(string resPath, bool ceiling)
        {
            if (!_cache.TryGetValue(resPath, out var prefab))
            {
                prefab = Resources.Load<GameObject>(resPath);
                _cache[resPath] = prefab;
            }
            GameObject go;
            if (prefab != null)
            {
                var wrapper = new GameObject("Prop_" + resPath);
                var inner = Instantiate(prefab, wrapper.transform);
                inner.name = "GLB";
                NormaliseProp(wrapper, ceiling);
                go = wrapper;
            }
            else
            {
                go = BuildFallbackProp(resPath, ceiling);
            }
            TintProp(go, ceiling);
            return go;
        }

        private void TintProp(GameObject go, bool ceiling)
        {
            // Ceiling lights emit the palette's horizon colour; side props
            // get a tiny palette-tinted emission so they register in fog.
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                foreach (var mat in mr.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (ceiling && mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        var c = _palette.HorizonColor * 1.6f; c.a = 1f;
                        mat.SetColor("_EmissionColor", c);
                    }
                    else if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        var c = _palette.HorizonColor * 0.15f; c.a = 1f;
                        mat.SetColor("_EmissionColor", c);
                    }
                }
            }
        }

        private static void NormaliseProp(GameObject wrapper, bool ceiling)
        {
            if (wrapper.transform.childCount == 0) return;
            var glb = wrapper.transform.GetChild(0);
            var filters = glb.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;

            bool have = false; Bounds b = new Bounds();
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var local = mf.sharedMesh.bounds;
                var mat = glb.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var min = local.min; var max = local.max;
                for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                for (int cz = 0; cz < 2; cz++)
                {
                    var p = new Vector3(cx == 0 ? min.x : max.x, cy == 0 ? min.y : max.y, cz == 0 ? min.z : max.z);
                    var wp = mat.MultiplyPoint3x4(p);
                    if (!have) { b = new Bounds(wp, Vector3.zero); have = true; }
                    else b.Encapsulate(wp);
                }
            }
            if (!have) return;

            float maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxExtent < 0.0001f) return;

            float targetSize = ceiling ? 3.5f : 3.0f;
            float k = targetSize / maxExtent;
            glb.localScale = Vector3.one * k;
            // Centre horizontally, place base on the floor for side props.
            var centre = b.center * k;
            glb.localPosition = ceiling
                ? new Vector3(-centre.x, -centre.y, -centre.z)
                : new Vector3(-centre.x, -b.min.y * k, -centre.z);
        }

        private GameObject BuildFallbackProp(string resPath, bool ceiling)
        {
            var go = GameObject.CreatePrimitive(ceiling ? PrimitiveType.Quad : PrimitiveType.Cube);
            go.name = "Fallback_" + resPath;
            Object.Destroy(go.GetComponent<Collider>());
            if (ceiling)
            {
                go.transform.localScale = new Vector3(3f, 3f, 3f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                go.transform.localScale = new Vector3(2f, 2f, 2f);
            }
            var c = _palette.HorizonColor * 0.5f; c.a = 1f;
            go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(c, c * 0.6f, 0.1f, 0.4f);
            return go;
        }

        private void Update()
        {
            // Drift everything toward the player; recycle past despawn line.
            for (int i = _props.Count - 1; i >= 0; i--)
            {
                var pr = _props[i];
                if (pr == null || pr.t == null) { _props.RemoveAt(i); continue; }
                pr.t.localPosition += Vector3.back * DriftSpeed * Time.deltaTime;
                if (pr.t.localPosition.z < DespawnZ)
                {
                    Destroy(pr.t.gameObject);
                    _props.RemoveAt(i);
                }
            }

            // Keep spawning so the corridor is always full in front.
            _nextSideZ -= DriftSpeed * Time.deltaTime;
            while (_nextSideZ < SpawnFarZ)
            {
                _nextSideZ += SideSpacing;
                SpawnSidePair(_nextSideZ);
            }
            _nextCeilingZ -= DriftSpeed * Time.deltaTime;
            while (_nextCeilingZ < SpawnFarZ)
            {
                _nextCeilingZ += CeilingSpacing;
                SpawnCeiling(_nextCeilingZ);
            }
        }
    }
}
