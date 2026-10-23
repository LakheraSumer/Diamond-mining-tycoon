using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiamondMine.Simulation
{
    public static class AdventureRules
    {
        const double MaxStack = 1000000000d;

        public static AdventureState CreateFresh()
        {
            var adventure = new AdventureState();
            adventure.PlayerX = YardLayout.HomeX;
            adventure.PlayerZ = YardLayout.HomeZ;
            EnsureMachines(adventure);
            for (int i = 0; i < JourneyCatalog.AllCharacters.Count; i++)
            {
                CharacterDef person = JourneyCatalog.AllCharacters[i];
                if (person.Free)
                    adventure.UnlockedCharacters.Add(person.Id);
            }
            if (!adventure.DiscoveredRegions.Contains(0))
                adventure.DiscoveredRegions.Add(0);
            GrantStarters(adventure);
            return adventure;
        }

        public static int GrantStarters(AdventureState adventure)
        {
            if (adventure.StarterGrantApplied)
                return 0;
            int added = 0;
            for (int plot = 0; plot < WorldGenerator.StarterCount; plot++)
            {
                if (adventure.Owned.Get(plot))
                    continue;
                adventure.Owned.Set(plot, true);
                added++;
            }
            adventure.StarterGrantApplied = true;
            return added;
        }

        public static void ApplyLegacyMigration(GameState state)
        {
            AdventureState adventure = state.Adventure;
            if (adventure.LegacyMigrationApplied)
                return;
            GrantStarters(adventure);
            MineState deep = state.FindMine("deepcore");
            if (deep != null && deep.Unlocked && !adventure.Owned.Get(WorldGenerator.DeepCorePlot))
                adventure.Owned.Set(WorldGenerator.DeepCorePlot, true);
            adventure.LegacyMigrationApplied = true;
            bool progressed = state.Coins > 0d || state.LifetimeCoins > 0d || state.PrestigeCount > 0 || state.TutorialStep > 0;
            if (!progressed)
            {
                for (int i = 0; i < state.Mines.Count; i++)
                {
                    if (state.Mines[i].UpgradeLevels.Count > 0 || state.Mines[i].OreMined > 0d)
                        progressed = true;
                }
            }
            if (progressed && string.IsNullOrEmpty(adventure.MigrationNote))
                adventure.MigrationNote = "Your saved works are safe. Five starter bunkers are in the yard.";
        }

        public static void ApplyPrestigeKeep(GameState state, AdventureState kept)
        {
            state.Adventure = kept.Clone();
            for (int i = 0; i < state.Adventure.Machines.Count; i++)
                state.Adventure.Machines[i].Level = 0;
            state.Adventure.WorkerHired = false;
            state.Adventure.WorkerPlotId = -1;
            state.Adventure.WorkerFraction = 0d;
        }

        public static void EnsureMachines(AdventureState adventure)
        {
            EnsureMachine(adventure, "crush");
            EnsureMachine(adventure, "wash");
            EnsureMachine(adventure, "recover");
            EnsureMachine(adventure, "cut");
            EnsureMachine(adventure, "polish");
            EnsureMachine(adventure, "refine");
        }

        static void EnsureMachine(AdventureState adventure, string id)
        {
            if (adventure.FindMachine(id) == null)
                adventure.Machines.Add(new MachineState { Id = id });
        }

        public static int OwnedCount(AdventureState adventure)
        {
            return adventure.Owned.Count();
        }

        public static int PaidOwned(AdventureState adventure)
        {
            int count = 0;
            for (int id = WorldGenerator.StarterCount; id < WorldGenerator.PlotCount; id++)
            {
                if (adventure.Owned.Get(id))
                    count++;
            }
            return count;
        }

        public static double PlotPrice(AdventureState adventure, int plotId)
        {
            if (!WorldGenerator.IsValid(plotId) || WorldGenerator.IsStarter(plotId))
                return 0d;
            int paid = PaidOwned(adventure);
            double price = 80d + 25d * Math.Sqrt(paid) + WorldGenerator.RegionOf(plotId) * 15d;
            return Math.Round(price);
        }

        public static double Count(AdventureState adventure, string itemId)
        {
            double value;
            return adventure.Inventory.TryGetValue(itemId, out value) ? value : 0d;
        }

        public static double WarehouseUsed(AdventureState adventure)
        {
            double total = 0d;
            foreach (var pair in adventure.Inventory)
                total += pair.Value;
            return total;
        }

        public static string NextHint(GameState state)
        {
            AdventureState adventure = state.Adventure;
            if (string.IsNullOrEmpty(adventure.SelectedCharacter))
                return "Choose a miner.";
            if (!string.IsNullOrEmpty(adventure.MigrationNote))
                return adventure.MigrationNote;
            if (adventure.LifetimeDiamondsRecovered < 1d)
                return "Mine diamond rock. Crush, wash, then sort.";
            if (adventure.RingsCrafted < 1)
                return "Polish a diamond, refine silver, make a ring.";
            if (PaidOwned(adventure) < 1)
                return "Sell the ring, then buy the next bunker.";
            return "Explore, craft, and sell.";
        }

        public static void Advance(GameState state, double seconds)
        {
            if (state == null || state.Adventure == null)
                return;
            if (!NumericFormat.IsFinite(seconds) || seconds < 0d)
                return;
            EnsureMachines(state.Adventure);
            if (seconds > 0d)
            {
                state.Adventure.ActionClock = NumericFormat.Add(state.Adventure.ActionClock, seconds);
                if (state.Adventure.PoseCelebrate > 0d)
                    state.Adventure.PoseCelebrate = Math.Max(0d, state.Adventure.PoseCelebrate - seconds);
                if (state.Adventure.PoseWave > 0d)
                    state.Adventure.PoseWave = Math.Max(0d, state.Adventure.PoseWave - seconds);
                TickMachines(state.Adventure, seconds);
                TickJobs(state, seconds);
                TickWorker(state, seconds);
            }
            else
            {
                TickMachines(state.Adventure, 0d);
                TickJobs(state, 0d);
            }
            RefreshUnlocks(state);
        }

        public static bool TrySelectCharacter(GameState state, string id, out string message)
        {
            CharacterDef person = JourneyCatalog.FindCharacter(id);
            if (person == null)
            {
                message = "That miner is not in this crew.";
                return false;
            }
            RefreshUnlocks(state);
            if (!state.Adventure.UnlockedCharacters.Contains(id))
            {
                message = person.Name + " unlocks when you " + person.UnlockText.ToLowerInvariant() + ".";
                return false;
            }
            state.Adventure.SelectedCharacter = id;
            state.Adventure.PoseWave = 1.2d;
            message = person.Name + " is ready.";
            return true;
        }

        public static bool TryVisit(GameState state, int plotId, out string message)
        {
            if (!state.Adventure.Owned.Get(plotId))
            {
                message = "This bunker is not yours yet.";
                return false;
            }
            if (WorldGenerator.IsStarter(plotId) && !state.Adventure.VisitedStarters.Contains(plotId))
                state.Adventure.VisitedStarters.Add(plotId);
            int region = WorldGenerator.RegionOf(plotId);
            if (!state.Adventure.DiscoveredRegions.Contains(region))
                state.Adventure.DiscoveredRegions.Add(region);
            state.Adventure.FocusPlotId = plotId;
            message = "Bunker " + (plotId + 1) + ".";
            return true;
        }

        public static bool TryDig(GameState state, int plotId, string itemId, out string message)
        {
            AdventureState adventure = state.Adventure;
            if (string.IsNullOrEmpty(adventure.SelectedCharacter))
            {
                message = "Choose a miner first.";
                return false;
            }
            if (!adventure.Owned.Get(plotId))
            {
                message = "You can look, but mining needs ownership.";
                return false;
            }
            if (adventure.ActionClock + 1e-9 < adventure.NextMineTime)
            {
                message = "The tool needs a short rest.";
                return false;
            }
            ItemDef item = JourneyCatalog.FindItem(itemId);
            if (item == null)
            {
                message = "That material is unknown.";
                return false;
            }
            double left = Remaining(adventure, plotId, itemId);
            if (left <= 0d)
            {
                message = "No " + item.Name.ToLowerInvariant() + " left here.";
                return false;
            }
            if (!string.IsNullOrEmpty(adventure.HandsItem) && adventure.HandsItem != itemId && adventure.HandsAmount > 0d)
            {
                message = "Hands are full of " + JourneyCatalog.DisplayName(adventure.HandsItem) + ".";
                return false;
            }
            double room = adventure.CarryCapacity - adventure.HandsAmount;
            if (room <= 0d)
            {
                message = "Hands are full. Store them at the factory.";
                return false;
            }
            double take = Math.Min(JourneyCatalog.MineYield, Math.Min(left, room));
            take = Math.Floor(take);
            if (take < 1d)
            {
                message = "Nothing to pick up.";
                return false;
            }
            SetRemaining(adventure, plotId, itemId, left - take);
            adventure.HandsItem = itemId;
            adventure.HandsAmount += take;
            adventure.NextMineTime = adventure.ActionClock + JourneyCatalog.MineCooldownSeconds;
            if (item.Ore)
                adventure.OreCollected += take;
            if (WorldGenerator.IsStarter(plotId) && !adventure.VisitedStarters.Contains(plotId))
                adventure.VisitedStarters.Add(plotId);
            adventure.FocusPlotId = plotId;
            adventure.PoseHint = "mine";
            message = "Picked up " + take.ToString("0", CultureInfo.InvariantCulture) + " " + item.Name + ".";
            return true;
        }

        public static bool TryStoreHands(GameState state, out string message)
        {
            AdventureState adventure = state.Adventure;
            if (adventure.HandsAmount <= 0d || string.IsNullOrEmpty(adventure.HandsItem))
            {
                message = "Hands are empty.";
                return false;
            }
            double space = adventure.WarehouseCapacity - WarehouseUsed(adventure);
            if (space < 1d)
            {
                message = "Storage is full.";
                return false;
            }
            double move = Math.Min(adventure.HandsAmount, Math.Floor(space));
            AddItem(adventure, adventure.HandsItem, move);
            adventure.HandsAmount -= move;
            if (adventure.HandsAmount <= 0d)
            {
                adventure.HandsAmount = 0d;
                adventure.HandsItem = "";
            }
            adventure.PoseHint = "collect";
            message = "Stored " + move.ToString("0", CultureInfo.InvariantCulture) + ".";
            return true;
        }

        public static bool TryLoadMachine(GameState state, string machineId, out string message)
        {
            EnsureMachines(state.Adventure);
            MachineState machine = state.Adventure.FindMachine(machineId);
            if (machine == null)
            {
                message = "Unknown machine.";
                return false;
            }
            if (machine.BusyUnits > 0 || machine.OutputAmount > 0d || machine.ExtraAmount > 0d)
            {
                message = "Empty the machine first.";
                return false;
            }
            string input;
            int count;
            if (!BatchInput(machineId, out input, out count))
            {
                message = "That machine has no batch.";
                return false;
            }
            if (Count(state.Adventure, input) + 1e-6 < count)
            {
                message = "Need " + count.ToString(CultureInfo.InvariantCulture) + " " + JourneyCatalog.DisplayName(input) + ".";
                return false;
            }
            if (!TryTake(state.Adventure, input, count))
            {
                message = "Could not take the materials.";
                return false;
            }
            machine.InputItem = input;
            machine.InputAmount = count;
            machine.BusyUnits = count;
            machine.BusySeconds = Duration(machine);
            machine.OutputGranted = false;
            machine.OutputItem = "";
            machine.OutputAmount = 0d;
            machine.ExtraItem = "";
            machine.ExtraAmount = 0d;
            state.Adventure.PoseHint = "machine";
            message = "Started " + machine.Id + ".";
            return true;
        }

        public static bool TryCollectMachine(GameState state, string machineId, out string message)
        {
            MachineState machine = state.Adventure.FindMachine(machineId);
            if (machine == null || (machine.OutputAmount <= 0d && machine.ExtraAmount <= 0d))
            {
                message = "Nothing is ready.";
                return false;
            }
            double pieces = machine.OutputAmount + machine.ExtraAmount;
            if (WarehouseUsed(state.Adventure) + pieces > state.Adventure.WarehouseCapacity + 1e-6)
            {
                message = "Storage is full.";
                return false;
            }
            if (machine.OutputAmount > 0d)
                AddItem(state.Adventure, machine.OutputItem, machine.OutputAmount);
            if (machine.ExtraAmount > 0d)
                AddItem(state.Adventure, machine.ExtraItem, machine.ExtraAmount);
            machine.OutputAmount = 0d;
            machine.ExtraAmount = 0d;
            machine.OutputItem = "";
            machine.ExtraItem = "";
            message = "Collected the batch.";
            return true;
        }

        public static bool TryUpgradeMachine(GameState state, string machineId, out string message)
        {
            MachineState machine = state.Adventure.FindMachine(machineId);
            if (machine == null)
            {
                message = "Unknown machine.";
                return false;
            }
            if (machine.Level >= 3)
            {
                message = "This machine is at its polish limit.";
                return false;
            }
            double cost = 100d * (machine.Level + 1);
            if (!Wallet.TrySpend(state, cost))
            {
                message = "Need " + cost.ToString("0", CultureInfo.InvariantCulture) + " coins.";
                return false;
            }
            machine.Level++;
            message = "Machine improved.";
            return true;
        }

        public static bool RecipeUnlocked(GameState state, JewelRecipe recipe)
        {
            AdventureState adventure = state.Adventure;
            switch (recipe.Unlock)
            {
                case "start": return true;
                case "ring": return adventure.RingsCrafted >= 1;
                case "sixth": return PaidOwned(adventure) >= 1;
                case "polish": return adventure.PolishCount >= 1;
                case "gold": return adventure.GoldRefined >= 1;
                case "order": return adventure.OrdersCompleted >= 1;
                case "ten": return adventure.LifetimeDiamondsRecovered >= 10d;
                case "region": return adventure.DiscoveredRegions.Contains(1);
                case "eight": return OwnedCount(adventure) >= 8;
                case "set":
                    return Crafted(adventure, JourneyCatalog.Ring) > 0
                        && Crafted(adventure, JourneyCatalog.Bracelet) > 0
                        && Crafted(adventure, JourneyCatalog.Necklace) > 0
                        && Crafted(adventure, JourneyCatalog.Pendant) > 0
                        && Crafted(adventure, JourneyCatalog.Earrings) > 0
                        && Crafted(adventure, JourneyCatalog.Bangle) > 0
                        && Crafted(adventure, JourneyCatalog.Brooch) > 0
                        && Crafted(adventure, JourneyCatalog.Anklet) > 0
                        && Crafted(adventure, JourneyCatalog.Tiara) > 0;
                default: return false;
            }
        }

        public static bool CanCraft(GameState state, JewelRecipe recipe, out string message)
        {
            if (recipe == null)
            {
                message = "Unknown recipe.";
                return false;
            }
            if (!RecipeUnlocked(state, recipe))
            {
                message = recipe.Name + " is still locked.";
                return false;
            }
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                Ingredient input = recipe.Inputs[i];
                if (input.ItemId == JourneyCatalog.AnyDiamond)
                {
                    if (DiamondCount(state.Adventure, input.MinGrade) < input.Count)
                    {
                        message = "Need " + input.Count.ToString(CultureInfo.InvariantCulture) + " finished diamonds.";
                        return false;
                    }
                }
                else if (Count(state.Adventure, input.ItemId) + 1e-6 < input.Count)
                {
                    message = "Need " + input.Count.ToString(CultureInfo.InvariantCulture) + " " + JourneyCatalog.DisplayName(input.ItemId) + ".";
                    return false;
                }
            }
            message = recipe.Note;
            return true;
        }

        public static bool TryStartCraft(GameState state, string recipeId, out string message)
        {
            JewelRecipe recipe = JourneyCatalog.FindRecipe(recipeId);
            if (!CanCraft(state, recipe, out message))
                return false;
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                Ingredient input = recipe.Inputs[i];
                bool took = input.ItemId == JourneyCatalog.AnyDiamond
                    ? TryTakeDiamonds(state.Adventure, input.Count, input.MinGrade)
                    : TryTake(state.Adventure, input.ItemId, input.Count);
                if (!took)
                {
                    message = "Materials changed.";
                    return false;
                }
            }
            state.Adventure.Jobs.Add(new CraftJob
            {
                Id = state.Adventure.NextJobId++,
                RecipeId = recipe.Id,
                Remaining = recipe.Seconds,
                OutputGranted = false
            });
            state.Adventure.PoseHint = "craft";
            message = "Making " + recipe.Name + ".";
            return true;
        }

        public static bool TrySell(GameState state, string itemId, int count, out string message)
        {
            ItemDef item = JourneyCatalog.FindItem(itemId);
            if (item == null || item.SellValue <= 0 || count < 1 || count > 1000000)
            {
                message = "Choose a sellable amount.";
                return false;
            }
            if (Count(state.Adventure, itemId) + 1e-6 < count)
            {
                message = "Not enough " + item.Name + ".";
                return false;
            }
            double payout = item.SellValue * (double)count;
            if (!NumericFormat.IsFinite(payout) || payout <= 0d)
            {
                message = "That price is not valid.";
                return false;
            }
            if (!TryTake(state.Adventure, itemId, count))
            {
                message = "Could not remove the goods.";
                return false;
            }
            Wallet.Credit(state, payout);
            message = "Sold for " + payout.ToString("0", CultureInfo.InvariantCulture) + " coins.";
            return true;
        }

        public static bool TryBuyPlot(GameState state, int plotId, out string message)
        {
            if (!WorldGenerator.IsValid(plotId) || WorldGenerator.IsStarter(plotId))
            {
                message = "That plot is not for sale.";
                return false;
            }
            if (state.Adventure.Owned.Get(plotId))
            {
                message = "You already own this bunker.";
                return false;
            }
            PlotProfile profile = WorldGenerator.Describe(state.Adventure.WorldSeed, plotId, state.Adventure.GeneratorVersion);
            double price = PlotPrice(state.Adventure, plotId);
            if (!Wallet.TrySpend(state, price))
            {
                message = "Need " + price.ToString("0", CultureInfo.InvariantCulture) + " coins. " + profile.Richness + ": " + WorldGenerator.ReserveText(profile);
                return false;
            }
            state.Adventure.Owned.Set(plotId, true);
            int region = WorldGenerator.RegionOf(plotId);
            if (!state.Adventure.DiscoveredRegions.Contains(region))
                state.Adventure.DiscoveredRegions.Add(region);
            message = "Bought bunker " + (plotId + 1) + ". " + WorldGenerator.ReserveText(profile);
            return true;
        }

        public static bool TryDeliverOrder(GameState state, out string message)
        {
            if (state.Adventure.OrderIndex >= JourneyCatalog.AllOrders.Count)
            {
                message = "No visitor is waiting.";
                return false;
            }
            CustomerOrderDef order = JourneyCatalog.AllOrders[state.Adventure.OrderIndex];
            if (Count(state.Adventure, order.ItemId) + 1e-6 < order.Amount)
            {
                message = "Need " + order.Amount.ToString(CultureInfo.InvariantCulture) + " " + JourneyCatalog.DisplayName(order.ItemId) + ".";
                return false;
            }
            if (!TryTake(state.Adventure, order.ItemId, order.Amount))
            {
                message = "Could not pack the order.";
                return false;
            }
            Wallet.Credit(state, order.Pay);
            state.Adventure.OrderIndex++;
            state.Adventure.OrdersCompleted++;
            message = order.Title + " paid " + order.Pay.ToString(CultureInfo.InvariantCulture) + ".";
            return true;
        }

        public static bool TryClaimTask(GameState state, string taskId, out string message)
        {
            YardTaskDef task = JourneyCatalog.FindTask(taskId);
            if (task == null)
            {
                message = "Unknown task.";
                return false;
            }
            if (state.Adventure.ClaimedTasks.Contains(taskId))
            {
                message = "Already claimed.";
                return false;
            }
            if (!TaskReady(state, taskId))
            {
                message = task.Detail;
                return false;
            }
            state.Adventure.ClaimedTasks.Add(taskId);
            if (task.Reward > 0)
                Wallet.Credit(state, task.Reward);
            state.Adventure.PoseCelebrate = 1.4d;
            RefreshUnlocks(state);
            message = "Task complete.";
            return true;
        }

        public static bool TaskReady(GameState state, string taskId)
        {
            AdventureState adventure = state.Adventure;
            switch (taskId)
            {
                case "ore20": return adventure.OreCollected >= 20d;
                case "visit5": return adventure.VisitedStarters.Count >= WorldGenerator.StarterCount;
                case "wash1": return adventure.WashBatches >= 1;
                case "rough5": return adventure.LifetimeDiamondsRecovered >= 5d;
                case "polish1": return adventure.PolishCount >= 1;
                case "silver1": return adventure.SilverRefined >= 1;
                case "ring1": return adventure.RingsCrafted >= 1;
                case "orders3": return adventure.OrdersCompleted >= 3;
                case "bunker6": return PaidOwned(adventure) >= 1;
                case "region2": return adventure.DiscoveredRegions.Contains(1);
                default: return false;
            }
        }

        public static bool TrySweepStone(GameState state, out string message)
        {
            AdventureState adventure = state.Adventure;
            if (adventure.ActionClock + 1e-9 < adventure.NextRecoveryTime)
            {
                message = "The path was just swept.";
                return false;
            }
            if (WarehouseUsed(adventure) + JourneyCatalog.RecoveryStone > adventure.WarehouseCapacity)
            {
                message = "Storage is full.";
                return false;
            }
            AddItem(adventure, JourneyCatalog.Stone, JourneyCatalog.RecoveryStone);
            adventure.NextRecoveryTime = adventure.ActionClock + JourneyCatalog.RecoveryCooldownSeconds;
            message = "Found loose stone to sell.";
            return true;
        }

        public static bool TryDiscoverGate(GameState state, out string message)
        {
            if (!state.Adventure.DiscoveredRegions.Contains(1))
                state.Adventure.DiscoveredRegions.Add(1);
            RefreshUnlocks(state);
            message = "Region 2 is on the map.";
            return true;
        }

        public static bool TryHire(GameState state, out string message)
        {
            if (state.Adventure.WorkerHired)
            {
                message = "A crew member is already hired.";
                return false;
            }
            if (!Wallet.TrySpend(state, JourneyCatalog.HireCost))
            {
                message = "Need " + JourneyCatalog.HireCost.ToString(CultureInfo.InvariantCulture) + " coins.";
                return false;
            }
            state.Adventure.WorkerHired = true;
            message = "Hired a yard crew member.";
            return true;
        }

        public static bool TryAssignWorker(GameState state, int plotId, out string message)
        {
            if (!state.Adventure.WorkerHired)
            {
                message = "Hire a crew member first.";
                return false;
            }
            if (!state.Adventure.Owned.Get(plotId))
            {
                message = "Assign them to a bunker you own.";
                return false;
            }
            state.Adventure.WorkerPlotId = plotId;
            message = "Crew posted at bunker " + (plotId + 1) + ".";
            return true;
        }

        public static void RefreshUnlocks(GameState state)
        {
            AdventureState adventure = state.Adventure;
            TryUnlock(adventure, "zela", OwnedCount(adventure) >= 6);
            TryUnlock(adventure, "pavi", adventure.WashBatches >= 1);
            TryUnlock(adventure, "nilo", adventure.LifetimeDiamondsRecovered >= 5d);
            TryUnlock(adventure, "etta", adventure.CutCount >= 1);
            TryUnlock(adventure, "rumi", adventure.PolishCount >= 1);
            TryUnlock(adventure, "kavi", adventure.RingsCrafted >= 1);
            TryUnlock(adventure, "beni", adventure.OrdersCompleted >= 3);
            TryUnlock(adventure, "daro", adventure.DiscoveredRegions.Contains(1));
        }

        public static string MachineStatus(MachineState machine)
        {
            if (machine == null)
                return "Missing";
            if (machine.BusyUnits > 0 && machine.BusySeconds > 0d)
                return "Working";
            if (machine.OutputAmount > 0d || machine.ExtraAmount > 0d)
                return "Ready";
            if (machine.BusyUnits > 0)
                return "Finishing";
            return "Idle";
        }

        public static string Bottleneck(AdventureState adventure)
        {
            string waiting = "";
            for (int i = 0; i < adventure.Machines.Count; i++)
            {
                MachineState machine = adventure.Machines[i];
                if (machine.OutputAmount > 0d || machine.ExtraAmount > 0d)
                    return machine.Id + " is full";
                if (machine.BusySeconds > 0d && (waiting.Length == 0 || machine.BusySeconds > 0d))
                    waiting = machine.Id;
            }
            return waiting.Length == 0 ? "All machines idle" : waiting + " is working";
        }

        public static double Remaining(AdventureState adventure, int plotId, string itemId)
        {
            DepositDelta delta;
            PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, plotId, adventure.GeneratorVersion);
            if (adventure.Deposits.TryGetValue(plotId, out delta))
            {
                double left;
                if (delta.Remaining.TryGetValue(itemId, out left))
                    return left;
            }
            return profile.AmountOf(itemId);
        }

        public static void Query(AdventureState adventure, string filter, int region, int start, int take, List<int> into)
        {
            into.Clear();
            if (take <= 0)
                return;
            if (take > 40)
                take = 40;
            if (filter == "owned")
            {
                int seen = 0;
                for (int id = 0; id < WorldGenerator.PlotCount && into.Count < take; id++)
                {
                    if (!adventure.Owned.Get(id))
                        continue;
                    if (region >= 0 && WorldGenerator.RegionOf(id) != region)
                        continue;
                    if (seen++ < start)
                        continue;
                    into.Add(id);
                }
                return;
            }
            int regionStart = region < 0 ? 0 : region;
            int regionEnd = region < 0 ? WorldGenerator.RegionCount : region + 1;
            int matched = 0;
            for (int r = regionStart; r < regionEnd && into.Count < take; r++)
            {
                int origin = r * WorldGenerator.PlotsPerRegion;
                for (int local = 0; local < WorldGenerator.PlotsPerRegion && into.Count < take; local++)
                {
                    int id = origin + local;
                    if (!Matches(adventure, filter, id))
                        continue;
                    if (matched++ < start)
                        continue;
                    into.Add(id);
                }
            }
        }

        static bool Matches(AdventureState adventure, string filter, int plotId)
        {
            bool owned = adventure.Owned.Get(plotId);
            if (filter == "sale")
                return !owned;
            if (filter == "exhausted")
                return owned && IsExhausted(adventure, plotId);
            if (filter == "active")
                return owned && !IsExhausted(adventure, plotId);
            return true;
        }

        static bool IsExhausted(AdventureState adventure, int plotId)
        {
            if (!adventure.Deposits.ContainsKey(plotId))
                return false;
            PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, plotId, adventure.GeneratorVersion);
            for (int i = 0; i < profile.Reserves.Count; i++)
            {
                if (Remaining(adventure, plotId, profile.Reserves[i].ItemId) > 0d)
                    return false;
            }
            return true;
        }

        static void TickMachines(AdventureState adventure, double seconds)
        {
            for (int i = 0; i < adventure.Machines.Count; i++)
            {
                MachineState machine = adventure.Machines[i];
                if (machine.BusyUnits <= 0 || machine.OutputGranted)
                    continue;
                if (seconds > 0d)
                    machine.BusySeconds -= seconds;
                if (machine.BusySeconds > 0d)
                    continue;
                CompleteMachine(adventure, machine);
            }
        }

        static void CompleteMachine(AdventureState adventure, MachineState machine)
        {
            if (machine.OutputGranted || machine.BusyUnits <= 0)
                return;
            machine.BusySeconds = 0d;
            machine.OutputGranted = true;
            int units = machine.BusyUnits;
            machine.BusyUnits = 0;
            machine.InputAmount = 0d;
            if (machine.Id == "crush")
            {
                machine.OutputItem = JourneyCatalog.CrushedOre;
                machine.OutputAmount = units;
            }
            else if (machine.Id == "wash")
            {
                machine.OutputItem = JourneyCatalog.WashedOre;
                machine.OutputAmount = 3d;
                machine.ExtraItem = JourneyCatalog.Gravel;
                machine.ExtraAmount = 1d;
                adventure.WashBatches++;
            }
            else if (machine.Id == "recover")
            {
                machine.OutputItem = JourneyCatalog.RoughDiamond;
                machine.OutputAmount = 1d;
                machine.ExtraItem = JourneyCatalog.Gravel;
                machine.ExtraAmount = 2d;
                adventure.LifetimeDiamondsRecovered += 1d;
            }
            else if (machine.Id == "cut")
            {
                machine.OutputItem = JourneyCatalog.CutDiamond;
                machine.OutputAmount = 1d;
                adventure.CutCount++;
            }
            else if (machine.Id == "polish")
            {
                int grade = PolishGrade(machine.Level);
                machine.OutputItem = JourneyCatalog.GradeItem(grade);
                machine.OutputAmount = 1d;
                adventure.PolishCount++;
            }
            else if (machine.Id == "refine")
            {
                machine.OutputItem = RefinedId(machine.InputItem);
                machine.OutputAmount = 1d;
                if (machine.OutputItem == JourneyCatalog.RefinedSilver)
                    adventure.SilverRefined++;
                if (machine.OutputItem == JourneyCatalog.RefinedGold)
                    adventure.GoldRefined++;
            }
        }

        static void TickJobs(GameState state, double seconds)
        {
            for (int i = 0; i < state.Adventure.Jobs.Count; i++)
            {
                CraftJob job = state.Adventure.Jobs[i];
                if (job.OutputGranted)
                    continue;
                if (seconds > 0d)
                    job.Remaining -= seconds;
                if (job.Remaining > 0d)
                    continue;
                JewelRecipe recipe = JourneyCatalog.FindRecipe(job.RecipeId);
                if (recipe == null)
                {
                    job.OutputGranted = true;
                    continue;
                }
                if (WarehouseUsed(state.Adventure) + recipe.OutputCount > state.Adventure.WarehouseCapacity)
                {
                    job.Remaining = 0d;
                    continue;
                }
                AddItem(state.Adventure, recipe.OutputItem, recipe.OutputCount);
                NoteCrafted(state.Adventure, recipe);
                job.Remaining = 0d;
                job.OutputGranted = true;
            }
        }

        static void TickWorker(GameState state, double seconds)
        {
            AdventureState adventure = state.Adventure;
            if (!adventure.WorkerHired || adventure.WorkerPlotId < 0 || seconds <= 0d)
                return;
            if (!adventure.Owned.Get(adventure.WorkerPlotId))
                return;
            adventure.WorkerFraction += seconds * JourneyCatalog.WorkerRate;
            if (adventure.WorkerFraction > 1000000d)
                adventure.WorkerFraction = 1000000d;
            int guard = 0;
            while (adventure.WorkerFraction >= 1d && guard++ < 100000)
            {
                string item = NextWorkerItem(adventure, adventure.WorkerPlotId);
                if (item == null)
                {
                    if (!adventure.WorkerSwitchesWhenBlocked || !SwitchWorker(adventure))
                    {
                        adventure.WorkerFraction = Math.Min(adventure.WorkerFraction, 1d);
                        return;
                    }
                    continue;
                }
                if (WarehouseUsed(adventure) + 1d > adventure.WarehouseCapacity)
                {
                    if (!adventure.WorkerSwitchesWhenBlocked)
                    {
                        adventure.WorkerFraction = Math.Min(adventure.WorkerFraction, 1d);
                        return;
                    }
                    if (!SwitchWorker(adventure))
                    {
                        adventure.WorkerFraction = Math.Min(adventure.WorkerFraction, 1d);
                        return;
                    }
                    continue;
                }
                double left = Remaining(adventure, adventure.WorkerPlotId, item);
                if (left < 1d)
                    continue;
                SetRemaining(adventure, adventure.WorkerPlotId, item, left - 1d);
                AddItem(adventure, item, 1d);
                ItemDef def = JourneyCatalog.FindItem(item);
                if (def != null && def.Ore)
                    adventure.OreCollected += 1d;
                adventure.WorkerFraction -= 1d;
            }
        }

        static bool SwitchWorker(AdventureState adventure)
        {
            int start = adventure.WorkerPlotId;
            for (int step = 1; step <= WorldGenerator.VisibleYardPlots; step++)
            {
                int plot = (start + step) % WorldGenerator.VisibleYardPlots;
                if (!adventure.Owned.Get(plot))
                    continue;
                if (NextWorkerItem(adventure, plot) != null && WarehouseUsed(adventure) + 1d <= adventure.WarehouseCapacity)
                {
                    adventure.WorkerPlotId = plot;
                    return true;
                }
            }
            return false;
        }

        static string NextWorkerItem(AdventureState adventure, int plotId)
        {
            PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, plotId, adventure.GeneratorVersion);
            for (int i = 0; i < profile.Reserves.Count; i++)
            {
                if (Remaining(adventure, plotId, profile.Reserves[i].ItemId) >= 1d)
                    return profile.Reserves[i].ItemId;
            }
            return null;
        }

        static void NoteCrafted(AdventureState adventure, JewelRecipe recipe)
        {
            int count;
            adventure.Crafted.TryGetValue(recipe.OutputItem, out count);
            adventure.Crafted[recipe.OutputItem] = count + recipe.OutputCount;
            if (recipe.OutputItem == JourneyCatalog.Ring)
                adventure.RingsCrafted += recipe.OutputCount;
        }

        static int Crafted(AdventureState adventure, string itemId)
        {
            int count;
            return adventure.Crafted.TryGetValue(itemId, out count) ? count : 0;
        }

        static int PolishGrade(int level)
        {
            int grade = 1 + Math.Max(0, level);
            if (grade > 4)
                grade = 4;
            if (grade >= JourneyCatalog.PerfectGrade)
                grade = 4;
            return grade;
        }

        static double Duration(MachineState machine)
        {
            double basis = 1.6d;
            if (machine.Id == "recover")
                basis = 1.8d;
            if (machine.Id == "cut" || machine.Id == "polish")
                basis = 1.4d;
            double scaled = basis * Math.Pow(0.85d, Math.Max(0, machine.Level));
            if (scaled < 0.35d)
                scaled = 0.35d;
            return scaled;
        }

        static bool BatchInput(string machineId, out string itemId, out int count)
        {
            itemId = "";
            count = 0;
            if (machineId == "crush")
            {
                itemId = JourneyCatalog.DiamondOre;
                count = 4;
                return true;
            }
            if (machineId == "wash")
            {
                itemId = JourneyCatalog.CrushedOre;
                count = 4;
                return true;
            }
            if (machineId == "recover")
            {
                itemId = JourneyCatalog.WashedOre;
                count = 3;
                return true;
            }
            if (machineId == "cut")
            {
                itemId = JourneyCatalog.RoughDiamond;
                count = 1;
                return true;
            }
            if (machineId == "polish")
            {
                itemId = JourneyCatalog.CutDiamond;
                count = 1;
                return true;
            }
            return false;
        }

        public static bool TryLoadRefine(GameState state, string oreId, out string message)
        {
            EnsureMachines(state.Adventure);
            if (RefinedId(oreId).Length == 0)
            {
                message = "That ore does not refine.";
                return false;
            }
            MachineState machine = state.Adventure.FindMachine("refine");
            if (machine.BusyUnits > 0 || machine.OutputAmount > 0d)
            {
                message = "Empty the furnace first.";
                return false;
            }
            if (Count(state.Adventure, oreId) < 2d)
            {
                message = "Need 2 " + JourneyCatalog.DisplayName(oreId) + ".";
                return false;
            }
            if (!TryTake(state.Adventure, oreId, 2d))
            {
                message = "Could not take the ore.";
                return false;
            }
            machine.InputItem = oreId;
            machine.InputAmount = 2d;
            machine.BusyUnits = 2;
            machine.BusySeconds = Duration(machine);
            machine.OutputGranted = false;
            message = "Refining.";
            return true;
        }

        static string RefinedId(string oreId)
        {
            if (oreId == JourneyCatalog.IronOre) return JourneyCatalog.RefinedIron;
            if (oreId == JourneyCatalog.CopperOre) return JourneyCatalog.RefinedCopper;
            if (oreId == JourneyCatalog.SilverOre) return JourneyCatalog.RefinedSilver;
            if (oreId == JourneyCatalog.GoldOre) return JourneyCatalog.RefinedGold;
            return "";
        }

        static void SetRemaining(AdventureState adventure, int plotId, string itemId, double remaining)
        {
            DepositDelta delta;
            if (!adventure.Deposits.TryGetValue(plotId, out delta))
            {
                delta = new DepositDelta { PlotId = plotId };
                PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, plotId, adventure.GeneratorVersion);
                for (int i = 0; i < profile.Reserves.Count; i++)
                    delta.Remaining[profile.Reserves[i].ItemId] = profile.Reserves[i].Amount;
                adventure.Deposits[plotId] = delta;
            }
            if (remaining < 0d)
                remaining = 0d;
            delta.Remaining[itemId] = remaining;
        }

        static void AddItem(AdventureState adventure, string itemId, double amount)
        {
            if (amount <= 0d || !NumericFormat.IsFinite(amount))
                return;
            double next = Count(adventure, itemId) + amount;
            if (next > MaxStack)
                next = MaxStack;
            adventure.Inventory[itemId] = next;
        }

        static bool TryTake(AdventureState adventure, string itemId, double amount)
        {
            if (amount <= 0d || Count(adventure, itemId) + 1e-6 < amount)
                return false;
            double left = Count(adventure, itemId) - amount;
            if (left <= 1e-9)
                adventure.Inventory.Remove(itemId);
            else
                adventure.Inventory[itemId] = left;
            return true;
        }

        static int DiamondCount(AdventureState adventure, int minGrade)
        {
            int total = 0;
            for (int grade = minGrade; grade <= 4; grade++)
                total += (int)Math.Floor(Count(adventure, JourneyCatalog.GradeItem(grade)));
            return total;
        }

        static bool TryTakeDiamonds(AdventureState adventure, int count, int minGrade)
        {
            if (DiamondCount(adventure, minGrade) < count)
                return false;
            int left = count;
            for (int grade = minGrade; grade <= 4 && left > 0; grade++)
            {
                string id = JourneyCatalog.GradeItem(grade);
                int stack = (int)Math.Floor(Count(adventure, id));
                int take = Math.Min(stack, left);
                if (take > 0 && TryTake(adventure, id, take))
                    left -= take;
            }
            return left == 0;
        }

        static void TryUnlock(AdventureState adventure, string id, bool ready)
        {
            if (!ready || adventure.UnlockedCharacters.Contains(id))
                return;
            if (JourneyCatalog.FindCharacter(id) == null)
                return;
            adventure.UnlockedCharacters.Add(id);
        }

        public static string PreviewPlot(AdventureState adventure, int plotId)
        {
            if (!WorldGenerator.IsValid(plotId))
                return "Unknown bunker.";
            PlotProfile profile = WorldGenerator.Describe(adventure.WorldSeed, plotId, adventure.GeneratorVersion);
            var text = new StringBuilder();
            text.Append("Bunker ");
            text.Append((plotId + 1).ToString(CultureInfo.InvariantCulture));
            text.Append(" · ");
            text.Append(profile.Theme);
            text.Append(" · ");
            text.Append(profile.Richness);
            text.Append(". ");
            text.Append(WorldGenerator.ReserveText(profile));
            if (!adventure.Owned.Get(plotId) && !profile.Starter)
            {
                text.Append(". Price ");
                text.Append(PlotPrice(adventure, plotId).ToString("0", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }
    }
}
