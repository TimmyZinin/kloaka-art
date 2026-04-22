using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Entry point: Unity calls this automatically after the scene loads.
    /// Builds the entire game world from code so we don't need scene/prefab authoring.
    /// </summary>
    public static class GameBootstrap
    {
        public static void BootFromRunner() => Boot();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var world = new GameObject("World");

            // Level 0 palette used to initialise the world. LevelManager will
            // re-apply the palette of whatever the current level is after init.
            var palette = Catalog.Levels[0].palette;

            // Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.clearFlags          = CameraClearFlags.SolidColor;
            cam.backgroundColor     = palette.BackgroundColor;
            cam.fieldOfView         = 60f;
            cam.nearClipPlane       = 0.1f;
            cam.farClipPlane        = 500f;
            cam.transform.position  = new Vector3(0f, 14f, -18f);
            cam.transform.rotation  = Quaternion.Euler(38f, 0f, 0f);

            // Camera shake — lives on the camera so it can read its rest pose.
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            if (cam.GetComponent<CameraIdleBob>() == null) cam.gameObject.AddComponent<CameraIdleBob>();

            // Post-processing stack — bloom + chromatic + vignette + grain
            PostFX.Attach(cam)?.SetLevelTint(palette);

            // Skybox (Poly Haven HDRI) for level 0; LevelManager re-applies on level change.
            SkyboxFactory.Apply(cam, 0, palette);

            // Audio + time warp — singletons hosted on the World object.
            world.AddComponent<AudioManager>();
            world.AddComponent<TimeWarp>();

            // Lighting — 3-light setup: key directional (warm fluorescent) with
            // soft shadows + cool fill from opposite side + rim light from
            // behind to pop silhouettes against the skybox.
            var existingLight = Object.FindAnyObjectByType<Light>();
            if (existingLight == null)
            {
                var lightGo = new GameObject("Key");
                existingLight = lightGo.AddComponent<Light>();
                lightGo.transform.SetParent(world.transform);
            }
            existingLight.type = LightType.Directional;
            existingLight.color = new Color(1.0f, 0.92f, 0.85f);
            existingLight.intensity = 1.6f;
            existingLight.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            existingLight.shadows = LightShadows.Soft;
            existingLight.shadowStrength = 0.55f;
            existingLight.shadowBias = 0.08f;

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.7f, 1.0f);
            fill.intensity = 0.55f;
            fill.transform.rotation = Quaternion.Euler(-18f, 150f, 0f);
            fill.shadows = LightShadows.None;
            fillGo.transform.SetParent(world.transform);

            var rimGo = new GameObject("Rim");
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            // Saturated palette colour so edges catch a bright tint — critical
            // for reading silhouettes through fog.
            rim.color = Color.Lerp(palette.HorizonColor, palette.TorpedoEmissive, 0.35f);
            rim.intensity = 1.4f;
            rim.transform.rotation = Quaternion.Euler(10f, 180f, 0f);   // from behind
            rim.shadows = LightShadows.None;
            rimGo.transform.SetParent(world.transform);

            // Office lighting — warmer ambient, fog starts closer to fake
            // "corridor depth" (stuff behind 80 units fades into darkness).
            RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight     = new Color(0.22f, 0.20f, 0.26f);
            RenderSettings.fog              = true;
            RenderSettings.fogColor         = palette.BackgroundColor;
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance   = 72f;

            // Office setting: carpet floor + corridor of office furniture +
            // ceiling panel lights. Replaces the old neon grid + nebula
            // parallax + LevelDecor billboards, which all implied "synthwave
            // void" instead of "real-life office hell".
            CreateStarfield(world.transform);
            var gridRenderer = OfficeFloor.Create(world.transform, palette);
            var office = OfficeScene.Create(world.transform, palette);
            DustField.Create(world.transform, palette);
            ParallaxLayers parallax = null;
            LevelDecor decor = null;

            // Game state / score
            var managerGo = new GameObject("GameManager");
            managerGo.transform.SetParent(world.transform);
            var manager = managerGo.AddComponent<GameManager>();
            manager.Init(palette);

            // Player
            var ship = ShipFactory.Create(palette);
            ship.transform.SetParent(world.transform);
            var player = ship.GetComponent<PlayerController>();
            manager.RegisterPlayer(player);

            // Spawner
            var spawnerGo = new GameObject("EnemySpawner");
            spawnerGo.transform.SetParent(world.transform);
            var spawner = spawnerGo.AddComponent<EnemySpawner>();

            // Level manager
            var levelGo = new GameObject("LevelManager");
            levelGo.transform.SetParent(world.transform);
            var levelManager = levelGo.AddComponent<LevelManager>();
            manager.LevelManager = levelManager;

            spawner.Configure(manager, levelManager);
            levelManager.Init(manager, spawner, gridRenderer, cam, parallax, player, decor, office);

            // HUD
            HudFactory.Create(manager, levelManager, player);

            // Music kicks in once everything is wired up.
            AudioManager.StartMusic();

            // Analytics — a single "game_start" ping so we know how many
            // people actually pressed "play" vs. just loaded the page.
            // Touch/keyboard and screen resolution are included so we can
            // later correlate device class with drop-off.
            UmamiTracker.Track("game_start",
                $"{{\"touch\":{(Input.touchSupported ? "true" : "false")},\"w\":{Screen.width},\"h\":{Screen.height}}}");
        }

        private static void CreateStarfield(Transform parent)
        {
            var go = new GameObject("Starfield");
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 9999f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 1f),
                new Color(0.7f, 0.8f, 1f, 1f));
            main.maxParticles = 800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(200f, 120f, 400f);
            shape.position = new Vector3(0f, 20f, 100f);

            ps.Emit(800);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialFactory.UnlitParticle(new Color(1f, 1f, 1f, 1f));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static MeshRenderer CreateNeonGrid(Transform parent, Palette palette)
        {
            var go = new GameObject("NeonGrid");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, -3f, 80f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            go.transform.localScale = new Vector3(400f, 1f, 400f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(go.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one;

            Object.Destroy(quad.GetComponent<Collider>());
            var mr = quad.GetComponent<MeshRenderer>();
            mr.material = MaterialFactory.NeonGrid(palette.HorizonColor);
            return mr;
        }
    }

    public struct Palette
    {
        public Color ShipColor;
        public Color ShipEmissive;
        public Color TorpedoColor;
        public Color TorpedoEmissive;
        public Color AsteroidColor;
        public Color AsteroidEmissive;
        public Color ExplosionColor;
        public Color BackgroundColor;
        public Color HorizonColor;
    }
}
