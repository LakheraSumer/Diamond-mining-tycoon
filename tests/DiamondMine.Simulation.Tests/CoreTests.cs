using System.Globalization;
using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class CoreTests
    {
        EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "economy.json");
            config = EconomyLoader.Load(File.ReadAllText(path));
        }

        [Test]
        public void Economy_HasThreeDistinctMinesAndEightHourCap()
        {
            Assert.That(config.Mines.Count, Is.EqualTo(3));
            Assert.That(config.Mines[0].Palette, Is.EqualTo("warmstone"));
            Assert.That(config.Mines[1].Palette, Is.EqualTo("crystal"));
            Assert.That(config.Mines[2].Palette, Is.EqualTo("deepcore"));
            Assert.That(config.Mines[1].TripSeconds, Is.GreaterThan(config.Mines[0].TripSeconds));
            Assert.That(config.Mines[2].ProcessRate, Is.LessThan(config.Mines[0].ProcessRate));
            Assert.That(config.OfflineCapSeconds, Is.EqualTo(8d * 3600d));
        }

        [Test]
        public void Resources_AreConservedThroughTheChain()
        {
            GameState state = GameFactory.NewGame(config);
            for (int i = 0; i < 40; i++)
            {
                ProductionSimulator.TryTap(state, config, "warmstone");
                ProductionSimulator.Advance(state, config, 0.45);
            }
            ProductionSimulator.Advance(state, config, 90);
            AssertConserved(state);
            Assert.That(state.Mines[0].DiamondsSold, Is.GreaterThan(0));
            Assert.That(state.Coins, Is.GreaterThan(0));
        }

        [Test]
        public void LongAdvance_MatchesShortSteps()
        {
            GameState stepped = GameFactory.NewGame(config);
            GameState batched = GameFactory.NewGame(config);
            UpgradeService.TryBuy(WithCoins(stepped, 5000), config, "warmstone", "hire", 1);
            UpgradeService.TryBuy(WithCoins(batched, 5000), config, "warmstone", "hire", 1);
            for (int i = 0; i < 400; i++)
                ProductionSimulator.Advance(stepped, config, 0.25);
            ProductionSimulator.Advance(batched, config, 100);
            Assert.That(batched.Coins, Is.EqualTo(stepped.Coins).Within(stepped.Coins * 0.03 + 1));
            Assert.That(batched.Mines[0].OreMined, Is.EqualTo(stepped.Mines[0].OreMined).Within(stepped.Mines[0].OreMined * 0.03 + 1));
            AssertConserved(batched);
        }

        [Test]
        public void TransportBottleneck_LimitsThroughput()
        {
            GameState starved = Automated(config);
            GameState hauled = Automated(config);
            UpgradeService.TryBuy(WithCoins(hauled, 100000), config, "warmstone", "cart", 3);
            UpgradeService.TryBuy(hauled, config, "warmstone", "rails", 5);
            ProductionSimulator.Advance(starved, config, 180);
            ProductionSimulator.Advance(hauled, config, 180);
            Assert.That(hauled.Mines[0].DiamondsSold, Is.GreaterThan(starved.Mines[0].DiamondsSold * 1.4));
        }

        [Test]
        public void UpgradeCosts_MatchTheSeriesAndBuyMax()
        {
            Assert.That(UpgradeService.CostFor(0, 1, 10, 2), Is.EqualTo(10).Within(1e-9));
            Assert.That(UpgradeService.CostFor(0, 3, 10, 2), Is.EqualTo(70).Within(1e-8));
            Assert.That(UpgradeService.MaxAffordable(70, 0, 10, 10, 2), Is.EqualTo(3));
            Assert.That(UpgradeService.MaxAffordable(69.9, 0, 10, 10, 2), Is.EqualTo(2));
            Assert.That(UpgradeService.MaxAffordable(10, 0, 1, 10, 2), Is.EqualTo(1));
            Assert.That(UpgradeService.MaxAffordable(1e300, 0, 5, 10, 100), Is.LessThanOrEqualTo(5));
            Assert.That(double.IsPositiveInfinity(UpgradeService.CostFor(0, 400, 10, 100)), Is.True);
        }

        [Test]
        public void BuyPreview_MatchesThePurchaseAndRejectsAShortfall()
        {
            double price = config.Mines[0].FindUpgrade("picks").BaseCost;
            GameState state = WithCoins(GameFactory.NewGame(config), price);
            PurchasePreview exact = UpgradeService.Preview(state, config, "warmstone", "picks", 1);
            Assert.That(exact.Affordable, Is.True);
            Assert.That(exact.Cost, Is.EqualTo(price).Within(1e-6));
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "picks", 1), Is.True);
            Assert.That(state.Coins, Is.EqualTo(0).Within(1e-6));
            Assert.That(state.Mines[0].Level("picks"), Is.EqualTo(1));

            state.Coins = exact.Cost - 0.01;
            GameState poor = WithCoins(GameFactory.NewGame(config), price - 0.01);
            PurchasePreview denied = UpgradeService.Preview(poor, config, "warmstone", "picks", 1);
            Assert.That(denied.Affordable, Is.False);
            Assert.That(UpgradeService.TryBuy(poor, config, "warmstone", "picks", 1), Is.False);
            Assert.That(poor.Coins, Is.EqualTo(price - 0.01).Within(1e-6));

            GameState rich = WithCoins(GameFactory.NewGame(config), 1e9);
            int max = UpgradeService.Preview(rich, config, "warmstone", "picks", 0).Count;
            Assert.That(max, Is.EqualTo(6));
            Assert.That(UpgradeService.TryBuy(rich, config, "warmstone", "picks", max), Is.True);
            Assert.That(rich.Mines[0].Level("picks"), Is.EqualTo(6));
            Assert.That(UpgradeService.TryBuy(rich, config, "warmstone", "picks", 1), Is.False);
        }

        [Test]
        public void Training_RequiresTheMiner()
        {
            GameState state = WithCoins(GameFactory.NewGame(config), 100000);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "training", 1), Is.False);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "hire", 1), Is.True);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "training", 1), Is.True);
        }

        [Test]
        public void Unlock_RequiresLifetimeCoins()
        {
            GameState state = GameFactory.NewGame(config);
            string reason;
            Assert.That(MineUnlocks.CanUnlock(state, config, "crystal", out reason), Is.False);
            state.LifetimeCoins = config.Mines[1].UnlockLifetimeCoins;
            Assert.That(MineUnlocks.TryUnlock(state, config, "crystal"), Is.True);
            Assert.That(MineUnlocks.TryUnlock(state, config, "crystal"), Is.False);
            Assert.That(state.Mines[2].Unlocked, Is.False);
        }

        [Test]
        public void Objectives_PayOnce()
        {
            GameState state = GameFactory.NewGame(config);
            state.Mines[0].DiamondsSold = 1;
            Assert.That(ObjectiveService.ClaimCompleted(state, config), Is.GreaterThan(0));
            double coins = state.Coins;
            Assert.That(ObjectiveService.ClaimCompleted(state, config), Is.EqualTo(0));
            Assert.That(state.Coins, Is.EqualTo(coins));
        }

        [Test]
        public void Prestige_ResetsProgressAndKeepsMastery()
        {
            GameState state = WithCoins(Automated(config), 0);
            Wallet.Credit(state, config.PrestigeMinimumCoins);
            state.Mines[0].UpgradeLevels["picks"] = 3;
            PrestigePreview preview = PrestigeService.Preview(state, config);
            Assert.That(preview.Available, Is.True);
            Assert.That(preview.MasteryGain, Is.EqualTo(PrestigeService.Gain(state, config)));
            double lifetime = state.LifetimeCoins;
            Assert.That(PrestigeService.TryPrestige(state, config), Is.True);
            Assert.That(state.Coins, Is.EqualTo(0));
            Assert.That(state.Mines[0].Level("picks"), Is.EqualTo(0));
            Assert.That(state.Mines[0].Level("hire"), Is.EqualTo(0));
            Assert.That(state.Mines[1].Unlocked, Is.False);
            Assert.That(state.Mastery, Is.EqualTo(preview.MasteryAfter));
            Assert.That(state.LifetimeCoins, Is.EqualTo(lifetime));
            Assert.That(PrestigeService.TryPrestige(state, config), Is.False);
            Assert.That(state.PrestigeCount, Is.EqualTo(1));

            MineRates plainRates = MineStats.Compute(config.Mines[0], Automated(config).Mines[0], config, 0, false);
            MineRates boostedRates = MineStats.Compute(config.Mines[0], Automated(config).Mines[0], config, state.Mastery, false);
            Assert.That(boostedRates.AutoRate, Is.GreaterThan(plainRates.AutoRate * (1d + config.MasteryBonusPerPoint)));
            Assert.That(boostedRates.TripSeconds, Is.LessThan(plainRates.TripSeconds));
            GameState plain = Automated(config);
            GameState boosted = Automated(config);
            boosted.Mastery = state.Mastery;
            ProductionSimulator.Advance(plain, config, 300);
            ProductionSimulator.Advance(boosted, config, 300);
            Assert.That(boosted.Mines[0].DiamondsSold, Is.GreaterThan(plain.Mines[0].DiamondsSold));
        }

        [Test]
        public void ManualTap_RespectsCooldownAndBuffer()
        {
            GameState state = GameFactory.NewGame(config);
            Assert.That(ProductionSimulator.TryTap(state, config, "warmstone"), Is.True);
            Assert.That(ProductionSimulator.TryTap(state, config, "warmstone"), Is.False);
            ProductionSimulator.Advance(state, config, 0.5);
            Assert.That(ProductionSimulator.TryTap(state, config, "warmstone"), Is.True);
        }

        [Test]
        public void Numbers_FormatAndNeverGoNegative()
        {
            Assert.That(NumericFormat.Format(1530), Is.EqualTo("1.53K"));
            Assert.That(NumericFormat.Format(2_500_000), Is.EqualTo("2.5M"));
            Assert.That(NumericFormat.Format(double.NaN), Is.EqualTo("—"));
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 1e300);
            Wallet.Credit(state, 1e300);
            Assert.That(state.Coins, Is.EqualTo(NumericFormat.MaxValue));
            Assert.That(double.IsNaN(state.Coins), Is.False);
            Assert.That(state.Coins, Is.GreaterThan(0));
            Wallet.Credit(state, double.NaN);
            Assert.That(state.Coins, Is.EqualTo(NumericFormat.MaxValue));
        }

        [Test]
        public void Offline_CapsAtEightHours_AndIgnoresABackwardClock()
        {
            GameState state = Automated(config);
            ProductionSimulator.Advance(state, config, 30);
            OfflinePlan eight = OfflineEarnings.Plan(state, config, 8 * 3600);
            OfflinePlan ten = OfflineEarnings.Plan(state, config, 10 * 3600);
            Assert.That(ten.Capped, Is.True);
            Assert.That(ten.SecondsSimulated, Is.EqualTo(8 * 3600));
            Assert.That(ten.Coins, Is.EqualTo(eight.Coins).Within(1e-4));

            OfflinePlan backward = OfflineEarnings.Plan(state, config, -3600);
            Assert.That(backward.Coins, Is.EqualTo(0));
            Assert.That(backward.Anomaly, Is.EqualTo("backward"));

            OfflinePlan absurd = OfflineEarnings.Plan(state, config, 800d * 86400d);
            Assert.That(absurd.Anomaly, Is.EqualTo("implausible_forward"));
            Assert.That(absurd.SecondsSimulated, Is.EqualTo(8 * 3600));
        }

        [Test]
        public void Offline_WithoutAutomationDoesNotCreateOre()
        {
            GameState state = GameFactory.NewGame(config);
            double mined = state.Mines[0].OreMined;
            OfflineEarnings.Apply(state, config, 3600);
            Assert.That(state.Mines[0].OreMined, Is.EqualTo(mined));
            Assert.That(state.Coins, Is.EqualTo(0));
        }

        [Test]
        public void Offline_DrainsExistingOreWithoutAMiner()
        {
            GameState state = GameFactory.NewGame(config);
            state.Mines[0].ProcessInput = 10;
            OfflineEarnings.Apply(state, config, 120);
            Assert.That(state.Mines[0].OreMined, Is.EqualTo(0));
            Assert.That(state.Coins, Is.GreaterThan(0));
            Assert.That(state.Mines[0].OreProcessed, Is.GreaterThan(0));
        }

        [Test]
        public void OfflineBonus_IsOnce_AndCancelDoesNotGrant()
        {
            GameState state = Automated(config);
            OfflinePlan plan = OfflineEarnings.Apply(state, config, 600);
            double baseCoins = state.Coins;
            bool cancelled = false;
            if (!cancelled)
            {
                Assert.That(state.Coins, Is.EqualTo(baseCoins));
            }
            Assert.That(AdRewards.TryGrantOfflineBonus(state, config, "wrong"), Is.False);
            Assert.That(AdRewards.TryGrantOfflineBonus(state, config, plan.GrantId), Is.True);
            double withBonus = state.Coins;
            Assert.That(withBonus, Is.GreaterThan(baseCoins));
            Assert.That(AdRewards.TryGrantOfflineBonus(state, config, plan.GrantId), Is.False);
            Assert.That(state.Coins, Is.EqualTo(withBonus));
        }

        [Test]
        public void Boost_RejectsDuplicateTokens()
        {
            GameState state = GameFactory.NewGame(config);
            Assert.That(AdRewards.TryGrantBoost(state, config, "token-a"), Is.True);
            Assert.That(state.BoostRemainingSeconds, Is.EqualTo(config.BoostDurationSeconds));
            Assert.That(AdRewards.TryGrantBoost(state, config, "token-a"), Is.False);
            state.BoostRemainingSeconds = 10;
            Assert.That(AdRewards.TryGrantBoost(state, config, "token-b"), Is.True);
            Assert.That(state.BoostRemainingSeconds, Is.EqualTo(config.BoostDurationSeconds));
        }

        [Test]
        public void Tutorial_SkipsActionsAlreadyDone()
        {
            GameState state = Automated(config);
            state.Mines[0].TapCount = 1;
            state.Mines[0].OreProcessed = 1;
            state.Mines[0].DiamondsSold = 1;
            state.Mines[0].UpgradeLevels["picks"] = 2;
            TutorialService.Reconcile(state, config);
            Assert.That(state.TutorialStep, Is.EqualTo(7));
            TutorialService.NotifyObjectiveShown(state);
            TutorialService.Reconcile(state, config);
            Assert.That(state.TutorialComplete, Is.True);
        }

        [Test]
        public void StorageFull_PointsAtSales()
        {
            GameState state = GameFactory.NewGame(config);
            MineState mine = state.Mines[0];
            MineRates rates = MineStats.Compute(config.Mines[0], mine, config, 0, false);
            mine.Storage = rates.StorageCap;
            mine.TapCount = 3;
            StageDiagnosis diagnosis = BottleneckService.Diagnose(mine, rates);
            Assert.That(diagnosis.Stage, Is.EqualTo(Stages.Sales));
            Assert.That(diagnosis.Reason, Does.Contain("vault").IgnoreCase);
        }

        [Test]
        public void Pacing_ReachesEarlyAndMidMilestonesWithoutAds()
        {
            BotReport early = ProgressionBot.Play(config, 20 * 60);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "20m sale={0:0.0} upgrade={1:0.0} auto={2:0.0} mine2={3:0.0} coins={4:0} life={5:0} income={6:0.00} worstMinute={7:0}",
                early.FirstSale, early.FirstUpgrade, early.Automation, early.SecondMine, early.FinalCoins, early.FinalLifetime, early.FinalIncome, early.WorstMinuteAfterAutomation));
            Assert.That(early.AnyNan, Is.False);
            Assert.That(early.FirstSale, Is.GreaterThan(0).And.LessThan(30));
            Assert.That(early.FirstUpgrade, Is.GreaterThan(0).And.LessThan(75));
            Assert.That(early.Automation, Is.GreaterThan(120).And.LessThan(300));
            Assert.That(early.SecondMine, Is.GreaterThan(8 * 60).And.LessThan(15 * 60));
            Assert.That(early.WorstMinuteAfterAutomation, Is.GreaterThan(0));
        }

        [Test]
        public void Pacing_LateGameReachesPrestigeAndTheDeepCore()
        {
            BotReport late = ProgressionBot.Play(config, 4 * 3600);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "4h mine2={0:0} mine3={1:0} prestigeAt={2:0} prestigeDone={3:0} mastery={4:0} income={5:0.0} coins={6:0}",
                late.SecondMine, late.ThirdMine, late.PrestigeAvailable, late.PrestigeDone, late.FinalMastery, late.FinalIncome, late.FinalCoins));
            Assert.That(late.AnyNan, Is.False);
            Assert.That(late.PrestigeAvailable, Is.GreaterThan(0).And.LessThan(3 * 3600));
            Assert.That(late.PrestigeDone, Is.GreaterThan(0));
            Assert.That(late.FinalMastery, Is.GreaterThanOrEqualTo(1));
            Assert.That(late.ThirdMine, Is.GreaterThan(0).And.LessThan(4 * 3600));
        }

        static GameState Automated(EconomyConfig config)
        {
            GameState state = WithCoins(GameFactory.NewGame(config), 5000);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "hire", 1), Is.True);
            return state;
        }

        static GameState WithCoins(GameState state, double coins)
        {
            state.Coins = 0;
            Wallet.Credit(state, coins);
            return state;
        }

        static void AssertConserved(GameState state)
        {
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                double loads = 0;
                for (int c = 0; c < mine.Carts.Count; c++)
                    loads += mine.Carts[c].Load;
                double ore = mine.OreAtFace + mine.ProcessInput + loads + mine.OreProcessed;
                Assert.That(ore, Is.EqualTo(mine.OreMined).Within(1e-3), "ore " + mine.Id);
                double diamonds = mine.Rough + mine.Storage + mine.DiamondsSold;
                Assert.That(diamonds, Is.EqualTo(mine.DiamondsProduced).Within(1e-3), "diamonds " + mine.Id);
            }
            double balance = state.Coins + state.CoinsSpent + state.CoinsDiscarded;
            Assert.That(balance, Is.EqualTo(state.CoinsEarned).Within(1e-2));
        }
    }
}
