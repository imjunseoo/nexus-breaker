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
        public int windowScale = 3;
        public int windowX = int.MinValue; // MinValue = 기본 위치(작업 영역 오른쪽 아래)
        public int windowY = int.MinValue;
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

        // 기타
        public List<string> codex = new List<string>(); // 발견한 아이템 id
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
