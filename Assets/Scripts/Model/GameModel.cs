using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    public enum HitSource { Click, Swing, Helper }

    public struct HitInfo
    {
        public HitSource Source;
        public int HelperIndex;
        public double Damage;
        public bool Crit;
    }

    /// <summary>
    /// 게임 규칙 전체(순수 C#, UnityEngine 의존 없음). 뷰는 이벤트를 구독해서 연출만 한다.
    /// 시간은 Tick(dt)로만 흐른다 → 오프라인 시뮬레이션·테스트가 같은 코드를 쓴다.
    /// </summary>
    public sealed partial class GameModel
    {
        public SaveData Data { get; }
        public Rng Rng { get; }

        // ── 이벤트 (뷰/오디오/UI가 구독) ──
        public event Action<HitSource, int> SwingStarted;        // 스윙 모션 시작(타격은 ImpactDelay 후)
        public event Action<HitInfo> Hit;
        public event Action<int> StageChanged;                   // 넥서스 파손 단계 0~4
        public event Action<int> StratumChanged;                 // 지층 0~3
        public event Action NexusDestroyed;
        public event Action<Grade, bool> BoxDropped;             // 등급, 지층(마일스톤) 박스 여부
        public event Action<ItemDef, Grade, bool> BoxOpened;     // 아이템, 박스 등급, 도감 신규
        public event Action<CustomerVisit> CustomerArrived;      // 손님이 걸어오기 시작
        public event Action<CustomerVisit> CustomerResolved;     // 카운터 도착 → 구매/거절 결정됨
        public event Action<UpgradeId, int> Upgraded;
        public event Action<MarketEventDef> MarketEventStarted;
        public event Action MarketEventEnded;
        public event Action Prestiged;
        public event Action EconomyChanged;                      // 너트/창고/진열대/시세 변경 → UI 갱신
        public event Action<string> Toast;

        /// <summary>스윙 시작 → 실제 타격까지 시간(수리공 swing 애니메이션 4번째 프레임 ≈ 3×70ms).</summary>
        public const double SwingImpactDelay = 0.21d;
        public const double HelperImpactDelay = 0.18d;

        readonly List<PendingImpact> _pendingImpacts = new List<PendingImpact>();
        double _swingTimer;
        double[] _helperTimers = new double[5];

        struct PendingImpact
        {
            public double Remaining;
            public HitSource Source;
            public int HelperIndex;
        }

        public GameModel(SaveData data, Rng rng)
        {
            Data = data ?? SaveData.NewGame();
            Rng = rng ?? new Rng();
            EnsureMarket();
            EnsureShelfSize();
            EnsureContracts();
            for (int i = 0; i < _helperTimers.Length; i++) _helperTimers[i] = HelperSwingInterval * (0.3d + 0.15d * i);
            _swingTimer = 0.5d;
            _customerTimer = 2d;
            _lastStage = Stats.DamageStage(HpFraction);
            _lastStratum = Stats.Stratum(HpFraction);
        }

        // ───────────────────────── 조회 ─────────────────────────

        public double HpFraction => Data.nexusMaxHp <= 0d ? 0d : Data.nexusHp / Data.nexusMaxHp;
        public int DamageStageNow => Stats.DamageStage(HpFraction);
        public int StratumNow => Stats.Stratum(HpFraction);
        public bool ReachedInnerCore => HpFraction <= 0.40d;

        public int Level(UpgradeId id)
        {
            string key = id.ToString();
            foreach (var u in Data.upgrades) if (u.id == key) return u.level;
            return 0;
        }

        void SetLevel(UpgradeId id, int level)
        {
            string key = id.ToString();
            foreach (var u in Data.upgrades)
                if (u.id == key) { u.level = level; return; }
            Data.upgrades.Add(new UpgradeLevel { id = key, level = level });
        }

        public double CoreMult => Stats.CoreDamageMult(Data.cores);
        /// <summary>현재 지층의 경도 배율(1 = 외피).</summary>
        public double HardnessMult => Stats.StratumHardness[StratumNow];
        public double SwingDamage => Stats.BaseSwingDamage * Stats.HammerMult(Level(UpgradeId.Hammer)) * CoreMult * HardnessMult;
        public double ClickDamage => SwingDamage;
        public double HelperDamage => Stats.BaseHelperDamage * Stats.HelperPowerMult(Level(UpgradeId.HelperPower)) * CoreMult * HardnessMult;
        public double SwingInterval => Stats.SwingInterval(Level(UpgradeId.SwingSpeed));
        public double HelperSwingInterval => Stats.HelperSwingInterval;
        public int HelperCount => Stats.HelperCount(Level(UpgradeId.Helpers));
        public double CritChance => Stats.CritChance(Level(UpgradeId.Crit));
        public double AvgCritFactor => 1d + CritChance * (Stats.CritMultiplier - 1d);

        /// <summary>자동 데미지(클릭 제외) 초당 기대값.</summary>
        public double AutoDps => (SwingDamage / SwingInterval + HelperCount * HelperDamage / HelperSwingInterval) * AvgCritFactor;

        // ───────────────────────── 시간 ─────────────────────────

        /// <summary>실시간 진행. 뷰가 있는 일반 플레이에서 매 프레임 호출.</summary>
        public void Tick(double dt)
        {
            if (dt <= 0d) return;
            Data.playTime += dt;

            TickCombat(dt);
            TickWeakPoint(dt);
            TickCustomers(dt);
            TickMarket(dt);
            TickContracts();
        }

        void RaiseEconomyChanged() => EconomyChanged?.Invoke();
        void RaiseToast(string msg) => Toast?.Invoke(msg);
    }
}
