using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    public enum Grade { Common = 0, Uncommon = 1, Rare = 2, Alien = 3, Unknown = 4 }

    public enum ItemCategory { Food, Smoke, Supply, Medical, Junk, Antique, Tech, Alien, Relic }

    public static class GradeInfo
    {
        public const int Count = 5;
        public static readonly string[] Names = { "일반", "고급", "희귀", "외계", "???" };
        public static readonly string[] BoxIds = { "box_common", "box_uncommon", "box_rare", "box_alien", "box_unknown" };
        public static string Name(Grade g) => Names[(int)g];
        public static string BoxId(Grade g) => BoxIds[(int)g];
    }

    // ───────────────────────────── 아이템 ─────────────────────────────

    public sealed class ItemDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Grade Grade;
        public readonly ItemCategory Category;
        public readonly double BasePrice;
        public readonly string Description;

        public ItemDef(string id, string name, Grade grade, ItemCategory category, double basePrice, string description)
        {
            Id = id; Name = name; Grade = grade; Category = category; BasePrice = basePrice; Description = description;
        }
    }

    public static class ItemCatalog
    {
        public static readonly IReadOnlyList<ItemDef> All = new List<ItemDef>
        {
            // 일반 (생필품)
            new ItemDef("snack_shrimp",   "새우맛 과자",        Grade.Common,   ItemCategory.Food,    15,   "봉지가 빵빵하다. 질소는 아직 안 빠졌다."),
            new ItemDef("choco_bun",      "눅눅한 초코빵",      Grade.Common,   ItemCategory.Food,    18,   "유통기한은 문명과 함께 사라졌다."),
            new ItemDef("water_bottle",   "생수",              Grade.Common,   ItemCategory.Supply,  20,   "투명한 물. 요즘 세상엔 사치품."),
            new ItemDef("lighter",        "라이터",            Grade.Common,   ItemCategory.Supply,  22,   "세 번 중 한 번은 켜진다."),
            new ItemDef("cup_ramen",      "컵라면",            Grade.Common,   ItemCategory.Food,    25,   "끓는 물만 있으면 3분 만에 문명의 맛."),
            new ItemDef("bandage",        "붕대",              Grade.Common,   ItemCategory.Medical, 28,   "깨끗한 편이다. 아마도."),
            new ItemDef("battery",        "건전지",            Grade.Common,   ItemCategory.Supply,  30,   "아직 찌릿하다."),
            new ItemDef("canned_food",    "통조림",            Grade.Common,   ItemCategory.Food,    35,   "라벨이 없다. 열어봐야 안다."),
            new ItemDef("cigarettes",     "담배 한 갑",         Grade.Common,   ItemCategory.Smoke,   40,   "폐허의 화폐. 반쯤 남았다."),
            new ItemDef("mask_filter",    "방독면 필터",        Grade.Common,   ItemCategory.Supply,  60,   "모래폭풍이 오면 값이 뛴다."),
            // 고급 (중고품·골동품)
            new ItemDef("diary",          "누군가의 일기장",     Grade.Uncommon, ItemCategory.Antique, 90,   "마지막 장은 '내일은 넥서스를 보러 간다'."),
            new ItemDef("teddy_bear",     "곰인형",            Grade.Uncommon, ItemCategory.Junk,    110,  "한쪽 눈이 단추로 바뀌어 있다."),
            new ItemDef("broken_radio",   "고장난 라디오",      Grade.Uncommon, ItemCategory.Tech,    120,  "가끔 지직거리며 누군가의 목소리가 들린다."),
            new ItemDef("melted_watch",   "반쯤 녹은 손목시계",  Grade.Uncommon, ItemCategory.Antique, 150,  "넥서스가 떨어진 시각에 멈춰 있다."),
            new ItemDef("rusty_toolbox",  "녹슨 공구함",        Grade.Uncommon, ItemCategory.Supply,  180,  "수리공의 눈이 반짝인다."),
            new ItemDef("lp_record",      "LP 레코드",          Grade.Uncommon, ItemCategory.Antique, 200,  "긁힘 없음. 틀 기계가 없을 뿐."),
            new ItemDef("gas_mask",       "방독면",            Grade.Uncommon, ItemCategory.Supply,  220,  "숨 쉴 권리를 판다."),
            // 희귀
            new ItemDef("medkit",         "구급상자",          Grade.Rare,     ItemCategory.Medical, 500,  "내용물이 거의 그대로다."),
            new ItemDef("gold_tooth",     "금니",              Grade.Rare,     ItemCategory.Junk,    600,  "주인은 묻지 않기로 하자."),
            new ItemDef("film_camera",    "필름 카메라",        Grade.Rare,     ItemCategory.Tech,    700,  "마지막 한 장이 남아 있다."),
            new ItemDef("generator_part", "발전기 부품",        Grade.Rare,     ItemCategory.Tech,    800,  "이거 하나면 마을 하나에 불이 켜진다."),
            // 외계
            new ItemDef("glow_jelly",     "발광 젤리",          Grade.Alien,    ItemCategory.Alien,   2200, "먹어도 되는지는 아무도 모른다."),
            new ItemDef("warm_stone",     "계속 따뜻한 돌",      Grade.Alien,    ItemCategory.Alien,   2500, "핵겨울에 이것만 한 게 없다."),
            new ItemDef("zero_g_spoon",   "무중력 숟가락",       Grade.Alien,    ItemCategory.Alien,   2800, "놓으면 둥실 떠오른다. 국은 못 떠먹는다."),
            new ItemDef("humming_cube",   "웅웅거리는 큐브",     Grade.Alien,    ItemCategory.Alien,   3200, "밤마다 조금씩 음이 바뀐다."),
            new ItemDef("alien_remote",   "해석불가 리모컨",     Grade.Alien,    ItemCategory.Alien,   4000, "버튼을 누르면 어딘가에서 뭔가가 켜진다."),
            new ItemDef("crystal_shard",  "넥서스 결정 조각",    Grade.Alien,    ItemCategory.Alien,   5000, "넥서스 숭배 교단이 눈독 들이는 물건."),
            // ???
            new ItemDef("core_fragment",  "넥서스 코어 파편",    Grade.Unknown,  ItemCategory.Relic,   25000, "손에 쥐면 심장 박동이 맞춰진다."),
            new ItemDef("singing_skull",  "노래하는 두개골",     Grade.Unknown,  ItemCategory.Relic,   30000, "사람 것이 아니다. 노래는 꽤 잘한다."),
            new ItemDef("star_map",       "별 지도",           Grade.Unknown,  ItemCategory.Relic,   40000, "지구가 표시되어 있다. 빨간 동그라미로."),
        };

        static Dictionary<string, ItemDef> _byId;

        public static ItemDef Get(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemDef>();
                foreach (var it in All) _byId[it.Id] = it;
            }
            return id != null && _byId.TryGetValue(id, out var def) ? def : null;
        }

        public static List<ItemDef> OfGrade(Grade g)
        {
            var list = new List<ItemDef>();
            foreach (var it in All) if (it.Grade == g) list.Add(it);
            return list;
        }
    }

    // ───────────────────────────── 손님 ─────────────────────────────

    public sealed class CustomerDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly double Median;          // 시장가 대비 지불의향 배수 중앙값
        public readonly double Sigma;           // 로그정규 σ
        public readonly double[] GradeFactor;   // 등급별 추가 배수 (길이 5)
        public readonly ItemCategory[] Likes;   // 선호 카테고리(보는 확률 ↑, 배수 ×1.15)
        public readonly double SpawnWeight;
        public readonly bool RequiresInnerCore; // 내핵(HP 40% 이하) 도달 후 등장

        public CustomerDef(string id, string name, double median, double sigma, double[] gradeFactor,
                           ItemCategory[] likes, double spawnWeight, bool requiresInnerCore = false)
        {
            Id = id; Name = name; Median = median; Sigma = sigma; GradeFactor = gradeFactor;
            Likes = likes; SpawnWeight = spawnWeight; RequiresInnerCore = requiresInnerCore;
        }

        public bool LikesCategory(ItemCategory c) => Array.IndexOf(Likes, c) >= 0;

        /// <summary>이 손님이 해당 아이템에 대해 갖는 WTP 분포의 중앙값(시장가 곱하기 전 배수).</summary>
        public double MedianFor(ItemDef item)
        {
            double m = Median * GradeFactor[(int)item.Grade];
            if (LikesCategory(item.Category)) m *= 1.15d;
            return m;
        }
    }

    public static class CustomerCatalog
    {
        static readonly double[] Flat = { 1, 1, 1, 1, 1 };

        public static readonly IReadOnlyList<CustomerDef> All = new List<CustomerDef>
        {
            new CustomerDef("survivor_a", "생존자",       1.00, 0.35, Flat, new[] { ItemCategory.Food, ItemCategory.Supply, ItemCategory.Medical }, 30),
            new CustomerDef("survivor_b", "생존자",       1.00, 0.35, Flat, new[] { ItemCategory.Food, ItemCategory.Smoke, ItemCategory.Supply }, 30),
            new CustomerDef("merchant",   "떠돌이 상인",   0.80, 0.25, Flat, new[] { ItemCategory.Antique, ItemCategory.Tech, ItemCategory.Junk }, 14),
            new CustomerDef("raider",     "약탈자",       0.60, 0.30, Flat, new[] { ItemCategory.Smoke, ItemCategory.Supply }, 10),
            new CustomerDef("soldier",    "군 잔당",      1.05, 0.30, Flat, new[] { ItemCategory.Supply, ItemCategory.Food, ItemCategory.Medical }, 10),
            new CustomerDef("cultist",    "넥서스 교단",   1.00, 0.40, new double[] { 0.9, 1.0, 1.2, 2.0, 2.2 }, new[] { ItemCategory.Alien, ItemCategory.Relic }, 6),
            new CustomerDef("collector",  "수집가",       1.00, 0.60, new double[] { 1.2, 1.8, 3.0, 3.0, 3.5 }, new[] { ItemCategory.Antique, ItemCategory.Relic, ItemCategory.Alien }, 3),
            new CustomerDef("alien",      "변장한 외계인", 1.00, 0.50, new double[] { 0.7, 0.8, 1.0, 5.0, 5.0 }, new[] { ItemCategory.Alien, ItemCategory.Relic }, 1, requiresInnerCore: true),
        };

        public static CustomerDef Get(string id)
        {
            foreach (var c in All) if (c.Id == id) return c;
            return null;
        }
    }

    // ───────────────────────────── 업그레이드 ─────────────────────────────

    public enum UpgradeId { Hammer, SwingSpeed, Helpers, HelperPower, Crit, BoxRate, BoxLuck, Warehouse, Shelf, Traffic, OfflineCap }

    public sealed class UpgradeDef
    {
        public readonly UpgradeId Id;
        public readonly string Name;
        public readonly string Icon;       // ui_icons 슬라이스 이름
        public readonly double BaseCost;
        public readonly double Growth;
        public readonly int MaxLevel;      // 0 = 무제한
        public readonly string Label;               // 효과 이름(짧게)
        public readonly Func<int, string> Value;    // 레벨 → 효과 값

        public UpgradeDef(UpgradeId id, string name, string icon, double baseCost, double growth, int maxLevel, string label, Func<int, string> value)
        {
            Id = id; Name = name; Icon = icon; BaseCost = baseCost; Growth = growth; MaxLevel = maxLevel; Label = label; Value = value;
        }

        public string Describe(int level) => $"{Label} {Value(level)}";

        /// <summary>"타격 ×901% → ×1,020%" 형식(최대 레벨이면 현재 값만).</summary>
        public string DescribeNext(int level) => IsMaxed(level) ? Describe(level) : $"{Label} {Value(level)} → {Value(level + 1)}";

        public double CostAt(int level) => Math.Ceiling(BaseCost * Math.Pow(Growth, level));
        public bool IsMaxed(int level) => MaxLevel > 0 && level >= MaxLevel;
    }

    public static class UpgradeCatalog
    {
        public static readonly IReadOnlyList<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef(UpgradeId.Hammer,      "망치 강화",     "hammer",    15,   1.15, 0,  "타격", l => "×" + BigNum.Mult(Stats.HammerMult(l))),
            new UpgradeDef(UpgradeId.SwingSpeed,  "손놀림",        "speed",     60,   1.30, 30, "스윙", l => $"{Stats.SwingInterval(l):0.00}초"),
            new UpgradeDef(UpgradeId.Helpers,     "조수 고용",     "helper",    250,  2.20, 5,  "조수", l => $"{l}명"),
            new UpgradeDef(UpgradeId.HelperPower, "조수 장비",     "helper",    400,  1.18, 0,  "조수 타격", l => "×" + BigNum.Mult(Stats.HelperPowerMult(l))),
            new UpgradeDef(UpgradeId.Crit,        "급소 찾기",     "crit",      150,  1.45, 20, "치명타", l => $"{Stats.CritChance(l) * 100:0}%"),
            new UpgradeDef(UpgradeId.BoxRate,     "잔해 뒤지기",   "box",       120,  1.40, 15, "박스", l => $"{Stats.HitsPerBox(l)}타마다"),
            new UpgradeDef(UpgradeId.BoxLuck,     "감별사의 눈",   "box",       500,  1.60, 15, "등급업", l => $"+{Stats.BoxLuck(l) * 100:0}%"),
            new UpgradeDef(UpgradeId.Warehouse,   "창고 확장",     "warehouse", 80,   1.35, 30, "창고", l => $"{Stats.WarehouseCap(l)}칸"),
            new UpgradeDef(UpgradeId.Shelf,       "진열대 증설",   "shelf",     200,  1.80, 18, "진열대", l => $"{Stats.ShelfSlots(l)}칸"),
            new UpgradeDef(UpgradeId.Traffic,     "간판 달기",     "customer",  180,  1.35, 25, "손님", l => $"{Stats.CustomerInterval(l, 0):0.0}초마다"),
            new UpgradeDef(UpgradeId.OfflineCap,  "야간 경비",     "settings",  1000, 1.90, 8,  "오프라인", l => $"{Stats.OfflineCapHours(l):0}시간"),
        };

        public static UpgradeDef Get(UpgradeId id)
        {
            foreach (var u in All) if (u.Id == id) return u;
            return null;
        }
    }

    // ───────────────────────────── 시장 이벤트 ─────────────────────────────

    public sealed class MarketEventDef
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly double Multiplier;
        public readonly Func<ItemDef, bool> Affects;

        public MarketEventDef(string id, string title, string description, double multiplier, Func<ItemDef, bool> affects)
        {
            Id = id; Title = title; Description = description; Multiplier = multiplier; Affects = affects;
        }
    }

    public static class MarketEventCatalog
    {
        public static readonly IReadOnlyList<MarketEventDef> All = new List<MarketEventDef>
        {
            new MarketEventDef("sandstorm", "모래폭풍 접근", "생수·방독면 수요 폭증", 1.8,
                i => i.Id == "water_bottle" || i.Id == "mask_filter" || i.Id == "gas_mask"),
            new MarketEventDef("blackout", "대정전", "건전지·라이터·발전기 부품 값 상승", 1.7,
                i => i.Id == "battery" || i.Id == "lighter" || i.Id == "generator_part"),
            new MarketEventDef("famine", "식량난", "먹을 것 값이 오른다", 1.6,
                i => i.Category == ItemCategory.Food),
            new MarketEventDef("cult_festival", "교단 축제", "외계 물건 열풍", 1.5,
                i => i.Category == ItemCategory.Alien || i.Category == ItemCategory.Relic),
            new MarketEventDef("flood", "물자 범람", "생필품이 넘쳐나 값이 내려간다", 0.7,
                i => i.Grade == Grade.Common),
        };

        public static MarketEventDef Get(string id)
        {
            foreach (var e in All) if (e.Id == id) return e;
            return null;
        }
    }

    // ───────────────────────────── 지역(환생) ─────────────────────────────

    public static class RegionCatalog
    {
        public static readonly string[] Names = { "폐허 도시", "사막 고속도로", "핵겨울 항구", "궤도 엘리베이터 잔해" };

        public static string Name(int region) =>
            region < Names.Length ? Names[region] : $"미지의 지역 {region - Names.Length + 1}";

        public static double MaxHp(int region) => 1e9 * Math.Pow(10d, region);
    }
}
