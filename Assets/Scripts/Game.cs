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
        public double TotalEarned; // 이번 판에 번 코인 (개척하면 0)
        public double Fame;
        public int SickleLevel;

        // ── 개척해도 유지 ──
        public double GoldenSeeds;
        public int Stage = 1;
        public int BestFieldIndex;
        public bool PrestigeOpened;
        public readonly int[] PermLevels = new int[Balance.Perms.Length];

        int Perm(PermKind k) => PermLevels[(int)k];

        public double RewardMult =>
            (1 + Balance.PermRewardStep * Perm(PermKind.Reward)) * Math.Pow(Balance.StageRewardMult, Stage - 1);
        public double HpMult => Math.Pow(Balance.StageHpMult, Stage - 1);
        public double SickleMult => 1 + Balance.PermSickleStep * Perm(PermKind.Sickle);
        public double ClearBonusRatio => Balance.ClearBonusRatio + Balance.PermClearBonusStep * Perm(PermKind.ClearBonus);
        double MachinePriceMult => Math.Pow(Balance.PermDiscountMult, Perm(PermKind.MachineDiscount));

        public double FieldHp(int i) => Balance.Fields[i].Hp * HpMult;
        public double FieldReward(int i) => Balance.Fields[i].Reward * RewardMult;

        /// <summary>지금 개척하면 받는 황금 씨앗.</summary>
        public double PrestigeSeeds => Math.Floor(Balance.PrestigeSeedFactor * Math.Sqrt(Math.Max(0, TotalEarned)) + 1e-9);

        public int FieldIndex { get; private set; }
        public FieldDef Field => Balance.Fields[FieldIndex];
        public readonly bool[] Unlocked = new bool[Balance.Fields.Length];

        /// <summary>다음에 열 수 있는 밭. 모두 열었으면 -1.</summary>
        public int NextLockedIndex
        {
            get
            {
                for (int i = 0; i < Unlocked.Length; i++)
                    if (!Unlocked[i]) return i;
                return -1;
            }
        }

        public readonly Crop[] Crops = new Crop[Balance.CellCount];
        public int AliveCount { get; private set; }
        public Crop Target { get; private set; }
        public bool Regenerating { get; private set; }
        public MachineState[] Machines { get; private set; }

        public double SickleDamage =>
            Balance.SickleBaseDamage * Math.Pow(Balance.SickleUpgradeMult, SickleLevel) * SickleMult;
        public double NextSickleDamage =>
            Balance.SickleBaseDamage * Math.Pow(Balance.SickleUpgradeMult, SickleLevel + 1) * SickleMult;
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
            Unlocked[0] = true;
            FieldIndex = 0;

            bool loaded = SaveSystem.TryLoad(out var data);
            if (loaded) ApplySaveData(data);
            SpawnField();
            if (loaded) fx.Toast("저장된 게임을 불러왔다");
        }

        void Update()
        {
            if (Machines == null) return;
            float dt = Time.deltaTime;

            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= AutoSaveInterval) Save();

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
            var def = Field;
            double hp = FieldHp(FieldIndex), reward = FieldReward(FieldIndex);
            foreach (var c in Crops)
            {
                c.Kind = def.Kind;
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
            double bonus = fieldEarned * ClearBonusRatio;
            Earn(bonus);
            double fame = Field.Fame;
            Fame += fame;

            fx.ClearBanner(bonus, fame);
            fx.Shake();
            sfx.Clear();

            StartRegen();
        }

        void StartRegen()
        {
            Regenerating = true;
            regenTimer = Balance.RegenDelay;
        }

        // ── 밭 이동 ──

        /// <summary>다음 밭을 명성으로 열고 바로 이동한다. 순서대로만 열 수 있다.</summary>
        public void UnlockField(int index)
        {
            if (index != NextLockedIndex) return;
            double cost = Balance.Fields[index].UnlockCost;
            if (Fame + 1e-9 < cost)
            {
                sfx.Deny();
                return;
            }
            Fame = Math.Max(0, Fame - cost);
            Unlocked[index] = true;
            BestFieldIndex = Math.Max(BestFieldIndex, index);
            sfx.Buy();
            MoveTo(index);

            if (!PrestigeOpened && index >= Balance.PrestigeFieldIndex)
            {
                PrestigeOpened = true;
                fx.Banner("새 농장 개척이 열렸다!\n상점 → 개척 탭");
            }
            Save();
        }

        // ── 새 농장 개척 ──

        /// <summary>이번 개척에 필요한 명성. 개척할 때마다 늘어난다.</summary>
        public double PrestigeFameCost =>
            Math.Ceiling(Balance.PrestigeFameBase * Math.Pow(Balance.PrestigeFameGrowth, Stage - 1) - 1e-9);

        public bool CanPrestige => PrestigeOpened && Fame + 1e-9 >= PrestigeFameCost;

        /// <summary>지금 농장을 팔고 황금 씨앗을 받아 다음 단계에서 다시 시작한다.</summary>
        public void Prestige()
        {
            if (!CanPrestige)
            {
                sfx.Deny();
                return;
            }
            double seeds = PrestigeSeeds; // 명성은 아래에서 어차피 0으로 초기화되므로 따로 차감하지 않는다
            GoldenSeeds += seeds;
            Stage++;
            ResetRun();

            fx.Banner($"새 농장 개척!\n{Stage}단계 · 황금 씨앗 +{Num.Gain(seeds)}");
            fx.Shake(24f);
            sfx.Clear();
            Save();
        }

        /// <summary>데이터 초기화: 황금 씨앗, 영구 강화, 단계까지 모두 지우고 처음부터.</summary>
        public void ResetAll()
        {
            GoldenSeeds = 0;
            Stage = 1;
            BestFieldIndex = 0;
            PrestigeOpened = false;
            for (int i = 0; i < PermLevels.Length; i++) PermLevels[i] = 0;
            ResetRun();

            fx.Banner("모든 진행을 초기화했다");
            sfx.Clear();
            Save();
        }

        /// <summary>이번 판 진행을 처음 상태로. 개척과 데이터 초기화가 함께 쓴다.</summary>
        void ResetRun()
        {
            Coins = 0;
            TotalEarned = 0;
            Fame = 0;
            SickleLevel = 0;
            for (int i = 0; i < Unlocked.Length; i++) Unlocked[i] = i == 0;
            foreach (var m in Machines)
            {
                m.Reset();
                m.PriceMult = MachinePriceMult;
            }
            if (Perm(PermKind.StartMower) > 0) GiveMachine(Machines[0]);

            FieldIndex = 0;
            foreach (var c in Crops) c.Alive = false;
            AliveCount = 0;
            Target = null;
            field.HideAll();
            StartRegen();
        }

        // ── 저장 ──

        public const float AutoSaveInterval = 10f;

        /// <summary>마지막 저장 시각(Time.realtimeSinceStartup). 아직 없으면 음수.</summary>
        public float LastSaveRealtime { get; private set; } = -1f;

        float saveTimer;

        public void Save()
        {
            if (Machines == null) return;
            if (SaveSystem.Write(ToSaveData())) LastSaveRealtime = Time.realtimeSinceStartup;
            saveTimer = 0f;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save(); // 모바일에서 앱을 내릴 때
        }

        void OnApplicationQuit() => Save(); // 에디터에서는 Play를 멈출 때

        SaveData ToSaveData()
        {
            var d = new SaveData
            {
                coins = Coins,
                totalEarned = TotalEarned,
                fame = Fame,
                sickleLevel = SickleLevel,
                fieldIndex = FieldIndex,
                unlocked = (bool[])Unlocked.Clone(),
                machines = new MachineSave[Machines.Length],
                goldenSeeds = GoldenSeeds,
                stage = Stage,
                bestFieldIndex = BestFieldIndex,
                prestigeOpened = PrestigeOpened,
                permLevels = (int[])PermLevels.Clone(),
            };
            for (int i = 0; i < Machines.Length; i++)
            {
                var m = Machines[i];
                d.machines[i] = new MachineSave
                {
                    count = m.Count,
                    damageLevel = m.DamageLevel,
                    cooldownLevel = m.CooldownLevel,
                    precision = m.Precision,
                };
            }
            return d;
        }

        /// <summary>저장 내용을 적용. 밭·영구 강화 개수가 바뀌어도 겹치는 만큼만 읽는다.</summary>
        void ApplySaveData(SaveData d)
        {
            Coins = Math.Max(0, d.coins);
            TotalEarned = Math.Max(0, d.totalEarned);
            Fame = Math.Max(0, d.fame);
            SickleLevel = Math.Max(0, d.sickleLevel);
            GoldenSeeds = Math.Max(0, d.goldenSeeds);
            Stage = Math.Max(1, d.stage);
            BestFieldIndex = Mathf.Clamp(d.bestFieldIndex, 0, Unlocked.Length - 1);
            PrestigeOpened = d.prestigeOpened;

            if (d.unlocked != null)
                for (int i = 0; i < Unlocked.Length && i < d.unlocked.Length; i++) Unlocked[i] = d.unlocked[i];
            Unlocked[0] = true;

            if (d.permLevels != null)
                for (int i = 0; i < PermLevels.Length && i < d.permLevels.Length; i++)
                    PermLevels[i] = Math.Max(0, d.permLevels[i]);

            for (int i = 0; i < Machines.Length; i++)
            {
                var m = Machines[i];
                m.Reset();
                m.PriceMult = MachinePriceMult;
                if (d.machines == null || i >= d.machines.Length || d.machines[i] == null) continue;
                var s = d.machines[i];
                m.DamageLevel = Math.Max(0, s.damageLevel);
                m.CooldownLevel = Mathf.Clamp(s.cooldownLevel, 0, Balance.CooldownUpMaxLevel);
                m.Precision = s.precision;
                int count = Mathf.Clamp(s.count, 0, Balance.MaxMachinesPerType);
                for (int k = 0; k < count; k++) GiveMachine(m); // 쿨타임 엇갈림도 새로 계산된다
            }

            FieldIndex = d.fieldIndex >= 0 && d.fieldIndex < Unlocked.Length && Unlocked[d.fieldIndex] ? d.fieldIndex : 0;
        }

        public double PermPrice(int i)
        {
            var d = Balance.Perms[i];
            return Math.Ceiling(d.BasePrice * Math.Pow(d.Growth, PermLevels[i]) - 1e-9);
        }

        public bool PermMaxed(int i) => Balance.Perms[i].MaxLevel > 0 && PermLevels[i] >= Balance.Perms[i].MaxLevel;

        public void BuyPerm(int i)
        {
            if (PermMaxed(i)) return;
            double price = PermPrice(i);
            if (GoldenSeeds + 1e-9 < price)
            {
                sfx.Deny();
                return;
            }
            GoldenSeeds -= price;
            PermLevels[i]++;
            foreach (var m in Machines) m.PriceMult = MachinePriceMult;
            sfx.Buy();
        }

        /// <summary>연 밭으로 이동. 지금 밭의 남은 작물은 보상 없이 사라진다.</summary>
        public void MoveTo(int index)
        {
            if (index < 0 || index >= Unlocked.Length || !Unlocked[index] || index == FieldIndex) return;
            FieldIndex = index;
            foreach (var c in Crops) c.Alive = false;
            AliveCount = 0;
            Target = null;
            field.HideAll();
            fx.Toast($"{Field.Name} 밭으로 이동");
            StartRegen();
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
            GiveMachine(m);
        }

        void GiveMachine(MachineState m)
        {
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
            if (kb.f1Key.wasPressedThisFrame)
            {
                double amount = Math.Max(1000, Field.Reward * Balance.CellCount * 10);
                Earn(amount);
                fx.Toast($"디버그: +{Num.Gain(amount)} 코인");
            }
            if (kb.f3Key.wasPressedThisFrame)
            {
                Fame += 10;
                fx.Toast("디버그: 명성 +10");
            }
            if (kb.f4Key.wasPressedThisFrame)
            {
                GoldenSeeds += 5;
                fx.Toast("디버그: 황금 씨앗 +5");
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
