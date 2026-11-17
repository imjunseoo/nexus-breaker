using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NexusBreaker.EditorTools
{
    /// <summary>
    /// 방치 플레이를 흉내 내는 봇으로 첫 넥서스(10억) 파괴까지 걸리는 시간을 추정한다.
    /// 봇: 1분마다 박스 전부 열기 → 시장가 ×1.1로 진열(희귀 이상은 ×1.4) → 살 수 있는 가장 싼 업그레이드 반복 구매. 클릭 없음.
    /// 결과: Docs/BALANCE_SIM.md
    /// </summary>
    public static class BalanceSim
    {
        [MenuItem("Nexus Breaker/Run Balance Simulation")]
        public static void RunMenu() => Debug.Log(Run(seed: 1, maxHours: 72));

        public static void RunBatch()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 밸런스 시뮬레이션 결과 (자동 생성)\n");
                sb.AppendLine("봇: 클릭 없음, 1분마다 박스 전부 열기·진열(시장가 ×1.1, 희귀 이상 ×1.4)·가장 싼 강화 구매.\n");
                for (int seed = 1; seed <= 3; seed++) sb.AppendLine(Run(seed, 72));
                Directory.CreateDirectory("Docs");
                File.WriteAllText("Docs/BALANCE_SIM.md", sb.ToString());
                Debug.Log(sb.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        public static string Run(int seed, double maxHours)
        {
            var model = new GameModel(SaveData.NewGame(), new Rng(seed));
            const double dt = 0.1;
            double t = 0, nextManage = 60, nextLog = 3600;
            var sb = new StringBuilder();
            sb.AppendLine($"## seed {seed}\n");
            sb.AppendLine("| 시간 | HP% | 지층 | 너트 | 누적 수입 | 자동 DPS | 망치 | 속도 | 조수 | 진열대 | 손님 간격 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            var strataReached = new double[4];
            for (int i = 1; i < 4; i++) strataReached[i] = -1;

            while (t < maxHours * 3600 && !model.Data.nexusDestroyed)
            {
                model.Tick(dt);
                t += dt;
                int st = model.StratumNow;
                if (strataReached[st] < 0) strataReached[st] = t;

                if (t >= nextManage)
                {
                    nextManage += 60;
                    Manage(model);
                }
                if (t >= nextLog)
                {
                    nextLog += t < 6 * 3600 ? 3600 : 6 * 3600;
                    sb.AppendLine(Row(model, t));
                }
            }
            sb.AppendLine(Row(model, t));
            sb.AppendLine();
            sb.AppendLine(model.Data.nexusDestroyed
                ? $"**넥서스 파괴: {t / 3600:0.0}시간**"
                : $"**{maxHours}시간 안에 파괴 못 함 (남은 HP {model.HpFraction * 100:0.0}%)**");
            for (int i = 1; i < 4; i++)
                sb.AppendLine($"- {Stats.StratumNames[i]} 도달: {(strataReached[i] < 0 ? "-" : (strataReached[i] / 3600).ToString("0.0") + "시간")}");
            sb.AppendLine($"- 판매 {model.Data.run.itemsSold}개, 박스 개봉 {model.Data.run.boxesOpened}개");
            sb.AppendLine();
            return sb.ToString();
        }

        static string Row(GameModel m, double t) =>
            $"| {t / 3600:0.0}h | {m.HpFraction * 100:0.00} | {Stats.StratumNames[m.StratumNow]} | {BigNum.Format(m.Data.nuts)} | {BigNum.Format(m.Data.run.nutsEarned)} | " +
            $"{BigNum.Format(m.AutoDps)} | {m.Level(UpgradeId.Hammer)} | {m.Level(UpgradeId.SwingSpeed)} | {m.HelperCount} | {m.ShelfSlots} | {m.CustomerInterval:0.0}s |";

        static void Manage(GameModel m)
        {
            m.OpenAllBoxes();

            // 진열: 비싼 물건부터
            var stacks = new List<ItemStack>(m.Data.warehouse);
            stacks.Sort((a, b) => ItemCatalog.Get(b.id).BasePrice.CompareTo(ItemCatalog.Get(a.id).BasePrice));
            foreach (var s in stacks)
            {
                var def = ItemCatalog.Get(s.id);
                double mult = def.Grade >= Grade.Rare ? 1.4 : 1.1;
                while (m.CountOf(s.id) > 0 && m.FirstEmptySlot() >= 0)
                    if (!m.PlaceOnShelf(s.id, m.MarketPrice(def) * mult)) break;
            }

            // 강화: 살 수 있는 가장 싼 것부터 반복
            for (int guard = 0; guard < 500; guard++)
            {
                UpgradeId? best = null;
                double bestCost = double.MaxValue;
                foreach (var u in UpgradeCatalog.All)
                {
                    if (u.Id == UpgradeId.OfflineCap) continue;
                    if (m.IsMaxed(u.Id)) continue;
                    double c = m.UpgradeCost(u.Id);
                    if (c < bestCost) { bestCost = c; best = u.Id; }
                }
                if (best == null || !m.TryBuyUpgrade(best.Value)) break;
            }
        }
    }
}
