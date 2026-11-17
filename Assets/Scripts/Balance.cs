using System;
using UnityEngine;

namespace HarvestInc
{
    public enum CropKind { Wheat, Rice, Corn, Carrot, Pumpkin }

    public enum MachinePattern { Short3, FullLine }

    public enum PermKind { Reward, Sickle, MachineDiscount, ClearBonus, StartMower }

    /// <summary>황금 씨앗으로 사는 영구 강화. 개척해도 유지된다.</summary>
    public sealed class PermDef
    {
        public PermKind Kind;
        public string Name;
        public string Desc;
        public double BasePrice;
        public double Growth;
        public int MaxLevel; // 0 = 제한 없음
    }

    /// <summary>밭 하나. 난이도는 고정이고, 비울 때마다 명성을 준다.</summary>
    public sealed class FieldDef
    {
        public string Name;
        public CropKind Kind;
        public double Hp;
        public double Reward;
        public double Fame;       // 밭을 모두 비웠을 때 얻는 명성
        public double UnlockCost; // 이 밭을 여는 데 드는 명성
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

        public const double ClearBonusRatio = 0.5;
        public const float RegenDelay = 0.5f;

        /// <summary>밭 목록. 순서대로 연다. Fields[0]은 처음부터 열려 있다.</summary>
        public static readonly FieldDef[] Fields =
        {
            new FieldDef { Name = "밀", Kind = CropKind.Wheat, Hp = 1, Reward = 1, Fame = 1, UnlockCost = 0 },
            new FieldDef { Name = "벼", Kind = CropKind.Rice, Hp = 5, Reward = 4, Fame = 2, UnlockCost = 5 },
            new FieldDef { Name = "옥수수", Kind = CropKind.Corn, Hp = 25, Reward = 18, Fame = 4, UnlockCost = 20 },
            new FieldDef { Name = "당근", Kind = CropKind.Carrot, Hp = 125, Reward = 75, Fame = 8, UnlockCost = 60 },
            new FieldDef { Name = "호박", Kind = CropKind.Pumpkin, Hp = 625, Reward = 320, Fame = 16, UnlockCost = 200 },
        };

        // ── 새 농장 개척 ──
        public const int PrestigeFieldIndex = 2;        // 옥수수 밭을 처음 열면 개척이 열린다
        public const double PrestigeSeedFactor = 0.1;   // 황금 씨앗 = floor(계수 × √이번 판 코인)
        public const double PrestigeFameBase = 30;      // 첫 개척에 필요한 명성
        public const double PrestigeFameGrowth = 2;     // 개척할 때마다 필요 명성 ×2
        public const double StageHpMult = 1.5;
        public const double StageRewardMult = 2;

        public const double PermRewardStep = 0.25;
        public const double PermSickleStep = 0.25;
        public const double PermDiscountMult = 0.95;
        public const double PermClearBonusStep = 0.25;

        public static readonly PermDef[] Perms =
        {
            new PermDef { Kind = PermKind.Reward, Name = "풍년", Desc = "모든 수확 보상 +25%", BasePrice = 1, Growth = 1.6 },
            new PermDef { Kind = PermKind.Sickle, Name = "명장의 낫", Desc = "낫 피해 +25%", BasePrice = 1, Growth = 1.6 },
            new PermDef { Kind = PermKind.MachineDiscount, Name = "농기계 할인", Desc = "농기계 구매가 −5%", BasePrice = 2, Growth = 1.8, MaxLevel = 10 },
            new PermDef { Kind = PermKind.ClearBonus, Name = "깔끔한 수확", Desc = "클리어 보너스 +25%p", BasePrice = 1, Growth = 1.6 },
            new PermDef { Kind = PermKind.StartMower, Name = "물려받은 예초기", Desc = "개척 직후 예초기 1대 보유", BasePrice = 3, Growth = 1, MaxLevel = 1 },
        };

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
