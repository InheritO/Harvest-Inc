using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>상점: 낫 강화, 농기계 구매와 강화.</summary>
    public sealed class ShopPanel : MonoBehaviour
    {
        sealed class Row
        {
            public Text Title, Desc, Price;
            public Button Btn;
            public Action<Row> Refresh;
        }

        const float Width = 1040f, Height = 990f, RowHeight = 92f, GroupGap = 14f;

        readonly List<Row> rows = new List<Row>();
        Game game;

        public static ShopPanel Build(Game game, RectTransform parent)
        {
            var bg = Ui.MakeImage("Shop", parent, new Vector2(0, Ui.Top(965)), new Vector2(Width, Height),
                Palette.Panel, Ui.Rounded, raycast: true); // 뒤쪽 밭 터치를 막는다
            var shop = bg.gameObject.AddComponent<ShopPanel>();
            shop.game = game;
            var t = bg.transform;
            float top = Height / 2f;

            Ui.MakeText("Title", t, new Vector2(-300, top - 55), new Vector2(400, 80), "상점", 56, Color.white,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.MakeButton("Close", t, new Vector2(400, top - 55), new Vector2(170, 80), "닫기", 38,
                Palette.Unaffordable, shop.Toggle, out _);

            float cursor = top - 105f;

            shop.AddRow(t, ref cursor, game.BuySickleUp, r =>
            {
                r.Title.text = $"낫 강화  Lv.{game.SickleLevel}";
                r.Desc.text = $"낫 피해 {Num.Stat(game.SickleDamage)} → {Num.Stat(game.NextSickleDamage)}";
                shop.SetPrice(r, game.SickleUpPrice, true, null);
            });
            cursor -= GroupGap;

            foreach (var m in game.Machines)
            {
                string pattern = m.Def.Pattern == MachinePattern.Short3 ? "3칸" : "한 줄";

                shop.AddRow(t, ref cursor, () => game.BuyMachine(m), r =>
                {
                    r.Title.text = $"{m.Def.Name} 구매  {m.Count}/{Balance.MaxMachinesPerType}대";
                    r.Desc.text = $"{Num.Sec(m.Cooldown)}초마다 {pattern}에 피해 {Num.Stat(m.Damage)}";
                    shop.SetPrice(r, m.BuyPrice, m.CanBuyMore, "최대");
                });

                shop.AddRow(t, ref cursor, () => game.BuyDamageUp(m), r =>
                {
                    r.Title.text = $"{m.Def.Name} 피해 강화  Lv.{m.DamageLevel}";
                    r.Desc.text = $"피해 {Num.Stat(m.Damage)} → {Num.Stat(m.NextDamage)}";
                    shop.SetPrice(r, m.DamageUpPrice, m.Count > 0, "보유 필요");
                });

                shop.AddRow(t, ref cursor, () => game.BuyCooldownUp(m), r =>
                {
                    r.Title.text = $"{m.Def.Name} 쿨타임 단축  {m.CooldownLevel}/{Balance.CooldownUpMaxLevel}";
                    r.Desc.text = m.CooldownMaxed
                        ? $"{Num.Sec(m.Cooldown)}초 (최대)"
                        : $"{Num.Sec(m.Cooldown)}초 → {Num.Sec(m.NextCooldown)}초";
                    shop.SetPrice(r, m.CooldownUpPrice, m.Count > 0 && !m.CooldownMaxed,
                        m.Count == 0 ? "보유 필요" : "최대");
                });

                shop.AddRow(t, ref cursor, () => game.BuyPrecision(m), r =>
                {
                    r.Title.text = $"{m.Def.Name} 정밀 조준";
                    r.Desc.text = m.Precision ? "작물이 가장 많은 경로를 고름 (적용 중)" : "작물이 가장 많은 경로를 고름";
                    shop.SetPrice(r, m.PrecisionPrice, m.Count > 0 && !m.Precision,
                        m.Precision ? "구매 완료" : "보유 필요");
                });

                cursor -= GroupGap;
            }

            bg.gameObject.SetActive(false);
            return shop;
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf) transform.SetAsLastSibling();
        }

        void AddRow(Transform parent, ref float cursor, Action buy, Action<Row> refresh)
        {
            float y = cursor - RowHeight / 2f;
            cursor -= RowHeight;

            var bg = Ui.MakeImage("Row", parent, new Vector2(0, y), new Vector2(1000, RowHeight - 8), Palette.Row,
                Ui.Rounded);
            var r = new Row { Refresh = refresh };
            r.Title = Ui.MakeText("Title", bg.transform, new Vector2(-160, 16), new Vector2(620, 44), "", 34,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            r.Desc = Ui.MakeText("Desc", bg.transform, new Vector2(-160, -20), new Vector2(620, 36), "", 26,
                Palette.TextDim, TextAnchor.MiddleLeft);
            r.Btn = Ui.MakeButton("Buy", bg.transform, new Vector2(330, 0), new Vector2(300, 70), "", 32,
                Palette.Affordable, () =>
                {
                    buy();
                    RefreshAll();
                }, out r.Price);
            rows.Add(r);
        }

        void SetPrice(Row r, double price, bool available, string unavailableText)
        {
            var img = (Image)r.Btn.targetGraphic;
            if (!available)
            {
                r.Price.text = unavailableText;
                r.Btn.interactable = false;
                img.color = Palette.Unaffordable;
                return;
            }
            bool affordable = game.Coins + 1e-9 >= price;
            r.Price.text = $"{Num.Price(price)} 코인";
            r.Btn.interactable = true; // 부족해도 눌러서 거절음을 듣게 한다
            img.color = affordable ? Palette.Affordable : Palette.Unaffordable;
        }

        void RefreshAll()
        {
            foreach (var r in rows) r.Refresh(r);
        }

        void OnEnable()
        {
            if (game != null) RefreshAll();
        }

        void Update() => RefreshAll();
    }
}
