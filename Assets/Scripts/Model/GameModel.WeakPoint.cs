using System;

namespace NexusBreaker
{
    /// <summary>
    /// 약점(v0.2): 가끔 넥서스 표면에 빛나는 균열이 생긴다. 제한 시간 안에 클릭하면 큰 데미지 + 지층 박스.
    /// 화면을 보고 있는 플레이어에게 주는 보상이라 실시간 Tick에서만 생기고, 오프라인 정산에는 없다.
    /// </summary>
    public sealed partial class GameModel
    {
        public event Action WeakPointSpawned;
        public event Action<HitInfo, Grade> WeakPointBroken;   // 타격 정보, 받은 박스 등급
        public event Action WeakPointExpired;

        double _weakTimer = Stats.WeakPointFirstDelay;
        bool _weakHintShown;

        public bool WeakPointActive { get; private set; }
        public double WeakPointRemaining { get; private set; }
        /// <summary>위치 굴림(0~1). 뷰가 넥서스 그림의 결정 몸통 위 후보 지점 중 하나로 바꾼다.</summary>
        public double WeakPointRoll { get; private set; }

        public double WeakPointDamage => ClickDamage * Stats.WeakPointDamageMult;

        void TickWeakPoint(double dt)
        {
            if (Data.nexusDestroyed)
            {
                if (WeakPointActive) { WeakPointActive = false; WeakPointExpired?.Invoke(); }
                return;
            }

            if (WeakPointActive)
            {
                WeakPointRemaining -= dt;
                if (WeakPointRemaining <= 0d)
                {
                    WeakPointActive = false;
                    WeakPointExpired?.Invoke();
                    ScheduleNextWeakPoint();
                }
                return;
            }

            // 튜토리얼 도중에는 헷갈리지 않게 생기지 않는다
            if (Data.tutorialStep >= 0) return;

            _weakTimer -= dt;
            if (_weakTimer > 0d) return;
            SpawnWeakPoint();
        }

        void ScheduleNextWeakPoint() =>
            _weakTimer = Math.Max(Stats.WeakPointMinInterval, Rng.Exponential(Stats.WeakPointMeanInterval));

        /// <summary>약점을 바로 만든다(테스트·자동 검증용으로도 쓴다).</summary>
        public void SpawnWeakPoint()
        {
            if (Data.nexusDestroyed) return;
            WeakPointActive = true;
            WeakPointRemaining = Stats.WeakPointLifetime;
            WeakPointRoll = Rng.Next01();
            WeakPointSpawned?.Invoke();
            if (Data.lifetime.weakPoints == 0 && !_weakHintShown)
            {
                _weakHintShown = true;
                RaiseToast("넥서스에 빛나는 균열이 생겼다! 사라지기 전에 클릭!");
            }
        }

        /// <summary>약점 클릭. 성공하면 클릭 데미지 × 30(항상 치명타 연출) + 지층 박스 1개.</summary>
        public bool HitWeakPoint()
        {
            if (!WeakPointActive || Data.nexusDestroyed) return false;
            WeakPointActive = false;
            ScheduleNextWeakPoint();

            Data.run.weakPoints++;
            Data.lifetime.weakPoints++;
            Data.run.clicks++;
            Data.lifetime.clicks++;

            var info = new HitInfo { Source = HitSource.Click, HelperIndex = -1, Damage = WeakPointDamage, Crit = true };
            Grade grade = RollBoxGrade(milestone: true);
            WeakPointBroken?.Invoke(info, grade);
            Hit?.Invoke(info);

            OnContractEvent(ContractKind.WeakPoints, null, 1d);
            OnContractEvent(ContractKind.Clicks, null, 1d);

            DealDamage(info.Damage, hits: 1, emitEvents: true);
            TryDropBox(grade, milestone: true, emitEvents: true);
            return true;
        }
    }
}
