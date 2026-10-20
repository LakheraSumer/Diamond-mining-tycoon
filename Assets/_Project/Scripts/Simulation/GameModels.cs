using System;
using System.Collections.Generic;

namespace DiamondMine.Simulation
{
    public static class Stages
    {
        public const string Mining = "mining";
        public const string Transport = "transport";
        public const string Processing = "processing";
        public const string Sorting = "sorting";
        public const string Storage = "storage";
        public const string Sales = "sales";
        public const string Automation = "automation";
    }

    public static class CartPhase
    {
        public const int Loading = 0;
        public const int Outbound = 1;
        public const int Unloading = 2;
        public const int Returning = 3;
    }

    public sealed class UpgradeDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string Stage;
        public int MaxLevel = 1;
        public double BaseCost = 1;
        public double CostGrowth = 1.15;
        public string Stat;
        public double Amount;
        public string Requires;
    }

    public sealed class MineDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string Palette;
        public double UnlockLifetimeCoins;
        public double TapOre = 1;
        public double TapCooldown = 0.4;
        public double MineBuffer = 20;
        public double ProcessRate = 1;
        public double Yield = 0.2;
        public double ProcessBuffer = 20;
        public double RoughBuffer = 10;
        public double SortRate = 1;
        public double StorageCap = 20;
        public double SellRate = 1;
        public double CoinValue = 1;
        public int CartCount = 1;
        public double CartCapacity = 5;
        public double TripSeconds = 4;
        public double DepartWait = 2;
        public double BaseAutoRate;
        public readonly List<UpgradeDefinition> Upgrades = new List<UpgradeDefinition>();

        public UpgradeDefinition FindUpgrade(string id)
        {
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == id)
                    return Upgrades[i];
            }
            return null;
        }
    }

    public sealed class OrderDefinition
    {
        public string Id;
        public string Title;
        public string MineId;
        public double Diamonds;
        public double Bonus;
    }

    public sealed class ObjectiveDefinition
    {
        public string Id;
        public string Title;
        public string Detail;
        public string Type;
        public string MineId;
        public string UpgradeId;
        public double Target = 1;
        public double Reward;
    }

    public sealed class EconomyConfig
    {
        public int Version = 1;
        public double OfflineCapSeconds = 8d * 3600d;
        public double BackwardClockGraceSeconds = 120d;
        public double ImplausibleForwardSeconds = 365d * 86400d;
        public double MasteryBonusPerPoint = 0.02;
        public double PrestigeDivisor = 20000d;
        public double PrestigeMinimumCoins = 80000d;
        public bool PrestigeRequiresAutomation = true;
        public double BoostMultiplier = 2d;
        public double BoostDurationSeconds = 300d;
        public double OfflineAdBonusFraction = 0.5;
        public int MaxAdTokenHistory = 24;
        public readonly List<MineDefinition> Mines = new List<MineDefinition>();
        public readonly List<ObjectiveDefinition> Objectives = new List<ObjectiveDefinition>();
        public readonly List<OrderDefinition> Orders = new List<OrderDefinition>();

        public MineDefinition FindMine(string id)
        {
            for (int i = 0; i < Mines.Count; i++)
            {
                if (Mines[i].Id == id)
                    return Mines[i];
            }
            return null;
        }

        public ObjectiveDefinition FindObjective(string id)
        {
            for (int i = 0; i < Objectives.Count; i++)
            {
                if (Objectives[i].Id == id)
                    return Objectives[i];
            }
            return null;
        }
    }

    public sealed class CartState
    {
        public int Phase;
        public double Timer;
        public double Load;

        public CartState Clone()
        {
            return new CartState { Phase = Phase, Timer = Timer, Load = Load };
        }
    }

    public sealed class SettingsState
    {
        public double Music = 0.7;
        public double Sfx = 0.85;
        public bool Haptics = true;
        public bool ReducedMotion;
        public int Quality = 1;

        public SettingsState Clone()
        {
            return new SettingsState
            {
                Music = Music,
                Sfx = Sfx,
                Haptics = Haptics,
                ReducedMotion = ReducedMotion,
                Quality = Quality
            };
        }
    }

    public sealed class MineState
    {
        public string Id;
        public bool Unlocked;
        public double OreAtFace;
        public double ProcessInput;
        public double Rough;
        public double Storage;
        public double OreMined;
        public double OreProcessed;
        public double DiamondsProduced;
        public double DiamondsSold;
        public int TapCount;
        public int TripsStarted;
        public double LastTapTime = -999d;
        public readonly Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();
        public readonly List<CartState> Carts = new List<CartState>();

        public int Level(string upgradeId)
        {
            int level;
            return UpgradeLevels.TryGetValue(upgradeId, out level) ? level : 0;
        }

        public MineState Clone()
        {
            var copy = new MineState
            {
                Id = Id,
                Unlocked = Unlocked,
                OreAtFace = OreAtFace,
                ProcessInput = ProcessInput,
                Rough = Rough,
                Storage = Storage,
                OreMined = OreMined,
                OreProcessed = OreProcessed,
                DiamondsProduced = DiamondsProduced,
                DiamondsSold = DiamondsSold,
                TapCount = TapCount,
                TripsStarted = TripsStarted,
                LastTapTime = LastTapTime
            };
            foreach (var pair in UpgradeLevels)
                copy.UpgradeLevels[pair.Key] = pair.Value;
            for (int i = 0; i < Carts.Count; i++)
                copy.Carts.Add(Carts[i].Clone());
            return copy;
        }
    }

    public sealed class GameState
    {
        public int Version = SaveCodec.CurrentVersion;
        public double Coins;
        public double Mastery;
        public double LifetimeCoins;
        public double CoinsSincePrestige;
        public double CoinsEarned;
        public double CoinsSpent;
        public double CoinsDiscarded;
        public int PrestigeCount;
        public string ActiveMineId = "warmstone";
        public double BoostRemainingSeconds;
        public double TotalPlaySeconds;
        public long LastActiveUnixMs;
        public int TutorialStep;
        public bool TutorialComplete;
        public bool BottleneckSeen;
        public string MarkedStage = "";
        public int MarkedStageLevels = -1;
        public bool ObjectivePresented;
        public string LastOfflineGrantId = "";
        public double LastOfflineBaseCoins;
        public bool LastOfflineBonusClaimed;
        public readonly List<string> UsedAdTokens = new List<string>();
        public readonly List<string> ClaimedObjectives = new List<string>();
        public readonly List<string> ClaimedOrders = new List<string>();
        public SettingsState Settings = new SettingsState();
        public readonly List<MineState> Mines = new List<MineState>();

        public MineState FindMine(string id)
        {
            for (int i = 0; i < Mines.Count; i++)
            {
                if (Mines[i].Id == id)
                    return Mines[i];
            }
            return null;
        }

        public GameState Clone()
        {
            var copy = new GameState
            {
                Version = Version,
                Coins = Coins,
                Mastery = Mastery,
                LifetimeCoins = LifetimeCoins,
                CoinsSincePrestige = CoinsSincePrestige,
                CoinsEarned = CoinsEarned,
                CoinsSpent = CoinsSpent,
                CoinsDiscarded = CoinsDiscarded,
                PrestigeCount = PrestigeCount,
                ActiveMineId = ActiveMineId,
                BoostRemainingSeconds = BoostRemainingSeconds,
                TotalPlaySeconds = TotalPlaySeconds,
                LastActiveUnixMs = LastActiveUnixMs,
                TutorialStep = TutorialStep,
                TutorialComplete = TutorialComplete,
                BottleneckSeen = BottleneckSeen,
                MarkedStage = MarkedStage,
                MarkedStageLevels = MarkedStageLevels,
                ObjectivePresented = ObjectivePresented,
                LastOfflineGrantId = LastOfflineGrantId,
                LastOfflineBaseCoins = LastOfflineBaseCoins,
                LastOfflineBonusClaimed = LastOfflineBonusClaimed,
                Settings = Settings.Clone()
            };
            for (int i = 0; i < UsedAdTokens.Count; i++)
                copy.UsedAdTokens.Add(UsedAdTokens[i]);
            for (int i = 0; i < ClaimedObjectives.Count; i++)
                copy.ClaimedObjectives.Add(ClaimedObjectives[i]);
            copy.ClaimedOrders.AddRange(ClaimedOrders);
            for (int i = 0; i < Mines.Count; i++)
                copy.Mines.Add(Mines[i].Clone());
            return copy;
        }

        public void CopyFrom(GameState other)
        {
            Version = other.Version;
            Coins = other.Coins;
            Mastery = other.Mastery;
            LifetimeCoins = other.LifetimeCoins;
            CoinsSincePrestige = other.CoinsSincePrestige;
            CoinsEarned = other.CoinsEarned;
            CoinsSpent = other.CoinsSpent;
            CoinsDiscarded = other.CoinsDiscarded;
            PrestigeCount = other.PrestigeCount;
            ActiveMineId = other.ActiveMineId;
            BoostRemainingSeconds = other.BoostRemainingSeconds;
            TotalPlaySeconds = other.TotalPlaySeconds;
            LastActiveUnixMs = other.LastActiveUnixMs;
            TutorialStep = other.TutorialStep;
            TutorialComplete = other.TutorialComplete;
            BottleneckSeen = other.BottleneckSeen;
            MarkedStage = other.MarkedStage;
            MarkedStageLevels = other.MarkedStageLevels;
            ObjectivePresented = other.ObjectivePresented;
            LastOfflineGrantId = other.LastOfflineGrantId;
            LastOfflineBaseCoins = other.LastOfflineBaseCoins;
            LastOfflineBonusClaimed = other.LastOfflineBonusClaimed;
            Settings = other.Settings;
            UsedAdTokens.Clear();
            for (int i = 0; i < other.UsedAdTokens.Count; i++)
                UsedAdTokens.Add(other.UsedAdTokens[i]);
            ClaimedObjectives.Clear();
            ClaimedOrders.Clear();
            ClaimedOrders.AddRange(other.ClaimedOrders);
            for (int i = 0; i < other.ClaimedObjectives.Count; i++)
                ClaimedObjectives.Add(other.ClaimedObjectives[i]);
            Mines.Clear();
            for (int i = 0; i < other.Mines.Count; i++)
                Mines.Add(other.Mines[i]);
        }
    }

    public static class Wallet
    {
        public static void Credit(GameState state, double amount)
        {
            if (!NumericFormat.IsFinite(amount) || amount <= 0d)
                return;
            double before = state.Coins;
            state.Coins = NumericFormat.Add(state.Coins, amount);
            double added = state.Coins - before;
            if (added <= 0d)
                return;
            state.LifetimeCoins = NumericFormat.Add(state.LifetimeCoins, added);
            state.CoinsSincePrestige = NumericFormat.Add(state.CoinsSincePrestige, added);
            state.CoinsEarned = NumericFormat.Add(state.CoinsEarned, added);
        }

        public static bool CanSpend(GameState state, double cost)
        {
            return NumericFormat.IsFinite(cost) && cost >= 0d && cost <= state.Coins;
        }

        public static bool TrySpend(GameState state, double cost)
        {
            if (!CanSpend(state, cost))
                return false;
            state.Coins = NumericFormat.Clamp(state.Coins - cost);
            state.CoinsSpent = NumericFormat.Add(state.CoinsSpent, cost);
            return true;
        }
    }
}
