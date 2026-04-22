using UnityEngine;

namespace SpaceShooter
{
    public static class HudFactory
    {
        public static void Create(GameManager manager, LevelManager levelManager, PlayerController player)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudBinder>();
            hud.manager = manager;
            hud.levelManager = levelManager;
            hud.player = player;
        }
    }

    /// <summary>
    /// Immediate-mode HUD — no uGUI package needed, works in Unity 6 Built-in RP out of the box.
    /// Renders:
    ///   - score / highscore / lives / weapon / level label
    ///   - boss HP bar during boss phases
    ///   - level-title banner on level transitions
    ///   - Game Over / Victory screen with a community CTA button + play-again button
    ///
    /// All coordinates use a virtual 1920x1080 layout and GUI.matrix scales it
    /// to the actual screen — so the HUD is readable on both desktop and phone.
    /// </summary>
    public class HudBinder : MonoBehaviour
    {
        public GameManager manager;
        public LevelManager levelManager;
        public PlayerController player;

        // Virtual reference resolution. Phones just scale this down uniformly.
        private const float RefW = 1920f;
        private const float RefH = 1080f;

        private GUIStyle _score;
        private GUIStyle _high;
        private GUIStyle _bigTitle;
        private GUIStyle _hint;
        private GUIStyle _ctrl;
        private GUIStyle _levelTag;
        private GUIStyle _weaponTag;
        private GUIStyle _bossLabel;
        private GUIStyle _pitch;
        private GUIStyle _ctaButton;
        private GUIStyle _ctaLabel;       // guaranteed-visible label over CTA bg
        private GUIStyle _restartButton;
        private GUIStyle _shareButton;
        private GUIStyle _comboBig;
        private GUIStyle _comboMul;
        private GUIStyle _stat;
        private GUIStyle _statLabel;
        private GUIStyle _contactButton;
        private Texture2D _bossBackTex;
        private Texture2D _bossFrontTex;
        private Texture2D _ctaButtonTex;
        private Texture2D _restartButtonTex;
        private Texture2D _shareTgTex;
        private Texture2D _shareXTex;
        private Texture2D _tgContactTex;
        private Texture2D _xContactTex;
        private Texture2D _statsPanelTex;
        private Texture2D _vignetteTex;

        // Powerup banner state (populated via static FlashBanner).
        private static string _bannerText;
        private static Color _bannerColor;
        private static float _bannerUntil;
        private GUIStyle _banner;

        // Intro credit banner — shows "Сделал @timzinin" for the first
        // ~4 seconds of each play session. Uses unscaledTime so it still
        // fades out while the game runs.
        private static float _introStartedAt;
        private GUIStyle _introTitle;
        private GUIStyle _introSub;
        private bool _stylesReady;

        public static void FlashBanner(string text, Color color, float duration = 0.9f)
        {
            _bannerText = text;
            _bannerColor = color;
            _bannerUntil = Time.unscaledTime + duration;
        }

        private void Start()
        {
            // Begin the intro credit timer so it starts counting from the
            // moment HUD is attached (end of GameBootstrap).
            _introStartedAt = Time.unscaledTime;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // Custom OFL fonts — Orbitron for titles/score, Inter for body.
            var titleFont = Resources.Load<Font>("Fonts/Orbitron");
            var bodyFont  = Resources.Load<Font>("Fonts/Inter");

            _score = new GUIStyle(GUI.skin.label)
            {
                fontSize = 52,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.4f, 1f, 1f, 1f) },
                font = titleFont,
            };
            _high = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(1f, 0.4f, 0.9f, 1f) },
                font = titleFont,
            };
            _bigTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 110,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.2f, 0.55f, 1f) },
                fontStyle = FontStyle.Bold,
                font = titleFont,
            };
            _hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.8f, 0.95f, 1f) },
                font = bodyFont,
            };
            _ctrl = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.LowerCenter,
                normal = { textColor = new Color(1f, 0.5f, 0.9f, 0.85f) },
                font = bodyFont,
            };
            _levelTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.4f, 1f) },
                fontStyle = FontStyle.Bold,
                font = titleFont,
            };
            _weaponTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.8f, 1f, 0.9f, 1f) },
                font = bodyFont,
            };
            _bossLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.3f, 0.4f, 1f) },
                fontStyle = FontStyle.Bold,
                font = titleFont,
            };
            _pitch = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.95f) },
                wordWrap = true,
                font = bodyFont,
            };
            // Styled buttons — every one owns its background texture so the
            // Unity default grey button chrome can't eat the label. CTA gets
            // a fat pink gradient, secondary actions get a quieter bg,
            // share buttons get platform colours.
            // (Real textures are assigned below after all tex generation.)
            // CTA button — rooted in GUI.skin.button (guaranteed fully-init
            // state), then EVERY field overridden. border=0 disables Unity's
            // 9-slicing so our gradient bitmap renders as-is instead of
            // getting stretched tiles.
            _ctaButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 38,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                font = titleFont,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(8, 8, 8, 8),
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                fixedHeight = 0,
                fixedWidth  = 0,
            };
            _restartButton  = MakeStyledButton(30, bodyFont,  FontStyle.Bold, null);
            _shareButton    = MakeStyledButton(20, bodyFont,  FontStyle.Bold, null);
            _contactButton  = MakeStyledButton(26, bodyFont,  FontStyle.Bold, null);
            // Plain label reused for CTA shadow pass.
            _ctaLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                font = titleFont,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = new Color(1f, 1f, 1f, 1f) },
            };
            _stat = new GUIStyle(GUI.skin.label)
            {
                fontSize = 52,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.6f, 1f) },
                fontStyle = FontStyle.Bold,
                font = titleFont,
            };
            _statLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) },
                font = bodyFont,
            };
            _banner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 80,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                font = titleFont,
                wordWrap = false,
                clipping = TextClipping.Overflow,
            };
            _introTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                font = titleFont,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = new Color(1f, 0.95f, 0.6f, 1f) },
            };
            _introSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter,
                font = bodyFont,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = new Color(1f, 0.55f, 0.9f, 1f) },
            };
            _comboBig = new GUIStyle(GUI.skin.label)
            {
                fontSize = 90,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(1f, 0.9f, 0.2f, 1f) },
                fontStyle = FontStyle.Bold,
            };
            _comboMul = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(1f, 0.7f, 0.95f, 1f) },
                fontStyle = FontStyle.Bold,
            };

            _bossBackTex = SolidTex(new Color(0.04f, 0.0f, 0.08f, 0.93f));
            _bossFrontTex = SolidTex(new Color(1f, 0.15f, 0.3f, 1f));
            _ctaButtonTex = MakeVerticalGradient(new Color(1f, 0.28f, 0.7f, 1f),
                                                 new Color(0.85f, 0.1f, 0.5f, 1f), 64);
            _restartButtonTex = SolidTex(new Color(0.08f, 0.05f, 0.15f, 0.92f));
            _shareTgTex = SolidTex(new Color(0.15f, 0.55f, 0.85f, 1f));
            _shareXTex  = SolidTex(new Color(0.05f, 0.05f, 0.05f, 1f));
            _tgContactTex = MakeVerticalGradient(new Color(0.30f, 0.70f, 0.95f, 1f),
                                                 new Color(0.15f, 0.55f, 0.85f, 1f), 64);
            _xContactTex  = MakeVerticalGradient(new Color(0.10f, 0.10f, 0.10f, 1f),
                                                 new Color(0.02f, 0.02f, 0.02f, 1f), 64);
            _statsPanelTex = SolidTex(new Color(1f, 1f, 1f, 0.06f));
            _vignetteTex = MakeVignette();

            // Bind backgrounds on the styled buttons now that textures exist.
            AssignButtonBg(_ctaButton,     _ctaButtonTex);
            AssignButtonBg(_restartButton, _restartButtonTex);
            // Share + (legacy) contact styles left transparent — drawn over
            // separate DrawTexture calls in their draw code.
            AssignButtonBg(_shareButton,   _restartButtonTex);
            AssignButtonBg(_contactButton, _restartButtonTex);
        }

        private static void AssignButtonBg(GUIStyle s, Texture2D bg)
        {
            if (s == null) return;
            s.normal.background  = bg;
            s.hover.background   = bg;
            s.active.background  = bg;
            s.focused.background = bg;
        }

        /// <summary>
        /// Button style with explicit background texture on every state, so
        /// Unity's default grey button chrome never leaks through and the
        /// label stays visible with guaranteed white text. If bg is null,
        /// renders as transparent text-only (for icon-less ghost buttons).
        /// </summary>
        private static GUIStyle MakeStyledButton(int fontSize, Font font, FontStyle style, Texture2D bg)
        {
            var s = new GUIStyle
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = style,
                font = font,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(12, 12, 8, 8),
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
            };
            var white = new Color(1f, 1f, 1f, 1f);
            s.normal.textColor  = white; s.normal.background  = bg;
            s.hover.textColor   = white; s.hover.background   = bg;
            s.active.textColor  = white; s.active.background  = bg;
            s.focused.textColor = white; s.focused.background = bg;
            return s;
        }

        private static Texture2D MakeVerticalGradient(Color top, Color bot, int h)
        {
            var t = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float k = (float)y / (h - 1);
                t.SetPixel(0, y, Color.Lerp(bot, top, k));
            }
            t.Apply();
            return t;
        }

        private static Texture2D MakeVignette()
        {
            int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - s * 0.5f) / (s * 0.5f);
                float dy = (y - s * 0.5f) / (s * 0.5f);
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(r, 2.5f);
                t.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
            t.Apply();
            return t;
        }

        private static Texture2D SolidTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void OnGUI()
        {
            if (manager == null) return;
            EnsureStyles();

            // Scale the whole HUD uniformly from RefW×RefH to the real screen.
            // Keeps layout readable on any aspect ratio without rewriting coords.
            float sx = Screen.width / RefW;
            float sy = Screen.height / RefH;
            float s  = Mathf.Min(sx, sy);
            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width  - RefW * s) * 0.5f,
                            (Screen.height - RefH * s) * 0.5f, 0f),
                Quaternion.identity,
                new Vector3(s, s, 1f));

            try
            {
                DrawHud();
            }
            finally
            {
                GUI.matrix = prevMatrix;
            }
        }

        private void DrawHud()
        {
            float w = RefW;
            float h = RefH;

            // Soft vignette around the playfield, tinted slightly stronger
            // during boss phases so the screen feels squeezed.
            if (_vignetteTex != null)
            {
                bool boss = levelManager != null && levelManager.IsBossPhase;
                var prevColor = GUI.color;
                GUI.color = boss ? new Color(1f, 0.4f, 0.5f, 0.85f) : new Color(1f, 1f, 1f, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, w, h), _vignetteTex);
                GUI.color = prevColor;
            }

            string scoreText = "SCORE  " + manager.Score.ToString("D6");
            string highText  = "HIGH  "  + manager.HighScore.ToString("D6");

            DrawLabelWithGlow(new Rect(40, 28, 900, 80), scoreText, _score);
            DrawLabelWithGlow(new Rect(w - 640, 28, 600, 60), highText, _high);

            // Lives
            string lives = "♥ × " + manager.Lives;
            GUI.Label(new Rect(40, 110, 400, 50), lives, _levelTag);

            // Level tag
            if (levelManager != null)
            {
                var lvl = levelManager.CurrentLevel;
                GUI.Label(new Rect(40, 160, 1400, 40), lvl.title, _levelTag);
            }

            // Weapon tag
            if (player != null && player.Weapon != null)
            {
                GUI.Label(new Rect(40, 210, 1400, 40),
                    "ОРУЖИЕ: " + player.Weapon.Current.displayName,
                    _weaponTag);
            }

            // Combo (top-right under high score)
            if (manager.Multiplier > 1)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.12f;
                var prevMatrix = GUI.matrix;
                GUI.matrix = prevMatrix * Matrix4x4.TRS(
                    new Vector3(w - 60, 110, 0f), Quaternion.identity, Vector3.one * pulse);
                GUI.Label(new Rect(-300, -80, 300, 100), "×" + manager.Multiplier, _comboBig);
                GUI.matrix = prevMatrix;
                GUI.Label(new Rect(w - 360, 200, 320, 40),
                    "COMBO " + manager.Combo, _comboMul);
            }

            // Boss HP bar
            if (levelManager != null && levelManager.IsBossPhase && levelManager.CurrentBoss != null)
            {
                var boss = levelManager.CurrentBoss;
                float ratio = Mathf.Clamp01((float)boss.hp / Mathf.Max(1, boss.def.hp));
                float barW = 1100f;
                float barH = 28f;
                float barX = (w - barW) * 0.5f;
                float barY = 90f;

                GUI.DrawTexture(new Rect(barX - 3, barY - 3, barW + 6, barH + 6), _bossBackTex);
                GUI.DrawTexture(new Rect(barX, barY, barW * ratio, barH), _bossFrontTex);
                GUI.Label(new Rect(barX, barY + barH + 6, barW, 50), boss.def.displayName, _bossLabel);
            }

            // Level banner — cinematic intro with dim background, pop-in
            // scale, hold, fade-out.
            if (levelManager != null && Time.time < levelManager.BannerUntil)
            {
                var lvl = levelManager.CurrentLevel;
                float k = levelManager.BannerProgress01();        // 0 → 1
                // Curves: fade-in first 12%, hold 76%, fade-out last 12%.
                float alpha = k < 0.12f ? (k / 0.12f)
                              : k > 0.88f ? (1f - (k - 0.88f) / 0.12f)
                              : 1f;
                float popScale = k < 0.18f ? 0.6f + (k / 0.18f) * 0.4f : 1f;

                var prevColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.75f * alpha);
                GUI.DrawTexture(new Rect(0, 0, w, h), _bossBackTex);
                GUI.color = new Color(1f, 1f, 1f, alpha);

                // BIG level-intro banner — oversized title + subtitle.
                string big = string.IsNullOrEmpty(lvl.introBanner) ? lvl.title : lvl.introBanner;
                string sub = string.IsNullOrEmpty(lvl.introSub)    ? lvl.subtitle : lvl.introSub;

                var prevM = GUI.matrix;
                GUI.matrix = prevM * Matrix4x4.TRS(
                    new Vector3(w * 0.5f, h * 0.4f, 0f),
                    Quaternion.identity,
                    new Vector3(popScale, popScale, 1f));

                // Title shadow
                var prevTitle = _bigTitle.normal.textColor;
                _bigTitle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);
                GUI.Label(new Rect(-2000 + 6, -140 + 6, 4000, 240), big, _bigTitle);
                _bigTitle.normal.textColor = new Color(1f, 0.28f, 0.65f, alpha);
                GUI.Label(new Rect(-2000, -140, 4000, 240), big, _bigTitle);
                _bigTitle.normal.textColor = prevTitle;

                // Subtitle — call to action, yellow
                var prevHint = _hint.normal.textColor;
                _hint.normal.textColor = new Color(1f, 0.95f, 0.45f, alpha);
                GUI.Label(new Rect(-2000, 100, 4000, 80), sub, _hint);
                _hint.normal.textColor = prevHint;

                GUI.matrix = prevM;
                GUI.color = prevColor;
            }

            // Intro credit banner — author attribution + art-project framing
            // for the first 4s of each session. Centered, pink on black strip,
            // fade out.
            if (_introStartedAt > 0f)
            {
                float introAge = Time.unscaledTime - _introStartedAt;
                const float introHold = 3.2f;
                const float introFade = 0.8f;
                if (introAge < introHold + introFade)
                {
                    float introAlpha = introAge < introHold
                        ? 1f
                        : Mathf.Clamp01(1f - (introAge - introHold) / introFade);
                    float introY = h * 0.18f;
                    var prevColor = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.55f * introAlpha);
                    GUI.DrawTexture(new Rect(0, introY, w, 200), _bossBackTex);
                    GUI.color = new Color(1f, 1f, 1f, introAlpha);
                    var tmpA = _introTitle.normal.textColor;
                    _introTitle.normal.textColor = new Color(1f, 0.95f, 0.6f, introAlpha);
                    GUI.Label(new Rect(0, introY + 20, w, 80), "КЛОАКА  ПЕЧАЛИ", _introTitle);
                    _introTitle.normal.textColor = tmpA;
                    var tmpB = _introSub.normal.textColor;
                    _introSub.normal.textColor = new Color(1f, 0.55f, 0.9f, introAlpha);
                    GUI.Label(new Rect(0, introY + 110, w, 60),
                        "художественный проект  ·  @timzinin", _introSub);
                    _introSub.normal.textColor = tmpB;
                    GUI.color = prevColor;
                }
            }

            // Powerup banner (B5) — drawn above combo area, fades last 30%.
            // Long text tips (first-sighting) auto-shrink fontSize so long
            // Russian lines fit on a single line.
            if (_bannerText != null && Time.unscaledTime < _bannerUntil)
            {
                float remaining = _bannerUntil - Time.unscaledTime;
                float k = Mathf.Clamp01(remaining / 0.4f); // fade tail
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 18f) * 0.06f;

                // Auto-size: fontSize 80 for short, down to 34 for long.
                int len = _bannerText.Length;
                int desiredFont = len <= 14 ? 80 : len <= 28 ? 60 : len <= 48 ? 44 : 34;
                _banner.fontSize = desiredFont;

                var prevColor = GUI.color;
                GUI.color = new Color(_bannerColor.r, _bannerColor.g, _bannerColor.b, k);
                var prevM = GUI.matrix;
                GUI.matrix = prevM * Matrix4x4.TRS(
                    new Vector3(w * 0.5f, h * 0.32f, 0f),
                    Quaternion.identity, Vector3.one * pulse);
                var tmp = _banner.normal.textColor;
                _banner.normal.textColor = new Color(0f, 0f, 0f, k * 0.6f);
                GUI.Label(new Rect(-1600, -80, 3200, 160), _bannerText, _banner);
                _banner.normal.textColor = GUI.color;
                GUI.Label(new Rect(-1604, -80, 3200, 160), _bannerText, _banner);
                _banner.normal.textColor = tmp;
                GUI.matrix = prevM;
                GUI.color = prevColor;
            }

            if (manager.IsVictory)
            {
                DrawEndScreen("ТЫ ПРОШЁЛ HR-АД");
            }
            else if (manager.IsGameOver)
            {
                DrawEndScreen("GAME OVER");
            }
            else
            {
                // Controls hint — different text on touch devices.
                string ctrl = Input.touchSupported
                    ? "проведи пальцем чтобы лететь  ·  огонь автоматический"
                    : "WASD / стрелки  ·  SPACE / клик — огонь";
                GUI.Label(new Rect(0, h - 70, w, 40), ctrl, _ctrl);
            }
        }

        private void DrawEndScreen(string title)
        {
            float w = RefW;
            float h = RefH;

            // Dim background — single dark-purple tint with tiny alpha gradient.
            GUI.DrawTexture(new Rect(0, 0, w, h), _bossBackTex);

            // Title
            DrawLabelWithGlow(new Rect(0, h * 0.06f, w, 160), title, _bigTitle);

            // ─── Stats 2×2 grid ────────────────────────────────────────────
            float panelW = 1000f, panelH = 210f;
            float panelX = (w - panelW) * 0.5f;
            float panelY = h * 0.22f;
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), _statsPanelTex);

            float cellW = panelW / 2f;
            float cellH = panelH / 2f;
            DrawStat(new Rect(panelX,         panelY,         cellW, cellH),
                     manager.Score.ToString("N0").Replace(",", " "), "ИТОГ");
            DrawStat(new Rect(panelX + cellW, panelY,         cellW, cellH),
                     FormatTime(manager.SessionSeconds), "ВРЕМЯ");
            DrawStat(new Rect(panelX,         panelY + cellH, cellW, cellH),
                     "×" + manager.MaxMultiplier, "МАКС КОМБО");
            DrawStat(new Rect(panelX + cellW, panelY + cellH, cellW, cellH),
                     manager.LevelReached.ToString(), "УРОВЕНЬ");

            // ─── Pitch copy ────────────────────────────────────────────────
            float pitchW = 1400f;
            GUI.Label(new Rect((w - pitchW) * 0.5f, h * 0.46f, pitchW, 160),
                "Художественный проект о ритуалах поиска работы.\nПрочти манифест — там всё по-честному.", _pitch);

            // ─── Primary CTA — единственный фокус: МАНИФЕСТ ──────────────
            // Three-layer render so the label is guaranteed visible:
            //   1) DrawTexture with the pink gradient bg
            //   2) GUI.Label with white text on top (no button chrome)
            //   3) Invisible GUI.Button as the click catcher (style=none)
            // Previous attempts ran afoul of Unity's default button skin
            // painting grey over our background and swallowing the label.
            float ctaW = 880f, ctaH = 150f;
            float ctaX = (w - ctaW) * 0.5f;
            float ctaY = h * 0.58f;
            var ctaRect = new Rect(ctaX, ctaY, ctaW, ctaH);
            // FIX v3 — use the PROVEN-RENDERING _bigTitle style (the one
            // that draws "GAME OVER" successfully). v1 used custom _ctaLabel
            // rooted in GUI.skin.label → text never appeared. v2 used
            // _ctaButton rooted in GUI.skin.button → also empty. The ONLY
            // style known to render reliably in our WebGL IL2CPP build is
            // _bigTitle; temporarily mutate its fontSize + color to fit
            // the CTA button, then restore.
            GUI.DrawTexture(ctaRect, _ctaButtonTex, ScaleMode.StretchToFill);
            string ctaText = "МАНИФЕСТ  →";
            int prevSize = _bigTitle.fontSize;
            var prevCol  = _bigTitle.normal.textColor;
            // Shadow
            _bigTitle.fontSize = 66;
            _bigTitle.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
            GUI.Label(new Rect(ctaRect.x + 3, ctaRect.y + 4, ctaRect.width, ctaRect.height),
                      ctaText, _bigTitle);
            // White on top
            _bigTitle.normal.textColor = new Color(1f, 1f, 1f, 1f);
            GUI.Label(ctaRect, ctaText, _bigTitle);
            _bigTitle.fontSize = prevSize;
            _bigTitle.normal.textColor = prevCol;
            // Invisible click-catcher covering the rect.
            if (GUI.Button(ctaRect, GUIContent.none, GUIStyle.none))
            {
                AudioManager.Play("ui_click");
                UmamiTracker.Track("cta_clicked_manifesto",
                    $"{{\"from\":\"{(manager.IsVictory ? "victory" : "game_over")}\",\"score\":{manager.Score},\"level\":{manager.LevelReached}}}");
                Application.OpenURL(Catalog.ManifestoUrl);
            }
            // Small supporting line under the CTA — the url target, quiet.
            var tmpHint = _hint.normal.textColor;
            _hint.normal.textColor = new Color(1f, 0.55f, 0.85f, 0.85f);
            GUI.Label(new Rect(0, ctaY + ctaH + 6, w, 36),
                      "kloaka.timzinin.com · художественный проект", _hint);
            _hint.normal.textColor = tmpHint;

            // ─── Retry button ──────────────────────────────────────────────
            float retryY = ctaY + ctaH + 56;
            float retryW = 380f, retryH = 68f;
            float retryX = (w - retryW) * 0.5f;
            if (GUI.Button(new Rect(retryX, retryY, retryW, retryH), "ИГРАТЬ СНОВА", _restartButton))
            {
                AudioManager.Play("ui_click");
                UmamiTracker.Track("retry_clicked",
                    $"{{\"from\":\"{(manager.IsVictory ? "victory" : "game_over")}\",\"score\":{manager.Score},\"level\":{manager.LevelReached}}}");
                if (manager != null) manager.Restart();
            }

            // ─── Share score row — each shares a link to the project site ──
            float shareW = 260f, shareH = 54f, shareGap = 16f;
            float shareY = retryY + retryH + 20;
            float shareTotal = shareW * 2f + shareGap;
            float shareX0 = (w - shareTotal) * 0.5f;
            AssignButtonBg(_shareButton, _shareTgTex);
            if (GUI.Button(new Rect(shareX0, shareY, shareW, shareH),
                           "ПОДЕЛИТЬСЯ · TG", _shareButton))
            {
                AudioManager.Play("ui_click");
                UmamiTracker.Track("share_clicked",
                    $"{{\"platform\":\"tg\",\"score\":{manager.Score},\"level\":{manager.LevelReached}}}");
                Application.OpenURL(BuildShareUrl("tg"));
            }
            AssignButtonBg(_shareButton, _shareXTex);
            if (GUI.Button(new Rect(shareX0 + shareW + shareGap, shareY, shareW, shareH),
                           "ПОДЕЛИТЬСЯ · X", _shareButton))
            {
                AudioManager.Play("ui_click");
                UmamiTracker.Track("share_clicked",
                    $"{{\"platform\":\"x\",\"score\":{manager.Score},\"level\":{manager.LevelReached}}}");
                Application.OpenURL(BuildShareUrl("x"));
            }
            // Restore default bg so subsequent draws aren't confused.
            AssignButtonBg(_shareButton, _restartButtonTex);

            GUI.Label(new Rect(0, shareY + shareH + 14, w, 40),
                "или нажми SPACE / R", _hint);
        }

        private void DrawStat(Rect cell, string value, string label)
        {
            GUI.Label(new Rect(cell.x, cell.y + 14,       cell.width, cell.height - 50), value, _stat);
            GUI.Label(new Rect(cell.x, cell.y + cell.height - 40, cell.width, 30), label, _statLabel);
        }

        private void DrawContactRow(Rect r, Texture2D bg, string label, string url)
        {
            GUI.DrawTexture(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), bg);
            if (GUI.Button(r, label, _contactButton))
            {
                AudioManager.Play("ui_click");
                Application.OpenURL(url);
            }
        }

        private static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            int m = s / 60;
            return m > 0 ? $"{m}:{(s % 60):D2}" : $"{s}\u00a0сек";
        }

        private string BuildShareUrl(string platform)
        {
            // Share link points at the project landing (manifest, disclaimer,
            // license). Preview card renders the project OG image.
            string url = Catalog.ProjectSiteUrl + "?utm_source=" + platform + "&utm_medium=game&utm_campaign=kloaka_pechali";
            string score = manager.Score.ToString("N0").Replace(",", " ").Replace("\u00a0", " ");

            // Platform-specific copy. Telegram accepts long, X has 280-char
            // limit (including URL + mentions). URL-encoded via
            // UnityWebRequest.EscapeURL which is percent-escape RFC 3986.
            string textTg = $"Я прошёл «Клоаку печали» — художественный проект про ощущение поиска работы.\n\n" +
                            $"Счёт: {score} очков.\n" +
                            $"Попробуй и ты:";
            string textX  = $"«Клоака печали» — арт-проект про поиск работы. Мой счёт: {score} очков. " +
                            $"Играй 👇 /cc @timzinin";

            string ue(string s) => UnityEngine.Networking.UnityWebRequest.EscapeURL(s);
            return platform switch
            {
                // Telegram share — URL separate arg + text. URL can contain ASCII-only.
                "tg" => "https://t.me/share/url?url=" + ue(url) + "&text=" + ue(textTg),
                // X/Twitter tweet intent — text + url. 280-char total.
                "x"  => "https://twitter.com/intent/tweet?text=" + ue(textX) + "&url=" + ue(url),
                _    => url,
            };
        }

        private void DrawLabelWithGlow(Rect rect, string text, GUIStyle style)
        {
            var prev = style.normal.textColor;
            var glow = new Color(prev.r, prev.g, prev.b, 0.3f);

            style.normal.textColor = glow;
            GUI.Label(new Rect(rect.x - 2, rect.y,     rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 2, rect.y,     rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x,     rect.y - 2, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x,     rect.y + 2, rect.width, rect.height), text, style);

            style.normal.textColor = prev;
            GUI.Label(rect, text, style);
        }
    }
}
