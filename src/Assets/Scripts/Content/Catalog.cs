using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Logical enemy kinds. Each maps to an EnemyDefinition in Catalog.Enemies,
    /// and eventually to a 3D model under Assets/Resources/&lt;resourcePath&gt;
    /// generated via the Tripo pipeline in tools/tripo/.
    /// </summary>
    public enum EnemyFlavor
    {
        Asteroid,           // procedural fallback / "noise vacancies"
        PaperDrone,         // striped paper-scroll drone
        EnvelopeSpammer,    // bloated paper envelope that spits job postings
        BlueGuru,           // floating corporate-coach head in a suit
        BlueRecruiterBot,   // spider-like corporate recruiter bot
        HrStealth,          // mimicking chameleon recruiter
        HrBlob,             // test-assignment slime
        BossInterviewer,    // hollow CEO-suit final boss
        BossPanel,          // three-headed interview panel boss
        PooEmoji,           // barakholka: flying poop-emoji (trash vacancy)
        MiniToilet          // barakholka: flying toilet with paper resume
    }

    public enum WeaponTier
    {
        Single,
        Double,
        Spread3,
        Laser
    }

    public enum FallbackShape
    {
        Icosphere,          // deformed-sphere rock (existing asteroid mesh)
        Cube,
        Capsule,
        Cylinder,
        FlatSphere          // squashed sphere (disc-like)
    }

    /// <summary>
    /// Data for a single enemy kind. Resolved at spawn time:
    /// if <see cref="resourcePath"/> loads, that prefab is instantiated and tinted;
    /// otherwise the spawner builds a primitive fallback (<see cref="fallbackShape"/>)
    /// so the game keeps running even before 3D models have been generated.
    /// </summary>
    [System.Serializable]
    public struct EnemyDefinition
    {
        public EnemyFlavor flavor;
        public string displayName;
        public string resourcePath;         // e.g. "Enemies/hh_drone" — optional
        public FallbackShape fallbackShape;
        public int hp;
        public float speedMin;
        public float speedMax;
        public float sizeMin;
        public float sizeMax;
        public int baseScore;
        public float powerupDropChance;     // 0..1
        public bool shootsBack;
        public float fireInterval;
        public Color tint;                  // fallback material color
        public Color emissive;              // fallback emissive
        // Yaw (degrees around Y) applied at spawn so the model faces the
        // camera (camera looks down -Z). Tripo defaults exports with front
        // along +Z → 180° rotation turns faces toward player. Per-flavor
        // overrides in Catalog.Enemies let us calibrate models whose
        // exported forward axis differs. 0 in initializer defaults to 180.
        public float facingYaw;

        // Pitch correction applied to the inner GLB (around local X). Use this
        // for models whose "face" points along +Y (flat discs lying down).
        // 90° stands the disc up so its face reads for the player. Default 0
        // for normal upright models.
        public float pitchCorrection;

        // One-line tip shown ONCE the first time this flavor spawns in the
        // player's session. Short, punchy, in-character. Empty = no tip.
        public string firstSightTip;

        public float ResolvedFacingYaw => Mathf.Approximately(facingYaw, 0f) ? 180f : facingYaw;
    }

    [System.Serializable]
    public struct WeaponTierDef
    {
        public WeaponTier tier;
        public string displayName;
        public float cooldown;
        public int damage;
        public float[] angles;              // bullet headings in degrees (0 = forward)
        public bool laser;                  // true = instant beam instead of projectile
    }

    [System.Serializable]
    public struct LevelDefinition
    {
        public string id;
        public string title;
        public string subtitle;
        // Big dramatic intro banner shown for 4.5s when the level starts.
        // Short and punchy — action-call, not description.
        public string introBanner;
        public string introSub;
        public EnemyFlavor[] roster;        // regular enemies to cycle through
        public int killsToBoss;
        public EnemyFlavor boss;
        public Palette palette;
        public float spawnIntervalMin;
        public float spawnIntervalMax;
    }

    public static class Catalog
    {
        /// <summary>
        /// Display name — "Клоака печали". Sub-line framed as an art statement,
        /// not a commercial pitch. See MANIFESTO.md at project root.
        /// </summary>
        public const string GameTitle    = "КЛОАКА ПЕЧАЛИ";
        public const string GameSubtitle = "интерактивное художественное произведение";

        /// <summary>
        /// Where the end-screen "от автора" row sends the player.
        /// Points at the project site (manifesto, license, disclaimer),
        /// NOT at any commercial service.
        /// </summary>
        public const string ProjectSiteUrl = "https://kloaka.timzinin.com/";
        public const string ManifestoUrl   = "https://kloaka.timzinin.com/manifesto/";

        public const string ProjectSiteLabel = "МАНИФЕСТ ПРОЕКТА";

        // Personal contact rows on the end screen — each opens in a new tab.
        public const string TelegramContactUrl = "https://t.me/timofeyzinin";
        public const string TwitterContactUrl  = "https://twitter.com/timzinin";
        public const string TelegramHandle     = "@timofeyzinin";
        public const string TwitterHandle      = "@timzinin";

        public static readonly EnemyDefinition[] Enemies =
        {
            new EnemyDefinition
            {
                flavor = EnemyFlavor.Asteroid,
                displayName = "Шум вакансий",
                resourcePath = "",
                fallbackShape = FallbackShape.Icosphere,
                hp = 1,
                speedMin = 6f, speedMax = 9f,
                sizeMin = 0.7f, sizeMax = 1.4f,
                baseScore = 50,
                powerupDropChance = 0.03f,
                shootsBack = false,
                fireInterval = 0f,
                tint = new Color(0.55f, 0.35f, 0.95f),
                emissive = new Color(0.4f, 0.1f, 0.7f) * 0.6f
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.PaperDrone,
                displayName = "дрон-резюме",
                resourcePath = "Enemies/paper_drone",
                fallbackShape = FallbackShape.Cube,
                hp = 2,
                speedMin = 7f, speedMax = 11f,
                sizeMin = 1.0f, sizeMax = 1.3f,
                baseScore = 120,
                powerupDropChance = 0.05f,
                shootsBack = false,
                fireInterval = 0f,
                tint = new Color(1.0f, 0.82f, 0.12f),
                emissive = new Color(1.0f, 0.7f, 0.0f) * 1.2f,
                firstSightTip = "ДРОН-РЕЗЮМЕ! свёрнутое резюме летит в тебя — сбивай пока не впилилось",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.EnvelopeSpammer,
                displayName = "спам-бот вакансий",
                resourcePath = "Enemies/envelope_spammer",
                fallbackShape = FallbackShape.FlatSphere,
                hp = 3,
                speedMin = 5f, speedMax = 7f,
                sizeMin = 1.3f, sizeMax = 1.6f,
                baseScore = 180,
                powerupDropChance = 0.08f,
                shootsBack = true,
                fireInterval = 2.2f,
                tint = new Color(1.0f, 0.6f, 0.1f),
                emissive = new Color(1.0f, 0.5f, 0.0f) * 1.4f,
                firstSightTip = "СПАМЕР! плюёт вакансиями-бумажками — стреляй, пока не засыпал",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.BlueGuru,
                displayName = "Гуру в костюме",
                resourcePath = "Enemies/blue_guru",
                fallbackShape = FallbackShape.Capsule,
                hp = 3,
                speedMin = 5f, speedMax = 8f,
                sizeMin = 1.1f, sizeMax = 1.4f,
                baseScore = 200,
                powerupDropChance = 0.06f,
                shootsBack = true,
                fireInterval = 2.0f,
                tint = new Color(0.15f, 0.5f, 0.95f),
                emissive = new Color(0.2f, 0.6f, 1.0f) * 1.3f,
                firstSightTip = "ГУРУ В КОСТЮМЕ! стреляет хэштегами — НЕ слушай его, сбивай!",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.BlueRecruiterBot,
                displayName = "Бот-рекрутёр",
                resourcePath = "Enemies/blue_recruiter",
                fallbackShape = FallbackShape.Cylinder,
                hp = 4,
                speedMin = 6f, speedMax = 9f,
                sizeMin = 1.0f, sizeMax = 1.3f,
                baseScore = 250,
                powerupDropChance = 0.08f,
                shootsBack = true,
                fireInterval = 1.6f,
                tint = new Color(0.3f, 0.7f, 1.0f),
                emissive = new Color(0.1f, 0.5f, 1.0f) * 1.4f,
                firstSightTip = "БОТ-РЕКРУТЁР! 4 HP, пишет из трёх каналов одновременно — мочи его",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.HrStealth,
                displayName = "HR-хамелеон",
                resourcePath = "Enemies/hr_stealth",
                fallbackShape = FallbackShape.Capsule,
                hp = 4,
                speedMin = 7f, speedMax = 11f,
                sizeMin = 0.9f, sizeMax = 1.2f,
                baseScore = 280,
                powerupDropChance = 0.1f,
                shootsBack = true,
                fireInterval = 1.4f,
                tint = new Color(0.3f, 0.9f, 0.45f),
                emissive = new Color(0.1f, 1.0f, 0.4f) * 1.3f,
                firstSightTip = "HR-ХАМЕЛЕОН! притворяется дружелюбным — кусай первый",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.HrBlob,
                displayName = "Слизь тестовых заданий",
                resourcePath = "Enemies/hr_blob",
                fallbackShape = FallbackShape.FlatSphere,
                hp = 5,
                speedMin = 4f, speedMax = 6f,
                sizeMin = 1.4f, sizeMax = 1.8f,
                baseScore = 320,
                powerupDropChance = 0.12f,
                shootsBack = true,
                fireInterval = 1.8f,
                tint = new Color(0.6f, 1.0f, 0.3f),
                emissive = new Color(0.4f, 1.0f, 0.2f) * 1.4f,
                firstSightTip = "СЛИЗЬ ТЕСТОВЫХ! 5 HP, облепит и утопит — поливай лазером",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.BossInterviewer,
                displayName = "БОСС: «Мы вам перезвоним»",
                resourcePath = "Bosses/boss_interviewer",
                fallbackShape = FallbackShape.Cube,
                hp = 60,
                speedMin = 2.5f, speedMax = 2.5f,
                // Bosses were reading as ordinary props — make them towering.
                sizeMin = 2.3f, sizeMax = 2.3f,
                baseScore = 5000,
                powerupDropChance = 1.0f,
                shootsBack = true,
                fireInterval = 0.6f,
                tint = new Color(1.0f, 0.15f, 0.25f),
                emissive = new Color(1.0f, 0.0f, 0.3f) * 1.6f,
                firstSightTip = "БОСС «Мы вам перезвоним»! 60 HP — лупи без остановки, дождёшься оффера",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.BossPanel,
                displayName = "БОСС: Панель-жюри",
                resourcePath = "Bosses/boss_panel",
                fallbackShape = FallbackShape.Cylinder,
                hp = 45,
                speedMin = 3f, speedMax = 3f,
                sizeMin = 2.1f, sizeMax = 2.1f,
                baseScore = 3500,
                powerupDropChance = 1.0f,
                shootsBack = true,
                fireInterval = 0.8f,
                tint = new Color(1.0f, 0.3f, 0.5f),
                // Was * 1.5f — clipped to white under the cinema_lobby skybox
                // rim light on level 2. Dropped so the silhouette reads.
                emissive = new Color(1.0f, 0.1f, 0.4f) * 0.9f,
                firstSightTip = "БОСС ПАНЕЛЬ-ЖЮРИ! 45 HP, три башки и три микрофона — цель в центр",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.PooEmoji,
                displayName = "Какашка-эмодзи",
                resourcePath = "Enemies/poo_emoji",
                fallbackShape = FallbackShape.FlatSphere,
                hp = 1,
                speedMin = 6f, speedMax = 9f,
                sizeMin = 1.0f, sizeMax = 1.4f,
                baseScore = 80,
                powerupDropChance = 0.04f,
                shootsBack = false,
                fireInterval = 0f,
                tint = new Color(0.55f, 0.30f, 0.10f),
                emissive = new Color(0.6f, 0.25f, 0.05f) * 0.8f,
                firstSightTip = "КАКАШКИ! шлак-вакансии, 1 HP — разноси пачками, растёт комбо",
            },
            new EnemyDefinition
            {
                flavor = EnemyFlavor.MiniToilet,
                displayName = "Унитаз с резюме",
                resourcePath = "Enemies/mini_toilet",
                fallbackShape = FallbackShape.Cube,
                hp = 3,
                speedMin = 5f, speedMax = 7f,
                sizeMin = 1.2f, sizeMax = 1.5f,
                baseScore = 200,
                powerupDropChance = 0.10f,
                shootsBack = true,
                fireInterval = 2.4f,
                tint = new Color(0.95f, 0.95f, 1.0f),
                emissive = new Color(1.0f, 0.7f, 0.7f) * 0.6f,
                // Toilets fly in profile (side view) so the bowl silhouette
                // reads and the "resume sticking out" gag is visible.
                // 90° around Y = rotated a quarter turn from "face-camera".
                facingYaw = 90f,
                firstSightTip = "УНИТАЗ С РЕЗЮМЕ! плюётся вакансиями — в смыв его!",
            },
        };

        public static readonly WeaponTierDef[] WeaponTiers =
        {
            new WeaponTierDef
            {
                tier = WeaponTier.Single,
                displayName = "Резюме v1",
                cooldown = 0.18f,
                damage = 1,
                angles = new[] { 0f },
                laser = false
            },
            new WeaponTierDef
            {
                tier = WeaponTier.Double,
                displayName = "Резюме v2 (×2)",
                cooldown = 0.16f,
                damage = 1,
                angles = new[] { -4f, 4f },
                laser = false
            },
            new WeaponTierDef
            {
                tier = WeaponTier.Spread3,
                displayName = "Резюме v3 (веер)",
                cooldown = 0.15f,
                damage = 1,
                angles = new[] { -12f, 0f, 12f },
                laser = false
            },
            new WeaponTierDef
            {
                tier = WeaponTier.Laser,
                displayName = "Лазер оффера",
                cooldown = 0.09f,
                damage = 2,
                angles = new[] { 0f },
                laser = false
            }
        };

        public static readonly LevelDefinition[] Levels =
        {
            new LevelDefinition
            {
                id = "barakholka",
                title = "Уровень 1: БАРАХОЛКА ВАКАНСИЙ",
                subtitle = "«Откликов: 0»",
                introBanner = "БАРАХОЛКА ВАКАНСИЙ",
                introSub    = "прорывайся сквозь поток вакансий-говна!",
                // Level 1 — open-air dump of rubbish vacancies: poo-emoji,
                // paper-scroll drones, the odd toilet and envelope spammer.
                roster = new[]
                {
                    EnemyFlavor.PooEmoji,         EnemyFlavor.PooEmoji,
                    EnemyFlavor.PooEmoji,         EnemyFlavor.PooEmoji,
                    EnemyFlavor.PooEmoji,         EnemyFlavor.PooEmoji,
                    EnemyFlavor.PaperDrone,       EnemyFlavor.PaperDrone,
                    EnemyFlavor.PaperDrone,       EnemyFlavor.PaperDrone,
                    EnemyFlavor.EnvelopeSpammer,
                    EnemyFlavor.MiniToilet,
                },
                killsToBoss = 20,
                boss = EnemyFlavor.BossInterviewer,
                // Fast spawns — level 1 is the spam-wave, enemies arrive
                // in clusters like vacancy-feed scroll.
                spawnIntervalMin = 0.35f,
                spawnIntervalMax = 0.8f,
                palette = new Palette
                {
                    ShipColor        = new Color(0.0f, 1.0f, 1.0f, 1.0f),
                    ShipEmissive     = new Color(0.0f, 0.9f, 1.0f, 1.0f) * 2.0f,
                    TorpedoColor     = new Color(1.0f, 0.0f, 1.0f, 1.0f),
                    TorpedoEmissive  = new Color(1.0f, 0.0f, 0.9f, 1.0f) * 3.5f,
                    AsteroidColor    = new Color(1.0f, 0.75f, 0.15f, 1.0f),
                    AsteroidEmissive = new Color(1.0f, 0.6f, 0.0f, 1.0f) * 0.8f,
                    ExplosionColor   = new Color(1.0f, 0.4f, 0.2f, 1.0f),
                    BackgroundColor  = new Color(0.06f, 0.02f, 0.08f, 1.0f),
                    HorizonColor     = new Color(1.0f, 0.35f, 0.15f, 1.0f)
                }
            },
            new LevelDefinition
            {
                id = "blue_galaxy",
                title = "Уровень 2: ГАЛАКТИКА ГУРУ",
                subtitle = "«#opentowork #motivated #blessed»",
                introBanner = "ГАЛАКТИКА ГУРУ",
                introSub    = "инфоцыгане не пройдут — пали!",
                roster = new[] { EnemyFlavor.BlueGuru, EnemyFlavor.BlueRecruiterBot, EnemyFlavor.BlueGuru, EnemyFlavor.PaperDrone },
                killsToBoss = 25,
                boss = EnemyFlavor.BossPanel,
                spawnIntervalMin = 0.55f,
                spawnIntervalMax = 1.2f,
                palette = new Palette
                {
                    ShipColor        = new Color(0.3f, 1.0f, 0.9f, 1.0f),
                    ShipEmissive     = new Color(0.0f, 1.0f, 0.9f, 1.0f) * 2.0f,
                    TorpedoColor     = new Color(1.0f, 0.6f, 0.0f, 1.0f),
                    TorpedoEmissive  = new Color(1.0f, 0.5f, 0.0f, 1.0f) * 3.5f,
                    AsteroidColor    = new Color(0.25f, 0.6f, 1.0f, 1.0f),
                    AsteroidEmissive = new Color(0.15f, 0.5f, 1.0f, 1.0f) * 0.9f,
                    ExplosionColor   = new Color(0.4f, 0.8f, 1.0f, 1.0f),
                    BackgroundColor  = new Color(0.01f, 0.04f, 0.12f, 1.0f),
                    HorizonColor     = new Color(0.2f, 0.5f, 1.0f, 1.0f)
                }
            },
            new LevelDefinition
            {
                id = "hr_swamp",
                title = "Уровень 3: HR-болото",
                subtitle = "«мы изучим ваше резюме и вернёмся»",
                introBanner = "HR-БОЛОТО",
                introSub    = "не утони в тестовых заданиях — стреляй!",
                roster = new[] { EnemyFlavor.HrStealth, EnemyFlavor.HrBlob, EnemyFlavor.BlueRecruiterBot, EnemyFlavor.HrStealth },
                killsToBoss = 28,
                boss = EnemyFlavor.BossPanel,
                spawnIntervalMin = 0.5f,
                spawnIntervalMax = 1.1f,
                palette = new Palette
                {
                    ShipColor        = new Color(0.0f, 1.0f, 0.9f, 1.0f),
                    ShipEmissive     = new Color(0.0f, 1.0f, 0.9f, 1.0f) * 2.0f,
                    TorpedoColor     = new Color(1.0f, 0.2f, 0.6f, 1.0f),
                    TorpedoEmissive  = new Color(1.0f, 0.1f, 0.6f, 1.0f) * 3.5f,
                    AsteroidColor    = new Color(0.4f, 1.0f, 0.3f, 1.0f),
                    AsteroidEmissive = new Color(0.2f, 1.0f, 0.2f, 1.0f) * 1.0f,
                    ExplosionColor   = new Color(0.6f, 1.0f, 0.3f, 1.0f),
                    BackgroundColor  = new Color(0.02f, 0.06f, 0.03f, 1.0f),
                    HorizonColor     = new Color(0.3f, 1.0f, 0.3f, 1.0f)
                }
            },
            new LevelDefinition
            {
                id = "final",
                title = "Уровень 4: Финальное собеседование",
                subtitle = "«а где вы себя видите через 5 лет?»",
                introBanner = "ФИНАЛЬНОЕ СОБЕСЕДОВАНИЕ",
                introSub    = "последний рывок — добей этот ад!",
                roster = new[] { EnemyFlavor.HrStealth, EnemyFlavor.HrBlob, EnemyFlavor.BlueGuru, EnemyFlavor.EnvelopeSpammer },
                killsToBoss = 15,
                boss = EnemyFlavor.BossInterviewer,
                spawnIntervalMin = 0.45f,
                spawnIntervalMax = 0.95f,
                palette = new Palette
                {
                    ShipColor        = new Color(0.0f, 1.0f, 1.0f, 1.0f),
                    ShipEmissive     = new Color(0.0f, 1.0f, 1.0f, 1.0f) * 2.2f,
                    TorpedoColor     = new Color(1.0f, 1.0f, 0.2f, 1.0f),
                    TorpedoEmissive  = new Color(1.0f, 1.0f, 0.0f, 1.0f) * 3.8f,
                    AsteroidColor    = new Color(1.0f, 0.2f, 0.25f, 1.0f),
                    AsteroidEmissive = new Color(1.0f, 0.0f, 0.2f, 1.0f) * 1.2f,
                    ExplosionColor   = new Color(1.0f, 0.3f, 0.4f, 1.0f),
                    BackgroundColor  = new Color(0.08f, 0.0f, 0.03f, 1.0f),
                    HorizonColor     = new Color(1.0f, 0.0f, 0.25f, 1.0f)
                }
            }
        };

        public static EnemyDefinition GetEnemy(EnemyFlavor flavor)
        {
            for (int i = 0; i < Enemies.Length; i++)
            {
                if (Enemies[i].flavor == flavor) return Enemies[i];
            }
            return Enemies[0];
        }

        public static WeaponTierDef GetWeapon(WeaponTier tier)
        {
            for (int i = 0; i < WeaponTiers.Length; i++)
            {
                if (WeaponTiers[i].tier == tier) return WeaponTiers[i];
            }
            return WeaponTiers[0];
        }

        public static WeaponTier NextTier(WeaponTier current)
        {
            switch (current)
            {
                case WeaponTier.Single:  return WeaponTier.Double;
                case WeaponTier.Double:  return WeaponTier.Spread3;
                case WeaponTier.Spread3: return WeaponTier.Laser;
                default:                 return WeaponTier.Laser;
            }
        }
    }
}
