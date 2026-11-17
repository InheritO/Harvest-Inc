using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>
    /// 화면 왼쪽 아래 구석의 디버그 버튼. "DBG"를 누르면 펼쳐진다.
    /// 출시 빌드에서는 Enabled를 false로 바꾼다.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        public static readonly bool Enabled = true;

        static readonly Color DebugColor = new Color(0.55f, 0.25f, 0.6f);

        GameObject menu;
        Text toggleLabel;

        public static void Build(Game game, RectTransform safe)
        {
            if (!Enabled) return;

            var root = Ui.MakeNode("Debug", safe, Vector2.zero, Vector2.zero);
            var panel = root.gameObject.AddComponent<DebugPanel>();

            // 펼쳤을 때 보이는 버튼 3개 (농기계 게이지 위를 덮는다)
            var menuBg = Ui.MakeImage("Menu", root, new Vector2(-385, Ui.Top(1660)), new Vector2(320, 330),
                new Color(0f, 0f, 0f, 0.75f), Ui.Rounded, raycast: true);
            panel.menu = menuBg.gameObject;
            var m = menuBg.transform;
            Ui.MakeButton("Coins", m, new Vector2(0, 105), new Vector2(290, 90), "코인 +", 34, DebugColor,
                game.DebugAddCoins, out _);
            Ui.MakeButton("HarvestAll", m, new Vector2(0, 0), new Vector2(290, 90), "작물 일괄 수확", 34, DebugColor,
                game.DebugHarvestAll, out _);
            Ui.MakeButton("LevelUp", m, new Vector2(0, -105), new Vector2(290, 90), "레벨업 (다음 밭)", 32, DebugColor,
                game.DebugLevelUp, out _);
            panel.menu.SetActive(false);

            // 구석의 작은 토글 버튼
            Ui.MakeButton("Toggle", root, new Vector2(-460, Ui.Top(1880)), new Vector2(130, 56), "DBG", 28,
                DebugColor, panel.Toggle, out panel.toggleLabel);
        }

        void Toggle()
        {
            menu.SetActive(!menu.activeSelf);
            toggleLabel.text = menu.activeSelf ? "닫기" : "DBG";
            if (menu.activeSelf) transform.SetAsLastSibling();
        }
    }
}
