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
        Text coins, fame, fieldLabel, progressText, sickleText;
        Image fieldButton;
        float blink;
        RectTransform progressFill;
        const float ProgressWidth = 972f;

        public static Hud Build(Game game, RectTransform safe, Action onShop, Action onField, Action onSettings)
        {
            var hud = safe.gameObject.AddComponent<Hud>();
            hud.game = game;

            // 상단
            Ui.MakeImage("TopBar", safe, new Vector2(0, Ui.Top(110)), new Vector2(1040, 200), Palette.Bar, Ui.Rounded);
            Ui.MakeImage("CoinIcon", safe, new Vector2(-450, Ui.Top(62)), new Vector2(64, 64), Palette.Gold, Ui.Circle);
            hud.coins = Ui.MakeText("Coins", safe, new Vector2(-240, Ui.Top(62)), new Vector2(320, 80), "0", 60,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            hud.fame = Ui.MakeText("Fame", safe, new Vector2(30, Ui.Top(62)), new Vector2(220, 80), "", 50,
                Palette.Fame, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 밭 이름 = 밭 이동 버튼
            var fieldBtn = Ui.MakeButton("FieldButton", safe, new Vector2(340, Ui.Top(62)), new Vector2(320, 84), "",
                40, Palette.Shop, onField, out hud.fieldLabel);
            hud.fieldButton = (Image)fieldBtn.targetGraphic;

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

            // 하단 오른쪽 아래: 설정
            Ui.MakeButton("SettingsButton", safe, new Vector2(380, Ui.Top(1855)), new Vector2(200, 70), "설정", 30,
                Palette.Unaffordable, onSettings, out _);

            // 하단 왼쪽: 보유 농기계 쿨타임 게이지
            MachineGauges.Build(game, safe, new Vector2(-530, Ui.Top(1480)));

            return hud;
        }

        void Update()
        {
            coins.text = Num.Coins(game.Coins);
            fame.text = "★ " + Num.Coins(game.Fame);
            fieldLabel.text = $"{game.Field.Name} 밭  ▼";

            // 다음 밭을 열 수 있으면 버튼이 깜빡인다
            int next = game.NextLockedIndex;
            bool canUnlock = next >= 0 && game.Fame + 1e-9 >= Balance.Fields[next].UnlockCost;
            blink = canUnlock ? blink + Time.deltaTime * 4f : 0f;
            fieldButton.color = canUnlock
                ? Color.Lerp(Palette.Shop, Palette.Affordable, 0.5f + 0.5f * Mathf.Sin(blink))
                : Palette.Shop;

            int total = Balance.CellCount;
            int alive = game.Regenerating ? 0 : game.AliveCount;
            float done = (total - alive) / (float)total;
            progressFill.sizeDelta = new Vector2(ProgressWidth * done, 36);
            progressFill.gameObject.SetActive(done > 0.001f);
            progressText.text = game.Regenerating ? "밭 클리어!" : $"남은 작물 {alive} / {total}";

            sickleText.text = $"낫 피해 {Num.Stat(game.SickleDamage)}";
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

        /// <summary>키보드 키를 손가락 하나처럼 다룬다 (터치 포인터 id와 겹치지 않는 값).</summary>
        const int KeyPointerId = -1000;

        /// <summary>PC: 스페이스바 = 수확 버튼 (모든 빌드에서 동작).</summary>
        void PollKeyboard()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            int i = pointers.IndexOf(KeyPointerId);
            if (kb.spaceKey.wasPressedThisFrame && i < 0)
            {
                pointers.Add(KeyPointerId);
                heldTimes.Add(0f);
                Fire();
            }
            else if (i >= 0 && !kb.spaceKey.isPressed)
            {
                pointers.RemoveAt(i);
                heldTimes.RemoveAt(i);
            }
        }

        void Update()
        {
            PollKeyboard();
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
