using System;

namespace NexusBreaker
{
    public sealed class OfflineReport
    {
        public double Seconds;
        public double Damage;
        public int[] Boxes = new int[GradeInfo.Count];
        public int ItemsSold;
        public double NutsEarned;
        public bool NexusDestroyed;
        public bool CappedByWarehouse;
        public bool HitTimeCap;
    }

    public sealed partial class GameModel
    {
        // ───────────────────────── 업그레이드 ─────────────────────────

        public double UpgradeCost(UpgradeId id) => UpgradeCatalog.Get(id).CostAt(Level(id));
        public bool IsMaxed(UpgradeId id) => UpgradeCatalog.Get(id).IsMaxed(Level(id));
        public bool CanAfford(UpgradeId id) => !IsMaxed(id) && Data.nuts >= UpgradeCost(id);

        public bool TryBuyUpgrade(UpgradeId id)
        {
            if (!CanAfford(id)) return false;
            Data.nuts -= UpgradeCost(id);
            int lv = Level(id) + 1;
            SetLevel(id, lv);
            if (id == UpgradeId.Shelf) EnsureShelfSize();
            Upgraded?.Invoke(id, lv);
            RaiseEconomyChanged();
            return true;
        }

        // ───────────────────────── 환생 ─────────────────────────

        public int CoresOnPrestige => Stats.CoresForRegion(Data.region);

        /// <summary>넥서스 파괴 후 다음 지역으로. 코어·도감·통계·설정은 유지.</summary>
        public bool Prestige()
        {
            if (!Data.nexusDestroyed) return false;

            Data.cores += CoresOnPrestige;
            Data.region++;
            Data.nexusMaxHp = RegionCatalog.MaxHp(Data.region);
            Data.nexusHp = Data.nexusMaxHp;
            Data.nexusDestroyed = false;
            Data.nextMilestonePercent = 99;
            Data.hitsSinceBox = 0;

            Data.nuts = 0d;
            Data.upgrades.Clear();
            Array.Clear(Data.boxes, 0, Data.boxes.Length);
            Data.warehouse.Clear();
            Data.shelf.Clear();
            EnsureShelfSize();
            foreach (var m in Data.market) m.mult = 1d;
            Data.activeEventId = null;
            Data.activeEventRemaining = 0d;
            Data.run = new RunStats();

            _pendingImpacts.Clear();
            _visits.Clear();
            _lastStage = DamageStageNow;
            _lastStratum = StratumNow;
            WeakPointActive = false;
            ScheduleNextWeakPoint();
            ResetContractsForPrestige();

            Prestiged?.Invoke();
            RaiseToast($"{RegionCatalog.Name(Data.region)}에 도착했다. 넥서스 체력 {BigNum.Format(Data.nexusMaxHp)}");
            RaiseEconomyChanged();
            return true;
        }

        // ───────────────────────── 오프라인 ─────────────────────────

        public double OfflineCapSeconds => Stats.OfflineCapHours(Level(UpgradeId.OfflineCap)) * 3600d;

        /// <summary>
        /// 꺼져 있던 시간만큼 빠르게 진행(연출 이벤트 없이). 타격은 묶음 단위 기대값으로, 손님은 1명씩 판정.
        /// </summary>
        public OfflineReport SimulateOffline(double seconds)
        {
            var report = new OfflineReport();
            if (seconds <= 0d) return report;
            if (seconds > OfflineCapSeconds) { seconds = OfflineCapSeconds; report.HitTimeCap = true; }
            report.Seconds = seconds;

            int[] boxesBefore = (int[])Data.boxes.Clone();
            double nutsBefore = Data.nuts;
            long soldBefore = Data.run.itemsSold;
            double hpBefore = Data.nexusHp;

            // 1) 타격: 1초 단위 묶음(지층·박스 확률이 진행에 따라 바뀌도록)
            double t = 0d;
            const double chunk = 1d;
            double swingFrac = 0d, helperFrac = 0d;
            while (t < seconds && !Data.nexusDestroyed)
            {
                double step = Math.Min(chunk, seconds - t);
                t += step;
                swingFrac += step / SwingInterval;
                helperFrac += step * HelperCount / HelperSwingInterval;
                long swings = (long)swingFrac; swingFrac -= swings;
                long helperHits = (long)helperFrac; helperFrac -= helperHits;
                long hits = swings + helperHits;
                if (hits <= 0) continue;
                double dmg = (swings * SwingDamage + helperHits * HelperDamage) * AvgCritFactor;
                if (WarehouseFull) report.CappedByWarehouse = true;
                DealDamage(dmg, hits, emitEvents: false);
            }
            Data.playTime += seconds;

            // 2) 판매: 기대 방문 수만큼 1명씩 판정(최대 5000명)
            int visits = (int)Math.Min(5000d, seconds / CustomerInterval);
            for (int i = 0; i < visits && OccupiedShelfCount > 0; i++)
            {
                var v = new CustomerVisit { Customer = PickCustomer(), Outcome = VisitOutcome.Pending };
                Resolve(v);
            }

            // 3) 시세 진행(최대 100단계)
            Data.marketTimer += seconds;
            int steps = 0;
            while (Data.marketTimer >= Stats.MarketStepSeconds && steps < 100)
            {
                Data.marketTimer -= Stats.MarketStepSeconds;
                MarketStep(emitEvents: false);
                steps++;
            }
            if (Data.marketTimer >= Stats.MarketStepSeconds) Data.marketTimer = 0d;
            if (!string.IsNullOrEmpty(Data.activeEventId))
            {
                Data.activeEventRemaining -= seconds;
                if (Data.activeEventRemaining <= 0d) { Data.activeEventId = null; Data.activeEventRemaining = 0d; }
            }

            for (int g = 0; g < GradeInfo.Count; g++) report.Boxes[g] = Math.Max(0, Data.boxes[g] - boxesBefore[g]);
            report.Damage = hpBefore - Data.nexusHp;
            report.ItemsSold = (int)(Data.run.itemsSold - soldBefore);
            report.NutsEarned = Data.nuts - nutsBefore;
            report.NexusDestroyed = Data.nexusDestroyed;

            _lastStage = DamageStageNow;
            _lastStratum = StratumNow;
            RaiseEconomyChanged();
            return report;
        }
    }
}
