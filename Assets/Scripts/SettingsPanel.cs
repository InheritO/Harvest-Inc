using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>설정: 저장 상태, 지금 저장, 데이터 초기화(두 번 눌러 확정).</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        const float ConfirmWindow = 3f;

        Game game;
        Text info, resetLabel, saveLabel;
        Image resetImg;
        float confirmUntil = -1f;
        float savedFlashUntil = -1f;

        public static SettingsPanel Build(Game game, RectTransform parent)
        {
            var bg = Ui.MakeImage("Settings", parent, new Vector2(0, Ui.Top(840)), new Vector2(900, 640),
                Palette.Panel, Ui.Rounded, raycast: true);
            var panel = bg.gameObject.AddComponent<SettingsPanel>();
            panel.game = game;
            var t = bg.transform;

            Ui.MakeText("Title", t, new Vector2(-230, 260), new Vector2(360, 80), "설정", 56, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeButton("Close", t, new Vector2(330, 260), new Vector2(170, 80), "닫기", 38,
                Palette.Unaffordable, panel.Close, out _);

            panel.info = Ui.MakeText("Info", t, new Vector2(0, 145), new Vector2(800, 110), "", 30,
                Palette.TextDim, TextAnchor.MiddleCenter);

            Ui.MakeButton("SaveNow", t, new Vector2(0, 20), new Vector2(520, 100), "", 40, Palette.Shop,
                panel.OnSaveClicked, out panel.saveLabel);

            var reset = Ui.MakeButton("Reset", t, new Vector2(0, -130), new Vector2(520, 120), "", 38,
                Palette.Unaffordable, panel.OnResetClicked, out panel.resetLabel);
            panel.resetImg = (Image)reset.targetGraphic;

            Ui.MakeText("ResetNote", t, new Vector2(0, -250), new Vector2(820, 70),
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

        void OnSaveClicked()
        {
            game.Save();
            savedFlashUntil = Time.unscaledTime + 1.2f;
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

            bool confirming = Time.unscaledTime <= confirmUntil;
            resetLabel.text = confirming ? "한 번 더 누르면\n모든 진행 삭제" : "데이터 초기화";
            resetImg.color = confirming ? Palette.HpFill : Palette.Danger;
        }
    }
}
