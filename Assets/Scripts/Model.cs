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

        /// <summary>구매가 배율 (영구 강화 "농기계 할인").</summary>
        public double PriceMult = 1;

        public MachineState(MachineDef def) { Def = def; }

        /// <summary>개척: 보유 대수와 강화를 모두 초기화. UI가 이 객체를 참조하므로 새로 만들지 않는다.</summary>
        public void Reset()
        {
            Count = 0;
            DamageLevel = 0;
            CooldownLevel = 0;
            Precision = false;
            Timers.Clear();
        }

        public double Damage => Def.Damage * Math.Pow(Balance.MachineDamageUpMult, DamageLevel);
        public double NextDamage => Def.Damage * Math.Pow(Balance.MachineDamageUpMult, DamageLevel + 1);
        public float Cooldown => CooldownAt(CooldownLevel);
        public float NextCooldown => CooldownAt(CooldownLevel + 1);

        public bool CanBuyMore => Count < Balance.MaxMachinesPerType;
        public bool CooldownMaxed => CooldownLevel >= Balance.CooldownUpMaxLevel;

        public double BuyPrice =>
            Math.Ceiling(Def.FirstPrice * Math.Pow(Balance.MachineBuyPriceGrowth, Count) * PriceMult);
        public double DamageUpPrice => Math.Ceiling(Def.DamageUpPrice * Math.Pow(Balance.MachineDamageUpPriceGrowth, DamageLevel));
        public double CooldownUpPrice => Math.Ceiling(Def.CooldownUpPrice * Math.Pow(Balance.CooldownUpPriceGrowth, CooldownLevel));
        public double PrecisionPrice => Def.PrecisionPrice;

        float CooldownAt(int level) => Def.Cooldown * (float)Math.Pow(Balance.CooldownUpMult, level);
    }
}
