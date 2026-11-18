using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>
    /// MainScene에서 Play를 누르면 게임 화면 전체를 코드로 만든다.
    /// 씬에 따로 배치할 것이 없어서, 프로토타입 동안 씬 파일 충돌 없이 코드만 고치면 된다.
    /// </summary>
    public static class Bootstrap
    {
        const string SceneName = "MainScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != SceneName) return;
            if (Object.FindAnyObjectByType<Game>() != null) return;
            Build();
        }

        static void Build()
        {
            Application.targetFrameRate = 60;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;

            Ui.Init();
            EnsureEventSystem();

            var root = new GameObject("Harvest Inc");
            if (Object.FindAnyObjectByType<AudioListener>() == null) root.AddComponent<AudioListener>();
            var game = root.AddComponent<Game>();
            var sfx = root.AddComponent<Sfx>();
            SoundSettings.Load();
            Haptics.Load();
            root.AddComponent<Music>();

            // 캔버스: 1080×1920 기준, 화면 비율이 달라도 전체가 보이도록 Expand
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Ui.RefWidth, Ui.RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var bg = Ui.MakeImage("Background", canvas.transform, Vector2.zero, Vector2.zero, Palette.Grass);
            Ui.Stretch(bg.rectTransform);
            var safe = Ui.MakeNode("Safe", canvas.transform, Vector2.zero, new Vector2(Ui.RefWidth, Ui.RefHeight));

            // 밭 (흔들림은 Board 단위)
            var board = Ui.MakeNode("Board", safe, new Vector2(0, Ui.Top(840)), new Vector2(930, 1230));
            Ui.MakeImage("BoardBg", board, Vector2.zero, new Vector2(930, 1230), Palette.Board, Ui.Rounded);
            var field = Ui.MakeNode("Field", board, Vector2.zero, new Vector2(900, 1200)).gameObject.AddComponent<FieldView>();
            var fx = Ui.MakeNode("Fx", board, Vector2.zero, new Vector2(900, 1200)).gameObject.AddComponent<Fx>();
            fx.Init(board);

            field.Build(game);
            game.Init(field, fx, sfx);

            // 상점, 개척, 밭 이동, 설정 창은 한 번에 하나만 열린다
            ShopPanel shop = null;
            FieldPanel fields = null;
            PrestigePanel prestige = null;
            SettingsPanel settings = null;
            Hud.Build(game, safe,
                onShop: () =>
                {
                    fields.Close();
                    settings.Close();
                    if (prestige.gameObject.activeSelf) prestige.Close();
                    else shop.Toggle();
                },
                onField: () => { shop.Close(); prestige.Close(); settings.Close(); fields.Toggle(); },
                onSettings: () => { shop.Close(); prestige.Close(); fields.Close(); settings.Toggle(); });
            shop = ShopPanel.Build(game, safe, onPrestigeTab: () => { shop.Close(); prestige.Open(); });
            prestige = PrestigePanel.Build(game, safe, onShopTab: () => { prestige.Close(); shop.Toggle(); });
            fields = FieldPanel.Build(game, safe);
            DebugPanel.Build(game, safe);

            // 게임 방법은 전체 화면을 덮으므로 캔버스 바로 아래, 타이틀보다 위에 둔다
            GuidePanel guide = null;
            settings = SettingsPanel.Build(game, safe, sfx, onGuide: () => guide.Open());

            // 타이틀: 처음 실행(저장 없음)이면 시작하자마자 게임 방법을 띄운다
            var title = TitleScreen.Build(game, canvas.transform,
                onGuide: () => guide.Open(),
                onStarted: () =>
                {
                    if (!game.LoadedFromSave) guide.Open();
                });
            guide = GuidePanel.Build(game, canvas.transform);

            // PC 단축키: ESC
            var keys = root.AddComponent<Hotkeys>();
            keys.Shop = shop;
            keys.Fields = fields;
            keys.Prestige = prestige;
            keys.Settings = settings;
            keys.Guide = guide;
            keys.Title = title;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
