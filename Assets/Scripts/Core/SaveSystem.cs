using System;
using System.IO;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>JSON 저장/불러오기. 쓰기 도중 종료돼도 깨지지 않게 임시 파일 → 교체 방식.</summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";
        const string BackupName = "save.bak.json";

        /// <summary>테스트·자동 검증용 저장 위치 덮어쓰기(null이면 기본 위치).</summary>
        public static string OverrideDir;

        public static string Dir => string.IsNullOrEmpty(OverrideDir) ? Application.persistentDataPath : OverrideDir;
        public static string PathMain => Path.Combine(Dir, FileName);
        static string PathBackup => Path.Combine(Dir, BackupName);
        static string PathTemp => Path.Combine(Dir, FileName + ".tmp");

        public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static void Save(SaveData data)
        {
            try
            {
                data.lastSaveUnix = NowUnix;
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                Directory.CreateDirectory(Dir);
                File.WriteAllText(PathTemp, json);
                if (File.Exists(PathMain)) File.Copy(PathMain, PathBackup, overwrite: true);
                if (File.Exists(PathMain)) File.Delete(PathMain);
                File.Move(PathTemp, PathMain);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 저장 실패: {e}");
            }
        }

        /// <summary>저장 파일이 없거나 깨졌으면 백업 → 새 게임 순서로 시도.</summary>
        public static SaveData Load()
        {
            var data = TryRead(PathMain) ?? TryRead(PathBackup);
            if (data == null) return SaveData.NewGame();
            Sanitize(data);
            return data;
        }

        public static void Delete()
        {
            foreach (var p in new[] { PathMain, PathBackup, PathTemp })
                if (File.Exists(p)) File.Delete(p);
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] {path} 읽기 실패: {e.Message}");
                return null;
            }
        }

        /// <summary>구버전·손상 데이터의 null/범위를 바로잡는다.</summary>
        static void Sanitize(SaveData d)
        {
            d.upgrades ??= new System.Collections.Generic.List<UpgradeLevel>();
            d.warehouse ??= new System.Collections.Generic.List<ItemStack>();
            d.shelf ??= new System.Collections.Generic.List<ShelfSlot>();
            d.market ??= new System.Collections.Generic.List<MarketEntry>();
            d.codex ??= new System.Collections.Generic.List<string>();
            d.run ??= new RunStats();
            d.lifetime ??= new RunStats();
            d.settings ??= new GameSettings();
            if (d.boxes == null || d.boxes.Length != GradeInfo.Count)
            {
                var fixedBoxes = new int[GradeInfo.Count];
                if (d.boxes != null) Array.Copy(d.boxes, fixedBoxes, Math.Min(d.boxes.Length, fixedBoxes.Length));
                d.boxes = fixedBoxes;
            }
            if (d.nexusMaxHp <= 0d) d.nexusMaxHp = RegionCatalog.MaxHp(d.region);
            d.nexusHp = Math.Max(0d, Math.Min(d.nexusHp, d.nexusMaxHp));
            d.warehouse.RemoveAll(s => s == null || ItemCatalog.Get(s.id) == null || s.count <= 0);
            foreach (var s in d.shelf)
                if (s != null && !s.IsEmpty && ItemCatalog.Get(s.itemId) == null) s.itemId = null;
            d.settings.windowScale = Mathf.Clamp(d.settings.windowScale, 1, 6);
        }
    }
}
