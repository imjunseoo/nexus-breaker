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
    }
}
