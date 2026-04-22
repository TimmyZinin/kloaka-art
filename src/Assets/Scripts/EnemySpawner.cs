using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Spawns enemies for the current <see cref="LevelManager.CurrentLevel"/>.
    /// For each spawn picks a random flavor from the level roster, then tries to
    /// instantiate a 3D model from Resources/&lt;resourcePath&gt;. If the resource
    /// isn't there yet (common before Tripo generation), falls back to a
    /// primitive + tinted material so the game is always playable.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public float xRange = 10f;
        public float spawnZ = 35f;
        public float baseSpeed = 7f;

        private GameManager _manager;
        private LevelManager _levelManager;
        private float _nextSpawn;
        private Mesh _cachedAsteroidMesh;
        // First-sighting tip: show each flavor's introduction banner once
        // per session the first time it spawns.
        private readonly System.Collections.Generic.HashSet<EnemyFlavor> _seen = new();

        public void Configure(GameManager manager, LevelManager levelManager)
        {
            _manager = manager;
            _levelManager = levelManager;
        }

        private void Update()
        {
            if (_manager == null || _manager.IsGameOver) return;
            if (_levelManager == null) return;
            if (_levelManager.IsBossPhase)
            {
                // Boss itself is spawned by LevelManager via SpawnSpecific; we just stop adds
                return;
            }

            if (Time.time >= _nextSpawn)
            {
                var level = _levelManager.CurrentLevel;
                float speedFactor = _manager.Difficulty;

                // Occasionally spawn a 5-enemy V-formation instead of a single
                // enemy. Waves become more frequent on higher levels.
                bool wave = Random.value < 0.18f + 0.05f * _levelManager.CurrentIndex;
                if (wave)
                {
                    var flavor = level.roster[Random.Range(0, level.roster.Length)];
                    var def = Catalog.GetEnemy(flavor);
                    float r = Random.value;
                    if (r < 0.5f) SpawnVFormation(def, count: 5);
                    else if (r < 0.75f) SpawnLineAbreast(def, count: 5);
                    else SpawnStaircase(def, count: 5);
                    _nextSpawn = Time.time + Random.Range(1.4f, 2.2f) / speedFactor;
                }
                else
                {
                    var flavor = level.roster[Random.Range(0, level.roster.Length)];
                    Spawn(Catalog.GetEnemy(flavor));
                    _nextSpawn = Time.time + Random.Range(level.spawnIntervalMin, level.spawnIntervalMax) / speedFactor;
                }
            }
        }

        /// <summary>
        /// Spawn <paramref name="count"/> enemies in a V-shape opening toward
        /// the camera. Each further enemy is slightly higher-Z (deeper into
        /// the field) and wider apart.
        /// </summary>
        private void SpawnVFormation(EnemyDefinition def, int count)
        {
            float xCenter = Random.Range(-6f, 6f);
            float spread = 2.6f;
            int half = count / 2;
            for (int i = 0; i < count; i++)
            {
                int offset = i - half;
                var pos = new Vector3(
                    xCenter + offset * spread,
                    Random.Range(-0.4f, 0.4f),
                    spawnZ + Mathf.Abs(offset) * 3.5f);
                Spawn(def, pos);
            }
        }

        /// <summary>Evenly spaced horizontal shelf — reads as "шеренга".</summary>
        private void SpawnLineAbreast(EnemyDefinition def, int count)
        {
            float xCenter = Random.Range(-5f, 5f);
            float spread = 3.2f;
            int half = count / 2;
            for (int i = 0; i < count; i++)
            {
                int offset = i - half;
                var pos = new Vector3(
                    xCenter + offset * spread,
                    Random.Range(-0.3f, 0.3f),
                    spawnZ);
                Spawn(def, pos);
            }
        }

        /// <summary>Diagonal staircase — each step offsets both X and Z.</summary>
        private void SpawnStaircase(EnemyDefinition def, int count)
        {
            float xStart = Random.Range(-7f, -2f);
            float dx = Random.value < 0.5f ? 1.8f : -1.8f;
            if (xStart > 0f) dx = -Mathf.Abs(dx);
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector3(
                    xStart + dx * i,
                    Random.Range(-0.3f, 0.3f),
                    spawnZ + i * 2.4f);
                Spawn(def, pos);
            }
        }

        /// <summary>
        /// Spawn a specific flavor (used by LevelManager to spawn bosses).
        /// </summary>
        public Enemy SpawnSpecific(EnemyFlavor flavor, Vector3? position = null)
        {
            var def = Catalog.GetEnemy(flavor);
            return Spawn(def, position);
        }

        private Enemy Spawn(EnemyDefinition def, Vector3? position = null)
        {
            GameObject go = TryLoadModel(def);
            bool modelLoaded = go != null;
            if (!modelLoaded)
            {
                Debug.LogWarning($"[Spawner] Resources.Load returned null for '{def.resourcePath}' — falling back to primitive.");
                go = BuildFallback(def);
            }
            else
            {
                Debug.Log($"[Spawner] Loaded GLB '{def.resourcePath}' — {go.GetComponentsInChildren<Renderer>().Length} renderers");
            }
            go.name = "Enemy_" + def.flavor;

            float size = Random.Range(def.sizeMin, def.sizeMax);
            if (!modelLoaded)
            {
                // Fallback primitives are unit-ish; scale to intended size
                go.transform.localScale = Vector3.one * size;
            }
            else
            {
                // Normalise the inner GLB (mesh-space math, scale-invariant) BEFORE
                // placing the wrapper, otherwise our bounds read picks up random
                // world coordinates and the k factor becomes nonsense.
                NormaliseToUnit(go);
                // Auto-rotate the inner GLB so its flat/wide axis faces the
                // camera. Tripo models export with inconsistent forward axes
                // — some are along +Z, some along +X. If the bounding box is
                // notably wider along X than along Z, the model is "lying
                // sideways" — rotate the GLB 90° around Y so its flat face
                // points toward the player.
                AutoFaceCamera(go);
                // Explicit per-flavor pitch correction (e.g. flat discs that
                // Tripo exported lying down — see pitchCorrection in Catalog).
                if (!Mathf.Approximately(def.pitchCorrection, 0f) && go.transform.childCount > 0)
                {
                    var glb = go.transform.GetChild(0);
                    glb.localRotation = Quaternion.Euler(def.pitchCorrection, 0f, 0f) * glb.localRotation;
                }
                go.transform.localScale = Vector3.one * size;
                // KEEP original Tripo PBR materials so the poop emoji face,
                // the recruiter suit, the paper-scroll stripes — all their
                // real colours and texture details stay visible. Previous
                // ApplyTint() flattened
                // every enemy to a single red/magenta tint which is why the
                // bosses read as "just red". Add only a subtle emissive rim so
                // they register through fog.
                AddEmissiveRim(go, def);
            }

            // Position / rotation AFTER scaling so spawn coords are actual spawn coords.
            go.transform.position = position ?? new Vector3(
                Random.Range(-xRange, xRange),
                Random.Range(-0.5f, 0.5f),
                spawnZ);
            // Every enemy now faces the camera. Per-flavor facingYaw in
            // Catalog.Enemies lets us calibrate models whose Tripo-exported
            // forward axis differs (default 180° = face -Z). Asteroid is the
            // only exception — it tumbles in flight (see Enemy.Configure).
            if (def.flavor == EnemyFlavor.Asteroid)
            {
                go.transform.rotation = Random.rotation;
            }
            else
            {
                go.transform.rotation = Quaternion.Euler(0f, def.ResolvedFacingYaw, 0f);
            }

            EnsureCollider(go, size);
            EnsureKinematicRigidbody(go);

            // Add visual life: pop-in scale + idle wobble + hit flash.
            // Bosses skip it to keep their pose; stationary enemies get a
            // gentler variant with zero rotation amplitude so they don't spin.
            if (go.GetComponent<HitFlash>() == null) go.AddComponent<HitFlash>();
            bool isBoss = def.flavor == EnemyFlavor.BossInterviewer || def.flavor == EnemyFlavor.BossPanel;
            // Worldspace HP bar for multi-HP non-boss enemies (hp>=3).
            if (!isBoss && def.hp >= 3 && go.GetComponent<EnemyHpBar>() == null)
                go.AddComponent<EnemyHpBar>();
            if (!isBoss && go.GetComponent<EnemyAnimator>() == null)
            {
                var anim = go.AddComponent<EnemyAnimator>();
                bool stationary =
                    def.flavor == EnemyFlavor.MiniToilet ||
                    def.flavor == EnemyFlavor.PooEmoji ||
                    def.flavor == EnemyFlavor.BlueGuru ||
                    def.flavor == EnemyFlavor.HrStealth;
                if (stationary)
                {
                    anim.wobbleAmplitude = 0f;   // no rotation jitter
                    anim.bobAmplitude    = 0.06f; // tiny vertical bob only
                }
            }

            var enemy = go.AddComponent<Enemy>();
            float speed = Random.Range(def.speedMin, def.speedMax);
            if (_manager != null) speed *= _manager.Difficulty;
            enemy.Configure(_manager, _levelManager, def, speed, size);

            // First-sighting tip — show each enemy's intro banner exactly
            // once per session, delayed by 0.4s so it pops alongside the
            // enemy entering the screen rather than before.
            if (!_seen.Contains(def.flavor) && !string.IsNullOrEmpty(def.firstSightTip))
            {
                _seen.Add(def.flavor);
                StartCoroutine(ShowFirstSightTip(def.firstSightTip));
            }
            return enemy;
        }

        private System.Collections.IEnumerator ShowFirstSightTip(string text)
        {
            yield return new UnityEngine.WaitForSeconds(0.4f);
            var c = _manager != null ? _manager.Palette.TorpedoEmissive : Color.white;
            c.a = 1f;
            HudBinder.FlashBanner(text, c, 2.4f);
            AudioManager.Play("combo_up", 0.8f, 0.5f);
        }

        private GameObject TryLoadModel(EnemyDefinition def)
        {
            if (string.IsNullOrEmpty(def.resourcePath)) return null;
            var prefab = Resources.Load<GameObject>(def.resourcePath);
            if (prefab == null) return null;

            // Wrap the instantiated GLB in a parent so we can scale/rotate/collide on
            // the wrapper while NormaliseToUnit fixes up the inner asset's local
            // transform. Without this, gltfast's root scale/origin fights ours.
            var wrapper = new GameObject("ModelWrapper");
            var inner = Instantiate(prefab, wrapper.transform);
            inner.name = "GLB";
            return wrapper;
        }

        private GameObject BuildFallback(EnemyDefinition def)
        {
            GameObject go;
            switch (def.fallbackShape)
            {
                case FallbackShape.Icosphere:
                    go = new GameObject("Fallback_Icosphere");
                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    mf.sharedMesh = GetOrBuildAsteroidMesh();
                    mr.material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.4f);
                    break;
                case FallbackShape.Cube:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    DestroyImmediate(go.GetComponent<Collider>());
                    go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.6f);
                    break;
                case FallbackShape.Capsule:
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    DestroyImmediate(go.GetComponent<Collider>());
                    go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.6f);
                    break;
                case FallbackShape.Cylinder:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    DestroyImmediate(go.GetComponent<Collider>());
                    go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.6f);
                    break;
                case FallbackShape.FlatSphere:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(go.GetComponent<Collider>());
                    go.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
                    go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.6f);
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(go.GetComponent<Collider>());
                    go.GetComponent<MeshRenderer>().material = MaterialFactory.Emissive(def.tint, def.emissive, 0.3f, 0.6f);
                    break;
            }
            return go;
        }

        /// <summary>
        /// Rescale and recenter the first child of <paramref name="wrapper"/>
        /// using mesh-local bounds (scale-invariant, independent of world position).
        /// </summary>
        private static void NormaliseToUnit(GameObject wrapper)
        {
            if (wrapper.transform.childCount == 0) return;
            var glb = wrapper.transform.GetChild(0);

            var filters = glb.GetComponentsInChildren<MeshFilter>(true);
            var skinned = glb.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (filters.Length == 0 && skinned.Length == 0) return;

            // Compute combined bounds in GLB-root local space by transforming
            // each mesh's local bounds through each sub-node's local matrix.
            bool haveBounds = false;
            Bounds b = new Bounds();

            void Accumulate(Bounds local, Transform t)
            {
                // Transform 8 corners of `local` by t's local-to-glb-root matrix.
                var mat = glb.worldToLocalMatrix * t.localToWorldMatrix;
                var min = local.min; var max = local.max;
                for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                for (int cz = 0; cz < 2; cz++)
                {
                    var p = new Vector3(cx == 0 ? min.x : max.x, cy == 0 ? min.y : max.y, cz == 0 ? min.z : max.z);
                    var wp = mat.MultiplyPoint3x4(p);
                    if (!haveBounds) { b = new Bounds(wp, Vector3.zero); haveBounds = true; }
                    else b.Encapsulate(wp);
                }
            }

            foreach (var mf in filters)
            {
                if (mf.sharedMesh != null) Accumulate(mf.sharedMesh.bounds, mf.transform);
            }
            foreach (var sm in skinned)
            {
                Accumulate(sm.localBounds, sm.transform);
            }
            if (!haveBounds) return;

            float maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxExtent < 0.0001f) return;

            float k = 2f / maxExtent;             // longest axis → 2 units
            glb.localScale = Vector3.one * k;
            glb.localPosition = -b.center * k;
            glb.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Rotate the inner GLB so its flat/wide face looks at the camera.
        ///
        /// Rule 1 — flat disc (Y is &lt; 40% of shortest horizontal extent):
        ///   Pitch 90° around X to stand it up.
        ///
        /// Rule 2 — upright models with shoulders along Z axis (model lying
        ///   "sideways" in its own local space): yaw 90° so shoulders align
        ///   with X (wide axis perpendicular to camera ray). NO extra 180°
        ///   added — the outer wrapper's ResolvedFacingYaw (180° default)
        ///   already handles the "point chest at camera" flip. Previous
        ///   version double-rotated and produced back-to-camera humanoids.
        /// </summary>
        private static void AutoFaceCamera(GameObject wrapper)
        {
            if (wrapper.transform.childCount == 0) return;
            var glb = wrapper.transform.GetChild(0);
            if (!LocalBbox(glb, out var b)) return;

            float sx = b.size.x;
            float sy = b.size.y;
            float sz = b.size.z;
            float minH = Mathf.Min(sx, sz);

            // Rule 1 — disc lying flat. Stand it up and return.
            if (sy > 0f && sy < minH * 0.4f)
            {
                glb.localRotation = Quaternion.Euler(90f, 0f, 0f) * glb.localRotation;
                return;
            }

            // Rule 2 — if shoulder axis is Z (model Z-wider than X), yaw 90°
            // to realign shoulders along world-X. Otherwise leave at 0° and
            // let the wrapper rotation do the facing. No +180° double-flip.
            if (sz > sx * 1.15f)
            {
                glb.localRotation = Quaternion.Euler(0f, 90f, 0f) * glb.localRotation;
            }
        }

        /// <summary>
        /// Compute axis-aligned bounding box in the given root's local space.
        /// Returns false if there's no mesh data.
        /// </summary>
        private static bool LocalBbox(Transform root, out Bounds b)
        {
            b = new Bounds();
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return false;
            bool have = false;
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var local = mf.sharedMesh.bounds;
                var m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var min = local.min; var max = local.max;
                for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                for (int cz = 0; cz < 2; cz++)
                {
                    var p = new Vector3(cx == 0 ? min.x : max.x, cy == 0 ? min.y : max.y, cz == 0 ? min.z : max.z);
                    var wp = m.MultiplyPoint3x4(p);
                    if (!have) { b = new Bounds(wp, Vector3.zero); have = true; }
                    else b.Encapsulate(wp);
                }
            }
            return have;
        }

        /// <summary>
        /// Flatten all renderers to a single emissive tint — used ONLY for
        /// procedural primitives (fallback path). GLB models keep their own
        /// materials; see <see cref="AddEmissiveRim"/>.
        /// </summary>
        private static void ApplyTint(GameObject root, EnemyDefinition def)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var tinted = MaterialFactory.Emissive(def.tint, def.emissive, 0.2f, 0.6f);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = tinted;
            }
        }

        /// <summary>
        /// Preserve the Tripo GLB's own albedo/normal/roughness but nudge its
        /// emission so the model registers through fog, AND bolt on an
        /// inverted-hull outline child for readable silhouette.
        /// </summary>
        private static void AddEmissiveRim(GameObject root, EnemyDefinition def)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var block = new MaterialPropertyBlock();
            // Bosses are already bright-emissive in their palette tint; adding
            // another 15% rim was blowing them to full white under the skybox
            // ambient + bloom. Cap boss rim to 4%. Regular enemies keep 15%.
            bool isBoss = def.flavor == EnemyFlavor.BossInterviewer || def.flavor == EnemyFlavor.BossPanel;
            float rimFactor = isBoss ? 0.04f : 0.15f;
            Color emission = def.emissive * rimFactor;
            emission.a = 1f;
            block.SetColor("_EmissionColor", emission);

            var outlineShader = Shader.Find("Hidden/SpaceShooter/Outline");
            Material outlineMat = null;
            if (outlineShader != null)
            {
                outlineMat = new Material(outlineShader);
                outlineMat.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 1f));
                // Thicker cartoon outline (was 0.025).
                outlineMat.SetFloat("_OutlineWidth", 0.045f);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(block);
                foreach (var mat in renderers[i].sharedMaterials)
                {
                    if (mat != null && mat.HasProperty("_EmissionColor"))
                        mat.EnableKeyword("_EMISSION");
                }
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                // Bosses don't receive shadows — point-light self-shadowing on
                // their torsos was producing oily dark blotches.
                renderers[i].receiveShadows = !isBoss;

                // Outline: duplicate the mesh into a sibling GameObject with
                // the inverted-hull material.
                if (outlineMat != null)
                {
                    var mf = renderers[i].GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var outlineGo = new GameObject("Outline");
                        outlineGo.transform.SetParent(renderers[i].transform, false);
                        outlineGo.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                        var omr = outlineGo.AddComponent<MeshRenderer>();
                        omr.sharedMaterial = outlineMat;
                        omr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        omr.receiveShadows = false;
                    }
                }
            }
        }

        private static void EnsureCollider(GameObject go, float size)
        {
            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<SphereCollider>();
                col.radius = 0.55f;
                col.isTrigger = true;
            }
            else
            {
                var existing = go.GetComponent<Collider>();
                existing.isTrigger = true;
            }
        }

        private static void EnsureKinematicRigidbody(GameObject go)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private Mesh GetOrBuildAsteroidMesh()
        {
            if (_cachedAsteroidMesh != null) return _cachedAsteroidMesh;

            var src = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var baseMesh = src.GetComponent<MeshFilter>().sharedMesh;
            var verts = baseMesh.vertices;
            var normals = baseMesh.normals;
            var tris = baseMesh.triangles;
            var uvs = baseMesh.uv;
            Object.DestroyImmediate(src);

            for (int i = 0; i < verts.Length; i++)
            {
                float n = Mathf.PerlinNoise(verts[i].x * 2f + 13.1f, verts[i].z * 2f - 7.3f);
                n += Mathf.PerlinNoise(verts[i].y * 3f + 0.5f, verts[i].x * 3f + 4.2f) * 0.5f;
                float d = 1f + (n - 0.6f) * 0.55f;
                verts[i] = normals[i] * d * verts[i].magnitude;
            }

            var m = new Mesh
            {
                vertices = verts,
                triangles = tris,
                uv = uvs
            };
            m.RecalculateNormals();
            m.RecalculateBounds();
            _cachedAsteroidMesh = m;
            return m;
        }
    }
}
