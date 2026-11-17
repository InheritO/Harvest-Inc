using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>
    /// 보유 농기계의 쿨타임 게이지. 기체마다 원형 게이지 1개(종류별 최대 10개, 5개씩 2줄).
    /// 시계 방향으로 차오르고, 다 차서 작동하는 순간 톡 튀어 오른다.
    /// </summary>
    public sealed class MachineGauges : MonoBehaviour
    {
        const int PerRow = 5;
        const float Slot = 52f, Spacing = 62f;

        sealed class Group
        {
            public MachineState State;
            public Text Label;
            public RectTransform[] Roots;
            public Image[] Fills;
            public float[] LastProgress;
            public float[] Punch;
        }

        Game game;
        Group[] groups;

        /// <param name="topLeft">블록 왼쪽 위 (Safe 기준 좌표)</param>
        public static MachineGauges Build(Game game, RectTransform parent, Vector2 topLeft)
        {
            var root = Ui.MakeNode("MachineGauges", parent, Vector2.zero, Vector2.zero);
            var g = root.gameObject.AddComponent<MachineGauges>();
            g.game = game;
            g.groups = new Group[game.Machines.Length];

            float y = topLeft.y;
            float width = Spacing * PerRow;
            for (int m = 0; m < game.Machines.Length; m++)
            {
                var state = game.Machines[m];
                var group = new Group
                {
                    State = state,
                    Roots = new RectTransform[Balance.MaxMachinesPerType],
                    Fills = new Image[Balance.MaxMachinesPerType],
                    LastProgress = new float[Balance.MaxMachinesPerType],
                    Punch = new float[Balance.MaxMachinesPerType],
                };

                group.Label = Ui.MakeText("Label", root, new Vector2(topLeft.x + width / 2f, y - 18f),
                    new Vector2(width, 36f), "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.AddOutline(group.Label, 1.5f);
                y -= 36f + 4f;

                for (int i = 0; i < Balance.MaxMachinesPerType; i++)
                {
                    int col = i % PerRow, row = i / PerRow;
                    var pos = new Vector2(topLeft.x + Spacing / 2f + col * Spacing, y - Spacing / 2f - row * Spacing);

                    var bg = Ui.MakeImage("Gauge" + i, root, pos, new Vector2(Slot, Slot),
                        new Color(0f, 0f, 0f, 0.5f), Ui.Circle);
                    var fill = Ui.MakeImage("Fill", bg.transform, Vector2.zero, new Vector2(Slot - 8f, Slot - 8f),
                        state.Def.FxColor, Ui.Circle);
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Radial360;
                    fill.fillOrigin = (int)Image.Origin360.Top;
                    fill.fillClockwise = true;

                    group.Roots[i] = bg.rectTransform;
                    group.Fills[i] = fill;
                    bg.gameObject.SetActive(false);
                }

                y -= Spacing * 2f + 14f;
                g.groups[m] = group;
            }
            return g;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var group in groups)
            {
                var s = group.State;
                float cd = s.Cooldown;
                group.Label.text = s.Count == 0
                    ? $"{s.Def.Name}  미보유"
                    : $"{s.Def.Name}  {Num.Sec(cd)}초";
                group.Label.color = s.Count == 0 ? Palette.TextDim : Color.white;

                for (int i = 0; i < group.Roots.Length; i++)
                {
                    bool owned = i < s.Timers.Count;
                    var rt = group.Roots[i];
                    if (rt.gameObject.activeSelf != owned) rt.gameObject.SetActive(owned);
                    if (!owned) continue;

                    float progress = Mathf.Clamp01(1f - s.Timers[i] / cd);
                    // 진행도가 크게 줄었다 = 방금 작동하고 쿨타임이 다시 시작됨
                    if (progress < group.LastProgress[i] - 0.5f) group.Punch[i] = 1f;
                    group.LastProgress[i] = progress;

                    var fill = group.Fills[i];
                    fill.fillAmount = progress;
                    // 정지(밭 재생성) 중에는 살짝 어둡게
                    var c = s.Def.FxColor;
                    fill.color = game.Regenerating ? new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, c.a) : c;

                    group.Punch[i] = Mathf.Max(0f, group.Punch[i] - dt * 6f);
                    float scale = 1f + 0.3f * group.Punch[i];
                    rt.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }
    }
}
