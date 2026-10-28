using System;
using System.Collections.Generic;
namespace DiamondMine.Simulation
{
    public static class AdventureCodec
    {
        public static void Write(JsonComposer json, AdventureState adventure)
        {
            json.Name("adventure");
            json.BeginObject();
            json.Prop("worldSeed", adventure.WorldSeed);
            json.Prop("generatorVersion", adventure.GeneratorVersion);
            json.Prop("starterGrantApplied", adventure.StarterGrantApplied);
            json.Prop("legacyMigrationApplied", adventure.LegacyMigrationApplied);
            json.Prop("migrationNote", adventure.MigrationNote ?? "");
            json.Prop("selectedCharacter", adventure.SelectedCharacter ?? "");
            json.Prop("owned", adventure.Owned.ToHex());
            json.Prop("handsItem", adventure.HandsItem ?? "");
            json.Prop("handsAmount", adventure.HandsAmount);
            json.Prop("cartItem", adventure.CartItem ?? "");
            json.Prop("cartLoad", adventure.CartLoad);
            json.Prop("cartSeconds", adventure.CartSeconds);
            json.Prop("cartGranted", adventure.CartGranted);
            json.Prop("carryCapacity", adventure.CarryCapacity);
            json.Prop("warehouseCapacity", adventure.WarehouseCapacity);
            json.Prop("actionClock", adventure.ActionClock);
            json.Prop("nextMineTime", adventure.NextMineTime);
            json.Prop("nextRecoveryTime", adventure.NextRecoveryTime);
            json.Prop("playerX", adventure.PlayerX);
            json.Prop("playerZ", adventure.PlayerZ);
            json.Prop("focusPlotId", adventure.FocusPlotId);
            json.Prop("nextJobId", adventure.NextJobId);
            json.Prop("oreCollected", adventure.OreCollected);
            json.Prop("lifetimeDiamondsRecovered", adventure.LifetimeDiamondsRecovered);
            json.Prop("washBatches", adventure.WashBatches);
            json.Prop("cutCount", adventure.CutCount);
            json.Prop("polishCount", adventure.PolishCount);
            json.Prop("silverRefined", adventure.SilverRefined);
            json.Prop("goldRefined", adventure.GoldRefined);
            json.Prop("ringsCrafted", adventure.RingsCrafted);
            json.Prop("ordersCompleted", adventure.OrdersCompleted);
            json.Prop("orderIndex", adventure.OrderIndex);
            json.Prop("workerHired", adventure.WorkerHired);
            json.Prop("workerPlotId", adventure.WorkerPlotId);
            json.Prop("workerFraction", adventure.WorkerFraction);
            json.Prop("workerSwitches", adventure.WorkerSwitchesWhenBlocked);
            WriteMap(json, "inventory", adventure.Inventory);
            WriteStrings(json, "characters", adventure.UnlockedCharacters);
            WriteStrings(json, "claimedTasks", adventure.ClaimedTasks);
            WriteInts(json, "visited", adventure.VisitedStarters);
            WriteInts(json, "regions", adventure.DiscoveredRegions);
            WriteCrafted(json, adventure);
            WriteMachines(json, adventure);
            WriteJobs(json, adventure);
            WriteDeposits(json, adventure);
            json.EndObject();
        }

        public static AdventureState Read(JsonValue node)
        {
            int generator = node.Int("generatorVersion", WorldGenerator.CurrentVersion);
            if (generator > WorldGenerator.CurrentVersion)
                throw new NewerSaveException("This world was made by a newer mine generator.");
            if (generator < 1)
                generator = 1;
            var adventure = new AdventureState
            {
                WorldSeed = node.Int("worldSeed", WorldGenerator.DefaultSeed),
                GeneratorVersion = generator,
                StarterGrantApplied = node.Bool("starterGrantApplied", false),
                LegacyMigrationApplied = node.Bool("legacyMigrationApplied", false),
                MigrationNote = node.Str("migrationNote", ""),
                SelectedCharacter = node.Str("selectedCharacter", ""),
                Owned = OwnershipBits.FromHex(node.Str("owned", "")),
                HandsItem = node.Str("handsItem", ""),
                HandsAmount = Whole(node, "handsAmount", 0d, JourneyCatalog.CarryCapacity),
                CartItem = node.Str("cartItem", ""),
                CartLoad = Whole(node, "cartLoad", 0d, JourneyCatalog.CarryCapacity),
                CartSeconds = Whole(node, "cartSeconds", 0d, 30d),
                CartGranted = node.Bool("cartGranted", false),
                CarryCapacity = Math.Max(1, Math.Min(40, node.Int("carryCapacity", JourneyCatalog.CarryCapacity))),
                WarehouseCapacity = Math.Max(1, Math.Min(5000, node.Int("warehouseCapacity", JourneyCatalog.WarehouseCapacity))),
                ActionClock = NonNegative(node, "actionClock"),
                NextMineTime = NonNegative(node, "nextMineTime"),
                NextRecoveryTime = NonNegative(node, "nextRecoveryTime"),
                PlayerX = Finite(node, "playerX", YardLayout.HomeX),
                PlayerZ = Finite(node, "playerZ", YardLayout.HomeZ),
                FocusPlotId = node.Int("focusPlotId", -1),
                NextJobId = Math.Max(1, node.Int("nextJobId", 1)),
                OreCollected = NonNegative(node, "oreCollected"),
                LifetimeDiamondsRecovered = NonNegative(node, "lifetimeDiamondsRecovered"),
                WashBatches = NonNegativeInt(node, "washBatches"),
                CutCount = NonNegativeInt(node, "cutCount"),
                PolishCount = NonNegativeInt(node, "polishCount"),
                SilverRefined = NonNegativeInt(node, "silverRefined"),
                GoldRefined = NonNegativeInt(node, "goldRefined"),
                RingsCrafted = NonNegativeInt(node, "ringsCrafted"),
                OrdersCompleted = NonNegativeInt(node, "ordersCompleted"),
                OrderIndex = NonNegativeInt(node, "orderIndex"),
                WorkerHired = node.Bool("workerHired", false),
                WorkerPlotId = node.Int("workerPlotId", -1),
                WorkerFraction = NonNegative(node, "workerFraction"),
                WorkerSwitchesWhenBlocked = node.Bool("workerSwitches", false)
            };
            if (adventure.WorkerFraction > 2d)
                adventure.WorkerFraction = 2d;
            ReadInventory(node, adventure);
            ReadCharacters(node, adventure);
            ReadStringList(node, "claimedTasks", adventure.ClaimedTasks);
            ReadIntList(node, "visited", adventure.VisitedStarters, 0, WorldGenerator.StarterCount - 1);
            ReadIntList(node, "regions", adventure.DiscoveredRegions, 0, WorldGenerator.RegionCount - 1);
            ReadCrafted(node, adventure);
            ReadMachines(node, adventure);
            ReadJobs(node, adventure);
            ReadDeposits(node, adventure);
            if (!string.IsNullOrEmpty(adventure.CartItem) && JourneyCatalog.FindItem(adventure.CartItem) == null)
                throw new InvalidSaveException("Cart load is unknown.");
            AdventureRules.EnsureMachines(adventure);
            for (int i = 0; i < JourneyCatalog.AllCharacters.Count; i++)
            {
                CharacterDef person = JourneyCatalog.AllCharacters[i];
                if (person.Free && !adventure.UnlockedCharacters.Contains(person.Id))
                    adventure.UnlockedCharacters.Add(person.Id);
            }
            if (!adventure.StarterGrantApplied)
                AdventureRules.GrantStarters(adventure);
            if (!WorldGenerator.IsValid(adventure.FocusPlotId))
                adventure.FocusPlotId = -1;
            if (adventure.WorkerPlotId >= 0 && !WorldGenerator.IsValid(adventure.WorkerPlotId))
                throw new InvalidSaveException("Worker plot is invalid.");
            return adventure;
        }

        static void WriteMap(JsonComposer json, string name, Dictionary<string, double> map)
        {
            json.Name(name);
            json.BeginObject();
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++)
                json.Prop(keys[i], map[keys[i]]);
            json.EndObject();
        }

        static void WriteStrings(JsonComposer json, string name, List<string> values)
        {
            json.Name(name);
            json.BeginArray();
            for (int i = 0; i < values.Count; i++)
                json.Value(values[i]);
            json.EndArray();
        }

        static void WriteInts(JsonComposer json, string name, List<int> values)
        {
            json.Name(name);
            json.BeginArray();
            for (int i = 0; i < values.Count; i++)
                json.Value(values[i]);
            json.EndArray();
        }

        static void WriteCrafted(JsonComposer json, AdventureState adventure)
        {
            json.Name("crafted");
            json.BeginObject();
            var keys = new List<string>(adventure.Crafted.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++)
                json.Prop(keys[i], adventure.Crafted[keys[i]]);
            json.EndObject();
        }

        static void WriteMachines(JsonComposer json, AdventureState adventure)
        {
            json.Name("machines");
            json.BeginArray();
            for (int i = 0; i < adventure.Machines.Count; i++)
            {
                MachineState machine = adventure.Machines[i];
                json.BeginObject();
                json.Prop("id", machine.Id ?? "");
                json.Prop("level", machine.Level);
                json.Prop("inputItem", machine.InputItem ?? "");
                json.Prop("inputAmount", machine.InputAmount);
                json.Prop("outputItem", machine.OutputItem ?? "");
                json.Prop("outputAmount", machine.OutputAmount);
                json.Prop("extraItem", machine.ExtraItem ?? "");
                json.Prop("extraAmount", machine.ExtraAmount);
                json.Prop("busySeconds", machine.BusySeconds);
                json.Prop("busyUnits", machine.BusyUnits);
                json.Prop("outputGranted", machine.OutputGranted);
                json.EndObject();
            }
            json.EndArray();
        }

        static void WriteJobs(JsonComposer json, AdventureState adventure)
        {
            json.Name("jobs");
            json.BeginArray();
            for (int i = 0; i < adventure.Jobs.Count; i++)
            {
                json.BeginObject();
                json.Prop("id", adventure.Jobs[i].Id);
                json.Prop("recipe", adventure.Jobs[i].RecipeId ?? "");
                json.Prop("remaining", adventure.Jobs[i].Remaining);
                json.Prop("granted", adventure.Jobs[i].OutputGranted);
                json.EndObject();
            }
            json.EndArray();
        }

        static void WriteDeposits(JsonComposer json, AdventureState adventure)
        {
            json.Name("deposits");
            json.BeginArray();
            var ids = new List<int>(adventure.Deposits.Keys);
            ids.Sort();
            for (int i = 0; i < ids.Count; i++)
            {
                DepositDelta delta = adventure.Deposits[ids[i]];
                json.BeginObject();
                json.Prop("id", delta.PlotId);
                json.Name("left");
                json.BeginObject();
                var keys = new List<string>(delta.Remaining.Keys);
                keys.Sort(StringComparer.Ordinal);
                for (int k = 0; k < keys.Count; k++)
                    json.Prop(keys[k], delta.Remaining[keys[k]]);
                json.EndObject();
                json.EndObject();
            }
            json.EndArray();
        }

        static void ReadInventory(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("inventory"))
                return;
            foreach (var pair in node.Get("inventory").AsObject())
            {
                if (JourneyCatalog.FindItem(pair.Key) == null)
                    throw new InvalidSaveException("Unknown pocket item.");
                double amount = pair.Value.AsNumber();
                if (!NumericFormat.IsFinite(amount) || amount < 0d || amount > 1000000000d || Math.Abs(amount - Math.Round(amount)) > 1e-6)
                    throw new InvalidSaveException("Pocket amount is invalid.");
                if (amount > 0d)
                    adventure.Inventory[pair.Key] = amount;
            }
        }

        static void ReadCharacters(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("characters"))
                return;
            var values = node.Array("characters");
            for (int i = 0; i < values.Count; i++)
            {
                string id = values[i].AsString();
                if (JourneyCatalog.FindCharacter(id) != null && !adventure.UnlockedCharacters.Contains(id))
                    adventure.UnlockedCharacters.Add(id);
            }
        }

        static void ReadStringList(JsonValue node, string name, List<string> into)
        {
            if (!node.Has(name))
                return;
            var values = node.Array(name);
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i].AsString();
                if (!into.Contains(value))
                    into.Add(value);
            }
        }

        static void ReadIntList(JsonValue node, string name, List<int> into, int min, int max)
        {
            if (!node.Has(name))
                return;
            var values = node.Array(name);
            for (int i = 0; i < values.Count; i++)
            {
                int value = JsonValue.CheckedInt(values[i].AsNumber());
                if (value < min || value > max || into.Contains(value))
                    continue;
                into.Add(value);
            }
        }

        static void ReadCrafted(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("crafted"))
                return;
            foreach (var pair in node.Get("crafted").AsObject())
            {
                if (JourneyCatalog.FindItem(pair.Key) == null)
                    continue;
                int count = JsonValue.CheckedInt(pair.Value.AsNumber());
                if (count < 0 || count > 1000000)
                    throw new InvalidSaveException("Craft count is invalid.");
                adventure.Crafted[pair.Key] = count;
            }
        }

        static void ReadMachines(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("machines"))
                return;
            var values = node.Array("machines");
            if (values.Count > 12)
                throw new InvalidSaveException("Too many machines.");
            for (int i = 0; i < values.Count; i++)
            {
                JsonValue machine = values[i];
                string id = machine.Str("id", "");
                if (id != "crush" && id != "wash" && id != "recover" && id != "cut" && id != "polish" && id != "refine")
                    throw new InvalidSaveException("Unknown machine.");
                if (adventure.FindMachine(id) != null)
                    throw new InvalidSaveException("Duplicate machine.");
                int level = machine.Int("level", 0);
                if (level < 0 || level > 3)
                    throw new InvalidSaveException("Machine level is invalid.");
                double busy = machine.Number("busySeconds", 0d);
                int units = machine.Int("busyUnits", 0);
                if (!NumericFormat.IsFinite(busy) || busy < 0d || busy > 60d || units < 0 || units > 8)
                    throw new InvalidSaveException("Machine timer is invalid.");
                adventure.Machines.Add(new MachineState
                {
                    Id = id,
                    Level = level,
                    InputItem = machine.Str("inputItem", ""),
                    InputAmount = NonNegative(machine, "inputAmount"),
                    OutputItem = machine.Str("outputItem", ""),
                    OutputAmount = NonNegative(machine, "outputAmount"),
                    ExtraItem = machine.Str("extraItem", ""),
                    ExtraAmount = NonNegative(machine, "extraAmount"),
                    BusySeconds = busy,
                    BusyUnits = units,
                    OutputGranted = machine.Bool("outputGranted", false)
                });
            }
        }

        static void ReadJobs(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("jobs"))
                return;
            var values = node.Array("jobs");
            if (values.Count > 8)
                throw new InvalidSaveException("Too many craft jobs.");
            for (int i = 0; i < values.Count; i++)
            {
                string recipe = values[i].Str("recipe", "");
                if (JourneyCatalog.FindRecipe(recipe) == null)
                    throw new InvalidSaveException("Unknown craft job.");
                double remaining = values[i].Number("remaining", 0d);
                if (!NumericFormat.IsFinite(remaining) || remaining < 0d || remaining > 120d)
                    throw new InvalidSaveException("Craft timer is invalid.");
                adventure.Jobs.Add(new CraftJob
                {
                    Id = Math.Max(1, values[i].Int("id", i + 1)),
                    RecipeId = recipe,
                    Remaining = remaining,
                    OutputGranted = values[i].Bool("granted", false)
                });
            }
        }

        static void ReadDeposits(JsonValue node, AdventureState adventure)
        {
            if (!node.Has("deposits"))
                return;
            var values = node.Array("deposits");
            if (values.Count > 20000)
                throw new InvalidSaveException("Too many mined plots.");
            for (int i = 0; i < values.Count; i++)
            {
                int id = values[i].Int("id", -1);
                if (!WorldGenerator.IsValid(id) || adventure.Deposits.ContainsKey(id))
                    throw new InvalidSaveException("Mined plot is invalid.");
                var delta = new DepositDelta { PlotId = id };
                PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, id, adventure.GeneratorVersion);
                if (values[i].Has("left"))
                {
                    foreach (var pair in values[i].Get("left").AsObject())
                    {
                        int initial = profile.AmountOf(pair.Key);
                        if (initial <= 0)
                            throw new InvalidSaveException("Mined material is not in this bunker.");
                        double left = pair.Value.AsNumber();
                        if (!NumericFormat.IsFinite(left) || left < 0d || left > initial + 1e-6)
                            throw new InvalidSaveException("Bunker reserve is invalid.");
                        delta.Remaining[pair.Key] = left;
                    }
                }
                adventure.Deposits[id] = delta;
            }
        }

        static double NonNegative(JsonValue node, string key)
        {
            if (!node.Has(key))
                return 0d;
            double value = node.Number(key, 0d);
            if (!NumericFormat.IsFinite(value) || value < 0d || value > 1000000000d)
                throw new InvalidSaveException("Field '" + key + "' is out of range.");
            return value;
        }

        static double Whole(JsonValue node, string key, double fallback, double max)
        {
            double value = node.Has(key) ? node.Number(key, fallback) : fallback;
            if (!NumericFormat.IsFinite(value) || value < 0d || value > max || Math.Abs(value - Math.Round(value)) > 1e-6)
                throw new InvalidSaveException("Field '" + key + "' is out of range.");
            return value;
        }

        static double Finite(JsonValue node, string key, double fallback)
        {
            if (!node.Has(key))
                return fallback;
            double value = node.Number(key, fallback);
            if (!NumericFormat.IsFinite(value) || Math.Abs(value) > 1000d)
                throw new InvalidSaveException("Field '" + key + "' is out of range.");
            return value;
        }

        static int NonNegativeInt(JsonValue node, string key)
        {
            int value = node.Int(key, 0);
            if (value < 0 || value > 100000000)
                throw new InvalidSaveException("Field '" + key + "' is out of range.");
            return value;
        }
    }

    public sealed class RankRow
    {
        public string Nickname;
        public string AvatarId;
        public int Diamonds;
        public long FirstReachedUnixMs;
    }

    public sealed class RankEvent
    {
        public string EventId;
        public int Diamonds;
        public int OreConsumed;
        public long ServerUnixMs;
    }

    public sealed class RankDecision
    {
        public bool Accepted;
        public bool Duplicate;
        public string Reason;
        public int VerifiedTotal;
    }

    public sealed class RankBook
    {
        public readonly List<string> SeenIds = new List<string>();
        public int VerifiedDiamonds;
        public long LastServerUnixMs;
    }

    /// <summary>
    /// Rules a server would enforce. The shipped game does not upload scores.
    /// </summary>
    public static class RankRules
    {
        public const bool ProductionEnabled = false;
        public const string PendingLabel = "Online ranking is pending. The number on this device is practice only, not a verified rank.";
        public const int RecoveryOrePerDiamond = 4;

        public static RankDecision Submit(RankBook book, RankEvent ev, long serverNowMs)
        {
            var decision = new RankDecision();
            if (book == null || ev == null || string.IsNullOrEmpty(ev.EventId) || ev.EventId.Length > 80)
            {
                decision.Reason = "rejected";
                return decision;
            }
            if (book.SeenIds.Contains(ev.EventId))
            {
                decision.Duplicate = true;
                decision.Reason = "duplicate";
                decision.VerifiedTotal = book.VerifiedDiamonds;
                return decision;
            }
            if (ev.Diamonds < 1 || ev.OreConsumed < ev.Diamonds * RecoveryOrePerDiamond)
            {
                decision.Reason = "over_recovery";
                return decision;
            }
            if (ev.ServerUnixMs <= 0 || ev.ServerUnixMs > serverNowMs + 5000L || (book.LastServerUnixMs > 0 && ev.ServerUnixMs < book.LastServerUnixMs))
            {
                decision.Reason = "time";
                return decision;
            }
            book.SeenIds.Add(ev.EventId);
            book.VerifiedDiamonds += ev.Diamonds;
            book.LastServerUnixMs = ev.ServerUnixMs;
            decision.Accepted = true;
            decision.Reason = "accepted";
            decision.VerifiedTotal = book.VerifiedDiamonds;
            return decision;
        }

        public static RankDecision SubmitClientTotal(int clientTotal)
        {
            return new RankDecision { Accepted = false, Reason = "client_total_unverified", VerifiedTotal = 0 };
        }

        public static int Compare(RankRow left, RankRow right)
        {
            int byScore = right.Diamonds.CompareTo(left.Diamonds);
            if (byScore != 0)
                return byScore;
            int byTime = left.FirstReachedUnixMs.CompareTo(right.FirstReachedUnixMs);
            if (byTime != 0)
                return byTime;
            return string.CompareOrdinal(left.Nickname ?? "", right.Nickname ?? "");
        }

        public static long WeekId(long unixMs)
        {
            if (unixMs < 0)
                unixMs = 0;
            return unixMs / (7L * 24L * 60L * 60L * 1000L);
        }

        public static bool AllowRequest(int requestsInWindow, int limit)
        {
            return requestsInWindow >= 0 && requestsInWindow < limit;
        }

        public static string Nickname(int seed)
        {
            string[] first = { "Pebble", "Clover", "Moss", "Amber", "Brook", "Fern", "Quartz", "Lark" };
            string[] second = { "Cart", "Lamp", "Wheel", "Pocket", "Trail", "Bench", "Badge", "Scoop" };
            int a = Math.Abs(seed) % first.Length;
            int b = Math.Abs(seed / 7) % second.Length;
            return first[a] + second[b];
        }
    }
}
