using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>상단(코인, 레벨, 진행 바)과 하단(수확 버튼, 상점 버튼).</summary>
    public sealed class Hud : MonoBehaviour
    {
        Game game;
        Text coins, level, progressText, sickleText, machinesText;
        RectTransform progressFill;
        const float ProgressWidth = 972f;

        public static Hud Build(Game game, RectTransform safe, Action onShop)
        {
            var hud = safe.gameObject.AddComponent<Hud>();
            hud.game = game;

            // 상단
            Ui.MakeImage("TopBar", safe, new Vector2(0, Ui.Top(110)), new Vector2(1040, 200), Palette.Bar, Ui.Rounded);
            Ui.MakeImage("CoinIcon", safe, new Vector2(-450, Ui.Top(62)), new Vector2(64, 64), Palette.Gold, Ui.Circle);
            hud.coins = Ui.MakeText("Coins", safe, new Vector2(-150, Ui.Top(62)), new Vector2(500, 80), "0", 60,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            hud.level = Ui.MakeText("Level", safe, new Vector2(290, Ui.Top(62)), new Vector2(420, 70), "", 42,
                Color.white, TextAnchor.MiddleRight);

            var barBg = Ui.MakeImage("Progress", safe, new Vector2(0, Ui.Top(150)), new Vector2(980, 44),
                new Color(0f, 0f, 0f, 0.45f), Ui.Rounded);
            var fill = Ui.MakeImage("Fill", barBg.transform, Vector2.zero, new Vector2(ProgressWidth, 36),
                Palette.Progress, Ui.Rounded);
            hud.progressFill = fill.rectTransform;
            hud.progressFill.anchorMin = hud.progressFill.anchorMax = new Vector2(0f, 0.5f);
            hud.progressFill.pivot = new Vector2(0f, 0.5f);
            hud.progressFill.anchoredPosition = new Vector2(4, 0);
            hud.progressText = Ui.MakeText("ProgressText", barBg.transform, Vector2.zero, new Vector2(980, 44), "", 30,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(hud.progressText, 1.5f);

            // 하단: 수확 버튼
            var harvest = Ui.MakeImage("HarvestButton", safe, new Vector2(0, Ui.Top(1690)), new Vector2(330, 330),
                Palette.Harvest, Ui.Circle, raycast: true);
            harvest.gameObject.AddComponent<HoldButton>().OnFire = game.Swing;
            Ui.MakeText("Label", harvest.transform, new Vector2(0, 22), new Vector2(330, 100), "수확", 76,
                Palette.Brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            hud.sickleText = Ui.MakeText("Sickle", harvest.transform, new Vector2(0, -58), new Vector2(330, 50), "", 30,
                Palette.Brown);

            // 하단: 상점 버튼
            Ui.MakeButton("ShopButton", safe, new Vector2(380, Ui.Top(1690)), new Vector2(240, 150), "상점", 52,
                Palette.Shop, onShop, out _);

            // 하단: 보유 농기계
            hud.machinesText = Ui.MakeText("Machines", safe, new Vector2(-380, Ui.Top(1690)), new Vector2(260, 200),
                "", 32, Color.white);
            Ui.AddOutline(hud.machinesText, 1.5f);

            return hud;
        }

        void Update()
        {
            coins.text = Num.Coins(game.Coins);
            level.text = $"밭 레벨 {game.Level} · {Balance.CropTypeFor(game.Level).Name}";

            int total = Balance.CellCount;
            int alive = game.Regenerating ? 0 : game.AliveCount;
            float done = (total - alive) / (float)total;
            progressFill.sizeDelta = new Vector2(ProgressWidth * done, 36);
            progressFill.gameObject.SetActive(done > 0.001f);
            progressText.text = game.Regenerating ? "밭 클리어!" : $"남은 작물 {alive} / {total}";

            sickleText.text = $"낫 피해 {Num.Stat(game.SickleDamage)}";
            var m = game.Machines;
            machinesText.text = $"{m[0].Def.Name} ×{m[0].Count}\n{m[1].Def.Name} ×{m[1].Count}";
        }
    }

    /// <summary>누르는 순간 1회, 계속 누르고 있으면 일정 간격으로 반복. 손가락마다 따로 센다.</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action OnFire;

        readonly List<int> pointers = new List<int>();
        readonly List<float> heldTimes = new List<float>();
        float repeatTimer;
        float punch;

        public void OnPointerDown(PointerEventData e)
        {
            int i = pointers.IndexOf(e.pointerId);
            if (i < 0)
            {
                pointers.Add(e.pointerId);
                heldTimes.Add(0f);
            }
            else heldTimes[i] = 0f;
            Fire();
        }

        public void OnPointerUp(PointerEventData e)
        {
            int i = pointers.IndexOf(e.pointerId);
            if (i < 0) return;
            pointers.RemoveAt(i);
            heldTimes.RemoveAt(i);
        }

        void OnDisable()
        {
            pointers.Clear();
            heldTimes.Clear();
        }

        void Fire()
        {
            punch = 1f;
            OnFire?.Invoke();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool repeating = false;
            for (int i = 0; i < heldTimes.Count; i++)
            {
                heldTimes[i] += dt;
                if (heldTimes[i] >= Balance.HoldDelay) repeating = true;
            }

            if (repeating)
            {
                repeatTimer -= dt;
                if (repeatTimer <= 0f)
                {
                    Fire();
                    repeatTimer += Balance.HoldInterval;
                    if (repeatTimer <= 0f) repeatTimer = Balance.HoldInterval;
                }
            }
            else repeatTimer = 0f;

            punch = Mathf.Max(0f, punch - dt * 8f);
            float s = (pointers.Count > 0 ? 0.93f : 1f) - 0.05f * punch;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
