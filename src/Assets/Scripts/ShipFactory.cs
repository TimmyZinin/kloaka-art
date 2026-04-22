using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Builds a readable delta-wing starfighter procedurally.
    /// Silhouette: sharp forward-pointing cone hull + two angled triangular
    /// wings + a square reactor bay at the tail. All built from custom
    /// meshes so the ship reads as a fighter, not a cluster of primitives.
    /// </summary>
    public static class ShipFactory
    {
        public static GameObject Create(Palette palette)
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.position = new Vector3(0f, 0f, -4f);
            root.transform.rotation = Quaternion.identity;

            // Prefer the Tripo-generated "exhausted job-seeker" model if present.
            var tripoPrefab = Resources.Load<GameObject>("Player/player_ship");
            if (tripoPrefab != null)
            {
                return BuildFromTripo(root, palette, tripoPrefab);
            }

            // Tilt the whole ship forward a bit so the nose is visible from
            // the behind-top camera angle.
            root.transform.Rotate(Vector3.right, -12f);

            var hullMat    = MaterialFactory.Emissive(palette.ShipColor,           palette.ShipEmissive,        0.4f, 0.85f);
            var wingMat    = MaterialFactory.Emissive(palette.ShipColor * 0.8f,    palette.ShipEmissive * 0.7f, 0.4f, 0.85f);
            var accentMat  = MaterialFactory.Emissive(palette.TorpedoColor,        palette.TorpedoEmissive,     0.0f, 1.0f);
            var engineMat  = MaterialFactory.Emissive(palette.TorpedoColor,        palette.TorpedoEmissive * 2f,0.0f, 1.0f);

            // ── Hull ──  sharp pyramid nose (4-sided cone) stretched along +Z
            var hull = new GameObject("Hull");
            hull.transform.SetParent(root.transform, false);
            hull.AddComponent<MeshFilter>().sharedMesh   = BuildPyramid(1.6f, 3.2f, 0.6f);
            hull.AddComponent<MeshRenderer>().sharedMaterial = hullMat;

            // Cockpit accent strip on top
            var cockpit = new GameObject("Cockpit");
            cockpit.transform.SetParent(root.transform, false);
            cockpit.transform.localPosition = new Vector3(0f, 0.38f, 0.2f);
            cockpit.transform.localScale = new Vector3(0.35f, 0.15f, 1.1f);
            cockpit.AddComponent<MeshFilter>().sharedMesh = BuildPyramid(1f, 1f, 1f);
            cockpit.AddComponent<MeshRenderer>().sharedMaterial = accentMat;

            // ── Wings ──  sharp triangles sweeping back from the hull
            var wingL = new GameObject("WingL");
            wingL.transform.SetParent(root.transform, false);
            wingL.AddComponent<MeshFilter>().sharedMesh = BuildDeltaWing(isLeft: true);
            wingL.AddComponent<MeshRenderer>().sharedMaterial = wingMat;

            var wingR = new GameObject("WingR");
            wingR.transform.SetParent(root.transform, false);
            wingR.AddComponent<MeshFilter>().sharedMesh = BuildDeltaWing(isLeft: false);
            wingR.AddComponent<MeshRenderer>().sharedMaterial = wingMat;

            // Wing-tip accents (small emissive cubes at the wing tips — reads as running lights)
            var tipL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipL.name = "TipL";
            tipL.transform.SetParent(root.transform, false);
            tipL.transform.localPosition = new Vector3(-2.1f, 0.05f, -0.9f);
            tipL.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            Object.Destroy(tipL.GetComponent<Collider>());
            tipL.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            var tipR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipR.name = "TipR";
            tipR.transform.SetParent(root.transform, false);
            tipR.transform.localPosition = new Vector3(2.1f, 0.05f, -0.9f);
            tipR.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            Object.Destroy(tipR.GetComponent<Collider>());
            tipR.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            // ── Reactor / engine ──
            var engine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            engine.name = "Engine";
            engine.transform.SetParent(root.transform, false);
            engine.transform.localPosition = new Vector3(0f, 0.05f, -1.55f);
            engine.transform.localScale = new Vector3(0.7f, 0.4f, 0.35f);
            Object.Destroy(engine.GetComponent<Collider>());
            engine.GetComponent<MeshRenderer>().sharedMaterial = engineMat;

            // Engine trail — neon streak behind the ship
            var trailGo = new GameObject("EngineTrail");
            trailGo.transform.SetParent(root.transform, false);
            trailGo.transform.localPosition = new Vector3(0f, 0.05f, -1.75f);
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.55f;
            trail.endWidth = 0.0f;
            trail.minVertexDistance = 0.05f;
            trail.material = MaterialFactory.UnlitParticle(palette.TorpedoEmissive);
            trail.startColor = palette.TorpedoEmissive;
            var tail = palette.TorpedoColor; tail.a = 0f;
            trail.endColor = tail;

            // Collider + rigidbody on root
            var sc = root.AddComponent<SphereCollider>();
            sc.radius = 1.1f;
            sc.isTrigger = true;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PlayerController>();

            var pv = root.AddComponent<PlayerVisuals>();
            pv.coloredMaterials = new[] { hullMat, wingMat };
            pv.engineMaterial = engineMat;
            pv.engineTrail = trail;

            return root;
        }

        /// <summary>
        /// Instantiate the Tripo player_ship GLB, normalise it into a unit
        /// sphere, upscale to ~1.8, and attach the same gameplay/visual
        /// components we use for the procedural ship (trail, collider, rigidbody,
        /// PlayerController). Falls through to the procedural builder if the
        /// GLB file isn't present.
        /// </summary>
        private static GameObject BuildFromTripo(GameObject root, Palette palette, GameObject tripoPrefab)
        {
            var model = Object.Instantiate(tripoPrefab, root.transform);
            model.name = "TripoBody";

            // Normalise to unit sphere, then upscale.
            NormaliseToUnit(model);
            model.transform.localScale *= 1.8f;
            // Face the camera. Measure the model's bounding box — pick the
            // Y-rotation that puts its widest horizontal face toward −Z
            // (camera side). Previously hard-coded to 180° which rendered
            // the back of the seated job-seeker model.
            AutoFaceShip(model);

            // Keep Tripo's own materials — just crank up emission a touch so the
            // ship glows against the dark field.
            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var mat in mr.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        var c = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                        mat.SetColor("_EmissionColor", c * 0.5f);
                    }
                }
            }

            // Engine trail — behind the cabinet
            var trailGo = new GameObject("EngineTrail");
            trailGo.transform.SetParent(root.transform, false);
            trailGo.transform.localPosition = new Vector3(0f, 0.1f, -1.1f);
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.55f;
            trail.endWidth = 0.0f;
            trail.minVertexDistance = 0.05f;
            trail.material = MaterialFactory.UnlitParticle(palette.TorpedoEmissive);
            trail.startColor = palette.TorpedoEmissive;
            var tail = palette.TorpedoColor; tail.a = 0f;
            trail.endColor = tail;

            var sc = root.AddComponent<SphereCollider>();
            sc.radius = 1.1f;
            sc.isTrigger = true;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PlayerController>();

            var pv = root.AddComponent<PlayerVisuals>();
            pv.coloredMaterials = new Material[0];   // don't re-tint Tripo materials
            pv.engineMaterial = null;
            pv.engineTrail = trail;

            return root;
        }

        /// <summary>
        /// Try each 90° yaw (0 / 90 / 180 / 270) and pick the one that gives
        /// the widest X extent after rotation — that's the "front-facing"
        /// orientation, best view of the model toward the camera (−Z).
        /// </summary>
        private static void AutoFaceShip(GameObject glb)
        {
            var filters = glb.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;

            // Compute local bbox in glb-root space (identity rotation).
            glb.transform.localRotation = Quaternion.identity;
            Bounds baseBounds = new Bounds();
            bool have = false;
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var local = mf.sharedMesh.bounds;
                var m = glb.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var min = local.min; var max = local.max;
                for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                for (int cz = 0; cz < 2; cz++)
                {
                    var p = new Vector3(cx == 0 ? min.x : max.x, cy == 0 ? min.y : max.y, cz == 0 ? min.z : max.z);
                    var wp = m.MultiplyPoint3x4(p);
                    if (!have) { baseBounds = new Bounds(wp, Vector3.zero); have = true; }
                    else baseBounds.Encapsulate(wp);
                }
            }
            if (!have) return;

            // Tripo character exports sit on their base along Y with chest-
            // normal along +X or +Z. The right "face the camera" yaw is the
            // one where the chest-normal lands along −Z (away from the
            // camera, so chest looks TOWARD camera). Heuristic: the widest
            // horizontal extent is shoulders, perpendicular to the face
            // direction. So we rotate until X > Z by the largest margin,
            // then the chest is along Z — then add 180° so the chest faces
            // the camera (−Z).
            float bestYaw = 0f;
            float bestShouldersX = -1f;
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 90f;
                // Bbox under yaw rotation: swap X/Z for 90°/270°.
                float sx = (i % 2 == 0) ? baseBounds.size.x : baseBounds.size.z;
                float sz = (i % 2 == 0) ? baseBounds.size.z : baseBounds.size.x;
                if (sx > sz && sx > bestShouldersX)
                {
                    bestShouldersX = sx;
                    bestYaw = yaw;
                }
            }
            // If no yaw has X > Z (nearly cubic model), default 180°.
            if (bestShouldersX < 0f) bestYaw = 180f;
            // Add 180° — we want the face toward the camera, shoulders across.
            bestYaw = (bestYaw + 180f) % 360f;
            glb.transform.localRotation = Quaternion.Euler(0f, bestYaw, 0f);
        }

        /// <summary>
        /// Resize/recenter root's first child so it fits in a unit sphere using
        /// local mesh bounds (same approach as EnemySpawner.NormaliseToUnit).
        /// </summary>
        private static void NormaliseToUnit(GameObject glb)
        {
            var filters = glb.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;

            bool have = false;
            Bounds b = new Bounds();
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var local = mf.sharedMesh.bounds;
                var mat = glb.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
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
            float k = 2f / maxExtent;
            glb.transform.localScale = Vector3.one * k;
            glb.transform.localPosition = -b.center * k;
        }

        /// <summary>
        /// Elongated pyramid pointing +Z (forward), X-width × Z-length × Y-height
        /// centred on origin. Used for the hull and the cockpit accent.
        /// </summary>
        private static Mesh BuildPyramid(float width, float length, float height)
        {
            // 5 verts: 4 back corners + 1 nose tip
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            var verts = new[]
            {
                new Vector3(-hw, -hh, -length * 0.5f), // 0 back-bottom-left
                new Vector3( hw, -hh, -length * 0.5f), // 1 back-bottom-right
                new Vector3( hw,  hh, -length * 0.5f), // 2 back-top-right
                new Vector3(-hw,  hh, -length * 0.5f), // 3 back-top-left
                new Vector3(0f, 0f, length * 0.5f),    // 4 nose
            };
            var tris = new[]
            {
                // back quad (as 2 tris, facing -Z)
                0, 2, 1,
                0, 3, 2,
                // 4 side tris
                0, 1, 4, // bottom
                1, 2, 4, // right
                2, 3, 4, // top
                3, 0, 4, // left
            };
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Triangular delta-wing swept back, attached to the hull on its inner
        /// edge. Left/right mirror via X sign.
        /// </summary>
        private static Mesh BuildDeltaWing(bool isLeft)
        {
            float s = isLeft ? -1f : 1f;
            // Outline (CCW from outside): root-front → tip → root-back
            var verts = new[]
            {
                new Vector3(s * 0.35f,  0f,  0.3f),   // 0 root-front
                new Vector3(s * 2.1f,   0f, -0.95f),  // 1 tip
                new Vector3(s * 0.35f,  0f, -1.2f),   // 2 root-back
            };
            // Two-sided so the wing is visible from top and bottom
            var tris = isLeft
                ? new[] { 0, 1, 2,  2, 1, 0 }
                : new[] { 0, 2, 1,  2, 0, 1 };

            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
