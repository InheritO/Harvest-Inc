using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>새 농장 개척: 미리 보기, 개척 버튼(두 번 눌러 확정), 영구 강화.</summary>
    public sealed class PrestigePanel : MonoBehaviour
    {
        sealed class Row
        {
            public Text Title, Desc, Price;
            public Button Btn;
        }

        const float Width = 1040f, Height = 990f, RowHeight = 92f;
        const float ConfirmWindow = 3f;

        readonly List<Row> rows = new List<Row>();
        Game game;
        Text stageText, seedsText, prestigeLabel;
        Button prestigeBtn;
        float confirmUntil = -1f;

        public static PrestigePanel Build(Game game, RectTransform parent, Action onShopTab)
        {
            var bg = Ui.MakeImage("PrestigePanel", parent, new Vector2(0, Ui.Top(965)), new Vector2(Width, Height),
                Palette.Panel, Ui.Rounded, raycast: true);
            var panel = bg.gameObject.AddComponent<PrestigePanel>();
            panel.game = game;
            var t = bg.transform;
            float top = Height / 2f;

            Ui.MakeText("Title", t, new Vector2(-300, top - 55), new Vector2(400, 80), "새 농장 개척", 50,
                Palette.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeButton("ShopTab", t, new Vector2(150, top - 55), new Vector2(210, 80), "◀ 강화", 34,
                Palette.Shop, onShopTab, out _);
            Ui.MakeButton("Close", t, new Vector2(400, top - 55), new Vector2(170, 80), "닫기", 38,
                Palette.Unaffordable, panel.Close, out _);

            // 설명 + 개척 버튼
            panel.stageText = Ui.MakeText("Stage", t, new Vector2(-180, top - 128), new Vector2(620, 46), "", 36,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeText("Info", t, new Vector2(-180, top - 196), new Vector2(620, 90),
                $"다음 단계: 작물 체력 ×{Num.Stat(Balance.StageHpMult)} · 보상 ×{Num.Stat(Balance.StageRewardMult)}\n" +
                "초기화: 코인 · 명성 · 연 밭 · 농기계 · 강화\n" +
                "유지: 황금 씨앗 · 영구 강화 · 단계", 24, Palette.TextDim, TextAnchor.MiddleLeft);
            panel.seedsText = Ui.MakeText("Seeds", t, new Vector2(-180, top - 268), new Vector2(620, 46), "", 34,
                Palette.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            panel.prestigeBtn = Ui.MakeButton("Prestige", t, new Vector2(310, top - 190), new Vector2(340, 150), "",
                36, Palette.GoldButton, panel.OnPrestigeClicked, out panel.prestigeLabel);
            panel.prestigeLabel.color = Palette.Brown;

            Ui.MakeText("PermTitle", t, new Vector2(-300, top - 310), new Vector2(400, 50), "영구 강화", 36,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

            float cursor = top - 345f;
            for (int i = 0; i < Balance.Perms.Length; i++)
            {
                int index = i;
                float y = cursor - RowHeight / 2f;
                cursor -= RowHeight;
                var rowBg = Ui.MakeImage("Row" + i, t, new Vector2(0, y), new Vector2(1000, RowHeight - 8),
                    Palette.Row, Ui.Rounded);
                var r = new Row();
                r.Title = Ui.MakeText("Title", rowBg.transform, new Vector2(-160, 16), new Vector2(620, 44), "", 34,
                    Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                r.Desc = Ui.MakeText("Desc", rowBg.transform, new Vector2(-160, -20), new Vector2(620, 36), "", 26,
                    Palette.TextDim, TextAnchor.MiddleLeft);
                r.Btn = Ui.MakeButton("Buy", rowBg.transform, new Vector2(330, 0), new Vector2(300, 70), "", 32,
                    Palette.Affordable, () => game.BuyPerm(index), out r.Price);
                panel.rows.Add(r);
            }

            bg.gameObject.SetActive(false);
            return panel;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close() => gameObject.SetActive(false);

        void OnPrestigeClicked()
        {
            if (!game.CanPrestige)
            {
                game.Prestige(); // 조건 미달이면 거절음만 난다
                return;
            }
            if (Time.unscaledTime > confirmUntil)
            {
                confirmUntil = Time.unscaledTime + ConfirmWindow; // 실수 방지: 한 번 더 눌러야 실행
                return;
            }
            confirmUntil = -1f;
            game.Prestige();
            Close();
        }

        void OnEnable()
        {
            confirmUntil = -1f;
            if (game != null) Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            stageText.text = $"{game.Stage}단계 → {game.Stage + 1}단계 · 필요 명성 ★{Num.Price(game.PrestigeFameCost)}";
            seedsText.text = $"보유 황금 씨앗 {Num.Coins(game.GoldenSeeds)}";

            var img = (Image)prestigeBtn.targetGraphic;
            string cost = $"★{Num.Coins(game.Fame)} / {Num.Price(game.PrestigeFameCost)}";
            if (!game.CanPrestige)
            {
                prestigeLabel.text = $"명성 부족\n{cost}";
                prestigeBtn.interactable = true; // 눌러서 거절음
                img.color = Palette.Unaffordable;
            }
            else
            {
                bool confirming = Time.unscaledTime <= confirmUntil;
                prestigeLabel.text = confirming
                    ? "한 번 더 누르면\n개척!"
                    : $"개척하기\n씨앗 +{Num.Coins(game.PrestigeSeeds)}";
                prestigeBtn.interactable = true;
                img.color = confirming ? Palette.HpFill : Palette.GoldButton;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var d = Balance.Perms[i];
                var r = rows[i];
                int lv = game.PermLevels[i];
                r.Title.text = d.MaxLevel == 1
                    ? d.Name + (lv > 0 ? "  (보유)" : "")
                    : $"{d.Name}  Lv.{lv}" + (d.MaxLevel > 0 ? $"/{d.MaxLevel}" : "");
                r.Desc.text = d.Desc;

                var b = (Image)r.Btn.targetGraphic;
                if (game.PermMaxed(i))
                {
                    r.Price.text = d.MaxLevel == 1 ? "구매 완료" : "최대";
                    r.Btn.interactable = false;
                    b.color = Palette.Unaffordable;
                }
                else
                {
                    double price = game.PermPrice(i);
                    r.Price.text = $"씨앗 {Num.Price(price)}";
                    r.Btn.interactable = true;
                    b.color = game.GoldenSeeds + 1e-9 >= price ? Palette.GoldButton : Palette.Unaffordable;
                }
            }
        }
    }
}
