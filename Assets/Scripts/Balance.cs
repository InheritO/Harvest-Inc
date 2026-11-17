using System;
using UnityEngine;

namespace HarvestInc
{
    public enum CropKind { Wheat, Rice, Corn }

    public enum MachinePattern { Short3, FullLine }

    public sealed class CropType
    {
        public CropKind Kind;
        public string Name;
        public int MinLevel;
        public double BaseReward;
    }

    public sealed class MachineDef
    {
        public string Name;
        public MachinePattern Pattern;
        public float Cooldown;
        public double Damage;
        public double FirstPrice;
        public double DamageUpPrice;
        public double CooldownUpPrice;
        public double PrecisionPrice;
        public Color FxColor;
        public float FxThickness;
    }

    /// <summary>
    /// 기획서 v0.2 수치와 프로토타입 기본값(Docs/prototype-defaults.md). 밸런스 조정은 여기서만 한다.
    /// </summary>
    public static class Balance
    {
        // ── 밭 ──
        public const int Cols = 6;
        public const int Rows = 8;
        public const int CellCount = Cols * Rows;

        public const double HpGrowth = 1.15;
        public const double RewardGrowth = 1.12;
        public const double ClearBonusRatio = 0.5;
        public const float RegenDelay = 0.5f;

        public static readonly CropType[] CropTypes =
        {
            new CropType { Kind = CropKind.Wheat, Name = "밀", MinLevel = 1, BaseReward = 1 },
            new CropType { Kind = CropKind.Rice, Name = "벼", MinLevel = 10, BaseReward = 4 },
            new CropType { Kind = CropKind.Corn, Name = "옥수수", MinLevel = 25, BaseReward = 15 },
        };

        public static CropType CropTypeFor(int level)
        {
            var type = CropTypes[0];
            foreach (var t in CropTypes)
                if (level >= t.MinLevel) type = t;
            return type;
        }

        public static double CropHp(int level) => Math.Ceiling(Math.Pow(HpGrowth, level - 1) - 1e-9);

        public static double CropReward(int level) =>
            CropTypeFor(level).BaseReward * Math.Pow(RewardGrowth, level - 1);

        // ── 직접 수확 ──
        public const double SickleBaseDamage = 1;
        public const double SickleUpgradeMult = 1.25;
        public const double SickleUpgradeBasePrice = 15;
        public const double SickleUpgradePriceGrowth = 1.5;
        public const float HoldDelay = 0.3f;
        public const float HoldInterval = 0.25f; // 초당 4회

        // ── 농기계 ──
        public const int MaxMachinesPerType = 10;
        public const double MachineBuyPriceGrowth = 1.25;
        public const double MachineDamageUpMult = 1.2;
        public const double MachineDamageUpPriceGrowth = 1.6;
        public const double CooldownUpMult = 0.95; // 곱연산
        public const int CooldownUpMaxLevel = 10;
        public const double CooldownUpPriceGrowth = 1.8;

        public static readonly MachineDef Mower = new MachineDef
        {
            Name = "예초기",
            Pattern = MachinePattern.Short3,
            Cooldown = 1.5f,
            Damage = 1,
            FirstPrice = 10,
            DamageUpPrice = 25,
            CooldownUpPrice = 30,
            PrecisionPrice = 200,
            FxColor = new Color(0.6f, 1f, 0.45f, 0.95f),
            FxThickness = 26f,
        };

        public static readonly MachineDef Combine = new MachineDef
        {
            Name = "콤바인",
            Pattern = MachinePattern.FullLine,
            Cooldown = 4f,
            Damage = 5,
            FirstPrice = 500,
            DamageUpPrice = 1000,
            CooldownUpPrice = 1500,
            PrecisionPrice = 5000,
            FxColor = new Color(1f, 0.62f, 0.15f, 0.8f),
            FxThickness = 120f,
        };
    }
}
