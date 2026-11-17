using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    /// <summary>시드 고정 가능한 난수 + 게임용 분포. 모델 로직은 UnityEngine.Random 대신 이것만 쓴다(테스트 가능).</summary>
    public sealed class Rng
    {
        readonly Random _r;
        double? _spareNormal;

        public Rng(int seed) { _r = new Random(seed); }
        public Rng() { _r = new Random(); }

        public double Next01() => _r.NextDouble();
        public int Range(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);
        public double Range(double min, double max) => min + (max - min) * _r.NextDouble();
        public bool Chance(double p) => _r.NextDouble() < p;

        /// <summary>표준정규분포 (Box–Muller).</summary>
        public double Normal()
        {
            if (_spareNormal.HasValue)
            {
                double s = _spareNormal.Value;
                _spareNormal = null;
                return s;
            }
            double u1 = 1d - _r.NextDouble();
            double u2 = _r.NextDouble();
            double mag = Math.Sqrt(-2d * Math.Log(u1));
            _spareNormal = mag * Math.Sin(2d * Math.PI * u2);
            return mag * Math.Cos(2d * Math.PI * u2);
        }

        /// <summary>중앙값 median, 로그 표준편차 sigma인 로그정규 표본.</summary>
        public double LogNormal(double median, double sigma) => median * Math.Exp(sigma * Normal());

        /// <summary>평균 mean인 지수분포 표본 (손님 도착 간격 등).</summary>
        public double Exponential(double mean) => -mean * Math.Log(1d - _r.NextDouble());

        public int WeightedIndex(IReadOnlyList<double> weights)
        {
            double total = 0d;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0d, weights[i]);
            if (total <= 0d) return -1;
            double pick = _r.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                pick -= Math.Max(0d, weights[i]);
                if (pick < 0d) return i;
            }
            return weights.Count - 1;
        }
    }

    public static class MathUtil
    {
        /// <summary>표준정규 누적분포 Φ(x). Abramowitz–Stegun 7.1.26 근사(오차 ~1e-7).</summary>
        public static double NormalCdf(double x)
        {
            double t = 1d / (1d + 0.3275911d * Math.Abs(x) / Math.Sqrt(2d));
            double y = 1d - (((((1.061405429d * t - 1.453152027d) * t) + 1.421413741d) * t - 0.284496736d) * t + 0.254829592d)
                       * t * Math.Exp(-x * x / 2d);
            return x >= 0d ? 0.5d * (1d + y) : 0.5d * (1d - y);
        }

        /// <summary>로그정규(median, sigma) 표본이 threshold 이상일 확률.</summary>
        public static double LogNormalExceed(double threshold, double median, double sigma)
        {
            if (threshold <= 0d) return 1d;
            if (median <= 0d) return 0d;
            if (sigma <= 0d) return median >= threshold ? 1d : 0d;
            double z = Math.Log(threshold / median) / sigma;
            return 1d - NormalCdf(z);
        }
    }
}
