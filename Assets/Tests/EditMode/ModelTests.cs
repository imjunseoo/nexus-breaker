using NUnit.Framework;

namespace NexusBreaker.Tests
{
    public class ModelTests
    {
        static GameModel NewModel(int seed = 1) => new GameModel(SaveData.NewGame(), new Rng(seed));

        [Test]
        public void BigNum_FormatsKoreanUnits()
        {
            Assert.AreEqual("0", BigNum.Format(0));
            Assert.AreEqual("9,999", BigNum.Format(9999));
            Assert.AreEqual("1.23만", BigNum.Format(12345));
            Assert.AreEqual("10억", BigNum.Format(1e9));
            Assert.AreEqual("1조", BigNum.Format(1e12));
        }

        [Test]
        public void LogNormalExceed_MatchesDesignTable()
        {
            // GDD: 일반 손님(σ 0.35) 0.5배 ~98%, 1.0배 50%, 1.5배 ~12%, 2배 ~2%
            Assert.AreEqual(0.976, MathUtil.LogNormalExceed(0.5, 1, 0.35), 0.01);
            Assert.AreEqual(0.5, MathUtil.LogNormalExceed(1.0, 1, 0.35), 0.001);
            Assert.AreEqual(0.123, MathUtil.LogNormalExceed(1.5, 1, 0.35), 0.01);
            Assert.AreEqual(0.024, MathUtil.LogNormalExceed(2.0, 1, 0.35), 0.01);
        }

        [Test]
        public void Click_DealsDamage_AndBoxesDropEveryNHits()
        {
            var m = NewModel();
            double hp = m.Data.nexusHp;
            int per = Stats.HitsPerBox(0);
            for (int i = 0; i < per * 3; i++) m.Click();
            Assert.Less(m.Data.nexusHp, hp);
            Assert.AreEqual(3, m.BoxCount);
        }

        [Test]
        public void Milestone_BoxesDropEveryPercent()
        {
            var d = SaveData.NewGame();
            d.nexusMaxHp = 1000; d.nexusHp = 1000;
            var m = new GameModel(d, new Rng(2));
            int dropped = 0;
            m.BoxDropped += (_, milestone) => { if (milestone) dropped++; };
            // 클릭 1회 데미지(2)로 1000 HP → 1%(10HP)마다 1개
            for (int i = 0; i < 25; i++) m.Click();
            Assert.GreaterOrEqual(dropped, 4);
        }

        [Test]
        public void OpenBox_MovesItemToWarehouse_AndCodex()
        {
            var m = NewModel();
            m.Data.boxes[(int)Grade.Common] = 2;
            var item = m.OpenNextBox();
            Assert.NotNull(item);
            Assert.AreEqual(Grade.Common, item.Grade);
            Assert.AreEqual(1, m.CountOf(item.Id));
            Assert.Contains(item.Id, m.Data.codex);
            Assert.AreEqual(1, m.BoxCount);
        }

        [Test]
        public void Shelf_PlaceSellAndTakeOff()
        {
            var m = NewModel(5);
            m.Data.warehouse.Add(new ItemStack { id = "cigarettes", count = 1 });
            Assert.IsTrue(m.PlaceOnShelf("cigarettes", 1));   // 1너트 → 거의 무조건 팔림
            Assert.AreEqual(0, m.CountOf("cigarettes"));
            for (int i = 0; i < 2000 && m.OccupiedShelfCount > 0; i++) m.Tick(0.1);
            Assert.AreEqual(0, m.OccupiedShelfCount, "싸게 내놓으면 팔려야 한다");
            Assert.AreEqual(1d, m.Data.nuts, 1e-9);

            m.Data.warehouse.Add(new ItemStack { id = "battery", count = 1 });
            Assert.IsTrue(m.PlaceOnShelf("battery", 999999));
            Assert.IsTrue(m.TakeOffShelf(0));
            Assert.AreEqual(1, m.CountOf("battery"));
        }

        [Test]
        public void PlaceAllAtMarket_FillsEmptySlots_MostValuableFirst()
        {
            var m = NewModel();
            m.Data.warehouse.Add(new ItemStack { id = "snack_shrimp", count = 5 });
            m.Data.warehouse.Add(new ItemStack { id = "medkit", count = 1 });
            m.Data.warehouse.Add(new ItemStack { id = "battery", count = 2 });
            int slots = m.Data.shelf.Count; // 6
            int placed = m.PlaceAllAtMarket();
            Assert.AreEqual(slots, placed);
            Assert.AreEqual(0, m.CountOf("medkit"), "비싼 물건이 먼저 진열돼야 한다");
            Assert.AreEqual(0, m.CountOf("battery"));
            Assert.AreEqual(2, m.CountOf("snack_shrimp"));
            foreach (var s in m.Data.shelf) Assert.AreEqual(m.MarketPrice(s.itemId), s.price, 0.5);
        }

        [Test]
        public void ExpectedSellTime_IncreasesWithPrice()
        {
            var m = NewModel();
            var item = ItemCatalog.Get("canned_food");
            double market = m.MarketPrice(item);
            double cheap = m.ExpectedSellSeconds(item, market * 0.5);
            double fair = m.ExpectedSellSeconds(item, market);
            double greedy = m.ExpectedSellSeconds(item, market * 3);
            Assert.Less(cheap, fair);
            Assert.Less(fair, greedy);
            Assert.AreEqual(SellSpeed.Instant, GameModel.ClassifySpeed(cheap));
        }

        [Test]
        public void AbsurdPrice_StillSellsRarely_ViaCollectors()
        {
            var m = NewModel();
            var item = ItemCatalog.Get("lp_record"); // 골동품 → 수집가 선호
            double p = m.BuyChancePerView(item, m.MarketPrice(item) * 4);
            Assert.Greater(p, 0d, "터무니없는 가격도 아주 가끔은 팔려야 한다");
            Assert.Less(p, 0.02d);
        }

        [Test]
        public void Upgrade_SpendsNuts_AndAddsShelfSlot()
        {
            var m = NewModel();
            m.Data.nuts = 1e6;
            int before = m.Data.shelf.Count;
            Assert.IsTrue(m.TryBuyUpgrade(UpgradeId.Shelf));
            Assert.AreEqual(before + 1, m.Data.shelf.Count);
            Assert.Less(m.Data.nuts, 1e6);
        }

        [Test]
        public void Prestige_ResetsRun_KeepsCodexAndAddsCores()
        {
            var m = NewModel();
            m.Data.codex.Add("diary");
            m.Data.nuts = 500;
            m.Data.nexusHp = 1; // 코어 지층은 경도 때문에 한 방에 안 깨질 수 있다
            for (int i = 0; i < 1000 && !m.Data.nexusDestroyed; i++) m.Click();
            Assert.IsTrue(m.Data.nexusDestroyed);
            Assert.IsTrue(m.Prestige());
            Assert.AreEqual(1, m.Data.region);
            Assert.AreEqual(Stats.CoresForRegion(0), m.Data.cores);
            Assert.AreEqual(0d, m.Data.nuts);
            Assert.AreEqual(RegionCatalog.MaxHp(1), m.Data.nexusHp);
            Assert.Contains("diary", m.Data.codex);
        }

        [Test]
        public void Tutorial_PrimeFirstBox_DropsAfterFewHits()
        {
            var m = NewModel();
            m.PrimeFirstBox(3);
            for (int i = 0; i < 3; i++) m.Click();
            Assert.AreEqual(1, m.BoxCount);
        }

        [Test]
        public void Tutorial_HurryNextCustomer_ArrivesQuickly()
        {
            var m = NewModel();
            int arrived = 0;
            m.CustomerArrived += _ => arrived++;
            m.Tick(2.5); // 첫 손님(2초) 소비
            arrived = 0;
            m.HurryNextCustomer(0.5);
            m.Tick(0.6);
            Assert.GreaterOrEqual(arrived, 1);
        }

        [Test]
        public void DeeperStrata_AreHarder()
        {
            var m = NewModel();
            double outer = m.SwingDamage;
            m.Data.nexusHp = m.Data.nexusMaxHp * 0.05; // 코어
            Assert.AreEqual(3, m.StratumNow);
            Assert.AreEqual(outer * Stats.StratumHardness[3], m.SwingDamage, 1e-9);
        }

        [Test]
        public void Offline_ProducesDamageBoxesAndSales()
        {
            var m = NewModel(9);
            m.Data.warehouse.Add(new ItemStack { id = "water_bottle", count = 1 });
            m.PlaceOnShelf("water_bottle", 5);
            var r = m.SimulateOffline(3600);
            Assert.Greater(r.Damage, 0d);
            int boxes = 0; foreach (var b in r.Boxes) boxes += b;
            Assert.Greater(boxes, 0);
            Assert.AreEqual(1, r.ItemsSold);
        }

        [Test]
        public void SaveRoundTrip_PreservesState()
        {
            var m = NewModel();
            m.Data.nuts = 1234;
            m.Data.boxes[2] = 3;
            m.Data.warehouse.Add(new ItemStack { id = "medkit", count = 2 });
            string json = UnityEngine.JsonUtility.ToJson(m.Data);
            var back = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(1234d, back.nuts);
            Assert.AreEqual(3, back.boxes[2]);
            Assert.AreEqual("medkit", back.warehouse[0].id);
        }

        // ───────────────────────── v0.2 ─────────────────────────

        static GameModel AfterTutorial(int seed = 1)
        {
            var d = SaveData.NewGame();
            d.tutorialStep = -1;
            return new GameModel(d, new Rng(seed));
        }

        [Test]
        public void WeakPoint_HitDealsBonusDamage_AndDropsBox()
        {
            var m = AfterTutorial();
            m.SpawnWeakPoint();
            Assert.IsTrue(m.WeakPointActive);
            double hp = m.Data.nexusHp;
            double expected = m.WeakPointDamage;
            Assert.AreEqual(m.ClickDamage * Stats.WeakPointDamageMult, expected, 1e-9);

            Assert.IsTrue(m.HitWeakPoint());
            Assert.AreEqual(hp - expected, m.Data.nexusHp, 1e-6);
            Assert.AreEqual(1, m.BoxCount);
            Assert.IsFalse(m.WeakPointActive);
            Assert.IsFalse(m.HitWeakPoint(), "한 번 깬 약점은 다시 못 깬다");
            Assert.AreEqual(1, m.Data.lifetime.weakPoints);
        }

        [Test]
        public void WeakPoint_ExpiresAfterLifetime()
        {
            var m = AfterTutorial();
            bool expired = false;
            m.WeakPointExpired += () => expired = true;
            m.SpawnWeakPoint();
            for (double t = 0; t < Stats.WeakPointLifetime + 0.5; t += 0.1) m.Tick(0.1);
            Assert.IsFalse(m.WeakPointActive);
            Assert.IsTrue(expired);
        }

        [Test]
        public void WeakPoint_NeverSpawnsDuringTutorial()
        {
            var m = NewModel(3);
            Assert.GreaterOrEqual(m.Data.tutorialStep, 0);
            bool spawned = false;
            m.WeakPointSpawned += () => spawned = true;
            for (int i = 0; i < 2000; i++) m.Tick(0.5);
            Assert.IsFalse(spawned);
        }

        [Test]
        public void WeakPoint_SpawnsRegularly_AfterTutorial()
        {
            var m = AfterTutorial(4);
            int spawns = 0;
            m.WeakPointSpawned += () => spawns++;
            for (int i = 0; i < 2 * 3600 * 2; i++) m.Tick(0.5); // 2시간
            // 평균 간격 약 80초 + 수명 7초 → 2시간에 약 80번
            Assert.Greater(spawns, 40);
            Assert.Less(spawns, 160);
        }

        [Test]
        public void Contracts_FillAfterTutorial_WithDistinctKinds()
        {
            var m = NewModel(5);
            m.Tick(0.1);
            foreach (var c in m.Contracts) Assert.IsTrue(c.IsEmpty, "튜토리얼 중에는 의뢰가 없다");

            m.Data.tutorialStep = -1;
            m.Tick(0.1);
            Assert.AreEqual(Stats.ContractSlots, m.Contracts.Count);
            var kinds = new System.Collections.Generic.HashSet<string>();
            foreach (var c in m.Contracts)
            {
                Assert.IsFalse(c.IsEmpty);
                Assert.Greater(c.goal, 0d);
                Assert.IsTrue(c.rewardNuts > 0d || c.rewardBoxGrade >= 0, "보상이 있어야 한다");
                Assert.IsFalse(string.IsNullOrEmpty(ContractText.Title(c)));
                kinds.Add(c.kind);
            }
            Assert.AreEqual(Stats.ContractSlots, kinds.Count, "같은 종류 의뢰가 겹치지 않는다");
        }

        [Test]
        public void Contracts_ClickContract_CompletesAndClaimsBox()
        {
            var m = AfterTutorial();
            m.Data.contracts[0] = new ContractState { kind = "Clicks", goal = 3, rewardBoxGrade = (int)Grade.Rare };
            int completed = -1;
            m.ContractCompleted += i => completed = i;
            for (int i = 0; i < 3; i++) m.Click();
            Assert.IsTrue(m.Contracts[0].IsComplete);
            Assert.AreEqual(0, completed);

            int rareBefore = m.Data.boxes[(int)Grade.Rare];
            Assert.IsTrue(m.ClaimContract(0));
            Assert.AreEqual(rareBefore + 1, m.Data.boxes[(int)Grade.Rare]);
            Assert.IsTrue(m.Contracts[0].IsEmpty);
            Assert.AreEqual(1, m.Data.lifetime.contractsDone);
            Assert.IsFalse(m.ClaimContract(0), "빈 칸은 받을 게 없다");

            // 일정 시간 뒤 다시 채워진다
            for (double t = 0; t < Stats.ContractRefillDelay + 1; t += 0.5) m.Tick(0.5);
            Assert.IsFalse(m.Contracts[0].IsEmpty);
        }

        [Test]
        public void Contracts_SellItem_ProgressFromSales_AndPaysNuts()
        {
            var m = AfterTutorial(6);
            m.Data.contracts[0] = new ContractState { kind = "SellItem", target = "cup_ramen", goal = 2, rewardNuts = 500 };
            m.Data.warehouse.Add(new ItemStack { id = "cup_ramen", count = 2 });
            m.PlaceOnShelf("cup_ramen", 1);
            m.PlaceOnShelf("cup_ramen", 1);
            for (int i = 0; i < 2400 && !m.Contracts[0].IsComplete; i++) m.Tick(0.1);
            Assert.IsTrue(m.Contracts[0].IsComplete);
            double nuts = m.Data.nuts;
            Assert.IsTrue(m.ClaimContract(0));
            Assert.AreEqual(nuts + 500, m.Data.nuts, 1e-9);
        }

        [Test]
        public void Contracts_Reroll_HasCooldown()
        {
            var m = AfterTutorial(7);
            m.FillContractsNow();
            string before = m.Contracts[1].kind;
            Assert.IsTrue(m.RerollContract(1));
            Assert.AreNotEqual(before, m.Contracts[1].kind);
            Assert.IsFalse(m.RerollContract(2), "쿨다운 중");
            for (double t = 0; t < Stats.ContractRerollCooldown + 1; t += 1) m.Tick(1);
            Assert.IsTrue(m.RerollContract(2));
        }

        [Test]
        public void Contracts_ResetOnPrestige()
        {
            var m = AfterTutorial(8);
            m.FillContractsNow();
            m.Data.nexusHp = 0; m.Data.nexusDestroyed = true;
            Assert.IsTrue(m.Prestige());
            foreach (var c in m.Contracts) Assert.IsTrue(c.IsEmpty);
        }

        [Test]
        public void Codex_RecordsFoundAndSold()
        {
            var m = AfterTutorial(9);
            m.Data.boxes[0] = 1;
            var item = m.OpenBox(Grade.Common);
            Assert.AreEqual(1, m.CodexOf(item.Id).found);
            m.PlaceOnShelf(item.Id, 1);
            for (int i = 0; i < 2400 && m.CodexOf(item.Id).sold == 0; i++) m.Tick(0.1);
            Assert.AreEqual(1, m.CodexOf(item.Id).sold);
            Assert.AreEqual(1d, m.CodexOf(item.Id).bestPrice);
        }

        [Test]
        public void NiceRound_MakesReadableNumbers()
        {
            Assert.AreEqual(5d, GameModel.NiceRound(3));
            Assert.AreEqual(45d, GameModel.NiceRound(43));
            Assert.AreEqual(1200d, GameModel.NiceRound(1234));
            Assert.AreEqual(57000d, GameModel.NiceRound(56789));
        }

        [Test]
        public void SaveRoundTrip_PreservesV02Fields()
        {
            var m = AfterTutorial(10);
            m.FillContractsNow();
            m.Data.contracts[0].progress = 2;
            m.Data.codexStats.Add(new CodexEntry { id = "medkit", found = 3, sold = 2, bestPrice = 900 });
            m.Data.lifetime.weakPoints = 4;
            string json = UnityEngine.JsonUtility.ToJson(m.Data);
            var back = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(3, back.contracts.Count);
            Assert.AreEqual(m.Data.contracts[0].kind, back.contracts[0].kind);
            Assert.AreEqual(2d, back.contracts[0].progress);
            Assert.AreEqual(900d, back.codexStats[0].bestPrice);
            Assert.AreEqual(4, back.lifetime.weakPoints);
        }
    }
}
