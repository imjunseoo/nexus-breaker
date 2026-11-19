namespace NexusBreaker
{
    /// <summary>도감(v0.2): 발견 여부(codex)와 아이템별 기록(얻은 수·판 수·최고 판매가).</summary>
    public sealed partial class GameModel
    {
        public bool IsDiscovered(string itemId) => Data.codex.Contains(itemId);
        public int DiscoveredCount => Data.codex.Count;

        /// <summary>기록 조회(없으면 null). v0.1 세이브는 발견 목록만 있고 기록은 0부터 시작한다.</summary>
        public CodexEntry CodexOf(string itemId)
        {
            foreach (var e in Data.codexStats) if (e.id == itemId) return e;
            return null;
        }

        CodexEntry CodexEntryFor(string itemId)
        {
            var e = CodexOf(itemId);
            if (e != null) return e;
            e = new CodexEntry { id = itemId };
            Data.codexStats.Add(e);
            return e;
        }

        void RecordFound(ItemDef item) => CodexEntryFor(item.Id).found++;

        void RecordSold(ItemDef item, double price)
        {
            var e = CodexEntryFor(item.Id);
            e.sold++;
            if (price > e.bestPrice) e.bestPrice = price;
        }

        /// <summary>등급별 발견 수 / 전체 수.</summary>
        public void GradeProgress(Grade g, out int found, out int total)
        {
            found = 0; total = 0;
            foreach (var it in ItemCatalog.All)
            {
                if (it.Grade != g) continue;
                total++;
                if (IsDiscovered(it.Id)) found++;
            }
        }
    }
}
