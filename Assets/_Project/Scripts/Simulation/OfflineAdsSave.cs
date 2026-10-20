using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DiamondMine.Simulation
{
    public sealed class StageDiagnosis
    {
        public string Stage;
        public string Title;
        public string Reason;
        public double LimitPerSecond;
    }

    public static class BottleneckService
    {
        public static StageDiagnosis Diagnose(MineState mine, MineRates rates)
        {
            double transportOre = rates.CartCount * rates.CartCapacity / (2d * Math.Max(1.1, rates.TripSeconds));
            double miningOre = rates.AutoUnlocked ? rates.AutoRate : 0d;
            double mining = miningOre * rates.Yield;
            double transport = transportOre * rates.Yield;
            double processing = rates.ProcessRate * rates.Yield;
            double sorting = rates.SortRate;
            double sales = rates.SellRate;

            bool storageFull = mine.Storage >= rates.StorageCap - 1e-6;
            bool roughFull = mine.Rough >= rates.RoughBuffer - 1e-6;
            bool processFull = mine.ProcessInput >= rates.ProcessBuffer - 1e-6;
            bool faceFull = mine.OreAtFace >= rates.MineBuffer - 1e-6;
            bool cartWaiting = false;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                if (mine.Carts[i].Phase == CartPhase.Unloading && mine.Carts[i].Load > 1e-6)
                    cartWaiting = true;
            }

            if (mine.TapCount == 0 && mine.OreMined <= 1e-6)
                return Make(Stages.Mining, "Mining", "Tap the rock to start the chain.", 0d);
            if (storageFull)
                return Make(Stages.Sales, "Sales", "The vault is full. Sales are the slowest stage, so the rest of the mine is waiting.", sales);
            if (roughFull)
                return Make(Stages.Sorting, "Polishing", "Rough gems are waiting at the polishing table.", sorting);
            if (processFull || cartWaiting)
                return Make(Stages.Processing, "Processing", "The crusher is full. Ore cannot move in until it clears.", processing);
            if (faceFull)
                return Make(Stages.Transport, "Transport", "Ore is piling up at the face. The carts are too slow.", transport);
            if (!rates.AutoUnlocked && mine.OreAtFace <= 1e-6 && mine.ProcessInput <= 1e-6 && mine.Storage <= 1e-6)
                return Make(Stages.Mining, "Mining", "The face is idle. Tap to mine, or hire a miner.", 0d);

            string stage = Stages.Sales;
            string title = "Sales";
            double limit = sales;
            if (sorting < limit) { stage = Stages.Sorting; title = "Polishing"; limit = sorting; }
            if (processing < limit) { stage = Stages.Processing; title = "Processing"; limit = processing; }
            if (transport < limit) { stage = Stages.Transport; title = "Transport"; limit = transport; }
            if (rates.AutoUnlocked && mining < limit) { stage = Stages.Mining; title = "Mining"; limit = mining; }
            return Make(stage, title, title + " is the slowest stage right now.", limit);
        }

        static StageDiagnosis Make(string stage, string title, string reason, double limit)
        {
            return new StageDiagnosis { Stage = stage, Title = title, Reason = reason, LimitPerSecond = limit };
        }
    }

    public sealed class ClockDecision
    {
        public double SimulatedSeconds;
        public string Anomaly;
        public bool Capped;
    }

    public static class ClockPolicy
    {
        public static ClockDecision Evaluate(double elapsedSeconds, EconomyConfig config)
        {
            var decision = new ClockDecision();
            if (!NumericFormat.IsFinite(elapsedSeconds))
            {
                decision.Anomaly = "invalid";
                return decision;
            }
            if (elapsedSeconds < -Math.Max(0d, config.BackwardClockGraceSeconds))
            {
                decision.Anomaly = "backward";
                return decision;
            }
            if (elapsedSeconds <= 0d)
                return decision;

            decision.Capped = elapsedSeconds > config.OfflineCapSeconds;
            decision.SimulatedSeconds = Math.Min(elapsedSeconds, config.OfflineCapSeconds);
            if (elapsedSeconds > config.ImplausibleForwardSeconds)
                decision.Anomaly = "implausible_forward";
            return decision;
        }
    }

    public sealed class OfflinePlan
    {
        public double SecondsAway;
        public double SecondsSimulated;
        public double Coins;
        public bool Capped;
        public string Anomaly;
        public string GrantId;
        public double BonusCoins;
    }

    public static class OfflineEarnings
    {
        public static OfflinePlan Plan(GameState state, EconomyConfig config, double elapsedSeconds)
        {
            ClockDecision clock = ClockPolicy.Evaluate(elapsedSeconds, config);
            GameState clone = state.Clone();
            double before = clone.Coins;
            if (clock.SimulatedSeconds > 0d)
                ProductionSimulator.Advance(clone, config, clock.SimulatedSeconds);
            double coins = Math.Max(0d, clone.Coins - before);
            var plan = new OfflinePlan
            {
                SecondsAway = NumericFormat.IsFinite(elapsedSeconds) ? elapsedSeconds : 0d,
                SecondsSimulated = clock.SimulatedSeconds,
                Coins = coins,
                Capped = clock.Capped,
                Anomaly = clock.Anomaly,
                GrantId = MakeGrantId(state, clock.SimulatedSeconds, coins),
                BonusCoins = coins * config.OfflineAdBonusFraction
            };
            return plan;
        }

        public static OfflinePlan Apply(GameState state, EconomyConfig config, double elapsedSeconds)
        {
            OfflinePlan plan = Plan(state, config, elapsedSeconds);
            if (plan.SecondsSimulated > 0d)
                ProductionSimulator.Advance(state, config, plan.SecondsSimulated);
            state.LastOfflineGrantId = plan.GrantId;
            state.LastOfflineBaseCoins = plan.Coins;
            state.LastOfflineBonusClaimed = false;
            return plan;
        }

        public static string MakeGrantId(GameState state, double simulatedSeconds, double coins)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + state.PrestigeCount;
                hash = hash * 31 + (int)Math.Round(state.TotalPlaySeconds * 10d);
                hash = hash * 31 + (int)Math.Round(simulatedSeconds);
                hash = hash * 31 + (int)Math.Round(coins * 100d);
                return "off-" + hash.ToString("X8", CultureInfo.InvariantCulture);
            }
        }
    }

    public static class AdRewards
    {
        public static bool TryGrantOfflineBonus(GameState state, EconomyConfig config, string grantId)
        {
            if (string.IsNullOrEmpty(grantId) || state.LastOfflineGrantId != grantId)
                return false;
            if (state.LastOfflineBonusClaimed)
                return false;
            double bonus = state.LastOfflineBaseCoins * config.OfflineAdBonusFraction;
            if (!NumericFormat.IsFinite(bonus) || bonus < 0d)
                return false;
            if (bonus > 0d)
                Wallet.Credit(state, bonus);
            state.LastOfflineBonusClaimed = true;
            return true;
        }

        public static bool TryGrantBoost(GameState state, EconomyConfig config, string token)
        {
            if (string.IsNullOrEmpty(token) || state.UsedAdTokens.Contains(token))
                return false;
            state.UsedAdTokens.Add(token);
            while (state.UsedAdTokens.Count > Math.Max(1, config.MaxAdTokenHistory))
                state.UsedAdTokens.RemoveAt(0);
            double duration = Math.Max(1d, config.BoostDurationSeconds);
            state.BoostRemainingSeconds = Math.Max(state.BoostRemainingSeconds, duration);
            return true;
        }
    }

    public interface ISaveIO
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        void ReplaceWithBackup(string tempPath, string finalPath, string backupPath);
        void Delete(string path);
    }

    public sealed class SystemSaveIO : ISaveIO
    {
        public bool Exists(string path) { return File.Exists(path); }

        public string ReadAllText(string path)
        {
            if (new FileInfo(path).Length > JsonValue.MaxTextLength)
                throw new InvalidSaveException("Save exceeds the supported size.");
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public void WriteAllText(string path, string contents)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            byte[] bytes = Encoding.UTF8.GetBytes(contents);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        public void ReplaceWithBackup(string tempPath, string finalPath, string backupPath)
        {
            if (!File.Exists(finalPath))
            {
                File.Move(tempPath, finalPath);
                return;
            }
            File.Replace(tempPath, finalPath, backupPath, true);
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    public enum LoadStatus
    {
        Ok,
        NewGame,
        RestoredBackup,
        RecoveredNewGame,
        NewerVersion
    }

    public sealed class LoadOutcome
    {
        public LoadStatus Status;
        public GameState State;
        public string Message;
    }

    public sealed class SaveOutcome
    {
        public bool Success;
        public string Error;
    }

    public static class SaveCodec
    {
        public const int CurrentVersion = 1;

        public static string Write(GameState state)
        {
            var json = new JsonComposer();
            json.BeginObject();
            json.Prop("version", CurrentVersion);
            json.Prop("coins", state.Coins);
            json.Prop("mastery", state.Mastery);
            json.Prop("lifetimeCoins", state.LifetimeCoins);
            json.Prop("coinsSincePrestige", state.CoinsSincePrestige);
            json.Prop("coinsEarned", state.CoinsEarned);
            json.Prop("coinsSpent", state.CoinsSpent);
            json.Prop("coinsDiscarded", state.CoinsDiscarded);
            json.Prop("prestigeCount", state.PrestigeCount);
            json.Prop("activeMineId", state.ActiveMineId ?? "");
            json.Prop("boostRemainingSeconds", state.BoostRemainingSeconds);
            json.Prop("totalPlaySeconds", state.TotalPlaySeconds);
            json.Prop("lastActiveUnixMs", state.LastActiveUnixMs);
            json.Prop("tutorialStep", state.TutorialStep);
            json.Prop("tutorialComplete", state.TutorialComplete);
            json.Prop("bottleneckSeen", state.BottleneckSeen);
            json.Prop("markedStage", state.MarkedStage ?? "");
            json.Prop("markedStageLevels", state.MarkedStageLevels);
            json.Prop("objectivePresented", state.ObjectivePresented);
            json.Prop("lastOfflineGrantId", state.LastOfflineGrantId ?? "");
            json.Prop("lastOfflineBaseCoins", state.LastOfflineBaseCoins);
            json.Prop("lastOfflineBonusClaimed", state.LastOfflineBonusClaimed);
            json.Name("usedAdTokens");
            json.BeginArray();
            for (int i = 0; i < state.UsedAdTokens.Count; i++)
                json.Value(state.UsedAdTokens[i]);
            json.EndArray();
            json.Name("claimedObjectives");
            json.BeginArray();
            for (int i = 0; i < state.ClaimedObjectives.Count; i++)
                json.Value(state.ClaimedObjectives[i]);
            json.EndArray();
            json.Name("claimedOrders");
            json.BeginArray();
            for (int i = 0; i < state.ClaimedOrders.Count; i++)
                json.Value(state.ClaimedOrders[i]);
            json.EndArray();
            json.Name("settings");
            json.BeginObject();
            json.Prop("music", state.Settings.Music);
            json.Prop("sfx", state.Settings.Sfx);
            json.Prop("haptics", state.Settings.Haptics);
            json.Prop("reducedMotion", state.Settings.ReducedMotion);
            json.Prop("quality", state.Settings.Quality);
            json.EndObject();
            json.Name("mines");
            json.BeginArray();
            for (int i = 0; i < state.Mines.Count; i++)
                WriteMine(json, state.Mines[i]);
            json.EndArray();
            json.EndObject();
            return json.ToString();
        }

        static void WriteMine(JsonComposer json, MineState mine)
        {
            json.BeginObject();
            json.Prop("id", mine.Id ?? "");
            json.Prop("unlocked", mine.Unlocked);
            json.Prop("oreAtFace", mine.OreAtFace);
            json.Prop("processInput", mine.ProcessInput);
            json.Prop("rough", mine.Rough);
            json.Prop("storage", mine.Storage);
            json.Prop("oreMined", mine.OreMined);
            json.Prop("oreProcessed", mine.OreProcessed);
            json.Prop("diamondsProduced", mine.DiamondsProduced);
            json.Prop("diamondsSold", mine.DiamondsSold);
            json.Prop("tapCount", mine.TapCount);
            json.Prop("tripsStarted", mine.TripsStarted);
            json.Prop("lastTapTime", mine.LastTapTime);
            json.Name("upgrades");
            json.BeginObject();
            var keys = new List<string>(mine.UpgradeLevels.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++)
                json.Prop(keys[i], mine.UpgradeLevels[keys[i]]);
            json.EndObject();
            json.Name("carts");
            json.BeginArray();
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                json.BeginObject();
                json.Prop("phase", mine.Carts[i].Phase);
                json.Prop("timer", mine.Carts[i].Timer);
                json.Prop("load", mine.Carts[i].Load);
                json.EndObject();
            }
            json.EndArray();
            json.EndObject();
        }

        public static GameState Read(string json, EconomyConfig config)
        {
            JsonValue root = JsonValue.Parse(SaveEnvelope.Unwrap(json));
            int version = root.Has("version") ? root.Int("version", 0) : 0;
            if (version > CurrentVersion)
                throw new NewerSaveException("Save version " + version + " is newer than " + CurrentVersion + ".");
            if (version < 1)
                root = MigrateFromLegacy(root);

            var state = GameFactory.NewGame(config);
            state.Version = CurrentVersion;
            bool legacyMoney = version < 1 && !root.Has("coins") && root.Has("money");
            state.Coins = legacyMoney ? NonNegative(root, "money") : NonNegative(root, "coins");
            state.Mastery = NonNegative(root, "mastery");
            state.LifetimeCoins = NonNegative(root, "lifetimeCoins");
            state.CoinsSincePrestige = NonNegative(root, "coinsSincePrestige");
            state.CoinsEarned = NonNegative(root, "coinsEarned");
            state.CoinsSpent = NonNegative(root, "coinsSpent");
            state.CoinsDiscarded = NonNegative(root, "coinsDiscarded");
            if (legacyMoney)
            {
                if (state.LifetimeCoins < state.Coins) state.LifetimeCoins = state.Coins;
                if (state.CoinsEarned < state.Coins) state.CoinsEarned = state.Coins;
                if (state.CoinsSincePrestige < state.Coins) state.CoinsSincePrestige = state.Coins;
            }
            state.PrestigeCount = Math.Max(0, root.Int("prestigeCount", 0));
            state.ActiveMineId = root.Str("activeMineId", state.ActiveMineId);
            state.BoostRemainingSeconds = NonNegative(root, "boostRemainingSeconds");
            state.TotalPlaySeconds = NonNegative(root, "totalPlaySeconds");
            double timestamp = root.Number("lastActiveUnixMs", 0d);
            if (!NumericFormat.IsFinite(timestamp) || timestamp < 0d || timestamp > 253402300799999d || Math.Truncate(timestamp) != timestamp)
                throw new InvalidSaveException("Save timestamp is outside the supported range.");
            state.LastActiveUnixMs = (long)timestamp;
            state.TutorialStep = Math.Max(0, Math.Min(TutorialService.Done, root.Int("tutorialStep", 0)));
            state.TutorialComplete = root.Bool("tutorialComplete", false);
            state.BottleneckSeen = root.Bool("bottleneckSeen", false);
            state.MarkedStage = root.Str("markedStage", "");
            state.MarkedStageLevels = root.Int("markedStageLevels", -1);
            state.ObjectivePresented = root.Bool("objectivePresented", false);
            state.LastOfflineGrantId = root.Str("lastOfflineGrantId", "");
            state.LastOfflineBaseCoins = NonNegative(root, "lastOfflineBaseCoins");
            state.LastOfflineBonusClaimed = root.Bool("lastOfflineBonusClaimed", false);
            ReadStrings(root, "usedAdTokens", state.UsedAdTokens);
            ReadStrings(root, "claimedObjectives", state.ClaimedObjectives);
            ReadStrings(root, "claimedOrders", state.ClaimedOrders);
            if (root.Has("settings"))
            {
                JsonValue settings = root.Get("settings");
                state.Settings.Music = Clamp01(settings.Number("music", 0.7));
                state.Settings.Sfx = Clamp01(settings.Number("sfx", 0.85));
                state.Settings.Haptics = settings.Bool("haptics", true);
                state.Settings.ReducedMotion = settings.Bool("reducedMotion", false);
                state.Settings.Quality = Math.Max(0, Math.Min(2, settings.Int("quality", 1)));
            }

            if (root.Has("mines"))
            {
                var saved = root.Array("mines");
                for (int i = 0; i < saved.Count; i++)
                    ApplyMine(state, config, saved[i]);
            }

            if (state.FindMine(state.ActiveMineId) == null && state.Mines.Count > 0)
                state.ActiveMineId = state.Mines[0].Id;
            return state;
        }

        static JsonValue MigrateFromLegacy(JsonValue root)
        {
            // Version 0 used "money" and omitted most fields. Missing fields keep defaults.
            return root;
        }

        static void ApplyMine(GameState state, EconomyConfig config, JsonValue node)
        {
            string id = node.Str("id", "");
            MineState mine = state.FindMine(id);
            MineDefinition def = config.FindMine(id);
            if (mine == null || def == null)
                return;
            mine.Unlocked = node.Bool("unlocked", mine.Unlocked);
            mine.OreAtFace = NonNegative(node, "oreAtFace");
            mine.ProcessInput = NonNegative(node, "processInput");
            mine.Rough = NonNegative(node, "rough");
            mine.Storage = NonNegative(node, "storage");
            mine.OreMined = NonNegative(node, "oreMined");
            mine.OreProcessed = NonNegative(node, "oreProcessed");
            mine.DiamondsProduced = NonNegative(node, "diamondsProduced");
            mine.DiamondsSold = NonNegative(node, "diamondsSold");
            mine.TapCount = Math.Max(0, node.Int("tapCount", 0));
            mine.TripsStarted = Math.Max(0, node.Int("tripsStarted", 0));
            mine.LastTapTime = node.Number("lastTapTime", -999d);
            if (!NumericFormat.IsFinite(mine.LastTapTime))
                throw new InvalidSaveException("Tap time is not finite.");
            if (node.Has("upgrades"))
            {
                foreach (var pair in node.Get("upgrades").AsObject())
                {
                    UpgradeDefinition upgrade = def.FindUpgrade(pair.Key);
                    int level = JsonValue.CheckedInt(pair.Value.AsNumber());
                    if (level < 0)
                        throw new InvalidSaveException("Negative upgrade level.");
                    if (upgrade != null && level > upgrade.MaxLevel)
                        level = upgrade.MaxLevel;
                    if (upgrade != null)
                        mine.UpgradeLevels[pair.Key] = level;
                }
            }
            if (node.Has("carts"))
            {
                mine.Carts.Clear();
                var carts = node.Array("carts");
                MineRates expected = MineStats.Compute(def, mine, config, state.Mastery, false);
                if (carts.Count > expected.CartCount)
                    throw new InvalidSaveException("Save has more carts than the mine owns.");
                for (int i = 0; i < carts.Count; i++)
                {
                    int phase = carts[i].Int("phase", 0);
                    if (phase < 0 || phase > 3)
                        throw new InvalidSaveException("Cart phase is invalid.");
                    double load = carts[i].Number("load", 0d);
                    double timer = carts[i].Number("timer", 0d);
                    if (!NumericFormat.IsFinite(load) || load < 0d || load > expected.CartCapacity * 1.01d ||
                        !NumericFormat.IsFinite(timer) || timer < 0d || timer > Math.Max(def.TripSeconds, expected.DepartWait) + 0.01d ||
                        ((phase == CartPhase.Loading || phase == CartPhase.Returning) && load > 1e-6))
                        throw new InvalidSaveException("Cart values are invalid.");
                    mine.Carts.Add(new CartState { Phase = phase, Timer = timer, Load = load });
                }
            }
            MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, false);
            if (mine.OreAtFace > rates.MineBuffer * 1.01d || mine.Storage > rates.StorageCap * 1.01d ||
                mine.ProcessInput > rates.ProcessBuffer * 1.01d || mine.Rough > rates.RoughBuffer * 1.01d)
                throw new InvalidSaveException("A buffer exceeds its capacity.");
            ProductionSimulator.SyncCarts(mine, rates);
        }

        static void ReadStrings(JsonValue root, string key, List<string> target)
        {
            target.Clear();
            if (!root.Has(key))
                return;
            var values = root.Array(key);
            for (int i = 0; i < values.Count; i++)
                target.Add(values[i].AsString());
        }

        static double NonNegative(JsonValue node, string key)
        {
            if (!node.Has(key))
                return 0d;
            double value = node.Number(key, 0d);
            if (!NumericFormat.IsFinite(value) || value < 0d)
                throw new InvalidSaveException("Field '" + key + "' must be a non-negative finite number.");
            return NumericFormat.Clamp(value);
        }

        static double Clamp01(double value)
        {
            if (!NumericFormat.IsFinite(value))
                return 0d;
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }
    }

    public sealed class NewerSaveException : Exception
    {
        public NewerSaveException(string message) : base(message) { }
    }

    public sealed class InvalidSaveException : Exception
    {
        public InvalidSaveException(string message) : base(message) { }
    }

    public sealed class SaveGameStore
    {
        readonly ISaveIO io;
        readonly string path;
        readonly string backupPath;
        readonly string tempPath;
        bool readOnly;
        readonly Dictionary<string, string> damagedFiles = new Dictionary<string, string>();

        public SaveGameStore(ISaveIO io, string path)
        {
            this.io = io;
            this.path = path;
            this.backupPath = path + ".bak";
            this.tempPath = path + ".tmp";
        }

        public string Path { get { return path; } }

        public LoadOutcome Load(EconomyConfig config)
        {
            readOnly = false;
            damagedFiles.Clear();
            if (!io.Exists(path))
            {
                if (io.Exists(backupPath))
                    return LoadPath(config, backupPath, LoadStatus.RestoredBackup, "Recovered progress from the backup save.");
                return Fresh("No save was found. Starting a new mine.");
            }

            try
            {
                GameState state = SaveCodec.Read(io.ReadAllText(path), config);
                return new LoadOutcome { Status = LoadStatus.Ok, State = state, Message = "" };
            }
            catch (NewerSaveException ex)
            {
                readOnly = true;
                return new LoadOutcome { Status = LoadStatus.NewerVersion, State = null, Message = ex.Message };
            }
            catch (Exception ex)
            {
                RememberDamage(path);
                if (io.Exists(backupPath))
                {
                    try
                    {
                        GameState restored = SaveCodec.Read(io.ReadAllText(backupPath), config);
                        return new LoadOutcome
                        {
                            Status = LoadStatus.RestoredBackup,
                            State = restored,
                            Message = "The save file could not be read (" + ex.Message + "). Restored the last good backup."
                        };
                    }
                    catch (NewerSaveException newer)
                    {
                        readOnly = true;
                        return new LoadOutcome { Status = LoadStatus.NewerVersion, Message = newer.Message };
                    }
                    catch (Exception backupEx)
                    {
                        RememberDamage(backupPath);
                        return new LoadOutcome
                {
                    Status = LoadStatus.RecoveredNewGame,
                    State = null,
                    Message = "The save and its backup could not be read (" + backupEx.Message + "). Starting a new mine. The broken files were left in place."
                };
                    }
                }
                return Fresh("The save could not be read (" + ex.Message + "). Starting a new mine.");
            }
        }

        public SaveOutcome Save(GameState state)
        {
            if (readOnly)
                return new SaveOutcome { Success = false, Error = "A newer save is protected from overwrite." };
            try
            {
                string json = SaveEnvelope.Wrap(SaveCodec.Write(state));
                foreach (var damaged in damagedFiles)
                    io.WriteAllText(damaged.Key + ".corrupt-" + Guid.NewGuid().ToString("N"), damaged.Value);
                damagedFiles.Clear();
                io.WriteAllText(tempPath, json);
                io.ReplaceWithBackup(tempPath, path, backupPath);
                return new SaveOutcome { Success = true, Error = "" };
            }
            catch (Exception ex)
            {
                return new SaveOutcome { Success = false, Error = ex.Message };
            }
        }

        public static bool Commit(GameState state, SaveGameStore store, Action<GameState> mutation, out string error)
        {
            GameState clone = state.Clone();
            mutation(clone);
            SaveOutcome outcome = store.Save(clone);
            if (!outcome.Success)
            {
                error = outcome.Error;
                return false;
            }
            state.CopyFrom(clone);
            error = null;
            return true;
        }

        LoadOutcome LoadPath(EconomyConfig config, string file, LoadStatus status, string message)
        {
            try
            {
                return new LoadOutcome { Status = status, State = SaveCodec.Read(io.ReadAllText(file), config), Message = message };
            }
            catch (NewerSaveException ex)
            {
                readOnly = true;
                return new LoadOutcome { Status = LoadStatus.NewerVersion, Message = ex.Message };
            }
            catch (Exception ex)
            {
                RememberDamage(file);
                return Fresh(message + " " + ex.Message);
            }
        }

        void RememberDamage(string file)
        {
            // If evidence cannot be read safely, fail closed on writes rather than erase it.
            try { damagedFiles[file] = io.ReadAllText(file); }
            catch { readOnly = true; }
        }

        static LoadOutcome Fresh(string message)
        {
            return new LoadOutcome { Status = LoadStatus.NewGame, State = null, Message = message };
        }
    }

    public static class LegacySave
    {
        public static double ReadLegacyMoney(string json)
        {
            JsonValue root = JsonValue.Parse(json);
            if (root.Has("coins"))
                return root.Number("coins", 0d);
            return root.Number("money", 0d);
        }
    }
}
