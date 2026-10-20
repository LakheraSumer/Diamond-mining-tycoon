using System;
using System.Globalization;

namespace DiamondMine.Simulation
{
    public static class EconomyLoader
    {
        public static EconomyConfig Load(string json)
        {
            JsonValue root = JsonValue.Parse(json);
            var config = new EconomyConfig();
            config.Version = root.Int("version", 1);
            config.OfflineCapSeconds = root.Number("offlineCapSeconds", 8d * 3600d);
            config.BackwardClockGraceSeconds = root.Number("backwardClockGraceSeconds", 120d);
            config.ImplausibleForwardSeconds = root.Number("implausibleForwardSeconds", 365d * 86400d);
            config.MasteryBonusPerPoint = root.Number("masteryBonusPerPoint", 0.02);
            config.PrestigeDivisor = root.Number("prestigeDivisor", 20000d);
            config.PrestigeMinimumCoins = root.Number("prestigeMinimumCoins", 80000d);
            config.PrestigeRequiresAutomation = root.Bool("prestigeRequiresAutomation", true);
            config.BoostMultiplier = root.Number("boostMultiplier", 2d);
            config.BoostDurationSeconds = root.Number("boostDurationSeconds", 300d);
            config.OfflineAdBonusFraction = root.Number("offlineAdBonusFraction", 0.5);
            config.MaxAdTokenHistory = root.Int("maxAdTokenHistory", 24);

            var mines = root.Array("mines");
            for (int i = 0; i < mines.Count; i++)
                config.Mines.Add(ReadMine(mines[i]));

            var objectives = root.Array("objectives");
            for (int i = 0; i < objectives.Count; i++)
                config.Objectives.Add(ReadObjective(objectives[i]));

            var orders = root.Array("orders");
            for (int i = 0; i < orders.Count; i++)
                config.Orders.Add(new OrderDefinition
                {
                    Id = orders[i].Str("id", ""), Title = orders[i].Str("title", "Delivery"),
                    MineId = orders[i].Str("mineId", ""), Diamonds = orders[i].Number("diamonds", 1),
                    Bonus = orders[i].Number("bonus", 0)
                });

            Validate(config);
            return config;
        }

        static MineDefinition ReadMine(JsonValue node)
        {
            var mine = new MineDefinition();
            mine.Id = node.Str("id", "");
            mine.Name = node.Str("name", mine.Id);
            mine.Description = node.Str("description", "");
            mine.Palette = node.Str("palette", "warmstone");
            mine.UnlockLifetimeCoins = node.Number("unlockLifetimeCoins", 0d);
            mine.TapOre = node.Number("tapOre", 1d);
            mine.TapCooldown = node.Number("tapCooldown", 0.4);
            mine.MineBuffer = node.Number("mineBuffer", 20d);
            mine.ProcessRate = node.Number("processRate", 1d);
            mine.Yield = node.Number("yield", 0.2);
            mine.ProcessBuffer = node.Number("processBuffer", 20d);
            mine.RoughBuffer = node.Number("roughBuffer", 10d);
            mine.SortRate = node.Number("sortRate", 1d);
            mine.StorageCap = node.Number("storage", 20d);
            mine.SellRate = node.Number("sellRate", 1d);
            mine.CoinValue = node.Number("coinValue", 1d);
            mine.CartCount = node.Int("cartCount", 1);
            mine.CartCapacity = node.Number("cartCapacity", 5d);
            mine.TripSeconds = node.Number("tripSeconds", 4d);
            mine.DepartWait = node.Number("departWait", 2d);
            mine.BaseAutoRate = node.Number("baseAutoRate", 1d);
            var upgrades = node.Array("upgrades");
            for (int i = 0; i < upgrades.Count; i++)
            {
                JsonValue item = upgrades[i];
                mine.Upgrades.Add(new UpgradeDefinition
                {
                    Id = item.Str("id", ""),
                    Name = item.Str("name", item.Str("id", "Upgrade")),
                    Description = item.Str("description", ""),
                    Stage = item.Str("stage", Stages.Mining),
                    MaxLevel = item.Int("maxLevel", 1),
                    BaseCost = item.Number("baseCost", 1d),
                    CostGrowth = item.Number("costGrowth", 1.2),
                    Stat = item.Str("stat", ""),
                    Amount = item.Number("amount", 0d),
                    Requires = item.Str("requires", "")
                });
            }
            return mine;
        }

        static ObjectiveDefinition ReadObjective(JsonValue node)
        {
            return new ObjectiveDefinition
            {
                Id = node.Str("id", ""),
                Title = node.Str("title", "Objective"),
                Detail = node.Str("detail", ""),
                Type = node.Str("type", ""),
                MineId = node.Str("mineId", ""),
                UpgradeId = node.Str("upgradeId", ""),
                Target = node.Number("target", 1d),
                Reward = node.Number("reward", 0d)
            };
        }

        static void Validate(EconomyConfig config)
        {
            if (config.Mines.Count < 3)
                throw new FormatException("Economy needs at least three mines.");
            if (config.OfflineCapSeconds <= 0d || config.PrestigeDivisor <= 0d || config.BoostMultiplier < 1d)
                throw new FormatException("Economy limits must be positive.");
            if (!(config.OfflineAdBonusFraction >= 0d) || config.OfflineAdBonusFraction > 5d)
                throw new FormatException("Offline ad bonus fraction is out of range.");

            var ids = new System.Collections.Generic.HashSet<string>();
            var orderIds = new System.Collections.Generic.HashSet<string>();
            foreach (OrderDefinition order in config.Orders)
                if (string.IsNullOrEmpty(order.Id) || !orderIds.Add(order.Id) || config.FindMine(order.MineId) == null ||
                    !NumericFormat.IsFinite(order.Diamonds) || order.Diamonds <= 0 || !NumericFormat.IsFinite(order.Bonus) || order.Bonus < 0)
                    throw new FormatException("Order configuration is invalid.");
            for (int i = 0; i < config.Mines.Count; i++)
            {
                MineDefinition mine = config.Mines[i];
                if (string.IsNullOrEmpty(mine.Id) || !ids.Add(mine.Id))
                    throw new FormatException("Mine ids must be unique.");
                if (mine.TapOre <= 0d || mine.ProcessRate <= 0d || mine.SortRate <= 0d || mine.SellRate <= 0d)
                    throw new FormatException("Mine " + mine.Id + " has a non-positive rate.");
                if (mine.Yield <= 0d || mine.CartCapacity <= 0d || mine.TripSeconds <= 0d || mine.CoinValue <= 0d)
                    throw new FormatException("Mine " + mine.Id + " has an invalid conversion value.");
                var upgradeIds = new System.Collections.Generic.HashSet<string>();
                for (int u = 0; u < mine.Upgrades.Count; u++)
                {
                    UpgradeDefinition upgrade = mine.Upgrades[u];
                    if (string.IsNullOrEmpty(upgrade.Id) || !upgradeIds.Add(upgrade.Id))
                        throw new FormatException("Upgrade ids must be unique inside " + mine.Id + ".");
                    if (upgrade.MaxLevel < 1 || upgrade.BaseCost <= 0d || upgrade.CostGrowth < 1d)
                        throw new FormatException("Upgrade " + upgrade.Id + " has invalid pricing.");
                }
            }
        }

        public static string FormatCoins(double value)
        {
            return NumericFormat.Format(value);
        }
    }

    public static class GameFactory
    {
        public static GameState NewGame(EconomyConfig config)
        {
            var state = new GameState();
            state.ActiveMineId = config.Mines.Count > 0 ? config.Mines[0].Id : "";
            for (int i = 0; i < config.Mines.Count; i++)
            {
                MineDefinition def = config.Mines[i];
                state.Mines.Add(CreateMine(def, def.UnlockLifetimeCoins <= 0d));
            }
            return state;
        }

        public static MineState CreateMine(MineDefinition def, bool unlocked)
        {
            var mine = new MineState { Id = def.Id, Unlocked = unlocked };
            var rates = MineStats.Compute(def, mine, new EconomyConfig(), 0d, false);
            ProductionSimulator.SyncCarts(mine, rates);
            return mine;
        }
    }
}
