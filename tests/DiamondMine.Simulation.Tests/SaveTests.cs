using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class SaveTests
    {
        EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            config = EconomyLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "economy.json")));
        }

        [Test]
        public void RoundTrip_PreservesProgress()
        {
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 1234.5);
            state.Mines[0].UpgradeLevels["picks"] = 2;
            state.TutorialStep = 4;
            state.Settings.ReducedMotion = true;
            state.LastActiveUnixMs = 1_700_000_000_000;
            state.UsedAdTokens.Add("ad-1");
            string json = SaveCodec.Write(state);
            GameState loaded = SaveCodec.Read(json, config);
            Assert.That(loaded.Coins, Is.EqualTo(state.Coins).Within(1e-6));
            Assert.That(loaded.Mines[0].Level("picks"), Is.EqualTo(2));
            Assert.That(loaded.TutorialStep, Is.EqualTo(4));
            Assert.That(loaded.Settings.ReducedMotion, Is.True);
            Assert.That(loaded.LastActiveUnixMs, Is.EqualTo(1_700_000_000_000));
            Assert.That(loaded.UsedAdTokens, Does.Contain("ad-1"));
        }

        [Test]
        public void LegacyMoney_MigratesToCoins()
        {
            GameState loaded = SaveCodec.Read("{\"money\":42}", config);
            Assert.That(loaded.Coins, Is.EqualTo(42));
            Assert.That(loaded.LifetimeCoins, Is.EqualTo(42));
            Assert.That(loaded.Version, Is.EqualTo(SaveCodec.CurrentVersion));
        }

        [Test]
        public void CorruptPrimary_RestoresBackup_AndLeavesBrokenFiles()
        {
            var io = new MemorySaveIO();
            var store = new SaveGameStore(io, "save.json");
            GameState good = GameFactory.NewGame(config);
            Wallet.Credit(good, 80);
            Assert.That(store.Save(good).Success, Is.True);
            Wallet.Credit(good, 20);
            Assert.That(store.Save(good).Success, Is.True);
            io.Files["save.json"] = "{broken";
            LoadOutcome outcome = store.Load(config);
            Assert.That(outcome.Status, Is.EqualTo(LoadStatus.RestoredBackup));
            Assert.That(outcome.State.Coins, Is.EqualTo(80).Within(1e-6));
            Assert.That(io.Files["save.json"], Is.EqualTo("{broken"));
        }

        [Test]
        public void BothCopiesCorrupt_StartFreshWithoutDeletingFiles()
        {
            var io = new MemorySaveIO();
            io.Files["save.json"] = "{";
            io.Files["save.json.bak"] = "{";
            var outcome = new SaveGameStore(io, "save.json").Load(config);
            Assert.That(outcome.Status, Is.EqualTo(LoadStatus.RecoveredNewGame));
            Assert.That(outcome.State, Is.Null);
            Assert.That(io.Files.ContainsKey("save.json"), Is.True);
            Assert.That(io.Files.ContainsKey("save.json.bak"), Is.True);
        }

        [Test]
        public void NewerSave_IsNotOverwritten()
        {
            var io = new MemorySaveIO();
            io.Files["save.json"] = "{\"version\":99,\"coins\":10}";
            LoadOutcome outcome = new SaveGameStore(io, "save.json").Load(config);
            Assert.That(outcome.Status, Is.EqualTo(LoadStatus.NewerVersion));
            Assert.That(io.Files["save.json"], Does.Contain("99"));
        }

        [Test]
        public void NegativeCoins_AreRejected()
        {
            Assert.Throws<InvalidSaveException>(() => SaveCodec.Read("{\"version\":1,\"coins\":-5}", config));
        }

        [Test]
        public void InterruptedReplace_KeepsThePreviousSave()
        {
            var io = new MemorySaveIO();
            var store = new SaveGameStore(io, "save.json");
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 50);
            Assert.That(store.Save(state).Success, Is.True);
            io.FailReplace = true;
            Wallet.Credit(state, 25);
            SaveOutcome failed = store.Save(state);
            Assert.That(failed.Success, Is.False);
            io.FailReplace = false;
            LoadOutcome loaded = store.Load(config);
            Assert.That(loaded.State.Coins, Is.EqualTo(50).Within(1e-6));
        }

        [Test]
        public void FailedWrite_DoesNotApplyAnUpgrade()
        {
            var io = new MemorySaveIO();
            var store = new SaveGameStore(io, "save.json");
            GameState state = GameFactory.NewGame(config);
            Wallet.Credit(state, 500);
            store.Save(state);
            io.FailWrites = true;
            string error;
            bool committed = SaveGameStore.Commit(state, store, delegate (GameState draft)
            {
                Assert.That(UpgradeService.TryBuy(draft, config, "warmstone", "picks", 1), Is.True);
            }, out error);
            Assert.That(committed, Is.False);
            Assert.That(state.Mines[0].Level("picks"), Is.EqualTo(0));
            Assert.That(state.Coins, Is.EqualTo(500).Within(1e-6));
        }

        [Test]
        public void RealFiles_ReplaceAtomicallyAndKeepABackup()
        {
            string directory = Path.Combine(Path.GetTempPath(), "diamond-mine-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var store = new SaveGameStore(new SystemSaveIO(), Path.Combine(directory, "profile.json"));
                GameState first = GameFactory.NewGame(config);
                Wallet.Credit(first, 15);
                Assert.That(store.Save(first).Success, Is.True);
                Wallet.Credit(first, 7);
                Assert.That(store.Save(first).Success, Is.True);
                Assert.That(File.Exists(store.Path + ".bak"), Is.True);
                GameState backup = SaveCodec.Read(File.ReadAllText(store.Path + ".bak"), config);
                GameState current = SaveCodec.Read(File.ReadAllText(store.Path), config);
                Assert.That(backup.Coins, Is.EqualTo(15).Within(1e-6));
                Assert.That(current.Coins, Is.EqualTo(22).Within(1e-6));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingPrimary_KeepsRecoveryBackupEvenWhenMoveFails(bool missingTemp)
        {
            string directory = Path.Combine(Path.GetTempPath(), "diamond-mine-recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string primary = Path.Combine(directory, "profile.json");
            string backup = primary + ".bak";
            string temp = primary + ".tmp";
            try
            {
                File.WriteAllText(backup, "last-good-save");
                if (!missingTemp) File.WriteAllText(temp, "new-save");
                var io = new SystemSaveIO();
                if (missingTemp)
                    Assert.Throws<FileNotFoundException>(() => io.ReplaceWithBackup(temp, primary, backup));
                else
                {
                    io.ReplaceWithBackup(temp, primary, backup);
                    Assert.That(File.ReadAllText(primary), Is.EqualTo("new-save"));
                }
                Assert.That(File.ReadAllText(backup), Is.EqualTo("last-good-save"));
            }
            finally
            {
                foreach (string file in new[] { primary, backup, temp })
                    if (File.Exists(file)) File.Delete(file);
                Directory.Delete(directory);
            }
        }

        sealed class MemorySaveIO : ISaveIO
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool FailWrites;
            public bool FailReplace;

            public bool Exists(string path) { return Files.ContainsKey(path); }
            public string ReadAllText(string path) { return Files[path]; }
            public void Delete(string path) { Files.Remove(path); }

            public void WriteAllText(string path, string contents)
            {
                if (FailWrites)
                    throw new IOException("No space left on device.");
                Files[path] = contents;
            }

            public void ReplaceWithBackup(string tempPath, string finalPath, string backupPath)
            {
                if (FailReplace)
                    throw new IOException("Replace failed.");
                if (!Files.ContainsKey(tempPath))
                    throw new IOException("Temp save missing.");
                if (Files.ContainsKey(finalPath))
                    Files[backupPath] = Files[finalPath];
                Files[finalPath] = Files[tempPath];
                Files.Remove(tempPath);
            }
        }
    }
}
