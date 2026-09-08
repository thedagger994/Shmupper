using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    public enum SfxId
    {
        ShootPistol,
        ShootShotgun,
        ShootFlux,
        ShootRocket,
        Impact,
        Explode,
        EnemyHurt,
        EnemyDie,
        PlayerHurt,
        Heal,
        Pickup,
        Coin,
        UiMove,
        UiSelect,
        UiDeny,
        WaveStart,
        Shrine,
        Descend,
        DryFire
    }

    /// Every sound in the game is synthesised at boot from a handful of oscillators and a noise
    /// source. Nothing is imported, so the audio ships inside the code, stays perfectly in the
    /// arcade register the game is aiming for, and costs a few hundred kilobytes of RAM.
    public static class Sfx
    {
        const int SampleRate = 22050;
        const int VoiceCount = 12;

        static readonly Dictionary<SfxId, AudioClip> Clips = new Dictionary<SfxId, AudioClip>();
        static AudioSource[] _voices;
        static AudioSource _ui;
        static int _next;
        static System.Random _rng;
        static bool _ready;

        public static float Volume = 0.55f;

        public static void Init()
        {
            if (_ready) return;
            _ready = true;
            _rng = new System.Random(1337);

            Bake();

            var host = new GameObject("Sfx");
            UnityEngine.Object.DontDestroyOnLoad(host);

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(host.transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 6f;
                src.maxDistance = 60f;
                _voices[i] = src;
            }

            var uiGo = new GameObject("UiVoice");
            uiGo.transform.SetParent(host.transform, false);
            _ui = uiGo.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;
        }

        public static void Play(SfxId id, Vector3 position, float pitch = 1f, float volume = 1f)
        {
            if (!_ready || !Clips.TryGetValue(id, out var clip)) return;

            var src = _voices[_next];
            _next = (_next + 1) % VoiceCount;

            src.transform.position = position;
            src.clip = clip;
            src.pitch = pitch * UnityEngine.Random.Range(0.94f, 1.06f);
            src.volume = Volume * volume;
            src.Play();
        }

        public static void Play2D(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (!_ready || _ui == null || !Clips.TryGetValue(id, out var clip)) return;

            _ui.pitch = pitch;
            _ui.PlayOneShot(clip, Volume * volume);
        }

        // ---------------------------------------------------------------- synthesis

        static void Bake()
        {
            Clips[SfxId.ShootPistol]  = Build("sfxPistol", 0.14f, (t, n) => Square(t, Sweep(n, 620f, 210f)) * Decay(n, 7f) * 0.5f);
            Clips[SfxId.ShootShotgun] = Build("sfxShotgun", 0.26f, (t, n) => (Noise() * 0.7f + Square(t, Sweep(n, 240f, 70f)) * 0.5f) * Decay(n, 5.5f) * 0.55f);
            Clips[SfxId.ShootFlux]    = Build("sfxFlux", 0.08f, (t, n) => Square(t, Sweep(n, 1500f, 900f)) * Decay(n, 12f) * 0.32f);
            Clips[SfxId.ShootRocket]  = Build("sfxRocket", 0.34f, (t, n) => (Noise() * 0.45f + Sine(t, Sweep(n, 420f, 110f)) * 0.8f) * Decay(n, 4f) * 0.55f);

            Clips[SfxId.Impact]       = Build("sfxImpact", 0.07f, (t, n) => Noise() * Decay(n, 22f) * 0.4f);
            Clips[SfxId.Explode]      = Build("sfxExplode", 0.75f, (t, n) => (Noise() * 0.8f + Sine(t, Sweep(n, 150f, 38f)) * 0.9f) * Decay(n, 3.4f) * 0.62f);

            Clips[SfxId.EnemyHurt]    = Build("sfxEnemyHurt", 0.10f, (t, n) => Square(t, Sweep(n, 300f, 520f)) * Decay(n, 13f) * 0.34f);
            Clips[SfxId.EnemyDie]     = Build("sfxEnemyDie", 0.42f, (t, n) => Saw(t, Sweep(n, 480f, 90f)) * Decay(n, 4.5f) * 0.45f);
            Clips[SfxId.PlayerHurt]   = Build("sfxPlayerHurt", 0.32f, (t, n) => (Noise() * 0.5f + Square(t, Sweep(n, 190f, 96f)) * 0.7f) * Decay(n, 5f) * 0.6f);

            Clips[SfxId.Heal]         = Build("sfxHeal", 0.5f, (t, n) => Sine(t, Steps(n, 392f, 523f, 659f, 784f)) * Decay(n, 2.6f) * 0.35f);
            Clips[SfxId.Pickup]       = Build("sfxPickup", 0.16f, (t, n) => Square(t, n < 0.5f ? 660f : 990f) * Decay(n, 6f) * 0.28f);
            Clips[SfxId.Coin]         = Build("sfxCoin", 0.22f, (t, n) => Square(t, n < 0.35f ? 988f : 1318f) * Decay(n, 4.5f) * 0.34f);

            Clips[SfxId.UiMove]       = Build("sfxUiMove", 0.05f, (t, n) => Square(t, 740f) * Decay(n, 18f) * 0.25f);
            Clips[SfxId.UiSelect]     = Build("sfxUiSelect", 0.18f, (t, n) => Square(t, Steps(n, 523f, 784f, 1046f)) * Decay(n, 5f) * 0.3f);
            Clips[SfxId.UiDeny]       = Build("sfxUiDeny", 0.2f, (t, n) => Square(t, Sweep(n, 220f, 120f)) * Decay(n, 6f) * 0.3f);

            Clips[SfxId.WaveStart]    = Build("sfxWave", 0.9f, (t, n) => (Saw(t, Steps(n, 110f, 138f, 165f)) * 0.6f + Sine(t, 55f) * 0.5f) * Env(n, 0.08f, 2.2f) * 0.45f);
            Clips[SfxId.Shrine]       = Build("sfxShrine", 1.1f, (t, n) => (Sine(t, 523f) + Sine(t, 659f) + Sine(t, 784f)) * 0.28f * Env(n, 0.15f, 1.8f) * 0.45f);
            Clips[SfxId.Descend]      = Build("sfxDescend", 1.0f, (t, n) => (Sine(t, Sweep(n, 440f, 82f)) * 0.8f + Noise() * 0.18f) * Env(n, 0.05f, 2.0f) * 0.5f);
            Clips[SfxId.DryFire]      = Build("sfxDry", 0.07f, (t, n) => Noise() * Decay(n, 26f) * 0.22f);
        }

        static AudioClip Build(string name, float duration, Func<float, float, float> gen)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float n = i / (float)samples;
                data[i] = Mathf.Clamp(gen(t, n), -1f, 1f);
            }

            // Short fade at both ends: without it every clip starts and ends on a discontinuity
            // and the speaker pops loudly enough to be the most noticeable sound in the game.
            int fade = Mathf.Min(120, samples / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[samples - 1 - i] *= k;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float t, float freq) => Mathf.Sin(t * freq * Mathf.PI * 2f);

        static float Square(float t, float freq) => Mathf.Sin(t * freq * Mathf.PI * 2f) >= 0f ? 1f : -1f;

        static float Saw(float t, float freq)
        {
            float phase = (t * freq) % 1f;
            return phase * 2f - 1f;
        }

        static float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        static float Sweep(float n, float from, float to) => Mathf.Lerp(from, to, n);

        static float Steps(float n, params float[] freqs)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(n * freqs.Length), 0, freqs.Length - 1);
            return freqs[i];
        }

        static float Decay(float n, float rate) => Mathf.Exp(-n * rate);

        static float Env(float n, float attack, float decayRate)
        {
            float a = attack <= 0f ? 1f : Mathf.Clamp01(n / attack);
            return a * Mathf.Exp(-n * decayRate);
        }
    }
}
