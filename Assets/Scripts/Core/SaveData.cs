using System;
using System.Collections.Generic;

namespace NexusBreaker
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class ShelfSlot
    {
        public string itemId;   // 비어 있으면 null 또는 ""
        public double price;
        public double listedAt; // 진열 시각(게임 시간, 초) — 진열 기간 표시용

        public bool IsEmpty => string.IsNullOrEmpty(itemId);
    }

    [Serializable]
    public class MarketEntry
    {
        public string id;
        public double mult = 1d; // 랜덤워크 배수
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class GameSettings
    {
        public float bgmVolume = 0.6f;
        public float sfxVolume = 0.8f;
        public float ambientVolume = 0.5f;
        public bool transparentBackground = false;
        public bool alwaysOnTop = true;
        public bool clickThrough = false;
        public bool showChat = true;          // 오른쪽 손님 방명록 패널(끄면 창 폭이 게임 화면만큼 줄어든다)
        public int windowScale = 3;
        public int windowX = int.MinValue;      // 창 왼쪽(화면 좌표). MinValue = 기본 위치(작업 영역 오른쪽 아래)
        public int windowY = int.MinValue;      // (구버전) 창 위쪽 — windowBottom이 없을 때만 참고
        public int windowBottom = int.MinValue; // 창 아래쪽 가장자리. 페이지를 열고 닫아도 이 선을 기준으로 위로 늘고 줄어든다
    }

    [Serializable]
    public class RunStats
    {
        public double totalDamage;
        public long totalHits;
        public long boxesOpened;
        public long itemsSold;
        public double nutsEarned;
        public double bestSale;
        public string bestSaleItem;
        public long clicks;           // 플레이어가 직접 때린 횟수 (v0.2)
        public long weakPoints;       // 깬 약점 수 (v0.2)
        public long contractsDone;    // 보상까지 받은 의뢰 수 (v0.2)
    }

    /// <summary>의뢰 한 칸. kind가 비어 있으면 빈 칸(readyAt 이후 새 의뢰가 생긴다).</summary>
    [Serializable]
    public class ContractState
    {
        public string kind;           // ContractKind 이름
        public string target;         // 카테고리 / 아이템 id / 등급 번호 (종류에 따라)
        public string requester;      // 의뢰한 손님 id(말투·이름용)
        public double goal;
        public double progress;
        public double rewardNuts;
        public int rewardBoxGrade = -1; // -1 = 박스 보상 없음
        public double readyAt;        // 빈 칸일 때: 새 의뢰가 생기는 게임 시간(초)

        public bool IsEmpty => string.IsNullOrEmpty(kind);
        public bool IsComplete => !IsEmpty && progress >= goal;
    }

    /// <summary>도감 기록: 얻은 수·판 수·최고 판매가.</summary>
    [Serializable]
    public class CodexEntry
    {
        public string id;
        public long found;
        public long sold;
        public double bestPrice;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;

        // 진행
        public int region;
        public int cores;
        public double nexusMaxHp = 1e9;
        public double nexusHp = 1e9;
        public bool nexusDestroyed;
        public int nextMilestonePercent = 99; // 다음 지층 박스가 나오는 HP %
        public int hitsSinceBox;

        // 경제
        public double nuts;
        public List<UpgradeLevel> upgrades = new List<UpgradeLevel>();
        public int[] boxes = new int[GradeInfo.Count]; // 미개봉 박스(등급별)
        public List<ItemStack> warehouse = new List<ItemStack>();
        public List<ShelfSlot> shelf = new List<ShelfSlot>();
        public List<MarketEntry> market = new List<MarketEntry>();
        public string activeEventId;
        public double activeEventRemaining;
        public double marketTimer;

        // 튜토리얼: 0부터 단계 진행, -1 = 완료 또는 건너뜀
        public int tutorialStep;

        // 의뢰(v0.2): 항상 3칸
        public List<ContractState> contracts = new List<ContractState>();
        public double contractRerollAt;               // 이 게임 시간 이후 무료 바꾸기 가능

        // 기타
        public List<string> codex = new List<string>(); // 발견한 아이템 id
        public List<CodexEntry> codexStats = new List<CodexEntry>(); // (v0.2) 아이템별 기록
        public double playTime;                        // 누적 게임 시간(초)
        public long lastSaveUnix;                      // 마지막 저장 UTC(초)
        public RunStats run = new RunStats();          // 이번 환생 회차 통계
        public RunStats lifetime = new RunStats();     // 전체 통계
        public GameSettings settings = new GameSettings();

        public static SaveData NewGame()
        {
            var d = new SaveData();
            d.nexusMaxHp = RegionCatalog.MaxHp(0);
            d.nexusHp = d.nexusMaxHp;
            return d;
        }
    }
}
