using System;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>실행하면 가장 먼저 보이는 화면. 떠 있는 동안 게임은 멈춘다.</summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        const float FadeTime = 0.35f;

        Game game;
        Action onStarted;
        CanvasGroup group;
        RectTransform icon, startButton;
        float fadeT = -1f;
        bool started;

        public static TitleScreen Build(Game game, Transform canvas, Action onGuide, Action onStarted)
        {
            var root = Ui.MakeNode("Title", canvas, Vector2.zero, Vector2.zero);
            Ui.Stretch(root);
            var title = root.gameObject.AddComponent<TitleScreen>();
            title.game = game;
            title.onStarted = onStarted;
            title.group = root.gameObject.AddComponent<CanvasGroup>();

            // 전체 화면을 덮고 뒤쪽 입력을 막는다
            var bg = Ui.MakeImage("Bg", root, Vector2.zero, Vector2.zero, Palette.Grass, raycast: true);
            Ui.Stretch(bg.rectTransform);
            var band = Ui.MakeImage("Band", root, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.12f));
            band.rectTransform.anchorMin = new Vector2(0f, 0f);
            band.rectTransform.anchorMax = new Vector2(1f, 0.35f);
            band.rectTransform.offsetMin = band.rectTransform.offsetMax = Vector2.zero;

            var safe = Ui.MakeNode("Safe", root, Vector2.zero, new Vector2(Ui.RefWidth, Ui.RefHeight));

            var art = Resources.Load<Texture2D>("TitleIcon");
            if (art != null)
            {
                var raw = Ui.MakeNode("Icon", safe, new Vector2(0, Ui.Top(560)), new Vector2(560, 560))
                    .gameObject.AddComponent<RawImage>();
                raw.texture = art;
                raw.raycastTarget = false;
                title.icon = raw.rectTransform;
            }

            var name = Ui.MakeText("Name", safe, new Vector2(0, Ui.Top(990)), new Vector2(1000, 150), "수확 주식회사", 112,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(name, 5f);
            var sub = Ui.MakeText("Sub", safe, new Vector2(0, Ui.Top(1095)), new Vector2(1000, 60), "HARVEST INC.", 42,
                new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(sub, 2f);

            var start = Ui.MakeButton("Start", safe, new Vector2(0, Ui.Top(1340)), new Vector2(560, 150),
                game.LoadedFromSave ? "이어하기" : "시작하기", 64, Palette.Harvest, title.StartGame, out var startLabel);
            startLabel.color = Palette.Brown;
            title.startButton = (RectTransform)start.transform;

            Ui.MakeButton("Guide", safe, new Vector2(0, Ui.Top(1530)), new Vector2(560, 110), "게임 방법", 44,
                Palette.Shop, onGuide, out _);

            Ui.MakeText("Version", safe, new Vector2(0, Ui.Top(1850)), new Vector2(1000, 50), "프로토타입", 28,
                new Color(1f, 1f, 1f, 0.55f));

            game.PushPause();
            return title;
        }

        void StartGame()
        {
            if (started) return;
            started = true;
            fadeT = 0f;
            group.blocksRaycasts = false;
            game.PopPause();
            onStarted?.Invoke();
        }

        void Update()
        {
            // 게임이 멈춘 동안에도 움직이도록 unscaled 시간 사용
            float t = Time.unscaledTime;
            if (icon != null) icon.anchoredPosition = new Vector2(0, Ui.Top(560) + Mathf.Sin(t * 1.6f) * 14f);
            if (!started)
            {
                float s = 1f + 0.035f * Mathf.Sin(t * 4f);
                startButton.localScale = new Vector3(s, s, 1f);
                return;
            }

            fadeT += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(fadeT / FadeTime);
            if (fadeT >= FadeTime) Destroy(gameObject);
        }
    }
}
