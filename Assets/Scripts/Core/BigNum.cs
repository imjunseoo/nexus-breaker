using System;

namespace NexusBreaker
{
    /// <summary>큰 수(double)를 한국식 단위(만·억·조·경…)로 표시한다.</summary>
    public static class BigNum
    {
        static readonly string[] Units = { "", "만", "억", "조", "경", "해", "자", "양", "구", "간", "정", "재", "극" };

        /// <summary>예: 1234 → "1,234", 12345678 → "1,234만", 1.5e9 → "15억".</summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "-∞";

            bool negative = value < 0;
            double v = Math.Abs(value);
            string sign = negative ? "-" : "";

            if (v < 10000d) return sign + Math.Floor(v).ToString("#,0");

            int unit = 0;
            while (v >= 10000d && unit < Units.Length - 1)
            {
                v /= 10000d;
                unit++;
            }

            // 큰 단위 + 바로 아래 단위 일부를 보여준다 (예: 3억 2,500만 → "3.25억")
            string number = v >= 1000d ? Math.Floor(v).ToString("#,0")
                          : v >= 100d ? (Math.Floor(v * 10d) / 10d).ToString("0.#")
                          : (Math.Floor(v * 100d) / 100d).ToString("0.##");
            return sign + number + Units[unit];
        }

        /// <summary>배율 표시: 1,000 미만은 소수 둘째 자리까지, 그 이상은 단위 표기.</summary>
        public static string Mult(double x) => x < 1000d ? x.ToString("0.##") : Format(x);

        /// <summary>정수 표시가 필요한 가격용(너트). 1만 미만은 쉼표, 이상은 Format.</summary>
        public static string Price(double value) => Format(Math.Round(value));

        /// <summary>초 → "1시간 3분", "45초" 형식.</summary>
        public static string Duration(double seconds)
        {
            if (double.IsInfinity(seconds) || seconds > 3600d * 24d * 365d) return "아주 오래";
            if (seconds < 1d) return "즉시";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalDays >= 1d) return $"{(int)ts.TotalDays}일 {ts.Hours}시간";
            if (ts.TotalHours >= 1d) return $"{(int)ts.TotalHours}시간 {ts.Minutes}분";
            if (ts.TotalMinutes >= 1d) return $"{ts.Minutes}분 {ts.Seconds}초";
            return $"{ts.Seconds}초";
        }
    }
}
