using System;
using System.Globalization;

namespace HarvestInc
{
    /// <summary>숫자 표기. 1000 이상은 K/M/B/T...</summary>
    public static class Num
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        /// <summary>보유 코인: 내림.</summary>
        public static string Coins(double v) => v < 1000 ? Math.Floor(v + 1e-9).ToString("0", Inv) : Big(v);

        /// <summary>가격: 올림.</summary>
        public static string Price(double v) => v < 1000 ? Math.Ceiling(v - 1e-9).ToString("0", Inv) : Big(v);

        /// <summary>획득량 팝업: 100 미만은 소수 1자리.</summary>
        public static string Gain(double v)
        {
            if (v < 100) return v.ToString("0.#", Inv);
            if (v < 1000) return Math.Floor(v).ToString("0", Inv);
            return Big(v);
        }

        /// <summary>피해량 등 능력치.</summary>
        public static string Stat(double v) => v < 1000 ? v.ToString("0.##", Inv) : Big(v);

        public static string Sec(float v) => v.ToString("0.##", Inv);

        public static string Big(double v)
        {
            int i = 0;
            while (v >= 1000 && i < Suffix.Length - 1) { v /= 1000; i++; }
            string n = v < 10 ? v.ToString("0.00", Inv) : v < 100 ? v.ToString("0.0", Inv) : v.ToString("0", Inv);
            return n + Suffix[i];
        }
    }
}
