using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    public sealed partial class GameModel
    {
        // ───────────────────────── 창고 ─────────────────────────

        public int WarehouseCap => Stats.WarehouseCap(Level(UpgradeId.Warehouse));

        public int BoxCount
        {
            get { int n = 0; foreach (var b in Data.boxes) n += b; return n; }
        }

        public int ItemCountInWarehouse
        {
            get { int n = 0; foreach (var s in Data.warehouse) n += s.count; return n; }
        }

        /// <summary>창고 사용량 = 미개봉 박스 + 보관 아이템 (진열 중인 물건은 제외).</summary>
        public int WarehouseUsed => BoxCount + ItemCountInWarehouse;
        public bool WarehouseFull => WarehouseUsed >= WarehouseCap;

        public int CountOf(string itemId)
        {
            foreach (var s in Data.warehouse) if (s.id == itemId) return s.count;
            return 0;
        }

        void AddItem(string itemId, int count)
        {
            foreach (var s in Data.warehouse)
                if (s.id == itemId) { s.count += count; return; }
            Data.warehouse.Add(new ItemStack { id = itemId, count = count });
        }

        bool RemoveItem(string itemId, int count)
        {
            for (int i = 0; i < Data.warehouse.Count; i++)
            {
                var s = Data.warehouse[i];
                if (s.id != itemId) continue;
                if (s.count < count) return false;
                s.count -= count;
                if (s.count <= 0) Data.warehouse.RemoveAt(i);
                return true;
            }
            return false;
        }

        // ───────────────────────── 박스 드랍 ─────────────────────────

        bool _warnedFull;

        Grade RollBoxGrade(bool milestone)
        {
            int stratum = StratumNow;
            var weights = Stats.StratumGradeWeights[stratum];
            int idx = Rng.WeightedIndex(weights);
            if (idx < 0) idx = 0;

            // 이 지층에서 나올 수 있는 최고 등급
            int maxInStratum = 0;
            for (int i = 0; i < weights.Length; i++) if (weights[i] > 0d) maxInStratum = i;

            if (milestone) idx = Math.Min(idx + 1, GradeInfo.Count - 1);
            else if (Rng.Chance(Stats.BoxLuck(Level(UpgradeId.BoxLuck)))) idx = Math.Min(idx + 1, maxInStratum);

            return (Grade)idx;
        }

        void TryDropBox(Grade grade, bool milestone, bool emitEvents)
        {
            // 지층(마일스톤) 박스는 창고가 가득 차도 들어온다(보상 보장)
            if (!milestone && WarehouseFull)
            {
                if (emitEvents && !_warnedFull)
                {
                    _warnedFull = true;
                    RaiseToast("창고가 가득 찼다! 박스를 열어 팔거나 창고를 넓히자.");
                }
                return;
            }
            _warnedFull = false;
            Data.boxes[(int)grade]++;
            if (emitEvents) BoxDropped?.Invoke(grade, milestone);
            RaiseEconomyChanged();
        }

        /// <summary>첫 박스가 몇 번만 더 때리면 나오게 한다(튜토리얼용, 이미 타격 기록이 있으면 무시).</summary>
        public void PrimeFirstBox(int hitsLeft = 3)
        {
            if (Data.lifetime.totalHits > 0 || BoxCount > 0) return;
            int perBox = Stats.HitsPerBox(Level(UpgradeId.BoxRate));
            Data.hitsSinceBox = Math.Max(Data.hitsSinceBox, perBox - hitsLeft);
        }

        // ───────────────────────── 박스 열기 ─────────────────────────

        /// <summary>해당 등급 박스 하나를 연다. 없으면 null.</summary>
        public ItemDef OpenBox(Grade grade)
        {
            int g = (int)grade;
            if (Data.boxes[g] <= 0) return null;

            var pool = ItemCatalog.OfGrade(grade);
            if (pool.Count == 0) return null;

            // 같은 등급 안에서는 싼 물건이 더 자주 나온다
            var weights = new List<double>(pool.Count);
            foreach (var it in pool) weights.Add(1d / Math.Sqrt(it.BasePrice));
            var item = pool[Math.Max(0, Rng.WeightedIndex(weights))];

            Data.boxes[g]--;
            AddItem(item.Id, 1);
            bool isNew = !Data.codex.Contains(item.Id);
            if (isNew) Data.codex.Add(item.Id);
            Data.run.boxesOpened++;
            Data.lifetime.boxesOpened++;

            BoxOpened?.Invoke(item, grade, isNew);
            RaiseEconomyChanged();
            return item;
        }

        /// <summary>가장 낮은 등급부터 하나 연다(“하나 열기” 버튼).</summary>
        public ItemDef OpenNextBox()
        {
            for (int g = 0; g < GradeInfo.Count; g++)
                if (Data.boxes[g] > 0) return OpenBox((Grade)g);
            return null;
        }

        public List<ItemDef> OpenAllBoxes()
        {
            var opened = new List<ItemDef>();
            for (int g = 0; g < GradeInfo.Count; g++)
                while (Data.boxes[g] > 0)
                {
                    var it = OpenBox((Grade)g);
                    if (it == null) break;
                    opened.Add(it);
                }
            return opened;
        }
    }
}
