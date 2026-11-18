using System;
using UnityEngine;

namespace HarvestInc
{
    /// <summary>
    /// 진동(햅틱). 플레이어가 직접 한 행동과 큰 순간에만 울린다. 농기계처럼 자동으로 일어나는 일에는 울리지 않는다.
    /// 안드로이드 8.0 이상은 세기·길이를 조절하고, 7.x는 길이만 적용된다. PC·에디터에서는 아무 일도 하지 않는다.
    /// 세기는 1~255, 길이는 ms. 실제 폰으로 해 보면서 아래 값만 고치면 된다.
    /// </summary>
    public static class Haptics
    {
        // ── 조절용 수치 ──
        const int SwingMs = 10, SwingAmp = 40;                 // 낫질: 아주 약한 틱
        const int SwingRepeatMs = 8, SwingRepeatAmp = 25;      // 꾹 누르기 반복: 더 약하게 (0이면 끔)
        const int KillMs = 20, KillAmp = 90;                   // 낫으로 쓰러뜨림: "툭"
        const int BuyMs = 15, BuyAmp = 60;                     // 구매 성공
        static readonly long[] ClearPattern = { 0, 30, 45, 30 };    // 밭 클리어: 두 번 (쉼, 울림, 쉼, 울림)
        static readonly int[] ClearAmps = { 0, 140, 0, 140 };
        static readonly long[] DenyPattern = { 0, 10, 50, 10 };     // 구매 실패: 약하게 두 번
        static readonly int[] DenyAmps = { 0, 60, 0, 60 };
        static readonly long[] BigPattern = { 0, 50, 60, 80 };      // 밭 열기, 개척: 강하게
        static readonly int[] BigAmps = { 0, 200, 0, 255 };

        const float MinGap = 0.04f; // 아주 빠른 연타에서 진동이 뭉개지지 않도록

        // ── 켜기/끄기 (PlayerPrefs, 데이터 초기화와 무관) ──
        const string EnabledKey = "haptics.enabled";
        public static bool Enabled { get; private set; } = true;

        public static void Load() => Enabled = PlayerPrefs.GetInt(EnabledKey, 1) == 1;

        public static void SetEnabled(bool on)
        {
            Enabled = on;
            PlayerPrefs.SetInt(EnabledKey, on ? 1 : 0);
            if (on) Buy(); // 켰을 때 한 번 울려서 확인
        }

        // ── 이벤트 ──
        public static void Swing() => Light(SwingMs, SwingAmp);
        public static void SwingRepeat() => Light(SwingRepeatMs, SwingRepeatAmp);
        public static void Kill() => Light(KillMs, KillAmp);
        public static void Buy() => Light(BuyMs, BuyAmp);
        public static void Deny() => Pattern(DenyPattern, DenyAmps, false);
        public static void Clear() => Pattern(ClearPattern, ClearAmps, false);
        public static void Big() => Pattern(BigPattern, BigAmps, true);

        static float lastTime = -1f;

        static void Light(int ms, int amp)
        {
            if (ms <= 0 || amp <= 0 || !Ready()) return;
            float now = Time.unscaledTime;
            if (now - lastTime < MinGap) return;
            lastTime = now;
            Platform.OneShot(ms, amp);
        }

        /// <summary>패턴은 드문 순간이라 간격 제한 없이 울린다. big이면 실패 시 기본 진동으로라도 울린다.</summary>
        static void Pattern(long[] timings, int[] amps, bool big)
        {
            if (!Ready()) return;
            lastTime = Time.unscaledTime;
            if (!Platform.Waveform(timings, amps) && big) Handheld.Vibrate();
        }

        static bool Ready() => Enabled && Platform.Available;

        // ─────────────────────────────────────────────

        static class Platform
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            static AndroidJavaObject vibrator;
            static AndroidJavaClass effectClass;
            static int sdk;
            static bool initialized;

            public static bool Available
            {
                get
                {
                    if (!initialized) Init();
                    return vibrator != null;
                }
            }

            static void Init()
            {
                initialized = true;
                try
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                        if (sdk >= 31)
                        {
                            var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                            vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                        }
                        else
                        {
                            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                        }
                    }
                    if (vibrator != null && !vibrator.Call<bool>("hasVibrator")) vibrator = null;
                    if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Haptics] 진동 초기화 실패: {e.Message}");
                    vibrator = null;
                }
            }

            public static void OneShot(int ms, int amp)
            {
                try
                {
                    if (effectClass != null)
                    {
                        using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)ms,
                                   Mathf.Clamp(amp, 1, 255)))
                            vibrator.Call("vibrate", effect);
                    }
                    else
                    {
                        vibrator.Call("vibrate", (long)ms); // 7.x: 세기 조절 없음
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Haptics] {e.Message}");
                }
            }

            public static bool Waveform(long[] timings, int[] amps)
            {
                try
                {
                    if (effectClass != null)
                    {
                        using (var effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amps, -1))
                            vibrator.Call("vibrate", effect);
                    }
                    else
                    {
                        vibrator.Call("vibrate", timings, -1);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Haptics] {e.Message}");
                    return false;
                }
            }
#else
            public static bool Available => false;
            public static void OneShot(int ms, int amp) { }
            public static bool Waveform(long[] timings, int[] amps) => true;
#endif
        }
    }
}
