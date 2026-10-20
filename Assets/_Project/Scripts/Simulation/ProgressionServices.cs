using System;
using System.Globalization;
using System.Text;

namespace DiamondMine.Simulation
{
    public sealed class PurchasePreview
    {
        public bool Affordable;
        public int Count;
        public double Cost;
        public double CoinsAfter;
        public string Summary;
        public string DenyReason;
    }

    public static class UpgradeService
    {
        public static double CostFor(int level, int count, double baseCost, double growth)
        {
            if (level < 0 || count < 0) return double.PositiveInfinity;
            if (count <= 0)
                return 0d;
            if (baseCost <= 0d || growth < 1d || !NumericFormat.IsFinite(baseCost) || !NumericFormat.IsFinite(growth))
                return double.PositiveInfinity;
            if (Math.Abs(growth - 1d) < 1e-12)
                return baseCost * count;
            double first = baseCost * Math.Pow(growth, level);
            double sum = first * (Math.Pow(growth, count) - 1d) / (growth - 1d);
            if (!NumericFormat.IsFinite(sum) || sum < 0d)
                return double.PositiveInfinity;
            return sum;
        }

        public static int MaxAffordable(double coins, int level, int maxLevel, double baseCost, double growth)
        {
            if (level < 0 || maxLevel < level) return 0;
            int remaining = maxLevel - level;
            if (remaining <= 0 || !(coins >= 0d) || !NumericFormat.IsFinite(coins))
                return 0;
            int lo = 0;
            int hi = remaining;
            while (lo < hi)
            {
                int distance = hi - lo;
                int mid = lo + distance / 2 + distance % 2;
                double cost = CostFor(level, mid, baseCost, growth);
                if (cost <= coins)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo;
        }

        public static PurchasePreview Preview(GameState state, EconomyConfig config, string mineId, string upgradeId, int count)
        {
            var preview = new PurchasePreview();
            if (count < 0)
            {
                preview.DenyReason = "Choose a positive quantity, or zero for maximum.";
                return preview;
            }
            string reason;
            UpgradeDefinition upgrade;
            MineDefinition def;
            MineState mine;
            if (!TryResolve(state, config, mineId, upgradeId, out def, out mine, out upgrade, out reason))
            {
                preview.DenyReason = reason;
                preview.Summary = reason;
                return preview;
            }

            int level = mine.Level(upgrade.Id);
            int room = upgrade.MaxLevel - level;
            if (count < 1)
                count = MaxAffordable(state.Coins, level, upgrade.MaxLevel, upgrade.BaseCost, upgrade.CostGrowth);
            if (count > room)
                count = room;
            preview.Count = count;
            if (count < 1)
            {
                preview.DenyReason = room < 1 ? "Fully upgraded." : "Not enough coins.";
                preview.Summary = Describe(def, mine, state, config, upgrade, level, level);
                preview.Cost = room < 1 ? 0d : upgrade.BaseCost * Math.Pow(upgrade.CostGrowth, level);
                return preview;
            }

            preview.Cost = CostFor(level, count, upgrade.BaseCost, upgrade.CostGrowth);
            preview.Affordable = Wallet.CanSpend(state, preview.Cost);
            preview.CoinsAfter = preview.Affordable ? Math.Max(0d, state.Coins - preview.Cost) : state.Coins;
            preview.Summary = Describe(def, mine, state, config, upgrade, level, level + count);
            if (!preview.Affordable)
                preview.DenyReason = "Not enough coins.";
            return preview;
        }

        public static bool TryBuy(GameState state, EconomyConfig config, string mineId, string upgradeId, int count)
        {
            PurchasePreview preview = Preview(state, config, mineId, upgradeId, count);
            if (!preview.Affordable || preview.Count < 1)
                return false;
            if (!Wallet.TrySpend(state, preview.Cost))
                return false;
            MineState mine = state.FindMine(mineId);
            mine.UpgradeLevels[upgradeId] = mine.Level(upgradeId) + preview.Count;
            MineDefinition def = config.FindMine(mineId);
            MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, state.BoostRemainingSeconds > 0d);
            ProductionSimulator.SyncCarts(mine, rates);
            return true;
        }

        public static bool TryBuyBestPayback(GameState state, EconomyConfig config)
        {
            string mineId;
            string upgradeId;
            if (!TrySelectBest(state, config, out mineId, out upgradeId))
                return false;
            return TryBuy(state, config, mineId, upgradeId, 1);
        }

        public static bool TrySelectBest(GameState state, EconomyConfig config, out string mineId, out string upgradeId)
        {
            mineId = null;
            upgradeId = null;
            double best = 0d;
            bool tapping = !StarterAutomated(state, config);
            double before = MineStats.SustainedIncome(state, config, tapping);
            for (int m = 0; m < state.Mines.Count; m++)
            {
                MineState mine = state.Mines[m];
                if (!mine.Unlocked)
                    continue;
                MineDefinition def = config.FindMine(mine.Id);
                if (def == null)
                    continue;
                for (int u = 0; u < def.Upgrades.Count; u++)
                {
                    UpgradeDefinition upgrade = def.Upgrades[u];
                    if (mine.Level(upgrade.Id) >= upgrade.MaxLevel)
                        continue;
                    if (!MineStats.RequirementMet(def, mine, upgrade))
                        continue;
                    PurchasePreview preview = Preview(state, config, mine.Id, upgrade.Id, 1);
                    if (!preview.Affordable)
                        continue;
                    GameState clone = state.Clone();
                    TryBuy(clone, config, mine.Id, upgrade.Id, 1);
                    double after = MineStats.SustainedIncome(clone, config, false);
                    double gain = after - before;
                    if (upgrade.Stat == "autoUnlock")
                        gain = Math.Max(gain, after);
                    if (upgrade.Stat == "storage")
                        gain = Math.Max(gain, upgrade.Amount * def.CoinValue * 0.2d);
                    if (preview.Cost <= 0d || gain <= 0d)
                        continue;
                    double score = gain / preview.Cost;
                    if (score > best)
                    {
                        best = score;
                        mineId = mine.Id;
                        upgradeId = upgrade.Id;
                    }
                }
            }
            return upgradeId != null;
        }

        public static int TotalLevels(GameState state)
        {
            int total = 0;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                foreach (var pair in state.Mines[i].UpgradeLevels)
                    total += pair.Value;
            }
            return total;
        }

        public static int StageLevels(MineDefinition def, MineState mine, string stage)
        {
            int total = 0;
            for (int i = 0; i < def.Upgrades.Count; i++)
            {
                if (def.Upgrades[i].Stage == stage)
                    total += mine.Level(def.Upgrades[i].Id);
            }
            return total;
        }

        static bool TryResolve(GameState state, EconomyConfig config, string mineId, string upgradeId, out MineDefinition def, out MineState mine, out UpgradeDefinition upgrade, out string reason)
        {
            def = config.FindMine(mineId);
            mine = state.FindMine(mineId);
            upgrade = def != null ? def.FindUpgrade(upgradeId) : null;
            if (def == null || mine == null || upgrade == null)
            {
                reason = "That upgrade is unavailable.";
                return false;
            }
            if (!mine.Unlocked)
            {
                reason = "Open this mine first.";
                return false;
            }
            if (!MineStats.RequirementMet(def, mine, upgrade))
            {
                reason = "Requires " + upgrade.Requires + ".";
                return false;
            }
            reason = null;
            return true;
        }

        static string Describe(MineDefinition def, MineState mine, GameState state, EconomyConfig config, UpgradeDefinition upgrade, int from, int to)
        {
            if (upgrade.Stat == "autoUnlock")
                return to > from ? "Hires a miner. The face works without tapping." : upgrade.Description;
            MineState beforeState = mine.Clone();
            beforeState.UpgradeLevels[upgrade.Id] = from;
            MineState afterState = mine.Clone();
            afterState.UpgradeLevels[upgrade.Id] = to;
            bool boost = state.BoostRemainingSeconds > 0d;
            MineRates before = MineStats.Compute(def, beforeState, config, state.Mastery, boost);
            MineRates after = MineStats.Compute(def, afterState, config, state.Mastery, boost);
            double a;
            double b;
            string unit;
            switch (upgrade.Stat)
            {
                case "tapOre": a = before.TapOre; b = after.TapOre; unit = " ore/tap"; break;
                case "mineBuffer": a = before.MineBuffer; b = after.MineBuffer; unit = " ore held"; break;
                case "processRate": a = before.ProcessRate; b = after.ProcessRate; unit = " ore/s"; break;
                case "yield": a = before.Yield; b = after.Yield; unit = " diamonds/ore"; break;
                case "processBuffer": a = before.ProcessBuffer; b = after.ProcessBuffer; unit = " ore held"; break;
                case "roughBuffer": a = before.RoughBuffer; b = after.RoughBuffer; unit = " held"; break;
                case "sortRate": a = before.SortRate; b = after.SortRate; unit = " diamonds/s"; break;
                case "storage": a = before.StorageCap; b = after.StorageCap; unit = " diamonds held"; break;
                case "sellRate": a = before.SellRate; b = after.SellRate; unit = " diamonds/s"; break;
                case "coinValue": a = before.CoinValue; b = after.CoinValue; unit = " coins each"; break;
                case "cartCount": a = before.CartCount; b = after.CartCount; unit = " carts"; break;
                case "cartCapacity": a = before.CartCapacity; b = after.CartCapacity; unit = " ore"; break;
                case "tripCut": a = before.TripSeconds; b = after.TripSeconds; unit = "s trip"; break;
                case "autoRate": a = before.AutoRate; b = after.AutoRate; unit = " ore/s"; break;
                default: return upgrade.Description;
            }
            return NumericFormat.Format(a) + unit + " → " + NumericFormat.Format(b) + unit;
        }

        static bool StarterAutomated(GameState state, EconomyConfig config)
        {
            if (config.Mines.Count == 0)
                return false;
            MineState mine = state.FindMine(config.Mines[0].Id);
            MineDefinition def = config.Mines[0];
            if (mine == null)
                return false;
            return MineStats.Compute(def, mine, config, state.Mastery, false).AutoUnlocked;
        }
    }

    public sealed class PrestigePreview
    {
        public bool Available;
        public double MasteryGain;
        public double MasteryAfter;
        public double BonusBeforePercent;
        public double BonusAfterPercent;
        public string Summary;
        public string DenyReason;
    }

    public static class PrestigeService
    {
        public static PrestigePreview Preview(GameState state, EconomyConfig config)
        {
            var preview = new PrestigePreview();
            preview.MasteryGain = Gain(state, config);
            preview.MasteryAfter = NumericFormat.Add(state.Mastery, preview.MasteryGain);
            preview.BonusBeforePercent = state.Mastery * config.MasteryBonusPerPoint * 100d;
            preview.BonusAfterPercent = preview.MasteryAfter * config.MasteryBonusPerPoint * 100d;
            if (state.PrestigeCount >= int.MaxValue)
            {
                preview.DenyReason = "The mastery cycle limit has been reached.";
            }
            else if (preview.MasteryGain < 1d)
            {
                preview.DenyReason = "Earn more coins before prestiging.";
            }
            else if (state.CoinsSincePrestige + 1e-6 < config.PrestigeMinimumCoins)
            {
                preview.DenyReason = "Reach " + NumericFormat.Format(config.PrestigeMinimumCoins) + " coins earned since the last prestige.";
            }
            else if (config.PrestigeRequiresAutomation && !IsAutomated(state, config))
            {
                preview.DenyReason = "Hire a miner before prestiging.";
            }
            else
            {
                preview.Available = true;
            }

            var text = new StringBuilder();
            text.Append("Mastery ");
            text.Append(state.Mastery.ToString("0", CultureInfo.InvariantCulture));
            text.Append(" → ");
            text.Append(preview.MasteryAfter.ToString("0", CultureInfo.InvariantCulture));
            text.Append(". Production bonus ");
            text.Append(preview.BonusBeforePercent.ToString("0.#", CultureInfo.InvariantCulture));
            text.Append("% → ");
            text.Append(preview.BonusAfterPercent.ToString("0.#", CultureInfo.InvariantCulture));
            text.Append("%. Coins, ore, diamonds, and upgrade levels reset. Mastery, settings, and the tutorial stay.");
            preview.Summary = text.ToString();
            return preview;
        }

        public static bool TryPrestige(GameState state, EconomyConfig config)
        {
            PrestigePreview preview = Preview(state, config);
            if (!preview.Available)
                return false;

            var settings = state.Settings.Clone();
            var claimed = new System.Collections.Generic.List<string>(state.ClaimedObjectives);
            var orders = new System.Collections.Generic.List<string>(state.ClaimedOrders);
            var tokens = new System.Collections.Generic.List<string>(state.UsedAdTokens);
            double mastery = preview.MasteryAfter;
            double lifetime = state.LifetimeCoins;
            double earned = state.CoinsEarned;
            double spent = state.CoinsSpent;
            double discarded = NumericFormat.Add(state.CoinsDiscarded, state.Coins);
            int prestige = state.PrestigeCount + 1;
            double play = state.TotalPlaySeconds;
            int tutorialStep = state.TutorialStep;
            bool tutorialDone = state.TutorialComplete;
            bool bottleneckSeen = state.BottleneckSeen;
            bool objectivePresented = state.ObjectivePresented;

            GameState fresh = GameFactory.NewGame(config);
            state.CopyFrom(fresh);
            state.Mastery = mastery;
            state.LifetimeCoins = lifetime;
            state.CoinsEarned = earned;
            state.CoinsSpent = spent;
            state.CoinsDiscarded = discarded;
            state.PrestigeCount = prestige;
            state.TotalPlaySeconds = play;
            state.TutorialStep = tutorialDone ? 8 : tutorialStep;
            state.TutorialComplete = tutorialDone;
            state.BottleneckSeen = bottleneckSeen;
            state.ObjectivePresented = objectivePresented;
            state.Settings = settings;
            state.ClaimedObjectives.Clear();
            state.ClaimedOrders.AddRange(orders);
            for (int i = 0; i < claimed.Count; i++)
                state.ClaimedObjectives.Add(claimed[i]);
            state.UsedAdTokens.Clear();
            for (int i = 0; i < tokens.Count; i++)
                state.UsedAdTokens.Add(tokens[i]);
            state.Coins = 0d;
            state.CoinsSincePrestige = 0d;
            state.BoostRemainingSeconds = 0d;
            return true;
        }

        public static double Gain(GameState state, EconomyConfig config)
        {
            if (state.CoinsSincePrestige <= 0d || config.PrestigeDivisor <= 0d)
                return 0d;
            double raw = Math.Sqrt(state.CoinsSincePrestige / config.PrestigeDivisor);
            if (!NumericFormat.IsFinite(raw) || raw < 1d)
                return raw >= 1d ? Math.Floor(raw) : 0d;
            return Math.Floor(raw);
        }

        static bool IsAutomated(GameState state, EconomyConfig config)
        {
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineDefinition def = config.FindMine(state.Mines[i].Id);
                if (def == null || !state.Mines[i].Unlocked)
                    continue;
                if (MineStats.Compute(def, state.Mines[i], config, 0d, false).AutoUnlocked)
                    return true;
            }
            return false;
        }
    }

    public static class MineUnlocks
    {
        public static bool CanUnlock(GameState state, EconomyConfig config, string mineId, out string reason)
        {
            MineDefinition def = config.FindMine(mineId);
            MineState mine = state.FindMine(mineId);
            if (def == null || mine == null)
            {
                reason = "Unknown mine.";
                return false;
            }
            if (mine.Unlocked)
            {
                reason = "Already open.";
                return false;
            }
            if (state.LifetimeCoins + 1e-6 < def.UnlockLifetimeCoins)
            {
                reason = "Earn " + NumericFormat.Format(def.UnlockLifetimeCoins) + " lifetime coins.";
                return false;
            }
            reason = null;
            return true;
        }

        public static bool TryUnlock(GameState state, EconomyConfig config, string mineId)
        {
            string reason;
            if (!CanUnlock(state, config, mineId, out reason))
                return false;
            MineState mine = state.FindMine(mineId);
            MineDefinition def = config.FindMine(mineId);
            mine.Unlocked = true;
            MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, state.BoostRemainingSeconds > 0d);
            ProductionSimulator.SyncCarts(mine, rates);
            return true;
        }
    }

    public static class ObjectiveService
    {
        public static bool IsMet(GameState state, EconomyConfig config, ObjectiveDefinition objective)
        {
            switch (objective.Type)
            {
                case "oreMined": return Sum(state, delegate (MineState mine) { return mine.OreMined; }) + 1e-6 >= objective.Target;
                case "diamondsSold": return Sum(state, delegate (MineState mine) { return mine.DiamondsSold; }) + 1e-6 >= objective.Target;
                case "lifetimeCoins": return state.LifetimeCoins + 1e-6 >= objective.Target;
                case "upgradesBought": return UpgradeService.TotalLevels(state) >= objective.Target;
                case "prestigeCount": return state.PrestigeCount >= objective.Target;
                case "mineUnlocked":
                    MineState unlocked = state.FindMine(objective.MineId);
                    return unlocked != null && unlocked.Unlocked;
                case "automation":
                    MineState autoMine = state.FindMine(objective.MineId);
                    MineDefinition autoDef = config.FindMine(objective.MineId);
                    return autoMine != null && autoDef != null && MineStats.Compute(autoDef, autoMine, config, 0d, false).AutoUnlocked;
                case "upgradeLevel":
                    MineState leveled = state.FindMine(objective.MineId);
                    return leveled != null && leveled.Level(objective.UpgradeId) + 1e-6 >= objective.Target;
                default:
                    return false;
            }
        }

        public static ObjectiveDefinition Current(GameState state, EconomyConfig config)
        {
            for (int i = 0; i < config.Objectives.Count; i++)
            {
                if (!IsMet(state, config, config.Objectives[i]))
                    return config.Objectives[i];
            }
            return null;
        }

        public static int ClaimCompleted(GameState state, EconomyConfig config)
        {
            int claimed = 0;
            for (int i = 0; i < config.Objectives.Count; i++)
            {
                ObjectiveDefinition objective = config.Objectives[i];
                if (state.ClaimedObjectives.Contains(objective.Id))
                    continue;
                if (!IsMet(state, config, objective))
                    continue;
                state.ClaimedObjectives.Add(objective.Id);
                Wallet.Credit(state, objective.Reward);
                claimed++;
            }
            return claimed;
        }

        static double Sum(GameState state, Func<MineState, double> select)
        {
            double total = 0d;
            for (int i = 0; i < state.Mines.Count; i++)
                total += select(state.Mines[i]);
            return total;
        }
    }

    public static class TutorialService
    {
        public const int Done = 8;

        public static string Prompt(GameState state, EconomyConfig config)
        {
            switch (state.TutorialStep)
            {
                case 0: return "Tap the rock to mine.";
                case 1: return "Watch the cart carry ore to the crusher.";
                case 2: return "Sorted diamonds sell for coins.";
                case 3: return "Buy an upgrade.";
                case 4: return "The marked stage is slowing the mine.";
                case 5: return "Upgrade that slow stage.";
                case 6: return "Hire a miner so the face works on its own.";
                case 7:
                    ObjectiveDefinition current = ObjectiveService.Current(state, config);
                    return current == null ? "Your objectives are complete." : "Next goal: " + current.Title + ".";
                default: return "";
            }
        }

        public static void NotifyBottleneckShown(GameState state)
        {
            if (state.TutorialStep == 4)
                state.BottleneckSeen = true;
        }

        public static void NotifyObjectiveShown(GameState state)
        {
            if (state.TutorialStep >= 7)
                state.ObjectivePresented = true;
        }

        public static void Reconcile(GameState state, EconomyConfig config)
        {
            if (state.TutorialComplete)
            {
                state.TutorialStep = Done;
                return;
            }

            int guard = 0;
            while (state.TutorialStep < Done && guard++ < 12 && IsStepComplete(state, config, state.TutorialStep))
                state.TutorialStep++;
            if (state.TutorialStep >= Done)
                state.TutorialComplete = true;
        }

        static bool IsStepComplete(GameState state, EconomyConfig config, int step)
        {
            MineState mine = config.Mines.Count > 0 ? state.FindMine(config.Mines[0].Id) : null;
            MineDefinition def = config.Mines.Count > 0 ? config.Mines[0] : null;
            if (mine == null || def == null)
                return false;
            int levels = UpgradeService.TotalLevels(state);
            bool automated = MineStats.Compute(def, mine, config, 0d, false).AutoUnlocked;
            switch (step)
            {
                case 0: return mine.TapCount > 0 || mine.OreMined > 0d;
                case 1: return mine.OreProcessed > 0d;
                case 2: return mine.DiamondsSold > 0d || state.LifetimeCoins > 0d;
                case 3: return levels >= 1 || automated;
                case 4: return state.BottleneckSeen || levels >= 2 || automated;
                case 5: return levels >= 2 || automated;
                case 6: return automated;
                case 7: return state.ObjectivePresented;
                default: return true;
            }
        }
    }
}
