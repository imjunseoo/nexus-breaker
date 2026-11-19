using System;

namespace NexusBreaker
{
    public sealed partial class GameModel
    {
        int _lastStage;
        int _lastStratum;

        void TickCombat(double dt)
        {
            if (Data.nexusDestroyed) { _pendingImpacts.Clear(); return; }

            // 예약된 타격 처리
            for (int i = _pendingImpacts.Count - 1; i >= 0; i--)
            {
                var p = _pendingImpacts[i];
                p.Remaining -= dt;
                if (p.Remaining <= 0d)
                {
                    _pendingImpacts.RemoveAt(i);
                    ApplyHit(p.Source, p.HelperIndex);
                    if (Data.nexusDestroyed) return;
                }
                else _pendingImpacts[i] = p;
            }

            // 수리공 자동 스윙
            _swingTimer -= dt;
            if (_swingTimer <= 0d)
            {
                _swingTimer += SwingInterval;
                if (_swingTimer < 0d) _swingTimer = SwingInterval; // 긴 프레임 드랍 시 몰아치기 방지
                SwingStarted?.Invoke(HitSource.Swing, -1);
                _pendingImpacts.Add(new PendingImpact { Remaining = SwingImpactDelay, Source = HitSource.Swing, HelperIndex = -1 });
            }

            // 조수
            int helpers = HelperCount;
            for (int h = 0; h < helpers; h++)
            {
                _helperTimers[h] -= dt;
                if (_helperTimers[h] <= 0d)
                {
                    _helperTimers[h] += HelperSwingInterval;
                    if (_helperTimers[h] < 0d) _helperTimers[h] = HelperSwingInterval;
                    SwingStarted?.Invoke(HitSource.Helper, h);
                    _pendingImpacts.Add(new PendingImpact { Remaining = HelperImpactDelay, Source = HitSource.Helper, HelperIndex = h });
                }
            }
        }

        /// <summary>플레이어가 넥서스를 클릭. 즉시 타격.</summary>
        public HitInfo Click()
        {
            if (Data.nexusDestroyed) return default;
            Data.run.clicks++;
            Data.lifetime.clicks++;
            OnContractEvent(ContractKind.Clicks, null, 1d);
            return ApplyHit(HitSource.Click, -1);
        }

        HitInfo ApplyHit(HitSource source, int helperIndex)
        {
            double baseDmg = source == HitSource.Helper ? HelperDamage : SwingDamage;
            bool crit = Rng.Chance(CritChance);
            double dmg = crit ? baseDmg * Stats.CritMultiplier : baseDmg;

            var info = new HitInfo { Source = source, HelperIndex = helperIndex, Damage = dmg, Crit = crit };
            Hit?.Invoke(info);

            DealDamage(dmg, hits: 1, emitEvents: true);
            return info;
        }

        /// <summary>데미지 적용 + 지층 박스 + 일반 박스 카운트 + 단계 변화 + 파괴 판정.</summary>
        void DealDamage(double dmg, long hits, bool emitEvents)
        {
            if (Data.nexusDestroyed || dmg <= 0d) return;

            double before = Data.nexusHp;
            Data.nexusHp = Math.Max(0d, Data.nexusHp - dmg);
            double dealt = before - Data.nexusHp;
            Data.run.totalDamage += dealt;
            Data.lifetime.totalDamage += dealt;
            Data.run.totalHits += hits;
            Data.lifetime.totalHits += hits;

            // 일반 박스: 타격 N회마다
            Data.hitsSinceBox += (int)Math.Min(hits, int.MaxValue / 2);
            int perBox = Stats.HitsPerBox(Level(UpgradeId.BoxRate));
            while (Data.hitsSinceBox >= perBox)
            {
                Data.hitsSinceBox -= perBox;
                TryDropBox(RollBoxGrade(milestone: false), milestone: false, emitEvents);
            }

            // 지층 박스: HP 1%마다 확정
            double percent = HpFraction * 100d;
            while (Data.nextMilestonePercent >= 0 && percent <= Data.nextMilestonePercent)
            {
                Data.nextMilestonePercent--;
                TryDropBox(RollBoxGrade(milestone: true), milestone: true, emitEvents);
            }

            int stage = DamageStageNow;
            if (stage != _lastStage)
            {
                _lastStage = stage;
                if (emitEvents) StageChanged?.Invoke(stage);
            }

            int stratum = StratumNow;
            if (stratum != _lastStratum)
            {
                _lastStratum = stratum;
                if (emitEvents)
                {
                    StratumChanged?.Invoke(stratum);
                    double hard = Stats.StratumHardness[0] / Stats.StratumHardness[stratum];
                    RaiseToast($"새 지층 도달: {Stats.StratumNames[stratum]} — 경도 ×{hard:0.#} (더 귀한 물건이 나온다)");
                }
            }

            if (Data.nexusHp <= 0d)
            {
                Data.nexusDestroyed = true;
                _pendingImpacts.Clear();
                if (emitEvents) NexusDestroyed?.Invoke();
                RaiseEconomyChanged();
            }
        }
    }
}
