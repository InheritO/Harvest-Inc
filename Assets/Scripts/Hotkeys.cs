using UnityEngine;
using UnityEngine.InputSystem;

namespace HarvestInc
{
    /// <summary>
    /// PC 단축키: ESC. (스페이스바 수확은 수확 버튼 HoldButton이 직접 처리한다.)
    /// ESC 순서: 게임 방법 닫기 → 열린 창 닫기 → 설정 열고 닫기.
    /// </summary>
    public sealed class Hotkeys : MonoBehaviour
    {
        public ShopPanel Shop;
        public FieldPanel Fields;
        public PrestigePanel Prestige;
        public SettingsPanel Settings;
        public GuidePanel Guide;
        public TitleScreen Title; // 시작하면 사라져서 null이 된다

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            if (Guide.IsOpen)
            {
                Guide.Close();
                return;
            }
            if (Title != null) return; // 타이틀 화면에서는 무시

            bool anyOpen = Shop.gameObject.activeSelf || Fields.gameObject.activeSelf ||
                           Prestige.gameObject.activeSelf;
            if (anyOpen)
            {
                Shop.Close();
                Fields.Close();
                Prestige.Close();
                return;
            }
            Settings.Toggle();
        }
    }
}
