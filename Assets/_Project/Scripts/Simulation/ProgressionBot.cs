using System;

namespace DiamondMine.Simulation
{
    public sealed class BotReport
    {
        public double FirstSale = -1;
        public double FirstUpgrade = -1;
        public double Automation = -1;
        public double SecondMine = -1;
        public double ThirdMine = -1;
        public double PrestigeAvailable = -1;
        public double PrestigeDone = -1;
        public double FinalCoins;
        public double FinalLifetime;
        public double FinalMastery;
        public double FinalIncome;
        public int FinalPrestige;
        public double WorstMinuteAfterAutomation = -1;
        public bool AnyNan;
    }

    /// <summary>
    /// A guided player used to measure pacing. It taps until a miner is hired, follows the tutorial, then buys the best payback upgrade.
    /// </summary>
    public static class ProgressionBot
    {
        public static BotReport Play(EconomyConfig config, double seconds)
        {
            GameState state = GameFactory.NewGame(config);
            var report = new BotReport();
            const double dt = 0.25;
            double t = 0d;
            int step = 0;
            bool didPrestige = false;
            double nextMinute = -1;
            double minuteLifetime = 0d;

            while (t < seconds)
            {
                if (!Finite(state))
                {
                    report.AnyNan = true;
                    break;
                }

                TapIdleMines(state, config);
                if (step % 4 == 0)
                    Decide(state, config, ref didPrestige);

                if (state.TutorialStep == 4)
                    TutorialService.NotifyBottleneckShown(state);
                if (state.TutorialStep >= 7)
                    TutorialService.NotifyObjectiveShown(state);
                TutorialService.Reconcile(state, config);

                ProductionSimulator.Advance(state, config, dt);
                ObjectiveService.ClaimCompleted(state, config);
                t += dt;
                step++;
                Note(state, config, t, report);

                if (report.Automation >= 0d)
                {
                    if (nextMinute < 0d)
                    {
                        nextMinute = t + 60d;
                        minuteLifetime = state.LifetimeCoins;
                    }
                    else if (t >= nextMinute)
                    {
                        double gain = state.LifetimeCoins - minuteLifetime;
                        if (report.WorstMinuteAfterAutomation < 0d || gain < report.WorstMinuteAfterAutomation)
                            report.WorstMinuteAfterAutomation = gain;
                        minuteLifetime = state.LifetimeCoins;
                        nextMinute += 60d;
                    }
                }
            }

            report.FinalCoins = state.Coins;
            report.FinalLifetime = state.LifetimeCoins;
            report.FinalMastery = state.Mastery;
            report.FinalIncome = MineStats.SustainedIncome(state, config, false);
            report.FinalPrestige = state.PrestigeCount;
            report.AnyNan = report.AnyNan || !Finite(state);
            return report;
        }

        static void TapIdleMines(GameState state, EconomyConfig config)
        {
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                if (!mine.Unlocked)
                    continue;
                MineDefinition def = config.FindMine(mine.Id);
                MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, state.BoostRemainingSeconds > 0d);
                if (rates.AutoUnlocked)
                    continue;
                if (mine.OreAtFace < rates.MineBuffer * 0.55d)
                    ProductionSimulator.TryTap(state, config, mine.Id);
            }
        }

        static void Decide(GameState state, EconomyConfig config, ref bool didPrestige)
        {
            MineDefinition starterDef = config.Mines[0];
            MineState starter = state.FindMine(starterDef.Id);
            MineRates rates = MineStats.Compute(starterDef, starter, config, state.Mastery, false);

            if (!rates.AutoUnlocked && state.TutorialStep <= 6)
            {
                if (state.TutorialStep >= 6)
                    TryHire(state, config, starterDef);
                else if (state.TutorialStep == 5)
                    BuyInStage(state, config, starter, starterDef, BottleneckService.Diagnose(starter, rates).Stage);
                else if (state.TutorialStep == 3 && UpgradeService.TotalLevels(state) < 1)
                    BuyCheapest(state, config, starter, starterDef);
            }
            else
            {
                UpgradeService.TryBuyBestPayback(state, config);
                for (int i = 0; i < config.Mines.Count; i++)
                    MineUnlocks.TryUnlock(state, config, config.Mines[i].Id);
                if (!didPrestige && config.Mines.Count > 1 && state.FindMine(config.Mines[1].Id).Unlocked)
                {
                    if (PrestigeService.Preview(state, config).Available && PrestigeService.TryPrestige(state, config))
                        didPrestige = true;
                }
            }
        }

        static void TryHire(GameState state, EconomyConfig config, MineDefinition def)
        {
            for (int i = 0; i < def.Upgrades.Count; i++)
            {
                if (def.Upgrades[i].Stat == "autoUnlock")
                    UpgradeService.TryBuy(state, config, def.Id, def.Upgrades[i].Id, 1);
            }
        }

        static void BuyInStage(GameState state, EconomyConfig config, MineState mine, MineDefinition def, string stage)
        {
            UpgradeDefinition best = null;
            double bestCost = double.MaxValue;
            for (int i = 0; i < def.Upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = def.Upgrades[i];
                if (upgrade.Stage != stage || mine.Level(upgrade.Id) >= upgrade.MaxLevel)
                    continue;
                if (!MineStats.RequirementMet(def, mine, upgrade))
                    continue;
                PurchasePreview preview = UpgradeService.Preview(state, config, mine.Id, upgrade.Id, 1);
                if (preview.Affordable && preview.Cost < bestCost)
                {
                    bestCost = preview.Cost;
                    best = upgrade;
                }
            }
            if (best != null)
                UpgradeService.TryBuy(state, config, mine.Id, best.Id, 1);
            else
                BuyCheapest(state, config, mine, def);
        }

        static void BuyCheapest(GameState state, EconomyConfig config, MineState mine, MineDefinition def)
        {
            UpgradeDefinition best = null;
            double bestCost = double.MaxValue;
            for (int i = 0; i < def.Upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = def.Upgrades[i];
                if (upgrade.Stat == "autoUnlock" || mine.Level(upgrade.Id) >= upgrade.MaxLevel)
                    continue;
                if (!MineStats.RequirementMet(def, mine, upgrade))
                    continue;
                PurchasePreview preview = UpgradeService.Preview(state, config, mine.Id, upgrade.Id, 1);
                if (preview.Affordable && preview.Cost < bestCost)
                {
                    bestCost = preview.Cost;
                    best = upgrade;
                }
            }
            if (best != null)
                UpgradeService.TryBuy(state, config, mine.Id, best.Id, 1);
        }

        static void Note(GameState state, EconomyConfig config, double t, BotReport report)
        {
            if (report.FirstSale < 0d && (state.LifetimeCoins > 0d || state.Mines[0].DiamondsSold > 0d))
                report.FirstSale = t;
            if (report.FirstUpgrade < 0d && UpgradeService.TotalLevels(state) > 0)
                report.FirstUpgrade = t;
            if (report.Automation < 0d && MineStats.Compute(config.Mines[0], state.Mines[0], config, 0d, false).AutoUnlocked)
                report.Automation = t;
            if (report.SecondMine < 0d && state.Mines.Count > 1 && state.Mines[1].Unlocked)
                report.SecondMine = t;
            if (report.ThirdMine < 0d && state.Mines.Count > 2 && state.Mines[2].Unlocked)
                report.ThirdMine = t;
            if (report.PrestigeAvailable < 0d && PrestigeService.Preview(state, config).Available)
                report.PrestigeAvailable = t;
            if (report.PrestigeDone < 0d && state.PrestigeCount > 0)
                report.PrestigeDone = t;
        }

        static bool Finite(GameState state)
        {
            if (!NumericFormat.IsFinite(state.Coins) || state.Coins < 0d)
                return false;
            if (!NumericFormat.IsFinite(state.Mastery) || !NumericFormat.IsFinite(state.LifetimeCoins))
                return false;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                if (!NumericFormat.IsFinite(mine.OreAtFace) || mine.OreAtFace < -1e-6)
                    return false;
                if (!NumericFormat.IsFinite(mine.Rough) || !NumericFormat.IsFinite(mine.Storage) || !NumericFormat.IsFinite(mine.DiamondsSold))
                    return false;
            }
            return true;
        }
    }
}
