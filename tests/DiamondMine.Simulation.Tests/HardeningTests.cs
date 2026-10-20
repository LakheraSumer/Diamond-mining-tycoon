using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class HardeningTests
    {
        EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            config = EconomyLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "economy.json")));
        }

        [Test]
        public void ChecksumEdit_RecoversLastGoodBackupAndArchivesDamage()
        {
            var io = new MemoryIO();
            var store = new SaveGameStore(io, "save");
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 15);
            Assert.That(store.Save(state).Success, Is.True);
            Wallet.Credit(state, 7);
            Assert.That(store.Save(state).Success, Is.True);
            io.Files["save"] = io.Files["save"].Replace("22", "99");
            string damaged = io.Files["save"];
            LoadOutcome loaded = store.Load(config);
            Assert.That(loaded.Status, Is.EqualTo(LoadStatus.RestoredBackup));
            Assert.That(loaded.State.Coins, Is.EqualTo(15));
            Assert.That(store.Save(loaded.State).Success, Is.True);
            Assert.That(io.Files.Where(p => p.Key.StartsWith("save.corrupt-")).Select(p => p.Value), Does.Contain(damaged));
            Assert.That(store.Load(config).State.Coins, Is.EqualTo(15));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewerBackup_IsProtectedEvenWithMissingOrBrokenPrimary(bool brokenPrimary)
        {
            var io = new MemoryIO();
            if (brokenPrimary) io.Files["save"] = "{";
            io.Files["save.bak"] = "{\"version\":99}";
            var store = new SaveGameStore(io, "save");
            Assert.That(store.Load(config).Status, Is.EqualTo(LoadStatus.NewerVersion));
            Assert.That(store.Save(GameFactory.NewGame(config)).Success, Is.False);
            Assert.That(io.Files["save.bak"], Does.Contain("99"));
        }

        [TestCase("processInput")]
        [TestCase("rough")]
        [TestCase("oreAtFace")]
        [TestCase("storage")]
        public void EditedBuffers_RejectQuantitiesAboveCapacity(string field)
        {
            string json = "{\"version\":1,\"mines\":[{\"id\":\"warmstone\",\"" + field + "\":1000000000}]}";
            Assert.Throws<InvalidSaveException>(() => SaveCodec.Read(json, config));
        }

        [TestCase("1e100")]
        [TestCase("1.5")]
        public void UpgradeLevels_RejectOverflowAndFractionalValues(string value)
        {
            string json = "{\"version\":1,\"mines\":[{\"id\":\"warmstone\",\"upgrades\":{\"picks\":" + value + "}}]}";
            Assert.Throws<FormatException>(() => SaveCodec.Read(json, config));
        }

        [TestCase("-1")]
        [TestCase("1e100")]
        [TestCase("1.5")]
        public void Timestamp_RejectsInvalidRanges(string value)
        {
            Assert.Throws<InvalidSaveException>(() => SaveCodec.Read("{\"lastActiveUnixMs\":" + value + "}", config));
        }

        [TestCase("{\"coins\":1,\"coins\":2}")]
        [TestCase("{\"version\":1.5}")]
        [TestCase("{\"coins\":01}")]
        [TestCase("{\"coins\":1.}")]
        [TestCase("{\"coins\":1e500}")]
        [TestCase("{\"activeMineId\":\"bad\ntext\"}")]
        public void AmbiguousJson_IsRejected(string json)
        {
            Assert.Throws<FormatException>(() => SaveCodec.Read(json, config));
        }

        [Test]
        public void JsonSizeAndDepth_AreBounded()
        {
            Assert.Throws<FormatException>(() => JsonValue.Parse(new string(' ', JsonValue.MaxTextLength + 1)));
            Assert.Throws<FormatException>(() => JsonValue.Parse(new string('[', 80) + "0" + new string(']', 80)));
        }

        [Test]
        public void EditedCart_RejectsResourceInjection()
        {
            string json = "{\"version\":1,\"mines\":[{\"id\":\"warmstone\",\"carts\":[{\"phase\":1,\"timer\":1,\"load\":1e20}]}]}";
            Assert.Throws<InvalidSaveException>(() => SaveCodec.Read(json, config));
        }

        [Test]
        public void NegativePurchaseAndHugeBuyMaximum_DoNotSpendOrOverflow()
        {
            var state = GameFactory.NewGame(config);
            Wallet.Credit(state, 1000);
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "picks", -5), Is.False);
            Assert.That(state.Coins, Is.EqualTo(1000));
            Assert.That(UpgradeService.MaxAffordable(1e20, 0, int.MaxValue, 1, 1), Is.EqualTo(int.MaxValue));
            Assert.That(UpgradeService.MaxAffordable(0.99999999, 0, 1, 1, 1), Is.EqualTo(0));
            state.Coins = 0.99999999;
            Assert.That(Wallet.TrySpend(state, 1), Is.False);
            Wallet.Credit(state, config.PrestigeMinimumCoins * 2);
            UpgradeService.TryBuy(state, config, "warmstone", "hire", 1);
            state.PrestigeCount = int.MaxValue;
            Assert.That(PrestigeService.TryPrestige(state, config), Is.False);
            Assert.That(state.PrestigeCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void UpgradeWhileCartTravels_StillLoadsSuccessfully()
        {
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 10000);
            state.Mines[0].Carts[0].Phase = CartPhase.Outbound;
            state.Mines[0].Carts[0].Timer = config.Mines[0].TripSeconds;
            state.Mines[0].Carts[0].Load = 1;
            Assert.That(UpgradeService.TryBuy(state, config, "warmstone", "rails", 1), Is.True);
            Assert.DoesNotThrow(() => SaveCodec.Read(SaveCodec.Write(state), config));
        }

        [Test]
        public void OfflineClaim_InterruptionAndRelaunchDoNotDuplicateReward()
        {
            var io = new MemoryIO();
            var store = new SaveGameStore(io, "save");
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 10000);
            UpgradeService.TryBuy(state, config, "warmstone", "hire", 1);
            state.LastActiveUnixMs = 1000000;
            store.Save(state);
            io.FailWrites = true;
            string error;
            Assert.That(SaveGameStore.Commit(state, store, draft =>
            {
                OfflineEarnings.Apply(draft, config, 3600);
                draft.LastActiveUnixMs += 3600000;
            }, out error), Is.False);
            Assert.That(state.LastActiveUnixMs, Is.EqualTo(1000000));
            io.FailWrites = false;
            Assert.That(SaveGameStore.Commit(state, store, draft =>
            {
                OfflineEarnings.Apply(draft, config, 3600);
                draft.LastActiveUnixMs += 3600000;
            }, out error), Is.True);
            GameState relaunched = store.Load(config).State;
            Assert.That(relaunched.LastActiveUnixMs, Is.EqualTo(4600000));
            double baseCoins = relaunched.Coins;
            string grant = relaunched.LastOfflineGrantId;
            Assert.That(SaveGameStore.Commit(relaunched, store, draft =>
                Assert.That(AdRewards.TryGrantOfflineBonus(draft, config, grant), Is.True), out error), Is.True);
            GameState again = store.Load(config).State;
            Assert.That(again.Coins, Is.GreaterThan(baseCoins));
            double claimed = again.Coins;
            Assert.That(AdRewards.TryGrantOfflineBonus(again, config, grant), Is.False);
            Assert.That(again.Coins, Is.EqualTo(claimed));
        }

        sealed class MemoryIO : ISaveIO
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool FailWrites;
            public bool Exists(string path) => Files.ContainsKey(path);
            public string ReadAllText(string path) => Files[path];
            public void WriteAllText(string path, string contents)
            {
                if (FailWrites) throw new IOException("Simulated full disk");
                Files[path] = contents;
            }
            public void Delete(string path) => Files.Remove(path);
            public void ReplaceWithBackup(string tempPath, string finalPath, string backupPath)
            {
                if (Files.ContainsKey(finalPath)) Files[backupPath] = Files[finalPath];
                Files[finalPath] = Files[tempPath];
                Files.Remove(tempPath);
            }
        }
    }
}
