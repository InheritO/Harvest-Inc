using System;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    public static class Palette
    {
        public static readonly Color Grass = new Color(0.38f, 0.58f, 0.27f);
        public static readonly Color Board = new Color(0.30f, 0.20f, 0.12f);
        public static readonly Color SoilA = new Color(0.56f, 0.39f, 0.23f);
        public static readonly Color SoilB = new Color(0.52f, 0.36f, 0.21f);
        public static readonly Color Stub = new Color(0.72f, 0.62f, 0.35f);
        public static readonly Color Bar = new Color(0.17f, 0.12f, 0.08f, 0.88f);
        public static readonly Color Panel = new Color(0.13f, 0.10f, 0.08f, 0.96f);
        public static readonly Color Row = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color Gold = new Color(1f, 0.84f, 0.25f);
        public static readonly Color Progress = new Color(0.55f, 0.85f, 0.3f);
        public static readonly Color HpBack = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color HpFill = new Color(1f, 0.42f, 0.2f);
        public static readonly Color Target = new Color(1f, 0.96f, 0.45f, 0.95f);
        public static readonly Color Harvest = new Color(0.98f, 0.76f, 0.2f);
        public static readonly Color Shop = new Color(0.29f, 0.58f, 0.86f);
        public static readonly Color Affordable = new Color(0.36f, 0.72f, 0.3f);
        public static readonly Color Unaffordable = new Color(0.32f, 0.3f, 0.28f);
        public static readonly Color TextDim = new Color(1f, 1f, 1f, 0.6f);
        public static readonly Color Brown = new Color(0.28f, 0.16f, 0.06f);
    }

    /// <summary>uGUI 요소를 코드로 만드는 도우미. 기준 해상도 1080×1920, 가운데 원점.</summary>
    public static class Ui
    {
        public const float RefWidth = 1080f;
        public const float RefHeight = 1920f;

        public static Font Font;
        public static Sprite Rounded, Hollow, Circle;

        /// <summary>화면 위에서부터의 거리 → 가운데 원점 y.</summary>
        public static float Top(float y) => RefHeight / 2f - y;

        public static void Init()
        {
            if (Font != null) return;
            // 프로토타입: OS 한글 폰트. 정식 버전은 Noto Sans KR 에셋으로 교체.
            Font = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Noto Sans KR",
                "Apple SD Gothic Neo", "AppleSDGothicNeo-Regular", "Droid Sans Fallback", "Arial",
            }, 40);
            Rounded = MakeRoundedSprite(64, 20f, 0f, 24);
            Hollow = MakeRoundedSprite(64, 18f, 7f, 24);
            Circle = MakeRoundedSprite(128, 64f, 0f, 0);
        }

        public static RectTransform MakeNode(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image MakeImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color,
            Sprite sprite = null, bool raycast = false)
        {
            var img = MakeNode(name, parent, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Text MakeText(string name, Transform parent, Vector2 pos, Vector2 size, string text,
            int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = MakeNode(name, parent, pos, size).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            return t;
        }

        public static Button MakeButton(string name, Transform parent, Vector2 pos, Vector2 size, string label,
            int fontSize, Color color, Action onClick, out Text labelText)
        {
            var img = MakeImage(name, parent, pos, size, color, Rounded, raycast: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            labelText = MakeText("Label", img.transform, Vector2.zero, size, label, fontSize, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            return btn;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void AddOutline(Graphic g, float distance = 2f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.65f);
            o.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>둥근 사각형(또는 원) 스프라이트. border &gt; 0이면 테두리만.</summary>
        static Sprite MakeRoundedSprite(int size, float radius, float border, int slice)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[size * size];
            float h = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius);
                float qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                if (border > 0f) a *= Mathf.Clamp01(d + border + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }
    }
}
