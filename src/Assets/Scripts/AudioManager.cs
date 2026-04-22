using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Procedural audio: every SFX is synthesised at runtime from sine /
    /// square / noise oscillators with simple AHDSR envelopes. Zero asset
    /// files, zero imports — works in WebGL, plays the same on every
    /// platform. Singleton, instantiated by GameBootstrap.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _sfx;
        private AudioSource _music;
        private const int SampleRate = 44100;

        // Cached procedural clips keyed by name so we don't re-synth on every shot.
        private System.Collections.Generic.Dictionary<string, AudioClip> _cache = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
            _sfx.volume = 0.55f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.volume = 0.18f;
        }

        public static void Play(string id, float pitch = 1f, float volume = 1f)
        {
            if (Instance == null) return;
            if (!Instance._cache.TryGetValue(id, out var clip))
            {
                clip = Instance.Synthesize(id);
                Instance._cache[id] = clip;
            }
            if (clip == null) return;
            Instance._sfx.pitch = pitch * Random.Range(0.94f, 1.06f);
            Instance._sfx.PlayOneShot(clip, volume);
        }

        public static void StartMusic()
        {
            SwitchMusic(0);
        }

        /// <summary>
        /// Per-level music swap. Each level has its own chord progression, BPM,
        /// and lead pattern so the soundtrack doesn't blur into one note across
        /// all four levels. Cached on first synth.
        /// </summary>
        public static void SwitchMusic(int levelIndex)
        {
            if (Instance == null) return;
            string id = "music_lvl_" + levelIndex;
            if (!Instance._cache.TryGetValue(id, out var clip))
            {
                clip = Instance.SynthMusicLoop(levelIndex);
                Instance._cache[id] = clip;
            }
            if (Instance._music.clip == clip && Instance._music.isPlaying) return;
            Instance._music.clip = clip;
            Instance._music.volume = 0.18f;
            Instance._music.Play();
        }

        public static void DuckMusic(float to, float seconds)
        {
            if (Instance == null) return;
            Instance.StartCoroutine(Instance.DuckRoutine(to, seconds));
        }

        private System.Collections.IEnumerator DuckRoutine(float to, float seconds)
        {
            float from = _music.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _music.volume = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            _music.volume = to;
        }

        // ───────────────────────────────────────────────── Synth ─────────

        private AudioClip Synthesize(string id)
        {
            switch (id)
            {
                case "shot":      return SynthShot();
                case "shot_laser":return SynthLaser();
                case "hit":       return SynthHit();
                case "explode":   return SynthExplode(0.55f);
                case "explode_big": return SynthExplode(1.2f);
                case "powerup":   return SynthPowerup();
                case "boss_in":   return SynthBossEntrance();
                case "level_up":  return SynthLevelUp();
                case "ui_click":  return SynthUiClick();
                case "player_hit":return SynthPlayerHit();
                case "combo_up":  return SynthComboUp();
                case "combo_break": return SynthComboBreak();
            }
            return null;
        }

        private AudioClip SynthComboUp()
        {
            // Short triumphant bleep — rising major triad.
            float dur = 0.25f;
            var samples = new float[(int)(dur * SampleRate)];
            float[] notes = { 659.25f, 830.61f, 987.77f, 1318.51f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                int step = Mathf.Clamp(Mathf.FloorToInt(t / (dur / notes.Length)), 0, notes.Length - 1);
                float stepT = t - step * dur / notes.Length;
                float env = Mathf.Min(stepT * 60f, 1f) * Mathf.Exp(-stepT * 14f);
                float s = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * notes[step] * t)) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * notes[step] * 2f * t) * 0.3f;
                samples[i] = s * env * 0.55f;
            }
            AddReverbTail(samples, 0.3f, 0.25f);
            return ClipFrom(samples, "combo_up");
        }

        private AudioClip SynthComboBreak()
        {
            // Falling minor third "uh-oh" — quick, low.
            float dur = 0.30f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 5f);
                float f = Mathf.Lerp(220f, 147f, Mathf.Clamp01(t / dur));
                samples[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f
                            + (Random.value * 2f - 1f) * 0.2f) * env * 0.55f;
            }
            AddReverbTail(samples, 0.2f, 0.15f);
            return ClipFrom(samples, "combo_break");
        }

        private AudioClip SynthShot()
        {
            // Two-layer: paper-flap noise + mid-pitch body. Reverb tail for size.
            float dur = 0.18f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 22f);
                float freq = Mathf.Lerp(720f, 180f, t / dur);
                float body = Mathf.Sin(2f * Mathf.PI * freq * t);
                float flap = (Random.value * 2f - 1f) * Mathf.Exp(-t * 55f) * 0.6f;
                samples[i] = (body * 0.7f + flap) * env * 0.7f;
            }
            AddReverbTail(samples, decay: 0.35f, feedback: 0.22f);
            return ClipFrom(samples, "shot");
        }

        private AudioClip SynthLaser()
        {
            float dur = 0.18f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 12f);
                float freq = Mathf.Lerp(1400f, 600f, t / dur);
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                float sine = Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                samples[i] = (square * 0.4f + sine * 0.6f) * env * 0.7f;
            }
            return ClipFrom(samples, "shot_laser");
        }

        private AudioClip SynthHit()
        {
            // Crumpled-paper thwack: noise burst + sub thump + tiny reverb tail.
            float dur = 0.14f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 32f);
                float noise = (Random.value * 2f - 1f);
                // Shape noise with a lowpass (simple RC one-pole)
                samples[i] = noise * env * 0.85f;
                if (i > 0) samples[i] = samples[i - 1] * 0.5f + samples[i] * 0.5f;
                float thump = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 50f) * 0.5f;
                samples[i] += thump;
            }
            AddReverbTail(samples, decay: 0.25f, feedback: 0.18f);
            return ClipFrom(samples, "hit");
        }

        private AudioClip SynthExplode(float scale)
        {
            float dur = 0.7f * scale;
            var samples = new float[(int)(dur * SampleRate)];
            float lp = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * (3.0f / scale));
                float noise = (Random.value * 2f - 1f);
                lp = Mathf.Lerp(lp, noise, 0.09f);  // slower lowpass → more "boom"
                float low  = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 28f, t / dur) * t);
                float sub  = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(45f, 18f, t / dur) * t) * 0.6f;
                samples[i] = (lp * 0.6f + low * 0.6f + sub * 0.4f) * env * 0.98f;
            }
            AddReverbTail(samples, decay: 0.5f, feedback: 0.35f);
            return ClipFrom(samples, "explode_" + scale);
        }

        private AudioClip SynthPowerup()
        {
            float dur = 0.42f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = t < 0.05f ? t / 0.05f : Mathf.Exp(-(t - 0.05f) * 6f);
                // Arpeggio C–E–G–C
                int step = Mathf.FloorToInt(t / 0.1f);
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                float f = notes[Mathf.Clamp(step, 0, 3)];
                samples[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f
                            + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.25f) * env * 0.8f;
            }
            return ClipFrom(samples, "powerup");
        }

        private AudioClip SynthBossEntrance()
        {
            float dur = 1.4f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(t * 2f, 1f) * Mathf.Exp(-t * 1.2f);
                float lowF = Mathf.Lerp(50f, 110f, t / dur);
                float scream = Mathf.Lerp(220f, 880f, t / dur);
                float low = Mathf.Sin(2f * Mathf.PI * lowF * t);
                float high = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * scream * t)) * 0.3f;
                samples[i] = (low * 0.7f + high) * env * 0.85f;
            }
            return ClipFrom(samples, "boss_in");
        }

        private AudioClip SynthLevelUp()
        {
            float dur = 0.65f;
            var samples = new float[(int)(dur * SampleRate)];
            float[] notes = { 440f, 554.37f, 659.25f, 880f, 1108.73f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                int step = Mathf.FloorToInt(t / (dur / notes.Length));
                step = Mathf.Clamp(step, 0, notes.Length - 1);
                float f = notes[step];
                float env = Mathf.Min((t - step * dur / notes.Length) * 30f, 1f)
                          * Mathf.Exp(-(t - step * dur / notes.Length) * 5f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f
                            + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.2f) * env * 0.7f;
            }
            return ClipFrom(samples, "level_up");
        }

        private AudioClip SynthUiClick()
        {
            float dur = 0.06f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 80f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.5f;
            }
            return ClipFrom(samples, "ui_click");
        }

        private AudioClip SynthPlayerHit()
        {
            float dur = 0.45f;
            var samples = new float[(int)(dur * SampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 4f);
                float low = Mathf.Sin(2f * Mathf.PI * 80f * t);
                float wobble = Mathf.Sin(2f * Mathf.PI * 250f * t + Mathf.Sin(t * 30f));
                float noise = (Random.value * 2f - 1f) * 0.3f;
                samples[i] = (low * 0.6f + wobble * 0.4f + noise) * env * 0.95f;
            }
            return ClipFrom(samples, "player_hit");
        }

        /// <summary>
        /// Per-level synthwave loop. Each level has different chord progression,
        /// BPM, lead pattern, and tone so the soundtrack actually changes when
        /// you advance.
        /// </summary>
        private AudioClip SynthMusicLoop(int levelIndex)
        {
            // Voicings: chord = root, third, fifth (Hz, 3rd octave-ish)
            float[][] amFcg     = { new[] {220.00f, 261.63f, 329.63f},   // Am
                                    new[] {174.61f, 220.00f, 261.63f},   // F
                                    new[] {261.63f, 329.63f, 392.00f},   // C
                                    new[] {196.00f, 246.94f, 293.66f}};  // G
            float[][] dmAmGm    = { new[] {146.83f, 174.61f, 220.00f},   // Dm
                                    new[] {220.00f, 261.63f, 329.63f},   // Am
                                    new[] {196.00f, 233.08f, 293.66f},   // Gm
                                    new[] {174.61f, 220.00f, 261.63f}};  // F
            float[][] em7sus    = { new[] {164.81f, 196.00f, 246.94f},   // Em
                                    new[] {110.00f, 130.81f, 164.81f},   // Asus
                                    new[] {196.00f, 246.94f, 293.66f},   // G
                                    new[] {185.00f, 220.00f, 277.18f}};  // F#m-ish
            float[][] cMinFinal = { new[] {130.81f, 155.56f, 196.00f},   // Cm
                                    new[] {138.59f, 174.61f, 207.65f},   // C#dim
                                    new[] {116.54f, 146.83f, 174.61f},   // Bbm
                                    new[] {123.47f, 155.56f, 185.00f}};  // Bdim

            // Per-level config: chord progression, BPM, lead waveform, lead octave
            // shift, hat density. Levels 0..3 = barakholka, blue galaxy, hr swamp, final.
            float[][] chords;
            float bpm;
            string leadWave;       // "saw", "square", "sine"
            float leadOctave;      // multiplier on chord top note
            float hatDensity;      // 0..1
            float duration;
            switch (levelIndex)
            {
                case 0: // barakholka — driving Am-F-C-G, 110 BPM, square lead
                    chords = amFcg; bpm = 110f; leadWave = "square";
                    leadOctave = 2f; hatDensity = 0.5f; duration = 8f; break;
                case 1: // blue galaxy — uplifting Em-A-G-F#m, 124 BPM, saw lead
                    chords = em7sus; bpm = 124f; leadWave = "saw";
                    leadOctave = 2f; hatDensity = 0.7f; duration = 7.74f; break;
                case 2: // HR swamp — slow Dm-Am-Gm-F, 88 BPM, sine sub
                    chords = dmAmGm; bpm = 88f; leadWave = "sine";
                    leadOctave = 1f; hatDensity = 0.3f; duration = 10.9f; break;
                default: // final — minor-dim Cm-C#dim-Bbm-Bdim, 140 BPM, square attack
                    chords = cMinFinal; bpm = 140f; leadWave = "square";
                    leadOctave = 4f; hatDensity = 0.85f; duration = 6.86f; break;
            }

            int total = (int)(duration * SampleRate);
            var samples = new float[total];
            float beat = 60f / bpm;
            float chordDur = duration / chords.Length;

            // Lead arpeggio note pattern — 8 notes per chord, with an
            // occasional octave-up accent for movement.
            int[] arpPattern = { 0, 2, 1, 2, 0, 2, 1, 2 };

            // Pre-generate a tiny pseudo-random noise table for hi-hats — keeps
            // the pattern deterministic across loops so the track "feels" like
            // a real loop instead of changing on every repetition.
            var noiseTable = new float[2048];
            var noiseRng = new System.Random(12345 + levelIndex);
            for (int i = 0; i < noiseTable.Length; i++)
                noiseTable[i] = (float)(noiseRng.NextDouble() * 2.0 - 1.0);

            for (int i = 0; i < total; i++)
            {
                float t = i / (float)SampleRate;
                int chordIdx = Mathf.Clamp(Mathf.FloorToInt(t / chordDur), 0, chords.Length - 1);
                var chord = chords[chordIdx];

                // ── Kick: every quarter-note, punchy pitch-sweep from 120→45Hz.
                float qPos = (t % beat) / beat;
                float kickFreq = Mathf.Lerp(140f, 48f, Mathf.Clamp01(qPos * 3f));
                float kickEnv = Mathf.Exp(-qPos * 9f);
                float kick = Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.35f;

                // ── Snare: backbeats (beats 2 & 4 of each bar = halfbeat 1 & 3).
                float halfBeatTime = beat * 2f; // bar = 4 beats, snare on half-bar off-step
                int beatIdx = Mathf.FloorToInt(t / beat);
                bool snareBeat = (beatIdx % 2) == 1;  // every other beat
                float sPos = (t % beat) / beat;
                float snareEnv = snareBeat ? Mathf.Exp(-sPos * 14f) : 0f;
                int nIdx = (int)(t * 11025f) & 2047;
                float snare = noiseTable[nIdx] * snareEnv * 0.18f;

                // ── Hi-hat: 16th-notes; closed/open alternating.
                float sixteenth = beat * 0.25f;
                float hPos = (t % sixteenth) / sixteenth;
                int sixteenthIdx = Mathf.FloorToInt(t / sixteenth);
                bool openHat = (sixteenthIdx % 4) == 2;
                float hatEnv = Mathf.Exp(-hPos * (openHat ? 10f : 45f));
                float hat = noiseTable[(int)(t * 17777f) & 2047] * hatEnv * 0.075f * hatDensity;

                // ── Sidechain envelope on kick → ducks bass + pad so kick hits.
                float duck = 1f - kickEnv * 0.75f;

                // ── Bass: rhythmic root, square wave one octave down.
                // Bass eighth-note pattern "on-on-rest-on" → 1,1,0,1 per beat.
                float bEighth = beat * 0.5f;
                int bEi = Mathf.FloorToInt(t / bEighth) % 4;
                bool bassOn = bEi != 2;
                float bEnv = bassOn ? Mathf.Exp(-((t % bEighth) / bEighth) * 3.5f) : 0f;
                float bass = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * chord[0] * 0.5f * t)) * 0.22f * bEnv * duck;

                // ── Pad: sine triad + detuned 5th for width, also ducked.
                float pad = 0f;
                for (int n = 0; n < chord.Length; n++)
                {
                    pad += Mathf.Sin(2f * Mathf.PI * chord[n] * t);
                    pad += Mathf.Sin(2f * Mathf.PI * (chord[n] * 1.003f) * t) * 0.6f;
                }
                pad *= 0.045f * duck;

                // ── Lead arpeggio (eighth-notes) with occasional octave jumps.
                int eighth = Mathf.FloorToInt(t / (beat * 0.5f)) % arpPattern.Length;
                int noteIdx = arpPattern[eighth];
                int barLocal = Mathf.FloorToInt(t / beat) % 4;
                float octMod = (barLocal == 3 && eighth >= 6) ? 2f : 1f; // last 2 eighths of each 4-beat phrase go +1 octave
                float leadF = chord[Mathf.Clamp(noteIdx, 0, chord.Length - 1)] * leadOctave * octMod;
                float eighthPos = (t % (beat * 0.5f)) / (beat * 0.5f);
                float leadEnv = Mathf.Exp(-eighthPos * 5f);
                float leadOsc;
                switch (leadWave)
                {
                    case "saw":    leadOsc = Mathf.Repeat(leadF * t, 1f) * 2f - 1f; break;
                    case "square": leadOsc = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * leadF * t)); break;
                    default:       leadOsc = Mathf.Sin(2f * Mathf.PI * leadF * t); break;
                }
                // Add a slight second-harmonic sine for warmth.
                leadOsc = leadOsc * 0.75f + Mathf.Sin(2f * Mathf.PI * leadF * 2f * t) * 0.25f;
                float lead = leadOsc * leadEnv * 0.09f;

                samples[i] = bass + pad + lead + kick + snare + hat;
            }

            // ── Feedback delay ("dub" echo) — 3/16 note tap for the synthwave feel.
            int delaySamples = Mathf.RoundToInt(beat * 0.75f * SampleRate);
            if (delaySamples > 0 && delaySamples < total / 2)
            {
                for (int i = delaySamples; i < total; i++)
                {
                    samples[i] += samples[i - delaySamples] * 0.22f;
                }
            }
            // Soft normalise to [-0.95, 0.95]
            float peak = 0f;
            for (int i = 0; i < total; i++) if (Mathf.Abs(samples[i]) > peak) peak = Mathf.Abs(samples[i]);
            if (peak > 0.95f)
            {
                float k = 0.95f / peak;
                for (int i = 0; i < total; i++) samples[i] *= k;
            }
            return ClipFrom(samples, "music_lvl_" + levelIndex);
        }

        private AudioClip ClipFrom(float[] samples, string name)
        {
            var c = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            c.SetData(samples, 0);
            return c;
        }

        /// <summary>
        /// Cheap Schroeder-style feedback-comb reverb. Mixes a decaying tail in
        /// place so tiny SFX have spatial size without needing an effect bus.
        /// </summary>
        private static void AddReverbTail(float[] samples, float decay, float feedback)
        {
            int[] taps = { 1411, 1663, 1789, 1999 };  // ~32/37/40/45 ms at 44.1kHz
            var tail = new float[samples.Length];
            for (int tap = 0; tap < taps.Length; tap++)
            {
                int d = taps[tap];
                for (int i = d; i < samples.Length; i++)
                {
                    tail[i] += (samples[i - d] + tail[i - d]) * feedback / taps.Length;
                }
            }
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] += tail[i] * decay;
            }
        }
    }
}
