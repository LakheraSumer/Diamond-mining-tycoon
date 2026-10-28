using System;
using System.Collections.Generic;
using System.Text;

namespace DiamondMine.Simulation
{
    public sealed class ReserveAmount
    {
        public string ItemId;
        public int Amount;
    }

    public sealed class PlotProfile
    {
        public int PlotId;
        public int RegionId;
        public int LocalIndex;
        public int GeneratorVersion;
        public string Theme;
        public string Richness;
        public bool Starter;
        public readonly List<ReserveAmount> Reserves = new List<ReserveAmount>();

        public int AmountOf(string itemId)
        {
            for (int i = 0; i < Reserves.Count; i++)
            {
                if (Reserves[i].ItemId == itemId)
                    return Reserves[i].Amount;
            }
            return 0;
        }
    }

    /// <summary>
    /// Deterministic mine addresses. Rendering and simulation must not allocate one object per plot.
    /// </summary>
    public static class WorldGenerator
    {
        public const int CurrentVersion = 1;
        public const int RegionCount = 100;
        public const int PlotsPerRegion = 1024;
        public const int PlotCount = RegionCount * PlotsPerRegion;
        public const int DefaultSeed = 841273;
        public const int StarterCount = 5;
        public const int WarmstonePlot = 0;
        public const int CrystalPlot = 2;
        public const int DeepCorePlot = 5;
        public const int VisibleYardPlots = 12;

        static readonly string[] Themes =
        {
            "Sunridge", "Moss bank", "Sand shelf", "River blue",
            "Plum rock", "Copper ridge", "Mint shelf", "Amber lamp"
        };

        static readonly string[] RichnessWords = { "Poor", "Fair", "Rich", "Bright" };

        public static bool IsValid(int plotId)
        {
            return plotId >= 0 && plotId < PlotCount;
        }

        public static bool IsStarter(int plotId)
        {
            return plotId >= 0 && plotId < StarterCount;
        }

        public static int RegionOf(int plotId)
        {
            return plotId / PlotsPerRegion;
        }

        public static int LocalIndex(int plotId)
        {
            return plotId % PlotsPerRegion;
        }

        public static string ThemeName(int regionId)
        {
            int index = regionId % Themes.Length;
            if (index < 0)
                index = 0;
            return Themes[index];
        }

        public static PlotProfile Describe(int worldSeed, int plotId, int generatorVersion)
        {
            if (!IsValid(plotId))
                throw new ArgumentOutOfRangeException("plotId");
            if (generatorVersion < 1)
                generatorVersion = 1;

            var profile = new PlotProfile
            {
                PlotId = plotId,
                RegionId = RegionOf(plotId),
                LocalIndex = LocalIndex(plotId),
                GeneratorVersion = generatorVersion,
                Theme = ThemeName(RegionOf(plotId)),
                Starter = IsStarter(plotId)
            };

            if (profile.Starter)
            {
                AddStarter(profile);
                profile.Richness = "Starter";
                return profile;
            }

            uint hash = Mix(worldSeed, plotId, generatorVersion);
            int richness = (int)((hash >> 8) & 3u);
            profile.Richness = RichnessWords[richness];
            AddGenerated(profile, hash, richness);
            return profile;
        }

        public static string ReserveText(PlotProfile profile)
        {
            var text = new StringBuilder();
            for (int i = 0; i < profile.Reserves.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                text.Append(JourneyCatalog.DisplayName(profile.Reserves[i].ItemId));
                text.Append(' ');
                text.Append(profile.Reserves[i].Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        public static void Page(int worldSeed, int regionId, int start, int count, int generatorVersion, List<PlotProfile> into)
        {
            into.Clear();
            if (regionId < 0 || regionId >= RegionCount || count <= 0)
                return;
            if (start < 0)
                start = 0;
            if (start > PlotsPerRegion)
                start = PlotsPerRegion;
            int last = start + count;
            if (last > PlotsPerRegion)
                last = PlotsPerRegion;
            int origin = regionId * PlotsPerRegion;
            for (int i = start; i < last; i++)
                into.Add(Describe(worldSeed, origin + i, generatorVersion));
        }

        static void AddStarter(PlotProfile profile)
        {
            switch (profile.PlotId)
            {
                case 0:
                    Add(profile, JourneyCatalog.Stone, 80);
                    Add(profile, JourneyCatalog.IronOre, 40);
                    Add(profile, JourneyCatalog.Gravel, 40);
                    break;
                case 1:
                    Add(profile, JourneyCatalog.CopperOre, 36);
                    Add(profile, JourneyCatalog.Stone, 24);
                    Add(profile, JourneyCatalog.Gravel, 16);
                    break;
                case 2:
                    Add(profile, JourneyCatalog.DiamondOre, 24);
                    Add(profile, JourneyCatalog.Gravel, 20);
                    Add(profile, JourneyCatalog.Stone, 12);
                    break;
                case 3:
                    Add(profile, JourneyCatalog.SilverOre, 20);
                    Add(profile, JourneyCatalog.Stone, 20);
                    Add(profile, JourneyCatalog.Gravel, 16);
                    break;
                default:
                    Add(profile, JourneyCatalog.GoldOre, 10);
                    Add(profile, JourneyCatalog.SilverOre, 10);
                    Add(profile, JourneyCatalog.DiamondOre, 12);
                    Add(profile, JourneyCatalog.CopperOre, 12);
                    break;
            }
        }

        static void AddGenerated(PlotProfile profile, uint hash, int richness)
        {
            int stone = 20 + (int)(hash & 31u);
            Add(profile, JourneyCatalog.Stone, stone);
            Add(profile, JourneyCatalog.Gravel, 8 + (int)((hash >> 4) & 15u));
            if (richness >= 0)
                Add(profile, ((hash >> 12) & 1u) == 0u ? JourneyCatalog.IronOre : JourneyCatalog.CopperOre, 6 + (int)((hash >> 16) & 15u));
            if (richness >= 2)
                Add(profile, ((hash >> 18) & 1u) == 0u ? JourneyCatalog.SilverOre : JourneyCatalog.DiamondOre, 4 + (int)((hash >> 20) & 7u));
            if (richness >= 3)
            {
                Add(profile, JourneyCatalog.GoldOre, 2 + (int)((hash >> 24) & 3u));
                if (profile.AmountOf(JourneyCatalog.DiamondOre) == 0)
                    Add(profile, JourneyCatalog.DiamondOre, 3 + (int)((hash >> 26) & 3u));
            }
        }

        static void Add(PlotProfile profile, string itemId, int amount)
        {
            if (amount <= 0)
                return;
            profile.Reserves.Add(new ReserveAmount { ItemId = itemId, Amount = amount });
        }

        public static uint Mix(int worldSeed, int plotId, int generatorVersion)
        {
            unchecked
            {
                uint x = (uint)worldSeed;
                x ^= (uint)plotId * 0x9E3779B9u;
                x ^= (uint)generatorVersion * 0x85EBCA6Bu;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return x;
            }
        }
    }

    public sealed class OwnershipBits
    {
        public readonly byte[] Bytes;

        public OwnershipBits()
        {
            Bytes = new byte[WorldGenerator.PlotCount / 8];
        }

        OwnershipBits(byte[] bytes)
        {
            Bytes = bytes;
        }

        public bool Get(int plotId)
        {
            if (!WorldGenerator.IsValid(plotId))
                return false;
            return (Bytes[plotId >> 3] & (1 << (plotId & 7))) != 0;
        }

        public void Set(int plotId, bool owned)
        {
            if (!WorldGenerator.IsValid(plotId))
                throw new ArgumentOutOfRangeException("plotId");
            int index = plotId >> 3;
            int mask = 1 << (plotId & 7);
            if (owned)
                Bytes[index] = (byte)(Bytes[index] | mask);
            else
                Bytes[index] = (byte)(Bytes[index] & ~mask);
        }

        public int Count()
        {
            int total = 0;
            for (int i = 0; i < WorldGenerator.PlotCount; i++)
            {
                if (Get(i))
                    total++;
            }
            return total;
        }

        public void Collect(int startId, int max, List<int> into)
        {
            into.Clear();
            if (max <= 0)
                return;
            if (startId < 0)
                startId = 0;
            for (int id = startId; id < WorldGenerator.PlotCount && into.Count < max; id++)
            {
                if (Get(id))
                    into.Add(id);
            }
        }

        public OwnershipBits Clone()
        {
            var copy = new byte[Bytes.Length];
            Buffer.BlockCopy(Bytes, 0, copy, 0, Bytes.Length);
            return new OwnershipBits(copy);
        }

        public string ToHex()
        {
            int last = Bytes.Length - 1;
            while (last > 0 && Bytes[last] == 0)
                last--;
            var text = new StringBuilder((last + 1) * 2);
            for (int i = 0; i <= last; i++)
                text.Append(Bytes[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return text.ToString();
        }

        public static OwnershipBits FromHex(string hex)
        {
            if (hex == null)
                hex = "";
            if ((hex.Length & 1) != 0)
                throw new InvalidSaveException("Ownership record is damaged.");
            int byteCount = hex.Length / 2;
            if (byteCount > WorldGenerator.PlotCount / 8)
                throw new InvalidSaveException("Ownership record is too large.");
            var bytes = new byte[WorldGenerator.PlotCount / 8];
            for (int i = 0; i < byteCount; i++)
            {
                int value = ParseNibble(hex[i * 2]) << 4 | ParseNibble(hex[i * 2 + 1]);
                bytes[i] = (byte)value;
            }
            return new OwnershipBits(bytes);
        }

        static int ParseNibble(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            throw new InvalidSaveException("Ownership record is damaged.");
        }
    }
}
