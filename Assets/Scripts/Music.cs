using System;
using System.Linq;
using UnityEngine;

namespace HarvestInc
{
    /// <summary>
    /// 소리 크기 설정. 게임 진행과 따로 PlayerPrefs에 저장하므로 데이터 초기화해도 유지된다.
    /// </summary>
    public static class SoundSettings
    {
        const string BgmKey = "volume.bgm";
        const string SfxKey = "volume.sfx";

        public static float Bgm { get; private set; } = 0.5f;
        public static float Sfx { get; private set; } = 1f;

        public static event Action Changed;

        public static void Load()
        {
            Bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 0.5f));
            Sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
        }

        public static void SetBgm(float v)
        {
            Bgm = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(BgmKey, Bgm);
            Changed?.Invoke();
        }

        public static void SetSfx(float v)
        {
            Sfx = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(SfxKey, Sfx);
            Changed?.Invoke();
        }

        public static void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// 배경음악. Assets/Resources/BGM 폴더의 곡을 이름순으로 첫 곡만 무한 반복한다.
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const string Folder = "BGM";
        const float FadeInTime = 1.5f;

        AudioSource source;
        float fade;

        void Awake()
        {
            var clip = Resources.LoadAll<AudioClip>(Folder).OrderBy(c => c.name).FirstOrDefault();
            if (clip == null)
            {
                Debug.LogWarning("[Music] Assets/Resources/BGM 폴더에 배경음악이 없다");
                enabled = false;
                return;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D
            source.volume = 0f;
            source.Play();
        }

        void Update()
        {
            // 타이틀·가이드로 게임이 멈춰도 음악은 흐르도록 unscaled 시간 사용
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / FadeInTime);
            source.volume = SoundSettings.Bgm * fade;
        }
    }
}
