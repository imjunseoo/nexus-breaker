using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    /// <summary>
    /// 의뢰(v0.2): 3칸이 돌아가며 10~20분짜리 짧은 목표를 준다. 목표·보상은 지금 경제 속도로 계산해서
    /// 초반이든 후반이든 비슷한 시간이 걸린다. 오프라인 판매도 진행에 들어간다.
    /// </summary>
    public sealed partial class GameModel
    {
        public event Action ContractsChanged;
        public event Action<int> ContractCompleted;           // 칸 번호
        public event Action<int, ContractState> ContractClaimed;

        public IReadOnlyList<ContractState> Contracts => Data.contracts;
        public bool ContractsUnlocked => Data.tutorialStep < 0;
        public bool CanRerollContract => Data.playTime >= Data.contractRerollAt;
        public double RerollCooldownLeft => Math.Max(0d, Data.contractRerollAt - Data.playTime);

        public int ClaimableContracts
        {
            get { int n = 0; foreach (var c in Data.contracts) if (c != null && c.IsComplete) n++; return n; }
        }

        void EnsureContracts()
        {
            while (Data.contracts.Count < Stats.ContractSlots) Data.contracts.Add(new ContractState { readyAt = 0d });
            while (Data.contracts.Count > Stats.ContractSlots) Data.contracts.RemoveAt(Data.contracts.Count - 1);
            for (int i = 0; i < Data.contracts.Count; i++)
            {
                var c = Data.contracts[i];
                if (c == null || (!c.IsEmpty && !ContractText.TryKind(c, out _)) || (!c.IsEmpty && c.goal <= 0d))
                    Data.contracts[i] = new ContractState { readyAt = Data.playTime };
            }
        }

        void TickContracts()
        {
            if (!ContractsUnlocked) return;
            bool changed = false;
            for (int i = 0; i < Data.contracts.Count; i++)
            {
                var c = Data.contracts[i];
                if (!c.IsEmpty || Data.playTime < c.readyAt) continue;
                Data.contracts[i] = GenerateContract(i, null);
                changed = true;
            }
            if (changed) ContractsChanged?.Invoke();
        }

        // ───────────────────────── 진행 ─────────────────────────

        /// <summary>판매·박스·클릭·약점이 일어날 때 호출. amount만큼 맞는 의뢰를 진행시킨다.</summary>
        void OnContractEvent(ContractKind kind, string target, double amount)
        {
            if (!ContractsUnlocked || amount <= 0d) return;
            for (int i = 0; i < Data.contracts.Count; i++)
            {
                var c = Data.contracts[i];
                if (!ContractText.TryKind(c, out var k) || k != kind || c.IsComplete) continue;
                if (!Matches(kind, c.target, target)) continue;
                c.progress = Math.Min(c.goal, c.progress + amount);
                if (c.IsComplete) ContractCompleted?.Invoke(i);
                ContractsChanged?.Invoke();
            }
        }

        static bool Matches(ContractKind kind, string want, string got)
        {
            switch (kind)
            {
                case ContractKind.SellCategory:
                case ContractKind.SellItem:
                    return want == got;
                case ContractKind.OpenBoxes:
                    int wg = int.TryParse(want, out var a) ? a : 0;
                    int gg = int.TryParse(got, out var b) ? b : 0;
                    return gg >= wg;
                default:
                    return true;
            }
        }

        void OnSaleForContracts(ItemDef item, double price, double market)
        {
            OnContractEvent(ContractKind.SellCategory, item.Category.ToString(), 1d);
            OnContractEvent(ContractKind.SellItem, item.Id, 1d);
            // 반올림 오차 허용(시장가 ×1.3을 그대로 입력하면 인정)
            if (price >= market * Stats.ContractPremiumMult - 0.5d) OnContractEvent(ContractKind.SellPremium, null, 1d);
            OnContractEvent(ContractKind.EarnNuts, null, price);
        }

        // ───────────────────────── 보상·바꾸기 ─────────────────────────

        public bool ClaimContract(int slot)
        {
            if (slot < 0 || slot >= Data.contracts.Count) return false;
            var c = Data.contracts[slot];
            if (!c.IsComplete) return false;

            if (c.rewardNuts > 0d)
            {
                Data.nuts += c.rewardNuts;
                Data.run.nutsEarned += c.rewardNuts;
                Data.lifetime.nutsEarned += c.rewardNuts;
            }
            Data.run.contractsDone++;
            Data.lifetime.contractsDone++;
            Data.contracts[slot] = new ContractState { readyAt = Data.playTime + Stats.ContractRefillDelay };

            ContractClaimed?.Invoke(slot, c);
            // 박스 보상은 창고가 가득 차도 들어온다(지층 박스와 같은 규칙) → 땅에 떨어지는 연출
            if (c.rewardBoxGrade >= 0) TryDropBox((Grade)Math.Min(c.rewardBoxGrade, GradeInfo.Count - 1), milestone: true, emitEvents: true);
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>무료 바꾸기(5분에 한 번): 진행 중인 의뢰를 다른 종류로 바꾼다.</summary>
        public bool RerollContract(int slot)
        {
            if (!CanRerollContract || slot < 0 || slot >= Data.contracts.Count) return false;
            var c = Data.contracts[slot];
            if (c.IsEmpty || c.IsComplete) return false;
            ContractText.TryKind(c, out var old);
            Data.contracts[slot] = GenerateContract(slot, old);
            Data.contractRerollAt = Data.playTime + Stats.ContractRerollCooldown;
            ContractsChanged?.Invoke();
            return true;
        }

        /// <summary>환생: 경제 규모가 바뀌므로 의뢰를 새로 받는다.</summary>
        void ResetContractsForPrestige()
        {
            for (int i = 0; i < Data.contracts.Count; i++) Data.contracts[i] = new ContractState { readyAt = Data.playTime + 5d };
            Data.contractRerollAt = 0d;
            ContractsChanged?.Invoke();
        }

        // ───────────────────────── 생성 ─────────────────────────

        /// <summary>테스트·자동 검증용: 모든 빈 칸을 즉시 채운다.</summary>
        public void FillContractsNow()
        {
            for (int i = 0; i < Data.contracts.Count; i++)
                if (Data.contracts[i].IsEmpty) Data.contracts[i] = GenerateContract(i, null);
            ContractsChanged?.Invoke();
        }

        ContractState GenerateContract(int slot, ContractKind? avoid)
        {
            // 다른 칸과 같은 종류는 피한다
            var used = new HashSet<ContractKind>();
            for (int i = 0; i < Data.contracts.Count; i++)
                if (i != slot && ContractText.TryKind(Data.contracts[i], out var k)) used.Add(k);
            if (avoid.HasValue) used.Add(avoid.Value);

            var kinds = new List<ContractKind>();
            var weights = new List<double>();
            void Add(ContractKind k, double w) { if (!used.Contains(k) && w > 0d) { kinds.Add(k); weights.Add(w); } }
            Add(ContractKind.SellCategory, 3d);
            Add(ContractKind.SellItem, PickKnownItem(peek: true) != null ? 2d : 0d);
            Add(ContractKind.SellPremium, 2d);
            Add(ContractKind.OpenBoxes, 2d);
            Add(ContractKind.EarnNuts, 2d);
            Add(ContractKind.Clicks, 1d);
            Add(ContractKind.WeakPoints, Data.lifetime.weakPoints > 0 ? 1d : 0d);
            if (kinds.Count == 0) { kinds.Add(ContractKind.EarnNuts); weights.Add(1d); }

            var kind = kinds[Math.Max(0, Rng.WeightedIndex(weights))];
            var c = new ContractState { kind = kind.ToString(), rewardBoxGrade = -1 };
            double m = Stats.ContractTargetMinutes;
            double sold = ItemsSoldPerMinute;

            switch (kind)
            {
                case ContractKind.SellCategory:
                {
                    var cat = PickCategory(out double share, out double avgPrice);
                    c.target = cat.ToString();
                    c.goal = Clamp(Math.Round(sold * m * share), 3, 60);
                    c.rewardNuts = NiceRound(c.goal * avgPrice * Stats.ContractSellRewardShare);
                    c.requester = ContractText.RequesterFor(cat);
                    break;
                }
                case ContractKind.SellItem:
                {
                    var item = PickKnownItem(peek: false) ?? ItemCatalog.All[0];
                    c.target = item.Id;
                    c.goal = Clamp(Math.Round(sold * m * DropShare(item)), 2, 30);
                    c.rewardNuts = NiceRound(c.goal * MarketPrice(item) * Stats.ContractSellRewardShare * 1.25d);
                    c.requester = ContractText.RequesterFor(item.Category);
                    break;
                }
                case ContractKind.SellPremium:
                {
                    double premiumRate = Math.Min(BoxesPerMinute, VisitsPerMinute * 0.3d);
                    c.goal = Clamp(Math.Round(premiumRate * m * 0.5d), 3, 30);
                    c.rewardNuts = NiceRound(c.goal * AvgDropValue * 0.75d);
                    c.requester = "merchant";
                    break;
                }
                case ContractKind.OpenBoxes:
                {
                    int grade = PickBoxGradeTarget(out double share);
                    c.target = grade.ToString();
                    c.goal = Clamp(Math.Round(BoxesPerMinute * m * share), 3, 80);
                    c.rewardBoxGrade = Math.Max((int)RollBoxGrade(milestone: true), Math.Min(grade + 1, GradeInfo.Count - 1));
                    c.requester = grade >= (int)Grade.Rare ? "collector" : "merchant";
                    break;
                }
                case ContractKind.EarnNuts:
                {
                    c.goal = Math.Max(100d, NiceRound(sold * m * AvgDropValue));
                    c.rewardNuts = NiceRound(c.goal * Stats.ContractEarnRewardShare);
                    c.requester = "survivor_b";
                    break;
                }
                case ContractKind.Clicks:
                {
                    c.goal = new[] { 40d, 60d, 80d }[Rng.Range(0, 3)];
                    c.rewardBoxGrade = (int)RollBoxGrade(milestone: true);
                    c.requester = "soldier";
                    break;
                }
                case ContractKind.WeakPoints:
                {
                    c.goal = 2d;
                    c.rewardBoxGrade = Math.Min((int)RollBoxGrade(milestone: true) + 1, GradeInfo.Count - 1);
                    c.requester = "cultist";
                    break;
                }
            }
            if (c.rewardNuts > 0d) c.rewardNuts = Math.Max(30d, c.rewardNuts);
            return c;
        }

        // ───────────────────────── 경제 속도 추정 ─────────────────────────

        double HitsPerSecond => 1d / SwingInterval + HelperCount / HelperSwingInterval;
        public double BoxesPerMinute => 60d * HitsPerSecond / Stats.HitsPerBox(Level(UpgradeId.BoxRate));
        public double VisitsPerMinute => 60d / CustomerInterval;
        /// <summary>팔리는 속도 ≈ min(박스가 나오는 속도, 손님의 60%가 사는 속도).</summary>
        public double ItemsSoldPerMinute => Math.Max(0.5d, Math.Min(BoxesPerMinute, VisitsPerMinute * 0.6d));

        /// <summary>지금 지층에서 박스 하나를 열었을 때 이 물건이 나올 확률.</summary>
        public double DropShare(ItemDef item)
        {
            var w = Stats.StratumGradeWeights[StratumNow];
            double sum = 0d; foreach (var x in w) sum += x;
            int g = (int)item.Grade;
            if (sum <= 0d || w[g] <= 0d) return 0d;
            double wSum = 0d;
            foreach (var it in ItemCatalog.All) if (it.Grade == item.Grade) wSum += 1d / Math.Sqrt(it.BasePrice);
            return w[g] / sum * (1d / Math.Sqrt(item.BasePrice)) / wSum;
        }

        /// <summary>박스 하나에서 나오는 물건의 평균 시장가.</summary>
        public double AvgDropValue
        {
            get
            {
                double v = 0d;
                foreach (var it in ItemCatalog.All) v += DropShare(it) * MarketPrice(it);
                return v;
            }
        }

        ItemCategory PickCategory(out double share, out double avgPrice)
        {
            var shares = new Dictionary<ItemCategory, double>();
            var values = new Dictionary<ItemCategory, double>();
            foreach (var it in ItemCatalog.All)
            {
                double s = DropShare(it);
                if (s <= 0d) continue;
                shares.TryGetValue(it.Category, out var a); shares[it.Category] = a + s;
                values.TryGetValue(it.Category, out var b); values[it.Category] = b + s * MarketPrice(it);
            }
            var cats = new List<ItemCategory>();
            var weights = new List<double>();
            foreach (var kv in shares) if (kv.Value >= 0.05d) { cats.Add(kv.Key); weights.Add(kv.Value); }
            if (cats.Count == 0) { cats.Add(ItemCategory.Food); weights.Add(1d); shares[ItemCategory.Food] = 0.3d; values[ItemCategory.Food] = 6d; }
            var cat = cats[Math.Max(0, Rng.WeightedIndex(weights))];
            share = shares[cat];
            avgPrice = values.TryGetValue(cat, out var val) && share > 0d ? val / share : 20d;
            return cat;
        }

        /// <summary>도감에 있는(본 적 있는) 물건 중 지금 자주 나오는 것 하나.</summary>
        ItemDef PickKnownItem(bool peek)
        {
            var items = new List<ItemDef>();
            var weights = new List<double>();
            foreach (var id in Data.codex)
            {
                var it = ItemCatalog.Get(id);
                if (it == null) continue;
                double s = DropShare(it);
                if (s < 0.03d) continue;
                items.Add(it); weights.Add(s);
            }
            if (items.Count == 0) return null;
            if (peek) return items[0];
            return items[Math.Max(0, Rng.WeightedIndex(weights))];
        }

        /// <summary>박스 의뢰 등급: 이 지층에서 15% 이상 나오는 가장 높은 등급(가끔은 아무 박스).</summary>
        int PickBoxGradeTarget(out double share)
        {
            var w = Stats.StratumGradeWeights[StratumNow];
            double sum = 0d; foreach (var x in w) sum += x;
            int best = 0;
            for (int g = 1; g < w.Length; g++)
            {
                double atLeast = 0d;
                for (int k = g; k < w.Length; k++) atLeast += w[k];
                if (atLeast / sum >= 0.15d) best = g;
            }
            int grade = best > 0 && Rng.Chance(0.6d) ? best : 0;
            double s = 0d;
            for (int k = grade; k < w.Length; k++) s += w[k];
            share = s / sum;
            return grade;
        }

        static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

        /// <summary>보기 좋은 숫자로: 100 미만은 5 단위, 그 이상은 유효숫자 2자리.</summary>
        public static double NiceRound(double v)
        {
            if (v <= 0d) return 0d;
            if (v < 100d) return Math.Max(5d, Math.Round(v / 5d) * 5d);
            double mag = Math.Pow(10d, Math.Floor(Math.Log10(v)) - 1d);
            return Math.Round(v / mag) * mag;
        }
    }
}
