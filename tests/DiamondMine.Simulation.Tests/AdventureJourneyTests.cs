using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class AdventureJourneyTests
    {
        EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            config = EconomyLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "economy.json")));
        }

        [Test]
        public void PlotIds_AreUniqueAcrossTheWorld()
        {
            Assert.That(WorldGenerator.RegionCount * WorldGenerator.PlotsPerRegion, Is.EqualTo(102400));
            var seen = new bool[WorldGenerator.PlotCount];
            for (int region = 0; region < WorldGenerator.RegionCount; region++)
            {
                for (int local = 0; local < WorldGenerator.PlotsPerRegion; local++)
                {
                    int id = region * WorldGenerator.PlotsPerRegion + local;
                    Assert.That(seen[id], Is.False);
                    seen[id] = true;
                    Assert.That(WorldGenerator.RegionOf(id), Is.EqualTo(region));
                }
            }
        }

        [Test]
        public void Describe_IsStable_AndStarterLayoutIgnoresGeneratorVersion()
        {
            PlotProfile first = WorldGenerator.Describe(WorldGenerator.DefaultSeed, 2, 1);
            PlotProfile second = WorldGenerator.Describe(WorldGenerator.DefaultSeed, 2, 2);
            Assert.That(second.AmountOf(JourneyCatalog.DiamondOre), Is.EqualTo(first.AmountOf(JourneyCatalog.DiamondOre)));
            Assert.That(WorldGenerator.Mix(WorldGenerator.DefaultSeed, 100, 1), Is.Not.EqualTo(WorldGenerator.Mix(WorldGenerator.DefaultSeed, 100, 2)));
            PlotProfile again = WorldGenerator.Describe(WorldGenerator.DefaultSeed, 100, 1);
            PlotProfile repeat = WorldGenerator.Describe(WorldGenerator.DefaultSeed, 100, 1);
            Assert.That(repeat.AmountOf(JourneyCatalog.Stone), Is.EqualTo(again.AmountOf(JourneyCatalog.Stone)));
            Assert.That(first.Richness, Is.EqualTo("Starter"));
            Assert.That(again.Richness, Is.Not.EqualTo("Starter"));
        }

        [Test]
        public void DescribingEveryPlot_DoesNotRequireInstances()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int reserves = 0;
            for (int id = 0; id < WorldGenerator.PlotCount; id++)
            {
                PlotProfile profile = WorldGenerator.Describe(WorldGenerator.DefaultSeed, id, WorldGenerator.CurrentVersion);
                reserves += profile.Reserves.Count;
                Assert.That(profile.PlotId, Is.EqualTo(id));
            }
            watch.Stop();
            Assert.That(reserves, Is.GreaterThan(WorldGenerator.PlotCount));
            Assert.That(watch.Elapsed.TotalSeconds, Is.LessThan(5d));
        }

        [Test]
        public void Starters_AreGrantedOnce()
        {
            GameState state = GameFactory.NewGame(config);
            Assert.That(state.Adventure.StarterGrantApplied, Is.True);
            Assert.That(state.Adventure.Owned.Count(), Is.EqualTo(5));
            for (int plot = 0; plot < 5; plot++)
                Assert.That(state.Adventure.Owned.Get(plot), Is.True);
            Assert.That(AdventureRules.GrantStarters(state.Adventure), Is.EqualTo(0));
            Assert.That(state.Adventure.Owned.Count(), Is.EqualTo(5));
            string json = SaveCodec.Write(state);
            GameState loaded = SaveCodec.Read(json, config);
            Assert.That(loaded.Adventure.Owned.Count(), Is.EqualTo(5));
            Assert.That(AdventureRules.GrantStarters(loaded.Adventure), Is.EqualTo(0));
        }

        [Test]
        public void LegacySave_KeepsWorksAndDoesNotGrantStartersTwice()
        {
            const string json = "{\"version\":1,\"coins\":50,\"mines\":[{\"id\":\"warmstone\",\"unlocked\":true,\"upgrades\":{\"picks\":2}},{\"id\":\"deepcore\",\"unlocked\":true}]}";
            GameState loaded = SaveCodec.Read(json, config);
            Assert.That(loaded.Coins, Is.EqualTo(50d));
            Assert.That(loaded.Mines[0].Level("picks"), Is.EqualTo(2));
            Assert.That(loaded.FindMine("deepcore").Unlocked, Is.True);
            Assert.That(loaded.Adventure.Owned.Get(WorldGenerator.WarmstonePlot), Is.True);
            Assert.That(loaded.Adventure.Owned.Get(WorldGenerator.CrystalPlot), Is.True);
            Assert.That(loaded.Adventure.Owned.Get(WorldGenerator.DeepCorePlot), Is.True);
            Assert.That(loaded.Adventure.Owned.Count(), Is.EqualTo(6));
            int owned = loaded.Adventure.Owned.Count();
            AdventureRules.ApplyLegacyMigration(loaded);
            Assert.That(loaded.Adventure.Owned.Count(), Is.EqualTo(owned));
            Assert.That(loaded.Adventure.MigrationNote, Is.Not.Empty);

            GameState freshLegacy = SaveCodec.Read("{\"version\":1,\"coins\":0}", config);
            Assert.That(freshLegacy.Adventure.Owned.Get(WorldGenerator.DeepCorePlot), Is.False);
            Assert.That(freshLegacy.Adventure.Owned.Count(), Is.EqualTo(5));
        }

        [Test]
        public void Journey_MineProcessCraftSellAndBuySixthBunker()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            Assert.That(AdventureRules.TrySelectCharacter(state, "kavi", out message), Is.False);
            Assert.That(AdventureRules.TrySelectCharacter(state, "tavi", out message), Is.True);
            Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.True);
            Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.False);
            AdventureRules.Advance(state, JourneyCatalog.MineCooldownSeconds);
            Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.True);
            Assert.That(state.Adventure.HandsAmount, Is.EqualTo(4d));
            Assert.That(AdventureRules.TryStoreHands(state, out message), Is.True);
            RunBatch(state, "crush");
            RunBatch(state, "wash");
            RunBatch(state, "recover");
            Assert.That(state.Adventure.LifetimeDiamondsRecovered, Is.EqualTo(1d));
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.RoughDiamond), Is.EqualTo(1d));
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.Gravel), Is.EqualTo(3d));
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(0d));
            RunBatch(state, "cut");
            RunBatch(state, "polish");
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondG1), Is.EqualTo(1d));
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondG4), Is.EqualTo(0d));
            double recovered = state.Adventure.LifetimeDiamondsRecovered;
            Assert.That(Dig(state, 3, JourneyCatalog.SilverOre), Is.True);
            Assert.That(AdventureRules.TryStoreHands(state, out message), Is.True);
            Assert.That(AdventureRules.TryLoadRefine(state, JourneyCatalog.SilverOre, out message), Is.True);
            AdventureRules.Advance(state, 2d);
            Assert.That(AdventureRules.TryCollectMachine(state, "refine", out message), Is.True);
            Assert.That(AdventureRules.TryStartCraft(state, "ring", out message), Is.True);
            AdventureRules.Advance(state, 1d);
            Assert.That(state.Adventure.ActionClock, Is.LessThan(20d));
            string midway = SaveCodec.Write(state);
            GameState resumed = SaveCodec.Read(midway, config);
            AdventureRules.Advance(resumed, 2d);
            AdventureRules.Advance(resumed, 5d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.Ring), Is.EqualTo(1d));
            Assert.That(resumed.Adventure.LifetimeDiamondsRecovered, Is.EqualTo(recovered));
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondG1), Is.EqualTo(0d));
            Assert.That(AdventureRules.TrySell(resumed, JourneyCatalog.Ring, 1, out message), Is.True);
            Assert.That(AdventureRules.TrySell(resumed, JourneyCatalog.Ring, 1, out message), Is.False);
            Assert.That(resumed.Coins, Is.EqualTo(120d));
            Assert.That(AdventureRules.PlotPrice(resumed.Adventure, 5), Is.LessThan(120d));
            YardPoint door = YardLayout.Entrance(5);
            resumed.Adventure.PlayerX = door.X;
            resumed.Adventure.PlayerZ = door.Z;
            Assert.That(resumed.Adventure.Owned.Get(5), Is.False);
            Assert.That(AdventureRules.TryBuyPlot(resumed, 5, out message), Is.True);
            Assert.That(resumed.Adventure.Owned.Count(), Is.EqualTo(6));
            Assert.That(resumed.Coins, Is.EqualTo(40d));
        }

        [Test]
        public void SellingAllDiamondRock_StillBuysTheNextBunker()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            Assert.That(AdventureRules.TrySelectCharacter(state, "mira", out message), Is.True);
            for (int i = 0; i < 12; i++)
            {
                if (i > 0 && i % 6 == 0)
                    Assert.That(AdventureRules.TryStoreHands(state, out message), Is.True, message);
                AdventureRules.Advance(state, JourneyCatalog.MineCooldownSeconds);
                Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.True, message);
            }
            Assert.That(AdventureRules.TryStoreHands(state, out message), Is.True, message);
            Assert.That(AdventureRules.TrySell(state, JourneyCatalog.DiamondOre, 24, out message), Is.True);
            Assert.That(state.Coins, Is.GreaterThanOrEqualTo(AdventureRules.PlotPrice(state.Adventure, 5)));
            Assert.That(AdventureRules.TryBuyPlot(state, 5, out message), Is.True);
        }

        [Test]
        public void RecoverySweep_WorksWithoutAdsOrDeposits()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            Assert.That(AdventureRules.TrySweepStone(state, out message), Is.True);
            Assert.That(AdventureRules.TrySweepStone(state, out message), Is.False);
            Assert.That(AdventureRules.TrySell(state, JourneyCatalog.Stone, 8, out message), Is.True);
            Assert.That(state.Coins, Is.EqualTo(8d));
            AdventureRules.Advance(state, JourneyCatalog.RecoveryCooldownSeconds);
            Assert.That(AdventureRules.TrySweepStone(state, out message), Is.True);
        }

        [Test]
        public void DepletedBunker_StopsMining()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            AdventureRules.TrySelectCharacter(state, "oren", out message);
            for (int swings = 0; swings < 40; swings++)
            {
                AdventureRules.Advance(state, JourneyCatalog.MineCooldownSeconds);
                if (Dig(state, 2, JourneyCatalog.DiamondOre))
                    continue;
                if (state.Adventure.HandsAmount > 0d)
                {
                    Assert.That(AdventureRules.TryStoreHands(state, out message), Is.True, message);
                    continue;
                }
                break;
            }
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(24d));
            AdventureRules.Advance(state, 1d);
            Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.False);
        }

        [Test]
        public void OfflineWorker_DoesNotDuplicateOrExceedTheDeposit()
        {
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 500d);
            string message;
            Assert.That(AdventureRules.TryHire(state, out message), Is.True);
            Assert.That(AdventureRules.TryAssignWorker(state, 0, out message), Is.True);
            AdventureRules.Advance(state, 8d * 3600d);
            double stone = AdventureRules.Count(state.Adventure, JourneyCatalog.Stone);
            Assert.That(stone, Is.EqualTo(80d));
            Assert.That(AdventureRules.Remaining(state.Adventure, 0, JourneyCatalog.Stone), Is.EqualTo(0d));
            string json = SaveCodec.Write(state);
            GameState loaded = SaveCodec.Read(json, config);
            AdventureRules.Advance(loaded, 3600d);
            Assert.That(AdventureRules.Count(loaded.Adventure, JourneyCatalog.Stone), Is.EqualTo(stone));
        }

        [Test]
        public void ManyOwnedPlots_UseTheSameWorkerMath()
        {
            GameState small = PreparedWorker();
            GameState large = PreparedWorker();
            for (int id = 20; id < 3020; id++)
                large.Adventure.Owned.Set(id, true);
            AdventureRules.Advance(small, 30d);
            AdventureRules.Advance(large, 30d);
            Assert.That(AdventureRules.Count(large.Adventure, JourneyCatalog.Stone), Is.EqualTo(AdventureRules.Count(small.Adventure, JourneyCatalog.Stone)));
            string json = SaveCodec.Write(large);
            Assert.That(json.Length, Is.LessThan(JsonValue.MaxTextLength));
            GameState loaded = SaveCodec.Read(json, config);
            Assert.That(loaded.Adventure.Owned.Count(), Is.EqualTo(large.Adventure.Owned.Count()));
        }

        [Test]
        public void Prestige_KeepsBunkerOwnershipAndClearsCrew()
        {
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, config.PrestigeMinimumCoins);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "hire", 1), Is.True);
            string message;
            Assert.That(AdventureRules.TrySelectCharacter(state, "sola", out message), Is.True);
            Wallet.Credit(state, 500d);
            Assert.That(AdventureRules.TryHire(state, out message), Is.True);
            Assert.That(AdventureRules.TryBuyPlot(state, 5, out message), Is.True);
            state.Adventure.Machines[0].Level = 2;
            Assert.That(PrestigeService.TryPrestige(state, config), Is.True);
            Assert.That(state.Coins, Is.EqualTo(0d));
            Assert.That(state.Mines[0].Level("hire"), Is.EqualTo(0));
            Assert.That(state.Adventure.Owned.Get(5), Is.True);
            Assert.That(state.Adventure.SelectedCharacter, Is.EqualTo("sola"));
            Assert.That(state.Adventure.WorkerHired, Is.False);
            Assert.That(state.Adventure.Machines[0].Level, Is.EqualTo(0));
            Assert.That(state.Adventure.StarterGrantApplied, Is.True);
            Assert.That(state.Adventure.Owned.Count(), Is.EqualTo(6));
        }

        [Test]
        public void TasksAndOrders_ClaimOnce()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            AdventureRules.TrySelectCharacter(state, "tavi", out message);
            for (int plot = 0; plot < 5; plot++)
                Assert.That(AdventureRules.TryVisit(state, plot, out message), Is.True);
            Assert.That(AdventureRules.TryClaimTask(state, "visit5", out message), Is.True);
            Assert.That(AdventureRules.TryClaimTask(state, "visit5", out message), Is.False);
            state.Adventure.Inventory[JourneyCatalog.Stone] = 5;
            double before = state.Coins;
            Assert.That(AdventureRules.TryDeliverOrder(state, out message), Is.True);
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.Stone), Is.EqualTo(0d));
            Assert.That(state.Coins, Is.EqualTo(before + 25d));
        }

        [Test]
        public void CharacterChoice_DoesNotChangeSalePrice()
        {
            GameState tavi = GameFactory.NewGame(config);
            GameState mira = GameFactory.NewGame(config);
            string message;
            AdventureRules.TrySelectCharacter(tavi, "tavi", out message);
            AdventureRules.TrySelectCharacter(mira, "mira", out message);
            tavi.Adventure.Inventory[JourneyCatalog.Stone] = 4;
            mira.Adventure.Inventory[JourneyCatalog.Stone] = 4;
            Assert.That(AdventureRules.TrySell(tavi, JourneyCatalog.Stone, 4, out message), Is.True);
            Assert.That(AdventureRules.TrySell(mira, JourneyCatalog.Stone, 4, out message), Is.True);
            Assert.That(mira.Coins, Is.EqualTo(tavi.Coins));
        }

        [Test]
        public void RankRules_RejectForgedTotalsAndReplays()
        {
            Assert.That(RankRules.ProductionEnabled, Is.False);
            var rejected = RankRules.SubmitClientTotal(999999);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("client_total_unverified"));
            var book = new RankBook();
            long now = 1_700_000_000_000L;
            var good = new RankEvent { EventId = "rec-1", Diamonds = 1, OreConsumed = 4, ServerUnixMs = now };
            RankDecision first = RankRules.Submit(book, good, now);
            RankDecision replay = RankRules.Submit(book, good, now);
            var forged = new RankEvent { EventId = "rec-2", Diamonds = 10, OreConsumed = 4, ServerUnixMs = now + 1 };
            RankDecision cheat = RankRules.Submit(book, forged, now + 1);
            Assert.That(first.Accepted, Is.True);
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.VerifiedTotal, Is.EqualTo(1));
            Assert.That(cheat.Accepted, Is.False);
            Assert.That(cheat.Reason, Is.EqualTo("over_recovery"));
            Assert.That(book.VerifiedDiamonds, Is.EqualTo(1));
            var early = new RankRow { Nickname = "AmberCart", Diamonds = 5, FirstReachedUnixMs = 20 };
            var late = new RankRow { Nickname = "PebbleLamp", Diamonds = 5, FirstReachedUnixMs = 30 };
            Assert.That(RankRules.Compare(late, early), Is.GreaterThan(0));
            Assert.That(RankRules.WeekId(8L * 24L * 60L * 60L * 1000L), Is.EqualTo(1L));
            Assert.That(RankRules.AllowRequest(30, 30), Is.False);
        }

        [Test]
        public void ExtremeSell_IsRejected()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            state.Adventure.Inventory[JourneyCatalog.Stone] = 4;
            Assert.That(AdventureRules.TrySell(state, JourneyCatalog.Stone, int.MaxValue, out message), Is.False);
            Assert.That(state.Coins, Is.EqualTo(0d));
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.Stone), Is.EqualTo(4d));
            AdventureRules.Advance(state, -50d);
            Assert.That(state.Adventure.ActionClock, Is.EqualTo(0d));
        }

        [Test]
        public void Movement_DoesNotBuyAndStopsAtRocks()
        {
            GameState state = GameFactory.NewGame(config);
            YardPoint rock = YardLayout.Rock(0);
            state.Adventure.PlayerX = rock.X;
            state.Adventure.PlayerZ = rock.Z - 2d;
            YardLayout.Move(state.Adventure, 0d, 3d);
            Assert.That(YardLayout.Blocked(state.Adventure.PlayerX, state.Adventure.PlayerZ), Is.False);
            Assert.That(state.Adventure.Owned.Count(), Is.EqualTo(5));
            YardLayout.ReturnHome(state.Adventure);
            Assert.That(state.Adventure.PlayerX, Is.EqualTo(YardLayout.HomeX));
        }

        [Test]
        public void Puppet_UsesTheSharedPoseSet()
        {
            Assert.That(PuppetPose.Required.Length, Is.EqualTo(10));
            Assert.That(PuppetPose.Resolve(new PoseInput { Moving = true, Sprint = true }), Is.EqualTo("run"));
            Assert.That(PuppetPose.Resolve(new PoseInput { Carrying = true }), Is.EqualTo("carry"));
            Assert.That(PuppetPose.Resolve(new PoseInput { WaveSeconds = 1d, Mining = true }), Is.EqualTo("wave"));
            Assert.That(JourneyCatalog.AllCharacters.Count, Is.EqualTo(12));
            Assert.That(JourneyCatalog.AllRecipes.Count, Is.EqualTo(10));
            JewelRecipe earrings = JourneyCatalog.FindRecipe("earrings");
            Assert.That(earrings.Inputs[0].Count, Is.EqualTo(2));
            JewelRecipe set = JourneyCatalog.FindRecipe("set");
            Assert.That(set.Inputs.Count, Is.EqualTo(9));
        }

        [Test]
        public void Queries_PageOwnedAndForSalePlots()
        {
            GameState state = GameFactory.NewGame(config);
            var owned = new List<int>();
            var sale = new List<int>();
            AdventureRules.Query(state.Adventure, "owned", 0, 0, 10, owned);
            AdventureRules.Query(state.Adventure, "sale", 0, 0, 3, sale);
            Assert.That(owned, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(sale, Is.EqualTo(new[] { 5, 6, 7 }));
            Assert.That(AdventureRules.PreviewPlot(state.Adventure, 80), Does.Contain("Price"));
        }

        [Test]
        public void Cart_DeliversTheSameLoadOnce_AfterReload_AndWaitsWhenTheMillIsFull()
        {
            GameState state = GameFactory.NewGame(config);
            string message;
            Assert.That(AdventureRules.TrySelectCharacter(state, "tavi", out message), Is.True, message);
            Assert.That(Dig(state, 2, JourneyCatalog.DiamondOre), Is.True);
            Assert.That(AdventureRules.TrySendCart(state, out message), Is.True, message);
            Assert.That(state.Adventure.HandsAmount, Is.EqualTo(0d));
            Assert.That(AdventureRules.TrySendCart(state, out message), Is.False);
            AdventureRules.Advance(state, 1d);
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(0d));

            GameState copy = state.Clone();
            AdventureRules.Advance(copy, 5d);
            Assert.That(AdventureRules.Count(state.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(0d));
            Assert.That(AdventureRules.Count(copy.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(2d));

            string json = SaveCodec.Write(state);
            GameState resumed = SaveCodec.Read(json, config);
            AdventureRules.Advance(resumed, 3d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(2d));
            AdventureRules.Advance(resumed, 5d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(2d));

            resumed.Adventure.WarehouseCapacity = 1;
            Assert.That(Dig(resumed, 2, JourneyCatalog.DiamondOre), Is.True);
            Assert.That(AdventureRules.TrySendCart(resumed, out message), Is.True, message);
            AdventureRules.Advance(resumed, 5d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(2d));
            Assert.That(resumed.Adventure.CartLoad, Is.EqualTo(2d));
            resumed.Adventure.WarehouseCapacity = 400;
            AdventureRules.Advance(resumed, 0d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(4d));
            Assert.That(resumed.Adventure.CartLoad, Is.EqualTo(0d));
            AdventureRules.Advance(resumed, 0d);
            Assert.That(AdventureRules.Count(resumed.Adventure, JourneyCatalog.DiamondOre), Is.EqualTo(4d));
        }

        static bool Dig(GameState state, int plot, string item)
        {
            string message;
            return AdventureRules.TryDig(state, plot, item, out message);
        }

        static void RunBatch(GameState state, string machineId)
        {
            string message;
            Assert.That(AdventureRules.TryLoadMachine(state, machineId, out message), Is.True, message);
            AdventureRules.Advance(state, 3d);
            Assert.That(AdventureRules.TryCollectMachine(state, machineId, out message), Is.True, message);
        }

        GameState PreparedWorker()
        {
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 500d);
            string message;
            AdventureRules.TryHire(state, out message);
            AdventureRules.TryAssignWorker(state, 0, out message);
            return state;
        }
    }
}
