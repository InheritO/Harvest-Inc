using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>게임 방법: 여러 쪽짜리 간단한 설명. 열려 있는 동안 게임은 멈춘다.</summary>
    public sealed class GuidePanel : MonoBehaviour
    {
        struct Page
        {
            public string Title, Badge, Body;
            public Color BadgeColor;
        }

        static readonly Page[] Pages =
        {
            new Page
            {
                Title = "낫으로 수확하기", Badge = "낫", BadgeColor = Palette.Harvest,
                Body = "아래 수확 버튼을 누르면 낫질을 한다.\n\n" +
                       "· 꾹 누르고 있으면 자동으로 연달아 벤다.\n" +
                       "· 작물을 직접 누르면 그 작물을 벤다.\n" +
                       "· 노란 테두리가 지금 노리는 작물이다.\n" +
                       "· PC: 스페이스바 = 수확, ESC = 설정\n\n" +
                       "작물을 쓰러뜨리면 코인을 얻는다.",
            },
            new Page
            {
                Title = "농기계 들이기", Badge = "기계", BadgeColor = Balance.Mower.FxColor,
                Body = "상점에서 농기계를 사면 알아서 일한다.\n\n" +
                       "· 예초기: 자주, 3칸을 벤다.\n" +
                       "· 콤바인: 느리지만 한 줄 전체를 강하게 벤다.\n\n" +
                       "왼쪽 아래 원이 다 차면 작동한다.\n" +
                       "코인으로 낫과 농기계를 강화하자.",
            },
            new Page
            {
                Title = "새 밭으로 이동", Badge = "★", BadgeColor = Palette.Fame,
                Body = "밭을 모두 비우면 보너스 코인과 명성(★)을 얻고\n같은 밭이 다시 자란다.\n\n" +
                       "· 명성을 모아 다음 밭을 연다.\n" +
                       "· 위쪽 밭일수록 단단하지만 보상이 크다.\n" +
                       "· 한 번 연 밭은 언제든 오갈 수 있다.\n\n" +
                       "위쪽의 밭 이름 버튼을 눌러 보자.",
            },
            new Page
            {
                Title = "새 농장 개척", Badge = "씨앗", BadgeColor = Palette.GoldButton,
                Body = "옥수수 밭을 열면 상점에 개척 탭이 생긴다.\n\n" +
                       "· 명성을 모아 개척하면 처음부터 다시 시작한다.\n" +
                       "· 대신 황금 씨앗을 받는다.\n" +
                       "· 황금 씨앗으로 사는 영구 강화는 사라지지 않는다.\n\n" +
                       "개척할수록 작물은 단단해지고 보상은 커진다.",
            },
        };

        Game game;
        Text title, badge, body, nextLabel;
        Image badgeBg;
        GameObject prevButton;
        Image[] dots;
        int page;
        bool open;

        public static GuidePanel Build(Game game, Transform canvas)
        {
            var root = Ui.MakeNode("Guide", canvas, Vector2.zero, Vector2.zero);
            Ui.Stretch(root);
            var panel = root.gameObject.AddComponent<GuidePanel>();
            panel.game = game;

            var dim = Ui.MakeImage("Dim", root, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f), raycast: true);
            Ui.Stretch(dim.rectTransform);

            var safe = Ui.MakeNode("Safe", root, Vector2.zero, new Vector2(Ui.RefWidth, Ui.RefHeight));
            var card = Ui.MakeImage("Card", safe, new Vector2(0, Ui.Top(900)), new Vector2(940, 1180), Palette.Panel,
                Ui.Rounded, raycast: true);
            var t = card.transform;

            Ui.MakeButton("Close", t, new Vector2(360, 520), new Vector2(150, 80), "닫기", 34, Palette.Unaffordable,
                panel.Close, out _);

            panel.badgeBg = Ui.MakeImage("Badge", t, new Vector2(0, 340), new Vector2(220, 220), Color.white, Ui.Circle);
            panel.badge = Ui.MakeText("BadgeText", panel.badgeBg.transform, Vector2.zero, new Vector2(220, 220), "", 76,
                Palette.Brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            panel.title = Ui.MakeText("Title", t, new Vector2(0, 175), new Vector2(860, 80), "", 58, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            panel.body = Ui.MakeText("Body", t, new Vector2(0, -110), new Vector2(820, 460), "", 36,
                new Color(1f, 1f, 1f, 0.88f), TextAnchor.UpperLeft);
            panel.body.lineSpacing = 1.15f;

            panel.prevButton = Ui.MakeButton("Prev", t, new Vector2(-300, -500), new Vector2(240, 100), "◀ 이전", 38,
                Palette.Unaffordable, () => panel.Go(-1), out _).gameObject;
            Ui.MakeButton("Next", t, new Vector2(300, -500), new Vector2(240, 100), "", 38, Palette.Affordable,
                () => panel.Go(+1), out panel.nextLabel);

            panel.dots = new Image[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                float x = (i - (Pages.Length - 1) / 2f) * 36f;
                panel.dots[i] = Ui.MakeImage("Dot" + i, t, new Vector2(x, -500), new Vector2(18, 18), Color.white, Ui.Circle);
            }

            root.gameObject.SetActive(false);
            return panel;
        }

        public bool IsOpen => open;

        public void Open()
        {
            page = 0;
            Show();
            if (open) return;
            open = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            game.PushPause();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            gameObject.SetActive(false);
            game.PopPause();
        }

        void Go(int delta)
        {
            int next = page + delta;
            if (next >= Pages.Length)
            {
                Close();
                return;
            }
            page = Mathf.Clamp(next, 0, Pages.Length - 1);
            Show();
        }

        void Show()
        {
            var p = Pages[page];
            title.text = p.Title;
            badge.text = p.Badge;
            badgeBg.color = p.BadgeColor;
            body.text = p.Body;
            prevButton.SetActive(page > 0);
            nextLabel.text = page == Pages.Length - 1 ? "시작!" : "다음 ▶";
            for (int i = 0; i < dots.Length; i++)
                dots[i].color = i == page ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}
