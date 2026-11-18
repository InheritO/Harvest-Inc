using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>설정: 저장 상태, 지금 저장, 데이터 초기화(두 번 눌러 확정).</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        const float ConfirmWindow = 3f;

        Game game;
        Text info, resetLabel, saveLabel, bgmValue, sfxValue, vibLabel;
        Image resetImg, vibImg;
        float confirmUntil = -1f;
        float savedFlashUntil = -1f;

        public static SettingsPanel Build(Game game, RectTransform parent, Sfx sfx, System.Action onGuide)
        {
            var bg = Ui.MakeImage("Settings", parent, new Vector2(0, Ui.Top(840)), new Vector2(900, 960),
                Palette.Panel, Ui.Rounded, raycast: true);
            var panel = bg.gameObject.AddComponent<SettingsPanel>();
            panel.game = game;
            var t = bg.transform;

            Ui.MakeText("Title", t, new Vector2(-230, 420), new Vector2(360, 80), "설정", 56, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeButton("Close", t, new Vector2(330, 420), new Vector2(170, 80), "닫기", 38,
                Palette.Unaffordable, panel.Close, out _);

            // 소리 크기
            panel.bgmValue = panel.VolumeRow(t, 320, "배경음악", SoundSettings.Bgm, SoundSettings.SetBgm);
            panel.sfxValue = panel.VolumeRow(t, 230, "효과음", SoundSettings.Sfx, v =>
            {
                SoundSettings.SetSfx(v);
                sfx.Coin(); // 움직이는 동안 크기를 바로 들어 볼 수 있게
            });

            // 진동 켜기/끄기
            Ui.MakeText("Vibration", t, new Vector2(-300, 140), new Vector2(220, 60), "진동", 36, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeText("VibrationNote", t, new Vector2(-40, 140), new Vector2(300, 60), "모바일 전용", 26,
                Palette.TextDim, TextAnchor.MiddleLeft);
            var vib = Ui.MakeButton("VibrationToggle", t, new Vector2(300, 140), new Vector2(190, 70), "", 34,
                Palette.Affordable, () => Haptics.SetEnabled(!Haptics.Enabled), out panel.vibLabel);
            panel.vibImg = (Image)vib.targetGraphic;

            panel.info = Ui.MakeText("Info", t, new Vector2(0, 50), new Vector2(800, 100), "", 30,
                Palette.TextDim, TextAnchor.MiddleCenter);

            Ui.MakeButton("SaveNow", t, new Vector2(-135, -55), new Vector2(250, 100), "", 38, Palette.Shop,
                panel.OnSaveClicked, out panel.saveLabel);
            Ui.MakeButton("Guide", t, new Vector2(135, -55), new Vector2(250, 100), "게임 방법", 38, Palette.Affordable,
                () =>
                {
                    panel.Close();
                    onGuide();
                }, out _);

            Ui.MakeButton("Quit", t, new Vector2(0, -165), new Vector2(520, 100), "저장하고 종료", 38,
                Palette.Unaffordable, panel.OnQuitClicked, out _);

            var reset = Ui.MakeButton("Reset", t, new Vector2(0, -285), new Vector2(520, 110), "", 36,
                Palette.Unaffordable, panel.OnResetClicked, out panel.resetLabel);
            panel.resetImg = (Image)reset.targetGraphic;

            Ui.MakeText("ResetNote", t, new Vector2(0, -390), new Vector2(820, 70),
                "코인 · 명성 · 밭 · 농기계 · 강화 · 황금 씨앗 · 영구 강화 · 개척 단계가 모두 지워진다", 24,
                Palette.TextDim, TextAnchor.MiddleCenter);

            bg.gameObject.SetActive(false);
            return panel;
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf) transform.SetAsLastSibling();
        }

        public void Close() => gameObject.SetActive(false);

        void OnDisable() => SoundSettings.Save(); // 창을 닫을 때 소리 설정을 디스크에 기록

        /// <summary>이름 + 슬라이더 + 퍼센트 한 줄.</summary>
        Text VolumeRow(Transform parent, float y, string label, float value, System.Action<float> onChanged)
        {
            Ui.MakeText(label, parent, new Vector2(-300, y), new Vector2(220, 60), label, 36, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var percent = Ui.MakeText(label + "Value", parent, new Vector2(355, y), new Vector2(120, 60), "", 34,
                Color.white, TextAnchor.MiddleRight);
            Ui.MakeSlider(label + "Slider", parent, new Vector2(45, y), new Vector2(440, 40), value, onChanged);
            return percent;
        }

        void OnSaveClicked()
        {
            game.Save();
            savedFlashUntil = Time.unscaledTime + 1.2f;
        }

        void OnQuitClicked()
        {
            game.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 Play를 멈춘다
#else
            Application.Quit(); // iOS는 앱이 스스로 종료하는 것을 막으므로 동작하지 않는다
#endif
        }

        void OnResetClicked()
        {
            if (Time.unscaledTime > confirmUntil)
            {
                confirmUntil = Time.unscaledTime + ConfirmWindow; // 실수 방지: 한 번 더 눌러야 실행
                return;
            }
            confirmUntil = -1f;
            game.ResetAll();
            Close();
        }

        void OnEnable() => confirmUntil = -1f;

        void Update()
        {
            string last = game.LastSaveRealtime < 0f
                ? "아직 저장 안 함"
                : $"{Mathf.FloorToInt(Time.realtimeSinceStartup - game.LastSaveRealtime)}초 전";
            info.text = $"자동 저장: {Mathf.RoundToInt(Game.AutoSaveInterval)}초마다, 앱을 내리거나 끌 때\n마지막 저장: {last}";

            saveLabel.text = Time.unscaledTime <= savedFlashUntil ? "저장했다!" : "지금 저장";
            bgmValue.text = Mathf.RoundToInt(SoundSettings.Bgm * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(SoundSettings.Sfx * 100f) + "%";
            vibLabel.text = Haptics.Enabled ? "켜짐" : "꺼짐";
            vibImg.color = Haptics.Enabled ? Palette.Affordable : Palette.Unaffordable;

            bool confirming = Time.unscaledTime <= confirmUntil;
            resetLabel.text = confirming ? "한 번 더 누르면\n모든 진행 삭제" : "데이터 초기화";
            resetImg.color = confirming ? Palette.HpFill : Palette.Danger;
        }
    }
}
