using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestInc
{
    /// <summary>낫질 궤적, 수확 파티클, 코인 팝업, 농기계 경로, 클리어 문구, 화면 흔들림. 풀링한다.</summary>
    public sealed class Fx : MonoBehaviour
    {
        sealed class P
        {
            public RectTransform Rt;
            public Graphic G;
            public Image Img;
            public Text Txt;
            public float T, Life, Fade, Gravity, Spin;
            public Vector2 Vel;
            public Vector3 S0, S1;
            public Color Color;
            public bool Sweep;
        }

        readonly List<P> live = new List<P>();
        readonly Stack<P> imgPool = new Stack<P>();
        readonly Stack<P> txtPool = new Stack<P>();

        RectTransform board;
        Vector2 boardBase;
        float shakeT, shakeAmp;
        const float ShakeTime = 0.3f;

        public void Init(RectTransform board)
        {
            this.board = board;
            boardBase = board.anchoredPosition;
        }

        P Get(bool text)
        {
            var pool = text ? txtPool : imgPool;
            P p;
            if (pool.Count > 0)
            {
                p = pool.Pop();
                p.Rt.gameObject.SetActive(true);
            }
            else
            {
                p = new P();
                if (text)
                {
                    p.Txt = Ui.MakeText("FxText", transform, Vector2.zero, new Vector2(400, 100), "", 40, Color.white,
                        TextAnchor.MiddleCenter, FontStyle.Bold);
                    p.Txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                    Ui.AddOutline(p.Txt, 3f);
                    p.G = p.Txt;
                }
                else
                {
                    p.Img = Ui.MakeImage("FxImage", transform, Vector2.zero, Vector2.one, Color.white);
                    p.G = p.Img;
                }
                p.Rt = p.G.rectTransform;
            }

            if (p.Img != null) p.Img.type = Image.Type.Simple;
            p.Rt.SetAsLastSibling();
            p.Rt.pivot = new Vector2(0.5f, 0.5f);
            p.Rt.localRotation = Quaternion.identity;
            p.Rt.localScale = Vector3.one;
            p.T = 0f;
            p.Life = 0.5f;
            p.Fade = 0.5f;
            p.Gravity = 0f;
            p.Spin = 0f;
            p.Vel = Vector2.zero;
            p.S0 = p.S1 = Vector3.one;
            p.Sweep = false;
            live.Add(p);
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.T += dt;
                float k = p.T / p.Life;
                if (k >= 1f)
                {
                    p.Rt.gameObject.SetActive(false);
                    (p.Txt != null ? txtPool : imgPool).Push(p);
                    int last = live.Count - 1;
                    live[i] = live[last];
                    live.RemoveAt(last);
                    continue;
                }

                p.Vel.y -= p.Gravity * dt;
                p.Rt.anchoredPosition += p.Vel * dt;
                if (p.Spin != 0f) p.Rt.localRotation *= Quaternion.Euler(0, 0, p.Spin * dt);

                if (p.Sweep) p.Rt.localScale = new Vector3(Mathf.Min(1f, k * 4f), Mathf.Lerp(1f, 0.3f, k), 1f);
                else p.Rt.localScale = Vector3.LerpUnclamped(p.S0, p.S1, 1f - (1f - k) * (1f - k));

                var c = p.Color;
                if (k > p.Fade) c.a *= 1f - (k - p.Fade) / (1f - p.Fade);
                p.G.color = c;
            }

            if (board != null)
            {
                if (shakeT > 0f)
                {
                    shakeT -= dt;
                    float a = shakeAmp * Mathf.Clamp01(shakeT / ShakeTime);
                    board.anchoredPosition = boardBase + Random.insideUnitCircle * a;
                }
                else board.anchoredPosition = boardBase;
            }
        }

        // ── 효과 ──

        public void Slash(int index)
        {
            var p = Get(false);
            p.Img.sprite = null;
            p.Rt.sizeDelta = new Vector2(170, 12);
            p.Rt.anchoredPosition = FieldView.CellPos(index) + Random.insideUnitCircle * 10f;
            p.Rt.localRotation = Quaternion.Euler(0, 0, Random.Range(30f, 60f) * (Random.value < 0.5f ? 1f : -1f));
            p.S0 = new Vector3(0.2f, 1.4f, 1f);
            p.S1 = new Vector3(1.15f, 0.3f, 1f);
            p.Life = 0.14f;
            p.Fade = 0.35f;
            p.Color = new Color(1f, 1f, 0.92f, 0.95f);
        }

        public void Burst(int index, CropKind kind)
        {
            var origin = FieldView.CellPos(index);
            var head = FieldView.HeadColor(kind);
            for (int i = 0; i < 9; i++)
            {
                var p = Get(false);
                p.Img.sprite = i % 3 == 0 ? null : Ui.Circle;
                float size = Random.Range(10f, 18f);
                p.Rt.sizeDelta = i % 3 == 0 ? new Vector2(size * 0.6f, size * 1.6f) : new Vector2(size, size);
                p.Rt.anchoredPosition = origin + Random.insideUnitCircle * 20f;
                float ang = Random.Range(-65f, 65f) * Mathf.Deg2Rad;
                float speed = Random.Range(320f, 720f);
                p.Vel = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * speed;
                p.Gravity = 1900f;
                p.Spin = Random.Range(-500f, 500f);
                p.S0 = Vector3.one;
                p.S1 = new Vector3(0.4f, 0.4f, 1f);
                p.Life = Random.Range(0.45f, 0.65f);
                p.Fade = 0.55f;
                p.Color = i % 3 == 0 ? FieldView.Leaf : head;
            }
        }

        public void CoinPopup(int index, double amount)
        {
            var p = Get(true);
            p.Txt.text = "+" + Num.Gain(amount);
            p.Txt.fontSize = 40;
            p.Rt.sizeDelta = new Vector2(300, 60);
            p.Rt.anchoredPosition = FieldView.CellPos(index) + new Vector2(0, 30);
            p.Vel = new Vector2(Random.Range(-20f, 20f), 160f);
            p.S0 = new Vector3(1.35f, 1.35f, 1f);
            p.S1 = Vector3.one;
            p.Life = 0.8f;
            p.Fade = 0.45f;
            p.Color = Palette.Gold;
        }

        public void MachineSweep(int fromIndex, int toIndex, MachineDef def)
        {
            var a = FieldView.CellPos(fromIndex);
            var b = FieldView.CellPos(toIndex);
            var dir = (b - a).normalized;
            var p = Get(false);
            p.Img.sprite = Ui.Rounded;
            p.Img.type = Image.Type.Sliced;
            p.Rt.pivot = new Vector2(0f, 0.5f);
            p.Rt.sizeDelta = new Vector2((b - a).magnitude + FieldView.CellSize * 0.9f, def.FxThickness);
            p.Rt.anchoredPosition = a - dir * FieldView.CellSize * 0.45f;
            p.Rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            p.Sweep = true;
            p.Life = def.Pattern == MachinePattern.FullLine ? 0.35f : 0.22f;
            p.Fade = 0.3f;
            p.Color = def.FxColor;
        }

        public void ClearBanner(double bonus, string unlock)
        {
            var p = Get(true);
            p.Txt.text = $"밭 클리어!\n보너스 +{Num.Gain(bonus)}" + (unlock != null ? "\n" + unlock : "");
            p.Txt.fontSize = 72;
            p.Rt.sizeDelta = new Vector2(900, 320);
            p.Rt.anchoredPosition = new Vector2(0, 40);
            p.Vel = new Vector2(0, 30f);
            p.S0 = new Vector3(0.6f, 0.6f, 1f);
            p.S1 = new Vector3(1.05f, 1.05f, 1f);
            p.Life = 1.4f;
            p.Fade = 0.6f;
            p.Color = Color.white;
        }

        public void Toast(string message)
        {
            var p = Get(true);
            p.Txt.text = message;
            p.Txt.fontSize = 44;
            p.Rt.sizeDelta = new Vector2(900, 80);
            p.Rt.anchoredPosition = new Vector2(0, -520);
            p.Vel = new Vector2(0, 40f);
            p.Life = 1.4f;
            p.Fade = 0.6f;
            p.Color = Color.white;
        }

        public void Shake(float amplitude = 16f)
        {
            shakeT = ShakeTime;
            shakeAmp = amplitude;
        }
    }
}
