using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HarvestInc
{
    /// <summary>프로토타입 사운드. 클립을 코드로 합성한다(애셋 불필요). 정식 사운드가 생기면 클립만 바꾼다.</summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int SR = 44100;
        const float TwoPi = Mathf.PI * 2f;

        AudioSource[] voices;
        int next;
        AudioClip[] slash;
        AudioClip harvest, coin, clear, mower, combine, buy, deny;
        readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

        void Awake()
        {
            voices = new AudioSource[12];
            for (int i = 0; i < voices.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                voices[i] = s;
            }

            slash = new[] { MakeSlash(1, 38f, 900f), MakeSlash(2, 46f, 1100f), MakeSlash(3, 32f, 750f) };
            harvest = MakeHarvest();
            coin = MakeCoin();
            clear = MakeClear();
            mower = MakeMower();
            combine = MakeCombine();
            buy = MakeBuy();
            deny = MakeDeny();
        }

        public void Slash() => Play(slash[Random.Range(0, slash.Length)], 0.55f, 0.08f, 0.02f);
        public void Harvest() => Play(harvest, 0.45f, 0.12f, 0.03f);
        public void Coin() => Play(coin, 0.16f, 0.03f, 0.07f);
        public void Clear() => Play(clear, 0.5f, 0f, 0.2f);
        public void Machine(MachineDef def) =>
            Play(def.Pattern == MachinePattern.FullLine ? combine : mower, def.Pattern == MachinePattern.FullLine ? 0.5f : 0.3f, 0.06f, 0.05f);
        public void Buy() => Play(buy, 0.4f, 0f, 0.03f);
        public void Deny() => Play(deny, 0.35f, 0f, 0.08f);

        void Play(AudioClip clip, float volume, float pitchJitter, float minGap)
        {
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(clip, out float t) && now - t < minGap) return; // 한꺼번에 몰릴 때 과하게 겹치지 않게
            lastPlayed[clip] = now;
            var s = voices[next];
            next = (next + 1) % voices.Length;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume);
        }

        // ── 합성 ──

        static AudioClip Make(string name, float duration, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(SR * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        static AudioClip MakeSlash(int seed, float decay, float tone)
        {
            var rng = new System.Random(seed);
            float prev = 0f, phase = 0f;
            const float dur = 0.12f;
            return Make("slash" + seed, dur, t =>
            {
                float n = (float)(rng.NextDouble() * 2 - 1);
                float hp = n - prev; // 고음 위주 바람 소리
                prev = n;
                phase += Mathf.Lerp(tone * 2f, tone, t / dur) / SR;
                float env = (1f - Mathf.Exp(-t * 600f)) * Mathf.Exp(-t * decay);
                return (hp * 0.55f + Mathf.Sin(phase * TwoPi) * 0.2f) * env;
            });
        }

        static AudioClip MakeHarvest()
        {
            var rng = new System.Random(7);
            float lp = 0f, phase = 0f;
            const float dur = 0.16f;
            return Make("harvest", dur, t =>
            {
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.25f;
                phase += Mathf.Lerp(520f, 180f, t / dur) / SR;
                float env = Mathf.Exp(-t * 22f) * (1f - Mathf.Exp(-t * 400f));
                return (lp * 0.7f + Mathf.Sin(phase * TwoPi) * 0.5f) * env;
            });
        }

        static AudioClip MakeCoin()
        {
            float phase = 0f;
            return Make("coin", 0.18f, t =>
            {
                bool first = t < 0.06f;
                phase += (first ? 1318.5f : 1975.5f) / SR;
                float local = first ? t : t - 0.06f;
                float env = Mathf.Exp(-local * 18f) * (1f - Mathf.Exp(-local * 2000f));
                return (Mathf.Sin(phase * TwoPi) + 0.3f * Mathf.Sin(phase * 2f * TwoPi)) * 0.6f * env;
            });
        }

        static AudioClip MakeClear()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float phase = 0f;
            return Make("clear", 0.8f, t =>
            {
                int i = Mathf.Min((int)(t / 0.09f), notes.Length - 1);
                float local = t - i * 0.09f;
                phase += notes[i] / SR;
                float env = Mathf.Exp(-local * (i == notes.Length - 1 ? 4f : 14f)) * (1f - Mathf.Exp(-local * 1500f));
                float tri = 4f * Mathf.Abs(Frac(phase) - 0.5f) - 1f;
                return tri * 0.6f * env;
            });
        }

        static AudioClip MakeMower()
        {
            var rng = new System.Random(11);
            float phase = 0f;
            const float dur = 0.22f;
            return Make("mower", dur, t =>
            {
                phase += (190f + 25f * Mathf.Sin(t * 30f * TwoPi)) / SR;
                float saw = 2f * Frac(phase) - 1f;
                float n = (float)(rng.NextDouble() * 2 - 1);
                float env = Mathf.Min(1f, t * 60f) * Mathf.Clamp01((dur - t) * 15f);
                return (saw * 0.35f + n * 0.2f) * env;
            });
        }

        static AudioClip MakeCombine()
        {
            var rng = new System.Random(13);
            float phase = 0f, lp = 0f;
            const float dur = 0.42f;
            return Make("combine", dur, t =>
            {
                phase += 72f / SR;
                float saw = 2f * Frac(phase) - 1f;
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.06f;
                float thunk = Mathf.Sin(t * 55f * TwoPi) * Mathf.Exp(-t * 18f);
                float env = Mathf.Min(1f, t * 25f) * Mathf.Clamp01((dur - t) * 8f);
                return (saw * 0.3f + lp * 1.6f) * env + thunk * 0.6f;
            });
        }

        static AudioClip MakeBuy()
        {
            float phase = 0f;
            const float dur = 0.14f;
            return Make("buy", dur, t =>
            {
                phase += Mathf.Lerp(660f, 990f, t / dur) / SR;
                return Mathf.Sin(phase * TwoPi) * 0.6f * Mathf.Exp(-t * 18f) * (1f - Mathf.Exp(-t * 1500f));
            });
        }

        static AudioClip MakeDeny()
        {
            float phase = 0f;
            return Make("deny", 0.14f, t =>
            {
                phase += 140f / SR;
                float sq = Frac(phase) < 0.5f ? 1f : -1f;
                return sq * 0.25f * Mathf.Exp(-t * 14f) * (1f - Mathf.Exp(-t * 1500f));
            });
        }
    }
}
