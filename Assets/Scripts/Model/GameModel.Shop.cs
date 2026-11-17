using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    public enum VisitOutcome { Pending, Bought, Rejected, EmptyShelf }

    public sealed class CustomerVisit
    {
        public int Serial;
        public CustomerDef Customer;
        public double WalkRemaining;      // 카운터까지 남은 시간
        public VisitOutcome Outcome;
        public int SlotIndex = -1;        // 구매한 진열대 칸
        public ItemDef Item;              // 구매한 물건
        public double Price;              // 지불 금액
        public double Market;             // 구매 시점 시장가
        public ItemDef LookedAtMostExpensive; // 거절 시 말풍선용
    }

    public enum SellSpeed { Instant, Fast, Normal, Slow, VerySlow, Miracle }

    public sealed partial class GameModel
    {
        public const double CustomerWalkTime = 3.0d;
        public const int MaxConcurrentVisits = 3;

        double _customerTimer;
        int _visitSerial;
        readonly List<CustomerVisit> _visits = new List<CustomerVisit>();
        public IReadOnlyList<CustomerVisit> ActiveVisits => _visits;

        public int ShelfSlots => Stats.ShelfSlots(Level(UpgradeId.Shelf));
        public double CustomerInterval => Stats.CustomerInterval(Level(UpgradeId.Traffic), Data.cores);

        void EnsureShelfSize()
        {
            while (Data.shelf.Count < ShelfSlots) Data.shelf.Add(new ShelfSlot());
        }

        public int OccupiedShelfCount
        {
            get { int n = 0; foreach (var s in Data.shelf) if (!s.IsEmpty) n++; return n; }
        }

        // ───────────────────────── 진열 조작 ─────────────────────────

        /// <summary>창고의 물건 하나를 빈 칸에 진열. slotIndex &lt; 0이면 첫 빈 칸.</summary>
        public bool PlaceOnShelf(string itemId, double price, int slotIndex = -1)
        {
            if (ItemCatalog.Get(itemId) == null || CountOf(itemId) <= 0) return false;
            if (slotIndex < 0) slotIndex = FirstEmptySlot();
            if (slotIndex < 0 || slotIndex >= Data.shelf.Count || !Data.shelf[slotIndex].IsEmpty) return false;

            RemoveItem(itemId, 1);
            var slot = Data.shelf[slotIndex];
            slot.itemId = itemId;
            slot.price = ClampPrice(price);
            slot.listedAt = Data.playTime;
            RaiseEconomyChanged();
            return true;
        }

        public int FirstEmptySlot()
        {
            for (int i = 0; i < Data.shelf.Count; i++) if (Data.shelf[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>진열 취소 → 창고로(창고가 가득 차도 돌려받는다).</summary>
        public bool TakeOffShelf(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Data.shelf.Count) return false;
            var slot = Data.shelf[slotIndex];
            if (slot.IsEmpty) return false;
            AddItem(slot.itemId, 1);
            slot.itemId = null;
            slot.price = 0d;
            RaiseEconomyChanged();
            return true;
        }

        public void SetPrice(int slotIndex, double price)
        {
            if (slotIndex < 0 || slotIndex >= Data.shelf.Count || Data.shelf[slotIndex].IsEmpty) return;
            Data.shelf[slotIndex].price = ClampPrice(price);
            RaiseEconomyChanged();
        }

        static double ClampPrice(double price) => Math.Max(1d, Math.Min(Math.Round(price), 1e15));

        // ───────────────────────── 손님 ─────────────────────────

        void TickCustomers(double dt)
        {
            // 걷는 중인 손님 → 카운터 도착 시 결정
            for (int i = _visits.Count - 1; i >= 0; i--)
            {
                var v = _visits[i];
                v.WalkRemaining -= dt;
                if (v.WalkRemaining <= 0d)
                {
                    _visits.RemoveAt(i);
                    Resolve(v);
                    CustomerResolved?.Invoke(v);
                }
            }

            _customerTimer -= dt;
            if (_customerTimer > 0d) return;
            _customerTimer = Rng.Exponential(CustomerInterval);

            if (_visits.Count >= MaxConcurrentVisits) return; // 줄이 꽉 차면 그냥 지나간다

            var visit = new CustomerVisit
            {
                Serial = ++_visitSerial,
                Customer = PickCustomer(),
                WalkRemaining = CustomerWalkTime,
                Outcome = VisitOutcome.Pending,
            };
            _visits.Add(visit);
            CustomerArrived?.Invoke(visit);
        }

        CustomerDef PickCustomer()
        {
            var list = CustomerCatalog.All;
            var weights = new List<double>(list.Count);
            bool inner = ReachedInnerCore || Data.region > 0;
            foreach (var c in list) weights.Add(c.RequiresInnerCore && !inner ? 0d : c.SpawnWeight);
            int idx = Rng.WeightedIndex(weights);
            return list[Math.Max(0, idx)];
        }

        /// <summary>진열대에서 최대 3개를 보고, 가격 ≤ 지불의향인 것 중 가장 이득인 물건을 산다.</summary>
        void Resolve(CustomerVisit v)
        {
            var occupied = new List<int>();
            for (int i = 0; i < Data.shelf.Count; i++) if (!Data.shelf[i].IsEmpty) occupied.Add(i);
            if (occupied.Count == 0) { v.Outcome = VisitOutcome.EmptyShelf; return; }

            // 볼 칸 고르기(선호 카테고리 가중, 중복 없이)
            var viewed = new List<int>();
            var candidates = new List<int>(occupied);
            int toView = Math.Min(Stats.MaxCustomersViewItems, candidates.Count);
            for (int k = 0; k < toView; k++)
            {
                var w = new List<double>(candidates.Count);
                foreach (int slot in candidates)
                {
                    var def = ItemCatalog.Get(Data.shelf[slot].itemId);
                    w.Add(v.Customer.LikesCategory(def.Category) ? 2.5d : 1d);
                }
                int pick = Rng.WeightedIndex(w);
                viewed.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            int bestSlot = -1;
            double bestSurplus = double.NegativeInfinity;
            double mostExpensive = -1d;
            foreach (int slot in viewed)
            {
                var s = Data.shelf[slot];
                var def = ItemCatalog.Get(s.itemId);
                double market = MarketPrice(def);
                double wtp = Rng.LogNormal(market * v.Customer.MedianFor(def), v.Customer.Sigma);
                if (s.price > mostExpensive) { mostExpensive = s.price; v.LookedAtMostExpensive = def; }
                if (s.price <= wtp)
                {
                    double surplus = (wtp - s.price) / wtp;
                    if (surplus > bestSurplus) { bestSurplus = surplus; bestSlot = slot; }
                }
            }

            if (bestSlot < 0) { v.Outcome = VisitOutcome.Rejected; return; }

            var sold = Data.shelf[bestSlot];
            var item = ItemCatalog.Get(sold.itemId);
            v.Outcome = VisitOutcome.Bought;
            v.SlotIndex = bestSlot;
            v.Item = item;
            v.Price = sold.price;
            v.Market = MarketPrice(item);

            Data.nuts += sold.price;
            Data.run.itemsSold++;
            Data.lifetime.itemsSold++;
            Data.run.nutsEarned += sold.price;
            Data.lifetime.nutsEarned += sold.price;
            if (sold.price > Data.lifetime.bestSale)
            {
                Data.lifetime.bestSale = sold.price;
                Data.lifetime.bestSaleItem = item.Id;
            }
            if (sold.price > Data.run.bestSale)
            {
                Data.run.bestSale = sold.price;
                Data.run.bestSaleItem = item.Id;
            }

            sold.itemId = null;
            sold.price = 0d;
            RaiseEconomyChanged();
        }

        // ───────────────────────── 예상 판매 속도 ─────────────────────────

        /// <summary>
        /// 손님 1명이 방문했을 때 이 가격의 물건을 살 확률(보는 확률 제외).
        /// 손님 타입 가중 평균 × P(로그정규 지불의향 ≥ 가격).
        /// </summary>
        public double BuyChancePerView(ItemDef item, double price)
        {
            double market = MarketPrice(item);
            bool inner = ReachedInnerCore || Data.region > 0;
            double wsum = 0d, psum = 0d;
            foreach (var c in CustomerCatalog.All)
            {
                if (c.RequiresInnerCore && !inner) continue;
                double w = c.SpawnWeight * (c.LikesCategory(item.Category) ? 2.5d : 1d);
                wsum += w;
                psum += w * MathUtil.LogNormalExceed(price, market * c.MedianFor(item), c.Sigma);
            }
            return wsum > 0d ? psum / wsum : 0d;
        }

        /// <summary>이 가격으로 진열했을 때 팔리기까지 기대 시간(초).</summary>
        public double ExpectedSellSeconds(ItemDef item, double price)
        {
            int occupied = Math.Max(1, OccupiedShelfCount);
            double pView = Math.Min(1d, (double)Stats.MaxCustomersViewItems / occupied);
            double p = pView * BuyChancePerView(item, price);
            if (p <= 1e-12) return double.PositiveInfinity;
            return CustomerInterval / p;
        }

        public static SellSpeed ClassifySpeed(double seconds) =>
            seconds < 20d ? SellSpeed.Instant
            : seconds < 90d ? SellSpeed.Fast
            : seconds < 600d ? SellSpeed.Normal
            : seconds < 3600d ? SellSpeed.Slow
            : seconds < 8d * 3600d ? SellSpeed.VerySlow
            : SellSpeed.Miracle;

        public static string SpeedLabel(SellSpeed s) => s switch
        {
            SellSpeed.Instant => "즉시",
            SellSpeed.Fast => "빠름",
            SellSpeed.Normal => "보통",
            SellSpeed.Slow => "느림",
            SellSpeed.VerySlow => "매우 느림",
            _ => "기적",
        };
    }
}
