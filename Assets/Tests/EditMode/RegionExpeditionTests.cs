using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 遠征の規則（SP-021 軸 H の最小の実装）の約束：日数がかかる・足りなければサーチしない・発見で拠点と日数と手掛かり・
    /// 反応の方角・日数が尽きると終わる。数値そのもの（仮の値）は確かめない。
    /// </summary>
    public class RegionExpeditionTests
    {
        private static RegionData Small()
        {
            return RegionGenerator.Generate(new RegionSpec { Seed = 7, Resolution = 128, ErosionIterations = 8, SiteCount = 10, NameStyle = RegionNameStyle.Systematic });
        }

        [Test]
        public void Begin_StartsAtACampOnLand_WithDaysAndSomeClues()
        {
            RegionData data = Small();
            var ex = new RegionExpedition(new ExplorationSession(data));
            Assert.AreEqual(ex.StartDays, ex.DaysLeft);
            Assert.AreEqual(1, ex.Camps.Count);
            Assert.AreSame(ex.StartCamp, ex.Camps[0]);
            Assert.Greater(data.HeightAtMap(ex.StartCamp.MapX, ex.StartCamp.MapY), 0f);
            Assert.IsFalse(ex.IsOver);
            int open = ex.OpenClues().Count;
            Assert.Greater(open, 0);
            Assert.LessOrEqual(open, ex.StartNamedClues + ex.StartCodeClues);
            foreach (RegionClue clue in ex.Clues)
            {
                Assert.IsNotEmpty(clue.Title);
                Assert.IsNotEmpty(clue.NameLine);
                Assert.IsNotEmpty(clue.DetailLine);
                Assert.IsFalse(clue.PlaceLine.Contains("採点"), clue.PlaceLine);
            }
        }

        [Test]
        public void Cost_GrowsWithDistanceFromTheNearestCamp()
        {
            var ex = new RegionExpedition(new ExplorationSession(Small()));
            ExpeditionCamp camp = ex.StartCamp;
            ExpeditionCost here = ex.CostAt(camp.MapX, camp.MapY);
            Assert.AreSame(camp, here.Camp);
            Assert.GreaterOrEqual(here.Total, 1);
            Assert.LessOrEqual(here.Total, 1 + ex.RoughDays);

            float farX = camp.MapX < 50f ? camp.MapX + ex.CampStep * 3.5f : camp.MapX - ex.CampStep * 3.5f;
            ExpeditionCost far = ex.CostAt(farX, camp.MapY);
            Assert.GreaterOrEqual(far.Total, 1 + 3);
            int sum = 0;
            foreach (ExpeditionCostPart part in far.Parts) sum += part.Days;
            Assert.AreEqual(far.Total, sum);
        }

        [Test]
        public void Search_UsesDays_AndRefusesWhenTooFew()
        {
            var session = new ExplorationSession(Small());
            var ex = new RegionExpedition(session) { StartDays = 2 };
            ex.Begin();
            ExpeditionCamp camp = ex.StartCamp;
            float farX = camp.MapX < 50f ? camp.MapX + ex.CampStep * 3.5f : camp.MapX - ex.CampStep * 3.5f;
            ExpeditionStep refused = ex.Search(farX, camp.MapY);
            Assert.IsFalse(refused.Done);
            Assert.IsNotEmpty(refused.Refusal);
            Assert.AreEqual(2, ex.DaysLeft);
            Assert.AreEqual(0, session.SearchCount);

            int cost = ex.CostAt(camp.MapX, camp.MapY).Total;
            ExpeditionStep step = ex.Search(camp.MapX, camp.MapY);
            Assert.IsTrue(step.Done);
            Assert.AreEqual(cost, step.DaysSpent);
            Assert.AreEqual(2 - cost, ex.DaysLeft);
            Assert.AreEqual(1, session.SearchCount);
        }

        [Test]
        public void Signal_PointsTowardTheNearestSite()
        {
            RegionData data = Small();
            var ex = new RegionExpedition(new ExplorationSession(data)) { StartDays = 500 };
            ex.Begin();
            RegionSite site = data.Sites[0];
            // 他の地点は 9 近く離れているので、4 ずらした位置では site がいちばん近い
            float shift = site.MapX < 50f ? 4f : -4f;
            ExpeditionStep step = ex.Search(site.MapX + shift, site.MapY);
            Assert.AreEqual(SearchSignal.Strong, step.Result.Signal);
            Assert.AreSame(site, step.Result.Nearest);
            Assert.AreEqual(shift > 0f ? CompassPoint.West : CompassPoint.East, step.Direction);
        }

        [Test]
        public void Find_MakesACamp_GivesDays_AndOpensNearbyClues()
        {
            RegionData data = Small();
            var session = new ExplorationSession(data);
            var ex = new RegionExpedition(session) { StartDays = 500 };
            ex.Begin();
            RegionSite site = data.Sites[data.Sites.Count - 1];
            int openBefore = ex.OpenClues().Count;
            bool wasOpen = ex.ClueFor(site).State != ClueState.Locked;
            int cost = ex.CostAt(site.MapX, site.MapY).Total;

            ExpeditionStep step = ex.Search(site.MapX, site.MapY);
            Assert.AreEqual(SearchSignal.Found, step.Result.Signal);
            Assert.IsNotNull(step.Reward);
            Assert.AreEqual(2, ex.Camps.Count);
            Assert.AreSame(site, ex.Camps[1].Site);
            Assert.AreEqual(500 - cost + ex.FindDays, ex.DaysLeft);
            Assert.AreEqual(ClueState.Solved, ex.ClueFor(site).State);
            Assert.AreEqual(ex.CluesPerFind, step.Reward.OpenedClues.Count);
            Assert.AreEqual(openBefore - (wasOpen ? 1 : 0) + ex.CluesPerFind, ex.OpenClues().Count);

            // 新しい拠点のそばは、移動の日数が要らない
            ExpeditionCost near = ex.CostAt(site.MapX + 1f, site.MapY);
            Assert.AreSame(ex.Camps[1], near.Camp);
        }

        [Test]
        public void ReadClue_UsesDays_AndRevealsTheDetail()
        {
            var ex = new RegionExpedition(new ExplorationSession(Small()));
            RegionClue clue = ex.OpenClues()[0];
            Assert.IsTrue(ex.ReadClue(clue));
            Assert.AreEqual(ClueState.Read, clue.State);
            Assert.AreEqual(ex.StartDays - ex.ReadDays, ex.DaysLeft);
            Assert.IsFalse(ex.ReadClue(clue), "読んだ手掛かりは二度は読まない");
        }

        [Test]
        public void Expedition_EndsWhenDaysRunOut_AndBeginRestarts()
        {
            var session = new ExplorationSession(Small());
            var ex = new RegionExpedition(session) { StartDays = 3 };
            ex.Begin();
            ExpeditionCamp camp = ex.StartCamp;
            for (int k = 0; k < 10 && !ex.IsOver; k++) ex.Search(camp.MapX + 0.5f, camp.MapY);
            Assert.IsTrue(ex.IsOver);
            Assert.IsFalse(ex.Search(camp.MapX, camp.MapY).Done);

            ex.Begin();
            Assert.IsFalse(ex.IsOver);
            Assert.AreEqual(3, ex.DaysLeft);
            Assert.AreEqual(0, session.SearchCount);
            Assert.AreEqual(0, session.Discovered.Count);
        }

        [Test]
        public void Compass_And_Code_Helpers()
        {
            // 地図の Y は南へ増える
            Assert.AreEqual(CompassPoint.North, RegionCompass.Toward(50f, 50f, 50f, 40f));
            Assert.AreEqual(CompassPoint.NorthEast, RegionCompass.Toward(50f, 50f, 60f, 40f));
            Assert.AreEqual(CompassPoint.SouthWest, RegionCompass.Toward(50f, 50f, 40f, 60f));
            Assert.AreEqual(CompassPoint.None, RegionCompass.Toward(50f, 50f, 50f, 50f));
            Assert.AreEqual("北東", RegionCompass.Label(CompassPoint.NorthEast));
            Assert.AreEqual("MN-5?5?", RegionExpedition.MaskCode("MN-5659"));
            Assert.AreEqual("呼び名", RegionExpedition.MaskCode("呼び名"));
        }
    }
}
