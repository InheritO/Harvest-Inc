using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>6×8 밭 화면. 작물 모양은 도형 조합(프로토타입용 임시 그림).</summary>
    public sealed class FieldView : MonoBehaviour
    {
        public const float CellSize = 150f;

        CellView[] cells;
        Image targetFrame;
        Game game;
        float pulse;

        public static Vector2 CellPos(int index)
        {
            int x = index % Balance.Cols, y = index / Balance.Cols;
            return new Vector2((x - (Balance.Cols - 1) / 2f) * CellSize, ((Balance.Rows - 1) / 2f - y) * CellSize);
        }

        public static Color HeadColor(CropKind kind) => kind switch
        {
            CropKind.Wheat => new Color(0.96f, 0.80f, 0.32f),
            CropKind.Rice => new Color(0.93f, 0.90f, 0.62f),
            CropKind.Corn => new Color(1f, 0.84f, 0.25f),
            CropKind.Carrot => new Color(1f, 0.55f, 0.15f),
            _ => new Color(0.95f, 0.5f, 0.1f),
        };

        public static readonly Color Leaf = new Color(0.42f, 0.68f, 0.26f);

        public void Build(Game game)
        {
            this.game = game;
            cells = new CellView[Balance.CellCount];
            for (int i = 0; i < cells.Length; i++) cells[i] = new CellView(transform, i, game);
            targetFrame = Ui.MakeImage("TargetFrame", transform, Vector2.zero, new Vector2(CellSize, CellSize),
                Palette.Target, Ui.Hollow);
            targetFrame.enabled = false;
        }

        public void OnFieldSpawned()
        {
            for (int i = 0; i < cells.Length; i++) cells[i].Spawn(game.Crops[i], Random.Range(0f, 0.18f));
        }

        /// <summary>밭 이동: 작물을 모두 걷어내고 빈 흙만 남긴다.</summary>
        public void HideAll()
        {
            foreach (var c in cells) c.Hide();
        }

        public void OnHit(Crop c) => cells[c.Index].Hit();
        public void OnKilled(Crop c) => cells[c.Index].Kill();

        void LateUpdate()
        {
            if (game == null || cells == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < cells.Length; i++) cells[i].Tick(dt, game.Crops[i]);

            var t = game.Target;
            bool show = t != null && t.Alive && !game.Regenerating;
            targetFrame.enabled = show;
            if (show)
            {
                pulse += dt * 6f;
                var rt = targetFrame.rectTransform;
                rt.anchoredPosition = CellPos(t.Index);
                float s = 1f + 0.035f * Mathf.Sin(pulse);
                rt.localScale = new Vector3(s, s, 1f);
            }
        }

        // ─────────────────────────────────────────────

        struct PartDef
        {
            public bool Round;
            public Vector2 Pos, Size;
            public float Rot;
            public Color Color;

            public PartDef(bool round, float x, float y, float w, float h, float rot, Color color)
            {
                Round = round; Pos = new Vector2(x, y); Size = new Vector2(w, h); Rot = rot; Color = color;
            }
        }

        static PartDef[] PlantShape(CropKind kind)
        {
            switch (kind)
            {
                case CropKind.Wheat:
                {
                    var stem = new Color(0.78f, 0.66f, 0.32f);
                    var head = HeadColor(kind);
                    return new[]
                    {
                        new PartDef(false, -13, 32, 6, 64, 12, stem),
                        new PartDef(false, 13, 32, 6, 64, -12, stem),
                        new PartDef(false, 0, 36, 7, 72, 0, stem),
                        new PartDef(true, -21, 70, 16, 34, 12, head),
                        new PartDef(true, 21, 70, 16, 34, -12, head),
                        new PartDef(true, 0, 80, 18, 38, 0, head),
                    };
                }
                case CropKind.Rice:
                {
                    var stem = new Color(0.42f, 0.66f, 0.26f);
                    var head = HeadColor(kind);
                    return new[]
                    {
                        new PartDef(false, -12, 30, 6, 60, 10, stem),
                        new PartDef(false, 12, 30, 6, 60, -10, stem),
                        new PartDef(false, 0, 33, 6, 66, 0, stem),
                        new PartDef(true, -22, 58, 13, 32, 45, head),
                        new PartDef(true, 24, 56, 13, 32, -50, head),
                        new PartDef(true, 9, 68, 14, 34, -55, head),
                    };
                }
                case CropKind.Carrot:
                {
                    var root = HeadColor(kind);
                    return new[]
                    {
                        new PartDef(true, -14, 48, 12, 52, 22, Leaf),
                        new PartDef(true, 14, 48, 12, 52, -22, Leaf),
                        new PartDef(true, 0, 54, 13, 60, 0, new Color(0.36f, 0.62f, 0.22f)),
                        new PartDef(true, 0, 16, 40, 34, 0, root),
                        new PartDef(false, -6, 12, 14, 3, 8, new Color(0.85f, 0.42f, 0.1f)),
                    };
                }
                case CropKind.Pumpkin:
                {
                    var body = HeadColor(kind);
                    var rib = new Color(0.85f, 0.42f, 0.08f);
                    return new[]
                    {
                        new PartDef(true, -36, 18, 30, 18, 20, Leaf),
                        new PartDef(true, 0, 32, 92, 64, 0, body),
                        new PartDef(true, -20, 32, 26, 60, 0, rib),
                        new PartDef(true, 20, 32, 26, 60, 0, rib),
                        new PartDef(true, 0, 32, 30, 64, 0, body),
                        new PartDef(false, 2, 70, 10, 18, -12, new Color(0.45f, 0.38f, 0.15f)),
                    };
                }
                default:
                {
                    var stalk = new Color(0.36f, 0.62f, 0.22f);
                    var cob = HeadColor(kind);
                    return new[]
                    {
                        new PartDef(false, 0, 46, 16, 92, 0, stalk),
                        new PartDef(true, -22, 38, 14, 56, 45, Leaf),
                        new PartDef(true, 22, 52, 14, 56, -45, Leaf),
                        new PartDef(true, 13, 64, 22, 46, -15, cob),
                        new PartDef(true, 0, 96, 10, 20, 0, new Color(0.92f, 0.82f, 0.55f)),
                    };
                }
            }
        }

        sealed class CellView
        {
            const float PlantHeight = 120f;

            readonly RectTransform plant;
            readonly Image stub;
            readonly GameObject hpBar;
            readonly RectTransform hpFill;
            readonly List<Image> parts = new List<Image>();
            readonly List<Color> baseColors = new List<Color>();

            bool hasKind;
            CropKind kind;
            float punch, tilt, tiltSign = 1f, spawnDelay, spawnT;
            bool darkened;

            public CellView(Transform parent, int index, Game game)
            {
                var root = Ui.MakeNode("Cell" + index, parent, CellPos(index), new Vector2(CellSize, CellSize));
                int x = index % Balance.Cols, y = index / Balance.Cols;
                var soil = Ui.MakeImage("Soil", root, Vector2.zero, new Vector2(140, 140),
                    (x + y) % 2 == 0 ? Palette.SoilA : Palette.SoilB, Ui.Rounded, raycast: true);
                soil.gameObject.AddComponent<CellInput>().Set(game, index);

                stub = Ui.MakeImage("Stub", root, new Vector2(0, -46), new Vector2(46, 10), Palette.Stub, Ui.Rounded);
                stub.enabled = false;

                plant = Ui.MakeNode("Plant", root, Vector2.zero, new Vector2(100, PlantHeight));
                plant.pivot = new Vector2(0.5f, 0f);
                plant.anchoredPosition = new Vector2(0, -52);

                var bar = Ui.MakeImage("HpBar", root, new Vector2(0, 58), new Vector2(112, 16), Palette.HpBack, Ui.Rounded);
                hpBar = bar.gameObject;
                var fill = Ui.MakeImage("Fill", bar.transform, Vector2.zero, new Vector2(108, 12), Palette.HpFill, Ui.Rounded);
                hpFill = fill.rectTransform;
                hpFill.anchorMin = hpFill.anchorMax = new Vector2(0f, 0.5f);
                hpFill.pivot = new Vector2(0f, 0.5f);
                hpFill.anchoredPosition = new Vector2(2, 0);
                hpBar.SetActive(false);
            }

            void BuildPlant(CropKind k)
            {
                foreach (var p in parts) Object.Destroy(p.gameObject);
                parts.Clear();
                baseColors.Clear();
                foreach (var d in PlantShape(k))
                {
                    // 부모 피벗이 바닥 가운데이므로 y를 사각형 중심 기준으로 옮긴다
                    var img = Ui.MakeImage("Part", plant, new Vector2(d.Pos.x, d.Pos.y - PlantHeight / 2f), d.Size,
                        d.Color, d.Round ? Ui.Circle : null);
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, d.Rot);
                    parts.Add(img);
                    baseColors.Add(d.Color);
                }
                kind = k;
                hasKind = true;
            }

            public void Spawn(Crop c, float delay)
            {
                if (!hasKind || kind != c.Kind) BuildPlant(c.Kind);
                plant.gameObject.SetActive(true);
                stub.enabled = false;
                hpBar.SetActive(false);
                spawnDelay = delay;
                spawnT = 0f;
                tilt = 0f;
                punch = 0f;
                tiltSign = Random.value < 0.5f ? -1f : 1f;
                SetDark(false);
                plant.localScale = new Vector3(0f, 0f, 1f);
            }

            public void Hit() => punch = 1f;

            public void Kill()
            {
                plant.gameObject.SetActive(false);
                hpBar.SetActive(false);
                stub.enabled = true;
            }

            public void Hide()
            {
                plant.gameObject.SetActive(false);
                hpBar.SetActive(false);
                stub.enabled = false;
            }

            public void Tick(float dt, Crop c)
            {
                if (!c.Alive) return;

                float grow;
                if (spawnDelay > 0f)
                {
                    spawnDelay -= dt;
                    grow = 0f;
                }
                else
                {
                    spawnT += dt;
                    grow = EaseOutBack(Mathf.Clamp01(spawnT / 0.25f));
                }

                punch = Mathf.Max(0f, punch - dt * 7f);
                float r = c.HpRatio;
                float targetTilt = r > 0.66f ? 0f : r > 0.33f ? 11f : 26f;
                tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-dt * 14f));
                float wobble = Mathf.Sin(punch * 20f) * 9f * punch;
                plant.localRotation = Quaternion.Euler(0, 0, tilt * tiltSign + wobble);
                plant.localScale = new Vector3(grow * (1f + 0.12f * punch), grow * (1f - 0.16f * punch), 1f);

                SetDark(r <= 0.33f);

                bool showHp = c.Hp < c.MaxHp - 1e-9;
                if (hpBar.activeSelf != showHp) hpBar.SetActive(showHp);
                if (showHp) hpFill.sizeDelta = new Vector2(108f * r, 12f);
            }

            void SetDark(bool dark)
            {
                if (dark == darkened) return;
                darkened = dark;
                for (int i = 0; i < parts.Count; i++)
                {
                    var b = baseColors[i];
                    parts[i].color = dark ? new Color(b.r * 0.7f, b.g * 0.7f, b.b * 0.6f, b.a) : b;
                }
            }

            static float EaseOutBack(float x)
            {
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float k = x - 1f;
                return 1f + c3 * k * k * k + c1 * k * k;
            }
        }
    }

    public sealed class CellInput : MonoBehaviour, IPointerDownHandler
    {
        Game game;
        int index;

        public void Set(Game g, int i)
        {
            game = g;
            index = i;
        }

        public void OnPointerDown(PointerEventData e) => game.TapCrop(index);
    }
}
