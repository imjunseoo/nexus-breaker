using System;

namespace NexusBreaker
{
    public sealed partial class GameModel
    {
        void EnsureMarket()
        {
            foreach (var item in ItemCatalog.All)
            {
                bool found = false;
                foreach (var m in Data.market) if (m.id == item.Id) { found = true; break; }
                if (!found) Data.market.Add(new MarketEntry { id = item.Id, mult = 1d });
            }
        }

        MarketEntry Entry(string id)
        {
            foreach (var m in Data.market) if (m.id == id) return m;
            var e = new MarketEntry { id = id, mult = 1d };
            Data.market.Add(e);
            return e;
        }

        public MarketEventDef ActiveEvent =>
            string.IsNullOrEmpty(Data.activeEventId) ? null : MarketEventCatalog.Get(Data.activeEventId);

        /// <summary>현재 시장가 = 기준가 × 랜덤워크 배수 × 이벤트 배수.</summary>
        public double MarketPrice(ItemDef item)
        {
            if (item == null) return 0d;
            double p = item.BasePrice * Entry(item.Id).mult;
            var ev = ActiveEvent;
            if (ev != null && ev.Affects(item)) p *= ev.Multiplier;
            return Math.Max(1d, Math.Round(p));
        }

        public double MarketPrice(string itemId) => MarketPrice(ItemCatalog.Get(itemId));

        /// <summary>기준가 대비 현재 추세(+면 상승).</summary>
        public double MarketTrend(ItemDef item) => item == null ? 0d : MarketPrice(item) / item.BasePrice - 1d;

        void TickMarket(double dt)
        {
            if (!string.IsNullOrEmpty(Data.activeEventId))
            {
                Data.activeEventRemaining -= dt;
                if (Data.activeEventRemaining <= 0d)
                {
                    Data.activeEventId = null;
                    Data.activeEventRemaining = 0d;
                    MarketEventEnded?.Invoke();
                    RaiseToast("시장이 진정됐다.");
                    RaiseEconomyChanged();
                }
            }

            Data.marketTimer += dt;
            while (Data.marketTimer >= Stats.MarketStepSeconds)
            {
                Data.marketTimer -= Stats.MarketStepSeconds;
                MarketStep(emitEvents: true);
            }
        }

        /// <summary>시세 한 단계: 로그 공간 평균회귀 랜덤워크 + 확률적 이벤트.</summary>
        void MarketStep(bool emitEvents)
        {
            double lnMin = Math.Log(Stats.MarketMinMult), lnMax = Math.Log(Stats.MarketMaxMult);
            foreach (var m in Data.market)
            {
                double ln = Math.Log(Math.Max(1e-6, m.mult));
                ln = 0.85d * ln + Stats.MarketStepSize * Rng.Normal();
                m.mult = Math.Exp(Math.Max(lnMin, Math.Min(lnMax, ln)));
            }

            if (string.IsNullOrEmpty(Data.activeEventId) && Rng.Chance(Stats.MarketEventChance))
            {
                var all = MarketEventCatalog.All;
                var ev = all[Rng.Range(0, all.Count)];
                Data.activeEventId = ev.Id;
                Data.activeEventRemaining = Stats.MarketEventSeconds;
                if (emitEvents)
                {
                    MarketEventStarted?.Invoke(ev);
                    RaiseToast($"[시장] {ev.Title} — {ev.Description}");
                }
            }
            RaiseEconomyChanged();
        }
    }
}
