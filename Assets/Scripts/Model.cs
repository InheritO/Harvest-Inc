using System;
using System.Collections.Generic;

namespace HarvestInc
{
    public sealed class Crop
    {
        public readonly int Index, X, Y;
        public CropKind Kind;
        public double MaxHp, Hp, Reward;
        public bool Alive;

        public Crop(int index)
        {
            Index = index;
            X = index % Balance.Cols;
            Y = index / Balance.Cols;
        }

        public float HpRatio => MaxHp <= 0 ? 0f : (float)(Hp / MaxHp);
    }

    public sealed class MachineState
    {
        public readonly MachineDef Def;
        public int Count;
        public int DamageLevel;
        public int CooldownLevel;
        public bool Precision;

        /// <summary>기체별 남은 대기 시간(초).</summary>
        public readonly List<float> Timers = new List<float>();

        public MachineState(MachineDef def) { Def = def; }

        public double Damage => Def.Damage * Math.Pow(Balance.MachineDamageUpMult, DamageLevel);
        public double NextDamage => Def.Damage * Math.Pow(Balance.MachineDamageUpMult, DamageLevel + 1);
        public float Cooldown => CooldownAt(CooldownLevel);
        public float NextCooldown => CooldownAt(CooldownLevel + 1);

        public bool CanBuyMore => Count < Balance.MaxMachinesPerType;
        public bool CooldownMaxed => CooldownLevel >= Balance.CooldownUpMaxLevel;

        public double BuyPrice => Math.Ceiling(Def.FirstPrice * Math.Pow(Balance.MachineBuyPriceGrowth, Count));
        public double DamageUpPrice => Math.Ceiling(Def.DamageUpPrice * Math.Pow(Balance.MachineDamageUpPriceGrowth, DamageLevel));
        public double CooldownUpPrice => Math.Ceiling(Def.CooldownUpPrice * Math.Pow(Balance.CooldownUpPriceGrowth, CooldownLevel));
        public double PrecisionPrice => Def.PrecisionPrice;

        float CooldownAt(int level) => Def.Cooldown * (float)Math.Pow(Balance.CooldownUpMult, level);
    }
}
