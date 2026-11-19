using System;

namespace NexusBreaker
{
    /// <summary>의뢰 종류(v0.2). 이름이 세이브에 문자열로 저장되므로 바꾸지 말 것.</summary>
    public enum ContractKind { SellCategory, SellItem, SellPremium, OpenBoxes, EarnNuts, Clicks, WeakPoints }

    /// <summary>의뢰 문구·아이콘·의뢰인. 수치(목표·보상)는 GameModel.Contracts에서 정한다.</summary>
    public static class ContractText
    {
        public static string CategoryName(ItemCategory c) => c switch
        {
            ItemCategory.Food => "먹을 것",
            ItemCategory.Smoke => "담배",
            ItemCategory.Supply => "생활용품",
            ItemCategory.Medical => "의약품",
            ItemCategory.Junk => "잡동사니",
            ItemCategory.Antique => "골동품",
            ItemCategory.Tech => "기계 부품",
            ItemCategory.Alien => "외계 물건",
            _ => "유물",
        };

        /// <summary>카테고리별 의뢰인(손님 id).</summary>
        public static string RequesterFor(ItemCategory c) => c switch
        {
            ItemCategory.Food => "survivor_a",
            ItemCategory.Smoke => "raider",
            ItemCategory.Supply => "soldier",
            ItemCategory.Medical => "survivor_b",
            ItemCategory.Junk => "merchant",
            ItemCategory.Antique => "collector",
            ItemCategory.Tech => "merchant",
            ItemCategory.Alien => "cultist",
            _ => "collector",
        };

        public static bool TryKind(ContractState c, out ContractKind kind)
        {
            kind = default;
            return c != null && !c.IsEmpty && Enum.TryParse(c.kind, out kind);
        }

        public static string RequesterName(ContractState c) => CustomerCatalog.Get(c?.requester)?.Name ?? "생존자";

        /// <summary>의뢰 제목(한두 줄).</summary>
        public static string Title(ContractState c)
        {
            if (!TryKind(c, out var kind)) return "";
            long n = (long)Math.Round(c.goal);
            switch (kind)
            {
                case ContractKind.SellCategory:
                    return Enum.TryParse(c.target, out ItemCategory cat) ? $"{CategoryName(cat)} {n}개 팔기" : $"물건 {n}개 팔기";
                case ContractKind.SellItem:
                    return $"{ItemCatalog.Get(c.target)?.Name ?? "물건"} {n}개 팔기";
                case ContractKind.SellPremium:
                    return $"시장가 ×{Stats.ContractPremiumMult:0.0} 이상으로 {n}개 팔기";
                case ContractKind.OpenBoxes:
                    int g = int.TryParse(c.target, out var gi) ? gi : 0;
                    return g <= 0 ? $"박스 {n}개 열기" : $"{GradeInfo.Names[Math.Min(g, GradeInfo.Count - 1)]} 이상 박스 {n}개 열기";
                case ContractKind.EarnNuts:
                    return $"판매로 {BigNum.Price(c.goal)} 너트 벌기";
                case ContractKind.Clicks:
                    return $"넥서스를 직접 {n}번 때리기";
                case ContractKind.WeakPoints:
                    return $"넥서스 약점 {n}번 깨기";
            }
            return "";
        }

        /// <summary>의뢰인 한마디(카드 아래 작은 글씨).</summary>
        public static string Quote(ContractState c)
        {
            if (!TryKind(c, out var kind)) return "";
            return kind switch
            {
                ContractKind.SellCategory => "\"꼭 필요해. 부탁할게.\"",
                ContractKind.SellItem => "\"그거 구할 수 있겠어?\"",
                ContractKind.SellPremium => "\"싸게 팔면 시세가 무너져.\"",
                ContractKind.OpenBoxes => "\"뭐가 나오는지 보고 싶군.\"",
                ContractKind.EarnNuts => "\"장사가 잘 돼야 마을이 산다.\"",
                ContractKind.Clicks => "\"그 망치 솜씨 좀 보자고.\"",
                _ => "\"넥서스의 상처를… 보여다오.\"",
            };
        }

        /// <summary>ui_icons 태그.</summary>
        public static string Icon(ContractState c)
        {
            if (!TryKind(c, out var kind)) return "contract";
            return kind switch
            {
                ContractKind.SellCategory => "shelf",
                ContractKind.SellItem => "shelf",
                ContractKind.SellPremium => "nut",
                ContractKind.OpenBoxes => "box",
                ContractKind.EarnNuts => "nut",
                ContractKind.Clicks => "hammer",
                _ => "weakpoint",
            };
        }

        public static string Reward(ContractState c)
        {
            if (c == null || c.IsEmpty) return "";
            string s = "";
            if (c.rewardNuts > 0) s = $"{BigNum.Price(c.rewardNuts)} 너트";
            if (c.rewardBoxGrade >= 0)
            {
                string box = $"{GradeInfo.Names[Math.Min(c.rewardBoxGrade, GradeInfo.Count - 1)]} 박스";
                s = s.Length > 0 ? s + " + " + box : box;
            }
            return s;
        }
    }
}
