using System;
using System.Collections.Generic;

namespace DiamondMine.Simulation
{
    public sealed class DepositDelta
    {
        public int PlotId;
        public readonly Dictionary<string, double> Remaining = new Dictionary<string, double>();

        public DepositDelta Clone()
        {
            var copy = new DepositDelta { PlotId = PlotId };
            foreach (var pair in Remaining)
                copy.Remaining[pair.Key] = pair.Value;
            return copy;
        }
    }

    public sealed class MachineState
    {
        public string Id;
        public int Level;
        public string InputItem = "";
        public double InputAmount;
        public string OutputItem = "";
        public double OutputAmount;
        public string ExtraItem = "";
        public double ExtraAmount;
        public double BusySeconds;
        public int BusyUnits;
        public bool OutputGranted;

        public MachineState Clone()
        {
            return new MachineState
            {
                Id = Id,
                Level = Level,
                InputItem = InputItem,
                InputAmount = InputAmount,
                OutputItem = OutputItem,
                OutputAmount = OutputAmount,
                ExtraItem = ExtraItem,
                ExtraAmount = ExtraAmount,
                BusySeconds = BusySeconds,
                BusyUnits = BusyUnits,
                OutputGranted = OutputGranted
            };
        }
    }

    public sealed class CraftJob
    {
        public int Id;
        public string RecipeId;
        public double Remaining;
        public bool OutputGranted;

        public CraftJob Clone()
        {
            return new CraftJob { Id = Id, RecipeId = RecipeId, Remaining = Remaining, OutputGranted = OutputGranted };
        }
    }

    public sealed class AdventureState
    {
        public int WorldSeed = WorldGenerator.DefaultSeed;
        public int GeneratorVersion = WorldGenerator.CurrentVersion;
        public bool StarterGrantApplied;
        public bool LegacyMigrationApplied;
        public string MigrationNote = "";
        public string SelectedCharacter = "";
        public OwnershipBits Owned = new OwnershipBits();
        public readonly Dictionary<int, DepositDelta> Deposits = new Dictionary<int, DepositDelta>();
        public readonly Dictionary<string, double> Inventory = new Dictionary<string, double>();
        public readonly List<string> UnlockedCharacters = new List<string>();
        public readonly List<string> ClaimedTasks = new List<string>();
        public readonly List<int> VisitedStarters = new List<int>();
        public readonly List<int> DiscoveredRegions = new List<int>();
        public readonly List<MachineState> Machines = new List<MachineState>();
        public readonly List<CraftJob> Jobs = new List<CraftJob>();
        public readonly Dictionary<string, int> Crafted = new Dictionary<string, int>();
        public string HandsItem = "";
        public double HandsAmount;
        public int CarryCapacity = JourneyCatalog.CarryCapacity;
        public int WarehouseCapacity = JourneyCatalog.WarehouseCapacity;
        public double ActionClock;
        public double NextMineTime;
        public double NextRecoveryTime;
        public double PlayerX;
        public double PlayerZ;
        public int FocusPlotId = -1;
        public int NextJobId = 1;
        public double OreCollected;
        public double LifetimeDiamondsRecovered;
        public int WashBatches;
        public int CutCount;
        public int PolishCount;
        public int SilverRefined;
        public int GoldRefined;
        public int RingsCrafted;
        public int OrdersCompleted;
        public int OrderIndex;
        public bool WorkerHired;
        public int WorkerPlotId = -1;
        public double WorkerFraction;
        public bool WorkerSwitchesWhenBlocked;
        public double PoseCelebrate;
        public double PoseWave;
        public string PoseHint = "";

        public MachineState FindMachine(string id)
        {
            for (int i = 0; i < Machines.Count; i++)
            {
                if (Machines[i].Id == id)
                    return Machines[i];
            }
            return null;
        }

        public AdventureState Clone()
        {
            var copy = new AdventureState
            {
                WorldSeed = WorldSeed,
                GeneratorVersion = GeneratorVersion,
                StarterGrantApplied = StarterGrantApplied,
                LegacyMigrationApplied = LegacyMigrationApplied,
                MigrationNote = MigrationNote,
                SelectedCharacter = SelectedCharacter,
                Owned = Owned.Clone(),
                HandsItem = HandsItem,
                HandsAmount = HandsAmount,
                CarryCapacity = CarryCapacity,
                WarehouseCapacity = WarehouseCapacity,
                ActionClock = ActionClock,
                NextMineTime = NextMineTime,
                NextRecoveryTime = NextRecoveryTime,
                PlayerX = PlayerX,
                PlayerZ = PlayerZ,
                FocusPlotId = FocusPlotId,
                NextJobId = NextJobId,
                OreCollected = OreCollected,
                LifetimeDiamondsRecovered = LifetimeDiamondsRecovered,
                WashBatches = WashBatches,
                CutCount = CutCount,
                PolishCount = PolishCount,
                SilverRefined = SilverRefined,
                GoldRefined = GoldRefined,
                RingsCrafted = RingsCrafted,
                OrdersCompleted = OrdersCompleted,
                OrderIndex = OrderIndex,
                WorkerHired = WorkerHired,
                WorkerPlotId = WorkerPlotId,
                WorkerFraction = WorkerFraction,
                WorkerSwitchesWhenBlocked = WorkerSwitchesWhenBlocked,
                PoseCelebrate = PoseCelebrate,
                PoseWave = PoseWave,
                PoseHint = PoseHint
            };
            foreach (var pair in Deposits)
                copy.Deposits[pair.Key] = pair.Value.Clone();
            foreach (var pair in Inventory)
                copy.Inventory[pair.Key] = pair.Value;
            for (int i = 0; i < UnlockedCharacters.Count; i++)
                copy.UnlockedCharacters.Add(UnlockedCharacters[i]);
            for (int i = 0; i < ClaimedTasks.Count; i++)
                copy.ClaimedTasks.Add(ClaimedTasks[i]);
            for (int i = 0; i < VisitedStarters.Count; i++)
                copy.VisitedStarters.Add(VisitedStarters[i]);
            for (int i = 0; i < DiscoveredRegions.Count; i++)
                copy.DiscoveredRegions.Add(DiscoveredRegions[i]);
            for (int i = 0; i < Machines.Count; i++)
                copy.Machines.Add(Machines[i].Clone());
            for (int i = 0; i < Jobs.Count; i++)
                copy.Jobs.Add(Jobs[i].Clone());
            foreach (var pair in Crafted)
                copy.Crafted[pair.Key] = pair.Value;
            return copy;
        }
    }

    public struct YardPoint
    {
        public double X;
        public double Z;
    }

    public struct InteractSpot
    {
        public string Kind;
        public int PlotId;
        public string Label;
    }

    public static class YardLayout
    {
        public const double HomeX = 0d;
        public const double HomeZ = -0.2d;
        public const double FactoryX = 0d;
        public const double FactoryZ = 0.8d;
        public const double ShopX = 4.2d;
        public const double ShopZ = -1.2d;
        public const double GateX = -6.2d;
        public const double GateZ = -1.2d;
        public const double MinX = -8.5d;
        public const double MaxX = 8.5d;
        public const double MinZ = -4.5d;
        public const double MaxZ = 16.5d;
        public const double InteractRadius = 1.55d;
        public const double PlayerRadius = 0.35d;

        public static int RockCount { get { return Rocks.Length; } }

        public static YardPoint Rock(int index)
        {
            return Rocks[index];
        }

        static readonly YardPoint[] Rocks =
        {
            new YardPoint { X = -3.2d, Z = 3.4d },
            new YardPoint { X = 3.1d, Z = 5.2d },
            new YardPoint { X = -2.4d, Z = 9.6d }
        };

        public static YardPoint Entrance(int plotId)
        {
            int local = plotId;
            if (local < 0)
                local = 0;
            int col = local % 4;
            int row = local / 4;
            return new YardPoint { X = -4.5d + col * 3d, Z = 4.2d + row * 3.4d };
        }

        public static bool Blocked(double x, double z)
        {
            if (x < MinX || x > MaxX || z < MinZ || z > MaxZ)
                return true;
            for (int i = 0; i < Rocks.Length; i++)
            {
                double dx = x - Rocks[i].X;
                double dz = z - Rocks[i].Z;
                if (dx * dx + dz * dz < 0.85d * 0.85d)
                    return true;
            }
            return false;
        }

        public static void Move(AdventureState adventure, double dx, double dz)
        {
            Slide(adventure, dx, 0d);
            Slide(adventure, 0d, dz);
        }

        public static void ReturnHome(AdventureState adventure)
        {
            adventure.PlayerX = HomeX;
            adventure.PlayerZ = HomeZ;
            adventure.FocusPlotId = -1;
        }

        public static InteractSpot Nearest(AdventureState adventure)
        {
            var best = new InteractSpot { Kind = "none", PlotId = -1, Label = "" };
            double bestDistance = InteractRadius * InteractRadius;
            Consider(adventure, FactoryX, FactoryZ, "factory", -1, "Factory", ref best, ref bestDistance);
            Consider(adventure, ShopX, ShopZ, "shop", -1, "Shop", ref best, ref bestDistance);
            Consider(adventure, GateX, GateZ, "gate", -1, "Region gate", ref best, ref bestDistance);
            int shown = WorldGenerator.VisibleYardPlots;
            for (int plot = 0; plot < shown; plot++)
            {
                YardPoint point = Entrance(plot);
                string label = adventure.Owned.Get(plot) ? "Bunker " + (plot + 1) : "Locked bunker " + (plot + 1);
                Consider(adventure, point.X, point.Z, "bunker", plot, label, ref best, ref bestDistance);
            }
            return best;
        }

        static void Consider(AdventureState adventure, double x, double z, string kind, int plotId, string label, ref InteractSpot best, ref double bestDistance)
        {
            double dx = adventure.PlayerX - x;
            double dz = adventure.PlayerZ - z;
            double distance = dx * dx + dz * dz;
            if (distance > bestDistance)
                return;
            bestDistance = distance;
            best.Kind = kind;
            best.PlotId = plotId;
            best.Label = label;
        }

        static void Slide(AdventureState adventure, double dx, double dz)
        {
            double x = adventure.PlayerX + dx;
            double z = adventure.PlayerZ + dz;
            if (!Blocked(x, z))
            {
                adventure.PlayerX = x;
                adventure.PlayerZ = z;
            }
        }
    }
}
