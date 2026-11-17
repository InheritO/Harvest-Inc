using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HarvestInc
{
    /// <summary>게임 상태와 규칙. 화면은 FieldView/Hud/ShopPanel, 연출은 Fx/Sfx가 맡는다.</summary>
    public sealed class Game : MonoBehaviour
    {
        public double Coins;
        public double TotalEarned;
        public int Level = 1;
        public int SickleLevel;

        public readonly Crop[] Crops = new Crop[Balance.CellCount];
        public int AliveCount { get; private set; }
        public Crop Target { get; private set; }
        public bool Regenerating { get; private set; }
        public MachineState[] Machines { get; private set; }

        public double SickleDamage => Balance.SickleBaseDamage * Math.Pow(Balance.SickleUpgradeMult, SickleLevel);
        public double NextSickleDamage => Balance.SickleBaseDamage * Math.Pow(Balance.SickleUpgradeMult, SickleLevel + 1);
        public double SickleUpPrice =>
            Math.Ceiling(Balance.SickleUpgradeBasePrice * Math.Pow(Balance.SickleUpgradePriceGrowth, SickleLevel));

        FieldView field;
        Fx fx;
        Sfx sfx;
        float regenTimer;
        double fieldEarned;
        readonly List<int> path = new List<int>(8);
        readonly List<int> scratch = new List<int>(8);

        public void Init(FieldView field, Fx fx, Sfx sfx)
        {
            this.field = field;
            this.fx = fx;
            this.sfx = sfx;
            Machines = new[] { new MachineState(Balance.Mower), new MachineState(Balance.Combine) };
            for (int i = 0; i < Crops.Length; i++) Crops[i] = new Crop(i);
            SpawnField();
        }

        void Update()
        {
            if (Machines == null) return;
            float dt = Time.deltaTime;

            if (Regenerating)
            {
                regenTimer -= dt;
                if (regenTimer <= 0f) SpawnField();
            }
            else
            {
                UpdateMachines(dt);
            }

            DebugKeys();
        }

        // ── 밭 ──

        void SpawnField()
        {
            var type = Balance.CropTypeFor(Level);
            double hp = Balance.CropHp(Level);
            double reward = Balance.CropReward(Level);
            foreach (var c in Crops)
            {
                c.Kind = type.Kind;
                c.MaxHp = c.Hp = hp;
                c.Reward = reward;
                c.Alive = true;
            }
            AliveCount = Crops.Length;
            fieldEarned = 0;
            Regenerating = false;
            Target = RandomAlive();
            field.OnFieldSpawned();
        }

        void Damage(Crop c, double dmg)
        {
            if (!c.Alive) return; // 제거된 작물에는 피해 없음
            c.Hp -= dmg;
            if (c.Hp <= 1e-6)
            {
                c.Hp = 0; // 초과 피해는 사라진다
                Kill(c);
            }
            else
            {
                field.OnHit(c);
            }
        }

        void Kill(Crop c)
        {
            c.Alive = false;
            AliveCount--;
            Earn(c.Reward);
            fieldEarned += c.Reward;

            field.OnKilled(c);
            fx.Burst(c.Index, c.Kind);
            fx.CoinPopup(c.Index, c.Reward);
            sfx.Harvest();
            sfx.Coin();

            if (AliveCount == 0)
            {
                Target = null;
                ClearField();
            }
            else if (Target == c)
            {
                Target = RandomAlive();
            }
        }

        void ClearField()
        {
            double bonus = fieldEarned * Balance.ClearBonusRatio;
            Earn(bonus);

            var before = Balance.CropTypeFor(Level);
            Level++;
            var after = Balance.CropTypeFor(Level);
            string unlock = before != after ? $"새 작물: {after.Name}!" : null;

            fx.ClearBanner(bonus, unlock);
            fx.Shake();
            sfx.Clear();

            Regenerating = true;
            regenTimer = Balance.RegenDelay;
        }

        void Earn(double v)
        {
            Coins += v;
            TotalEarned += v;
        }

        Crop RandomAlive()
        {
            if (AliveCount <= 0) return null;
            int n = Random.Range(0, AliveCount);
            foreach (var c in Crops)
                if (c.Alive && n-- == 0) return c;
            return null;
        }

        // ── 직접 수확 ──

        public void Swing()
        {
            if (Regenerating || AliveCount == 0) return;
            if (Target == null || !Target.Alive) Target = RandomAlive();
            var t = Target;
            fx.Slash(t.Index);
            sfx.Slash();
            Damage(t, SickleDamage);
        }

        public void TapCrop(int index)
        {
            var c = Crops[index];
            if (Regenerating || !c.Alive) return;
            Target = c;
            Swing();
        }

        // ── 농기계 ──

        void UpdateMachines(float dt)
        {
            foreach (var m in Machines)
            {
                float cd = m.Cooldown;
                for (int i = 0; i < m.Timers.Count; i++)
                {
                    if (Regenerating) return; // 재생성 중에는 쿨타임 정지
                    float t = m.Timers[i] - dt;
                    if (t <= 0f)
                    {
                        FireMachine(m);
                        t += cd;
                        if (t <= 0f) t = cd;
                    }
                    m.Timers[i] = t;
                }
            }
        }

        void FireMachine(MachineState m)
        {
            if (AliveCount == 0) return;
            if (m.Precision) BestPath(m.Def.Pattern, path);
            else RandomPath(m.Def.Pattern, path);

            fx.MachineSweep(path[0], path[path.Count - 1], m.Def);
            sfx.Machine(m.Def);

            double dmg = m.Damage;
            foreach (int idx in path) Damage(Crops[idx], dmg);
        }

        void RandomPath(MachinePattern p, List<int> outPath)
        {
            var b = RandomAlive();
            BuildPath(p, Random.value < 0.5f, b.X, b.Y, outPath);
        }

        static void BuildPath(MachinePattern p, bool horizontal, int x, int y, List<int> outPath)
        {
            outPath.Clear();
            if (p == MachinePattern.FullLine)
            {
                if (horizontal) for (int i = 0; i < Balance.Cols; i++) outPath.Add(y * Balance.Cols + i);
                else for (int j = 0; j < Balance.Rows; j++) outPath.Add(j * Balance.Cols + x);
            }
            else
            {
                // 기준 칸 가운데 3칸, 가장자리면 안쪽으로 민다
                if (horizontal)
                {
                    int s = Mathf.Clamp(x - 1, 0, Balance.Cols - 3);
                    for (int i = s; i < s + 3; i++) outPath.Add(y * Balance.Cols + i);
                }
                else
                {
                    int s = Mathf.Clamp(y - 1, 0, Balance.Rows - 3);
                    for (int j = s; j < s + 3; j++) outPath.Add(j * Balance.Cols + x);
                }
            }
        }

        int CountAlive(List<int> cells)
        {
            int n = 0;
            foreach (int i in cells) if (Crops[i].Alive) n++;
            return n;
        }

        /// <summary>정밀 조준: 살아 있는 작물이 가장 많은 경로. 동점은 무작위.</summary>
        void BestPath(MachinePattern p, List<int> outPath)
        {
            bool full = p == MachinePattern.FullLine;
            int hCount = full ? 1 : Balance.Cols - 2;
            int vCount = full ? 1 : Balance.Rows - 2;
            int offset = full ? 0 : 1;

            int best = -1, ties = 0, bx = 0, by = 0;
            bool bh = true;
            for (int y = 0; y < Balance.Rows; y++)
                for (int s = 0; s < hCount; s++)
                    Consider(p, true, s + offset, y, ref best, ref ties, ref bh, ref bx, ref by);
            for (int x = 0; x < Balance.Cols; x++)
                for (int s = 0; s < vCount; s++)
                    Consider(p, false, x, s + offset, ref best, ref ties, ref bh, ref bx, ref by);

            BuildPath(p, bh, bx, by, outPath);
        }

        void Consider(MachinePattern p, bool h, int x, int y,
            ref int best, ref int ties, ref bool bh, ref int bx, ref int by)
        {
            BuildPath(p, h, x, y, scratch);
            int n = CountAlive(scratch);
            if (n > best)
            {
                best = n; ties = 1; bh = h; bx = x; by = y;
            }
            else if (n == best && Random.Range(0, ++ties) == 0)
            {
                bh = h; bx = x; by = y;
            }
        }

        // ── 구매 ──

        bool TrySpend(double price)
        {
            if (Coins + 1e-9 < price)
            {
                sfx.Deny();
                return false;
            }
            Coins = Math.Max(0, Coins - price);
            sfx.Buy();
            return true;
        }

        public void BuySickleUp()
        {
            if (TrySpend(SickleUpPrice)) SickleLevel++;
        }

        public void BuyMachine(MachineState m)
        {
            if (!m.CanBuyMore || !TrySpend(m.BuyPrice)) return;
            int k = m.Count;
            m.Count++;
            float frac = k * 0.618034f % 1f; // 쿨타임을 엇갈리게
            m.Timers.Add(m.Cooldown * (1f - frac));
        }

        public void BuyDamageUp(MachineState m)
        {
            if (m.Count == 0 || !TrySpend(m.DamageUpPrice)) return;
            m.DamageLevel++;
        }

        public void BuyCooldownUp(MachineState m)
        {
            if (m.Count == 0 || m.CooldownMaxed) return;
            float old = m.Cooldown;
            if (!TrySpend(m.CooldownUpPrice)) return;
            m.CooldownLevel++;
            float ratio = m.Cooldown / old;
            for (int i = 0; i < m.Timers.Count; i++) m.Timers[i] *= ratio;
        }

        public void BuyPrecision(MachineState m)
        {
            if (m.Count == 0 || m.Precision || !TrySpend(m.PrecisionPrice)) return;
            m.Precision = true;
        }

        // ── 개발용 ──

        void DebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) Swing();
            if (kb.f1Key.wasPressedThisFrame)
            {
                double amount = Math.Max(1000, Balance.CropReward(Level) * Balance.CellCount * 10);
                Earn(amount);
                fx.Toast($"디버그: +{Num.Gain(amount)} 코인");
            }
            if (kb.f2Key.wasPressedThisFrame && !Regenerating)
            {
                foreach (var c in Crops)
                    if (c.Alive) Kill(c);
            }
#endif
        }
    }
}
