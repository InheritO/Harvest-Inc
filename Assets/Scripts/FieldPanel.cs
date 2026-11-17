using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>밭 이동: 연 밭은 자유롭게 이동, 다음 밭은 명성으로 연다.</summary>
    public sealed class FieldPanel : MonoBehaviour
    {
        sealed class Row
        {
            public Image Bg;
            public Text Title, Desc, Button;
            public Button Btn;
        }

        const float Width = 1040f, Height = 990f, RowHeight = 150f;

        readonly List<Row> rows = new List<Row>();
        Game game;
        Text fameText;

        public static FieldPanel Build(Game game, RectTransform parent)
        {
            var bg = Ui.MakeImage("FieldPanel", parent, new Vector2(0, Ui.Top(965)), new Vector2(Width, Height),
                Palette.Panel, Ui.Rounded, raycast: true);
            var panel = bg.gameObject.AddComponent<FieldPanel>();
            panel.game = game;
            var t = bg.transform;
            float top = Height / 2f;

            Ui.MakeText("Title", t, new Vector2(-300, top - 55), new Vector2(400, 80), "밭 이동", 56, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            panel.fameText = Ui.MakeText("Fame", t, new Vector2(60, top - 55), new Vector2(360, 80), "", 40,
                Palette.Fame, TextAnchor.MiddleRight, FontStyle.Bold);
            Ui.MakeButton("Close", t, new Vector2(400, top - 55), new Vector2(170, 80), "닫기", 38,
                Palette.Unaffordable, panel.Close, out _);

            float cursor = top - 110f;
            for (int i = 0; i < Balance.Fields.Length; i++)
            {
                int index = i;
                float y = cursor - RowHeight / 2f;
                cursor -= RowHeight + 8f;

                var row = new Row();
                row.Bg = Ui.MakeImage("Row" + i, t, new Vector2(0, y), new Vector2(1000, RowHeight), Palette.Row,
                    Ui.Rounded);
                var rt = row.Bg.transform;

                // 작물 색 아이콘
                Ui.MakeImage("Icon", rt, new Vector2(-440, 0), new Vector2(70, 70),
                    FieldView.HeadColor(Balance.Fields[i].Kind), Ui.Circle);

                row.Title = Ui.MakeText("Title", rt, new Vector2(-120, 30), new Vector2(560, 50), "", 40,
                    Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                row.Desc = Ui.MakeText("Desc", rt, new Vector2(-120, -26), new Vector2(560, 60), "", 26,
                    Palette.TextDim, TextAnchor.MiddleLeft);
                row.Btn = Ui.MakeButton("Action", rt, new Vector2(340, 0), new Vector2(270, 90), "", 34,
                    Palette.Shop, () => panel.OnRowClicked(index), out row.Button);
                panel.rows.Add(row);
            }

            bg.gameObject.SetActive(false);
            return panel;
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf) transform.SetAsLastSibling();
        }

        public void Close() => gameObject.SetActive(false);

        void OnRowClicked(int index)
        {
            if (game.Unlocked[index])
            {
                if (index == game.FieldIndex) return;
                game.MoveTo(index);
                Close();
            }
            else if (index == game.NextLockedIndex)
            {
                bool before = game.Unlocked[index];
                game.UnlockField(index);
                if (!before && game.Unlocked[index]) Close();
            }
        }

        void OnEnable()
        {
            if (game != null) Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            fameText.text = (game.Stage > 1 ? $"{game.Stage}단계 · " : "") + "★ " + Num.Coins(game.Fame);
            int next = game.NextLockedIndex;

            for (int i = 0; i < rows.Count; i++)
            {
                var def = Balance.Fields[i];
                var r = rows[i];
                var img = (Image)r.Btn.targetGraphic;
                bool unlocked = game.Unlocked[i];
                bool current = i == game.FieldIndex;
                bool visible = unlocked || i == next; // 다음 밭까지만 정체를 보여준다

                r.Title.text = visible ? $"{def.Name} 밭" + (current ? "  (지금 여기)" : "") : "??? 밭";
                r.Desc.text = visible
                    ? $"체력 {Num.Stat(game.FieldHp(i))} · 보상 {Num.Gain(game.FieldReward(i))} · 클리어 명성 ★{Num.Gain(def.Fame)}"
                    : "앞의 밭을 먼저 열어야 한다";
                r.Bg.color = current ? new Color(1f, 1f, 1f, 0.16f) : Palette.Row;

                if (current)
                {
                    r.Button.text = "현재 밭";
                    r.Btn.interactable = false;
                    img.color = Palette.Unaffordable;
                }
                else if (unlocked)
                {
                    r.Button.text = "이동";
                    r.Btn.interactable = true;
                    img.color = Palette.Shop;
                }
                else if (i == next)
                {
                    bool affordable = game.Fame + 1e-9 >= def.UnlockCost;
                    r.Button.text = $"열기 ★{Num.Price(def.UnlockCost)}";
                    r.Btn.interactable = true; // 부족하면 거절음
                    img.color = affordable ? Palette.Affordable : Palette.Unaffordable;
                }
                else
                {
                    r.Button.text = "잠김";
                    r.Btn.interactable = false;
                    img.color = Palette.Unaffordable;
                }
            }
        }
    }
}
