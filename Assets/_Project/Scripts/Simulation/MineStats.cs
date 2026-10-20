using System;

namespace DiamondMine.Simulation
{
    public sealed class MineRates
    {
        public double TapOre;
        public double TapCooldown;
        public double AutoRate;
        public bool AutoUnlocked;
        public double MineBuffer;
        public double ProcessRate;
        public double Yield;
        public double ProcessBuffer;
        public double RoughBuffer;
        public double SortRate;
        public double StorageCap;
        public double SellRate;
        public double CoinValue;
        public int CartCount;
        public double CartCapacity;
        public double TripSeconds;
        public double DepartWait;
        public double Multiplier;
    }

    public static class MineStats
    {
        public static MineRates Compute(MineDefinition def, MineState mine, EconomyConfig config, double mastery, bool boost)
        {
            double tap = def.TapOre;
            double buffer = def.MineBuffer;
            double process = def.ProcessRate;
            double yield = def.Yield;
            double processBuffer = def.ProcessBuffer;
            double roughBuffer = def.RoughBuffer;
            double sort = def.SortRate;
            double storage = def.StorageCap;
            double sell = def.SellRate;
            double coin = def.CoinValue;
            int carts = def.CartCount;
            double cartCap = def.CartCapacity;
            double trip = def.TripSeconds;
            double auto = def.BaseAutoRate;
            bool unlocked = false;

            for (int i = 0; i < def.Upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = def.Upgrades[i];
                int level = mine.Level(upgrade.Id);
                if (level <= 0)
                    continue;
                if (!RequirementMet(def, mine, upgrade))
                    continue;
                double mag = upgrade.Amount * level;
                switch (upgrade.Stat)
                {
                    case "tapOre": tap += mag; break;
                    case "mineBuffer": buffer += mag; break;
                    case "processRate": process += mag; break;
                    case "yield": yield += mag; break;
                    case "processBuffer": processBuffer += mag; break;
                    case "roughBuffer": roughBuffer += mag; break;
                    case "sortRate": sort += mag; break;
                    case "storage": storage += mag; break;
                    case "sellRate": sell += mag; break;
                    case "coinValue": coin += mag; break;
                    case "cartCount": carts += (int)Math.Round(mag); break;
                    case "cartCapacity": cartCap += mag; break;
                    case "tripCut": trip -= mag; break;
                    case "autoRate": auto += mag; break;
                    case "autoUnlock": unlocked = true; break;
                }
            }

            double masteryFactor = 1d + Math.Max(0d, mastery) * config.MasteryBonusPerPoint;
            double multiplier = masteryFactor * (boost ? config.BoostMultiplier : 1d);
            if (!NumericFormat.IsFinite(multiplier) || multiplier < 0d)
                multiplier = 1d;

            var rates = new MineRates();
            rates.TapOre = Math.Max(0.01, tap) * multiplier;
            rates.TapCooldown = Math.Max(0.05, def.TapCooldown);
            rates.AutoUnlocked = unlocked;
            rates.AutoRate = unlocked ? Math.Max(0d, auto) * multiplier : 0d;
            rates.MineBuffer = Math.Max(1d, buffer);
            rates.ProcessRate = Math.Max(0.01, process) * multiplier;
            rates.Yield = Math.Max(0.01, Math.Min(2d, yield));
            rates.ProcessBuffer = Math.Max(1d, processBuffer);
            rates.RoughBuffer = Math.Max(1d, roughBuffer);
            rates.SortRate = Math.Max(0.01, sort) * multiplier;
            rates.StorageCap = Math.Max(1d, storage);
            rates.SellRate = Math.Max(0.01, sell) * multiplier;
            rates.CoinValue = Math.Max(0.01, coin);
            rates.CartCount = Math.Max(1, Math.Min(8, carts));
            rates.CartCapacity = Math.Max(1d, cartCap);
            rates.TripSeconds = Math.Max(1.1, trip / multiplier);
            rates.DepartWait = Math.Max(0.2, def.DepartWait);
            rates.Multiplier = multiplier;
            return rates;
        }

        public static bool RequirementMet(MineDefinition def, MineState mine, UpgradeDefinition upgrade)
        {
            if (string.IsNullOrEmpty(upgrade.Requires))
                return true;
            return mine.Level(upgrade.Requires) > 0;
        }

        public static double SustainedCoinsPerSecond(MineRates rates, bool assumeTapping)
        {
            double trip = Math.Max(1.1, rates.TripSeconds);
            double transportOre = rates.CartCount * rates.CartCapacity / (2d * trip);
            double mineOre = rates.AutoUnlocked
                ? rates.AutoRate
                : (assumeTapping ? rates.TapOre / rates.TapCooldown : 0d);
            double ore = Min3(mineOre, transportOre, rates.ProcessRate);
            double diamonds = Min3(ore * rates.Yield, rates.SortRate, rates.SellRate);
            double coins = diamonds * rates.CoinValue;
            return NumericFormat.IsFinite(coins) ? Math.Max(0d, coins) : 0d;
        }

        public static double SustainedIncome(GameState state, EconomyConfig config, bool assumeTapping)
        {
            double total = 0d;
            bool boost = state.BoostRemainingSeconds > 0d;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                if (!mine.Unlocked)
                    continue;
                MineDefinition def = config.FindMine(mine.Id);
                if (def == null)
                    continue;
                MineRates rates = Compute(def, mine, config, state.Mastery, boost);
                bool tapping = assumeTapping && !rates.AutoUnlocked;
                total += SustainedCoinsPerSecond(rates, tapping);
            }
            return total;
        }

        static double Min3(double a, double b, double c)
        {
            return Math.Min(a, Math.Min(b, c));
        }
    }
}
