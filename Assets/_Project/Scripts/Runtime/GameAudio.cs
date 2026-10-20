using System.Collections.Generic;
using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Original synthesized tones. Playback follows the simulation; it never creates resources.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        const int SampleRate = 22050;

        AudioSource music;
        readonly AudioSource[] voices = new AudioSource[4];
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        AudioClip musicClip;
        AudioClip tapClip;
        AudioClip cartClip;
        AudioClip processClip;
        AudioClip saleClip;
        AudioClip upgradeClip;
        AudioClip unlockClip;
        AudioClip goalClip;
        AudioClip prestigeClip;
        AudioClip uiClip;

        int voice;
        double oreProcessed;
        double diamondsSold;
        int trips;
        int taps;
        int levels;
        int prestige;
        int unlockedMines;
        bool primed;

        void OnEnable()
        {
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        void Awake()
        {
            musicClip = Drone();
            tapClip = Tone("tap", 180f, 0.07f, 18f, true);
            cartClip = Tone("cart", 96f, 0.12f, 8f, false);
            processClip = Tone("process", 240f, 0.09f, 14f, true);
            saleClip = Chord("sale", new float[] { 660f, 880f }, 0.16f);
            upgradeClip = Tone("upgrade", 523f, 0.14f, 6f, false);
            unlockClip = Chord("unlock", new float[] { 392f, 523f, 659f }, 0.28f);
            goalClip = Tone("goal", 784f, 0.18f, 5f, false);
            prestigeClip = Tone("prestige", 82f, 0.45f, 2.5f, false);
            uiClip = Tone("ui", 480f, 0.04f, 22f, false);

            music = gameObject.AddComponent<AudioSource>();
            music.clip = musicClip;
            music.loop = true;
            music.spatialBlend = 0f;
            music.playOnAwake = false;
            music.Play();

            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].spatialBlend = 0f;
                voices[i].playOnAwake = false;
            }
        }

        public void Observe(GameSession session)
        {
            if (session == null || !session.Ready || session.State == null)
                return;

            SettingsState settings = session.State.Settings;
            music.volume = (float)settings.Music * 0.22f;
            if (settings.Music <= 0.001 && music.isPlaying)
                music.Pause();
            else if (settings.Music > 0.001 && !music.isPlaying && !AudioListener.pause)
                music.UnPause();

            Snapshot snapshot = Count(session.State);
            if (!primed)
            {
                Remember(snapshot);
                primed = true;
                return;
            }

            float sfx = (float)settings.Sfx;
            if (snapshot.Taps > taps)
                Play(tapClip, "tap", 0.05f, sfx);
            if (snapshot.Trips > trips)
                Play(cartClip, "cart", 0.15f, sfx * 0.8f);
            if (snapshot.OreProcessed > oreProcessed + 0.5d)
                Play(processClip, "process", 0.18f, sfx * 0.55f);
            if (snapshot.DiamondsSold > diamondsSold + 0.05d)
                Play(saleClip, "sale", 0.12f, sfx);
            if (snapshot.Levels > levels)
                Play(upgradeClip, "upgrade", 0.05f, sfx);
            if (snapshot.Unlocked > unlockedMines)
                Play(unlockClip, "unlock", 0.05f, sfx);
            if (snapshot.Goals > goalsCached)
                Play(goalClip, "goal", 0.05f, sfx);
            if (snapshot.Prestige > prestige)
                Play(prestigeClip, "prestige", 0.2f, sfx);
            Remember(snapshot);
        }

        int goalsCached;

        public void PlayUi(GameSession session)
        {
            float sfx = session != null && session.State != null ? (float)session.State.Settings.Sfx : 0.5f;
            Play(uiClip, "ui", 0.04f, sfx * 0.7f);
        }

        void Remember(Snapshot snapshot)
        {
            taps = snapshot.Taps;
            trips = snapshot.Trips;
            oreProcessed = snapshot.OreProcessed;
            diamondsSold = snapshot.DiamondsSold;
            levels = snapshot.Levels;
            unlockedMines = snapshot.Unlocked;
            goalsCached = snapshot.Goals;
            prestige = snapshot.Prestige;
        }

        static Snapshot Count(GameState state)
        {
            var snapshot = new Snapshot();
            snapshot.Goals = state.ClaimedObjectives.Count;
            snapshot.Prestige = state.PrestigeCount;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                if (!mine.Unlocked)
                    continue;
                snapshot.Unlocked++;
                snapshot.Taps += mine.TapCount;
                snapshot.Trips += mine.TripsStarted;
                snapshot.OreProcessed += mine.OreProcessed;
                snapshot.DiamondsSold += mine.DiamondsSold;
                foreach (var pair in mine.UpgradeLevels)
                    snapshot.Levels += pair.Value;
            }
            return snapshot;
        }

        void Play(AudioClip clip, string key, float gap, float volume)
        {
            if (clip == null || volume <= 0.001f)
                return;
            float now = Time.unscaledTime;
            float previous;
            if (lastPlayed.TryGetValue(key, out previous) && now - previous < gap)
                return;
            lastPlayed[key] = now;
            AudioSource source = voices[voice];
            voice = (voice + 1) % voices.Length;
            source.pitch = 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (musicClip == null || music == null)
                return;
            if (!music.isPlaying && music.volume > 0.001f && !AudioListener.pause)
                music.Play();
        }

        struct Snapshot
        {
            public int Taps;
            public int Trips;
            public double OreProcessed;
            public double DiamondsSold;
            public int Levels;
            public int Unlocked;
            public int Goals;
            public int Prestige;
        }

        static AudioClip Tone(string name, float frequency, float seconds, float decay, bool noise)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * seconds));
            float[] data = new float[count];
            float phase = 0f;
            float step = frequency / SampleRate;
            var random = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                phase += step;
                float wave = Mathf.Sin(phase * Mathf.PI * 2f);
                if (noise)
                    wave = wave * 0.65f + ((float)random.NextDouble() * 2f - 1f) * 0.35f;
                float env = Mathf.Exp(-decay * (i / (float)SampleRate));
                data[i] = wave * env * 0.35f;
            }
            return Clip(name, data);
        }

        static AudioClip Chord(string name, float[] frequencies, float seconds)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * seconds));
            float[] data = new float[count];
            for (int n = 0; n < frequencies.Length; n++)
            {
                float phase = 0f;
                float step = frequencies[n] / SampleRate;
                float start = n * 0.04f;
                for (int i = 0; i < count; i++)
                {
                    float time = i / (float)SampleRate;
                    if (time < start)
                        continue;
                    phase += step;
                    float env = Mathf.Exp(-4f * (time - start));
                    data[i] += Mathf.Sin(phase * Mathf.PI * 2f) * env * 0.22f;
                }
            }
            return Clip(name, data);
        }

        static AudioClip Drone()
        {
            int count = SampleRate * 8;
            float[] data = new float[count];
            float a = 0f;
            float e = 0f;
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                a += 110f / SampleRate;
                e += 164.81f / SampleRate;
                float pulse = 0.55f + 0.45f * Mathf.Sin(time * Mathf.PI * 2f / 8f);
                data[i] = (Mathf.Sin(a * Mathf.PI * 2f) * 0.6f + Mathf.Sin(e * Mathf.PI * 2f) * 0.4f) * 0.18f * pulse;
            }
            return Clip("music", data);
        }

        static AudioClip Clip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
