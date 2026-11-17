using System;

namespace NexusBreaker
{
    /// <summary>
    /// 업그레이드 레벨 → 수치 공식. 밸런스 조정은 여기서만 한다.
    /// (밸런스 시뮬레이션: 메뉴 Nexus Breaker/Run Balance Simulation → Docs/BALANCE_SIM.md)
    /// 주의: 조수 데미지는 망치 배율과 곱하지 않는다(곱하면 후반 DPS가 너트의 1.2제곱으로 폭주함).
    /// </summary>
    public static class Stats
    {
        // ── 타격 ──
        public const double BaseSwingDamage = 5d;
        public const double BaseHelperDamage = 3d;
        public const double CritMultiplier = 10d;
        public const double HelperSwingInterval = 1.2d;

        public const double HammerGrowth = 1.07d, HammerLinear = 0.25d;
        public const double HelperPowerGrowth = 1.07d, HelperPowerLinear = 0.3d;

        /// <summary>
        /// 지층 경도: 깊을수록 단단해서 데미지 배율이 줄어든다(외피/압축층/내핵/코어).
        /// 깊은 지층의 비싼 박스로 수입이 폭증하는 걸 상쇄해 후반이 순식간에 끝나지 않게 한다.
        /// </summary>
        public static readonly double[] StratumHardness = { 1.0d, 0.2d, 0.045d, 0.012d };

        public static double HammerMult(int level) => Math.Pow(HammerGrowth, level) * (1d + HammerLinear * level);
        public static double SwingInterval(int level) => Math.Max(0.25d, Math.Pow(0.95d, level));
        public static double HelperPowerMult(int level) => Math.Pow(HelperPowerGrowth, level) * (1d + HelperPowerLinear * level);
        public static double CritChance(int level) => 0.05d + 0.01d * level;
        public static int HelperCount(int level) => Math.Min(5, level);

        // ── 박스·창고 ──
        public static int HitsPerBox(int level) => Math.Max(5, 20 - level);
        public static double BoxLuck(int level) => 0.03d * level;
        public static int WarehouseCap(int level) => 20 + 10 * level;

        // ── 상점 ──
        public const int MaxCustomersViewItems = 3;
        public static int ShelfSlots(int level) => 6 + level;

        /// <summary>손님 평균 도착 간격(초).</summary>
        public static double CustomerInterval(int trafficLevel, int cores) =>
            Math.Max(1.5d, 8d * Math.Pow(0.93d, trafficLevel)) / (1d + 0.02d * cores);

        // ── 오프라인 ──
        public static double OfflineCapHours(int level) => 8d + 2d * level;

        // ── 환생 ──
        public static double CoreDamageMult(int cores) => 1d + 0.1d * cores;
        public static int CoresForRegion(int region) => 10 * (region + 1);

        // ── 지층 ──
        /// <summary>HP 비율(0~1) → 지층 인덱스 0(외피)~3(코어).</summary>
        public static int Stratum(double hpFraction) =>
            hpFraction > 0.70d ? 0 : hpFraction > 0.40d ? 1 : hpFraction > 0.10d ? 2 : 3;

        public static readonly string[] StratumNames = { "외피", "압축층", "내핵", "코어" };

        /// <summary>지층별 박스 등급 확률(일반/고급/희귀/외계/???).</summary>
        public static readonly double[][] StratumGradeWeights =
        {
            new double[] { 85, 13, 2, 0, 0 },
            new double[] { 60, 30, 8, 2, 0 },
            new double[] { 35, 35, 18, 11, 1 },
            new double[] { 15, 30, 25, 25, 5 },
        };

        /// <summary>HP 비율 → 넥서스 파손 단계 0~4.</summary>
        public static int DamageStage(double hpFraction) =>
            hpFraction > 0.8d ? 0 : hpFraction > 0.6d ? 1 : hpFraction > 0.4d ? 2 : hpFraction > 0.2d ? 3 : 4;

        // ── 시장 ──
        public const double MarketStepSeconds = 300d;
        public const double MarketStepSize = 0.10d;
        public const double MarketMinMult = 0.5d;
        public const double MarketMaxMult = 2.0d;
        public const double MarketEventChance = 0.25d;
        public const double MarketEventSeconds = 600d;
    }
}
