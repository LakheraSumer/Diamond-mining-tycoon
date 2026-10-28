using System.Collections.Generic;

namespace DiamondMine.Simulation
{
    public sealed class Ingredient
    {
        public string ItemId;
        public int Count;
        public int MinGrade;
    }

    public sealed class JewelRecipe
    {
        public string Id;
        public string Name;
        public string OutputItem;
        public int OutputCount = 1;
        public double Seconds = 2d;
        public string Unlock = "start";
        public string Note;
        public readonly List<Ingredient> Inputs = new List<Ingredient>();
    }

    public sealed class CustomerOrderDef
    {
        public string Id;
        public string Title;
        public string ItemId;
        public int Amount;
        public int Pay;
    }

    public sealed class YardTaskDef
    {
        public string Id;
        public string Title;
        public string Detail;
        public int Reward;
    }

    public sealed class CharacterDef
    {
        public string Id;
        public string Name;
        public string Role;
        public string Summary;
        public string UnlockText;
        public bool Free;
        public int Helmet;
        public int Cloth;
        public int Accent;
        public int Skin;
        public int Hair;
        public string Accessory;
    }

    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Route;
        public int SellValue;
        public bool Ore;
    }

    public static class JourneyCatalog
    {
        public const string Stone = "stone";
        public const string Gravel = "gravel";
        public const string IronOre = "iron_ore";
        public const string CopperOre = "copper_ore";
        public const string SilverOre = "silver_ore";
        public const string GoldOre = "gold_ore";
        public const string DiamondOre = "diamond_ore";
        public const string CrushedOre = "crushed_ore";
        public const string WashedOre = "washed_ore";
        public const string RoughDiamond = "rough_diamond";
        public const string CutDiamond = "diamond_cut";
        public const string DiamondG1 = "diamond_g1";
        public const string DiamondG2 = "diamond_g2";
        public const string DiamondG3 = "diamond_g3";
        public const string DiamondG4 = "diamond_g4";
        public const string RefinedIron = "refined_iron";
        public const string RefinedCopper = "refined_copper";
        public const string RefinedSilver = "refined_silver";
        public const string RefinedGold = "refined_gold";
        public const string Ring = "ring";
        public const string Bracelet = "bracelet";
        public const string Necklace = "necklace";
        public const string Pendant = "pendant";
        public const string Earrings = "earrings";
        public const string Bangle = "bangle";
        public const string Brooch = "brooch";
        public const string Anklet = "anklet";
        public const string Tiara = "tiara";
        public const string JewelSet = "jewellery_set";
        public const string AnyDiamond = "any_diamond";

        public const double MineCooldownSeconds = 0.35d;
        public const int MineYield = 2;
        public const int CarryCapacity = 12;
        public const int WarehouseCapacity = 400;
        public const int RecoveryStone = 8;
        public const double RecoveryCooldownSeconds = 15d;
        public const double CartTripSeconds = 3d;
        public const int HireCost = 200;
        public const double WorkerRate = 0.4d;
        public const int PerfectGrade = 5;

        static readonly ItemDef[] Items = BuildItems();
        static readonly JewelRecipe[] Recipes = BuildRecipes();
        static readonly CharacterDef[] Characters = BuildCharacters();
        static readonly YardTaskDef[] Tasks = BuildTasks();
        static readonly CustomerOrderDef[] Orders = BuildOrders();

        public static IList<ItemDef> AllItems { get { return Items; } }
        public static IList<JewelRecipe> AllRecipes { get { return Recipes; } }
        public static IList<CharacterDef> AllCharacters { get { return Characters; } }
        public static IList<YardTaskDef> AllTasks { get { return Tasks; } }
        public static IList<CustomerOrderDef> AllOrders { get { return Orders; } }

        public static ItemDef FindItem(string id)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Id == id)
                    return Items[i];
            }
            return null;
        }

        public static string DisplayName(string id)
        {
            ItemDef item = FindItem(id);
            return item == null ? id : item.Name;
        }

        public static string Mark(string id)
        {
            if (id == RoughDiamond || id == CutDiamond || GradeOf(id) > 0 || id == DiamondOre)
                return "Diamond";
            if (id == Ring || id == Bracelet || id == Necklace || id == Pendant || id == Earrings || id == Bangle || id == Brooch || id == Anklet || id == Tiara || id == JewelSet)
                return "Jewel";
            ItemDef item = FindItem(id);
            if (item != null && item.Ore)
                return "Ore";
            return "Goods";
        }

        public static int SellValue(string id)
        {
            ItemDef item = FindItem(id);
            return item == null ? 0 : item.SellValue;
        }

        public static JewelRecipe FindRecipe(string id)
        {
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (Recipes[i].Id == id)
                    return Recipes[i];
            }
            return null;
        }

        public static CharacterDef FindCharacter(string id)
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Id == id)
                    return Characters[i];
            }
            return null;
        }

        public static YardTaskDef FindTask(string id)
        {
            for (int i = 0; i < Tasks.Length; i++)
            {
                if (Tasks[i].Id == id)
                    return Tasks[i];
            }
            return null;
        }

        public static string GradeItem(int grade)
        {
            switch (grade)
            {
                case 2: return DiamondG2;
                case 3: return DiamondG3;
                case 4: return DiamondG4;
                default: return DiamondG1;
            }
        }

        public static int GradeOf(string itemId)
        {
            if (itemId == DiamondG2) return 2;
            if (itemId == DiamondG3) return 3;
            if (itemId == DiamondG4) return 4;
            if (itemId == DiamondG1) return 1;
            return 0;
        }

        static ItemDef Item(string id, string name, string route, int value, bool ore)
        {
            return new ItemDef { Id = id, Name = name, Route = route, SellValue = value, Ore = ore };
        }

        static ItemDef[] BuildItems()
        {
            return new[]
            {
                Item(Stone, "Stone", "Sell to the shop", 1, false),
                Item(Gravel, "Gravel", "Sell to the shop", 1, false),
                Item(IronOre, "Iron ore", "Refine or sell", 3, true),
                Item(CopperOre, "Copper ore", "Refine or sell", 4, true),
                Item(SilverOre, "Silver ore", "Refine or sell", 8, true),
                Item(GoldOre, "Gold ore", "Refine or sell", 14, true),
                Item(DiamondOre, "Diamond rock", "Crush, or sell", 6, true),
                Item(CrushedOre, "Crushed rock", "Wash, or sell", 4, false),
                Item(WashedOre, "Washed grit", "Sort, or sell", 3, false),
                Item(RoughDiamond, "Rough diamond", "Cut, or sell", 36, false),
                Item(CutDiamond, "Cut diamond", "Polish, or sell", 52, false),
                Item(DiamondG1, "Bright diamond", "Craft or sell", 70, false),
                Item(DiamondG2, "Clear diamond", "Craft or sell", 90, false),
                Item(DiamondG3, "Fine diamond", "Craft or sell", 115, false),
                Item(DiamondG4, "Superb diamond", "Craft or sell", 140, false),
                Item(RefinedIron, "Iron bar", "Craft or sell", 8, false),
                Item(RefinedCopper, "Copper bar", "Craft or sell", 12, false),
                Item(RefinedSilver, "Silver bar", "Craft or sell", 28, false),
                Item(RefinedGold, "Gold bar", "Craft or sell", 40, false),
                Item(Ring, "Diamond ring", "Sell or deliver", 120, false),
                Item(Bracelet, "Bracelet", "Sell or deliver", 180, false),
                Item(Necklace, "Necklace", "Sell or deliver", 260, false),
                Item(Pendant, "Pendant", "Sell or deliver", 160, false),
                Item(Earrings, "Earrings (pair)", "Sell or deliver", 220, false),
                Item(Bangle, "Bangle", "Sell or deliver", 200, false),
                Item(Brooch, "Brooch", "Sell or deliver", 240, false),
                Item(Anklet, "Anklet", "Sell or deliver", 210, false),
                Item(Tiara, "Tiara", "Sell or deliver", 420, false),
                Item(JewelSet, "Jewellery set", "Sell", 2400, false)
            };
        }

        static Ingredient Need(string id, int count)
        {
            return new Ingredient { ItemId = id, Count = count, MinGrade = 0 };
        }

        static Ingredient Diamond(int count)
        {
            return new Ingredient { ItemId = AnyDiamond, Count = count, MinGrade = 1 };
        }

        static JewelRecipe Recipe(string id, string name, string output, double seconds, string unlock, string note, params Ingredient[] inputs)
        {
            var recipe = new JewelRecipe
            {
                Id = id,
                Name = name,
                OutputItem = output,
                OutputCount = 1,
                Seconds = seconds,
                Unlock = unlock,
                Note = note
            };
            for (int i = 0; i < inputs.Length; i++)
                recipe.Inputs.Add(inputs[i]);
            return recipe;
        }

        static JewelRecipe[] BuildRecipes()
        {
            return new[]
            {
                Recipe("ring", "Diamond ring", Ring, 2d, "start", "One ring.", Diamond(1), Need(RefinedSilver, 1)),
                Recipe("bracelet", "Bracelet", Bracelet, 2.4d, "ring", "One bracelet.", Diamond(1), Need(RefinedCopper, 2)),
                Recipe("necklace", "Necklace", Necklace, 3d, "sixth", "One necklace.", Diamond(2), Need(RefinedGold, 1)),
                Recipe("pendant", "Pendant", Pendant, 2.2d, "polish", "One pendant.", Diamond(1), Need(RefinedSilver, 1)),
                Recipe("earrings", "Earrings", Earrings, 2.6d, "gold", "One pair. Uses two diamonds.", Diamond(2), Need(RefinedGold, 1)),
                Recipe("bangle", "Bangle", Bangle, 2.4d, "order", "One bangle.", Diamond(1), Need(RefinedSilver, 2)),
                Recipe("brooch", "Brooch", Brooch, 2.8d, "ten", "One brooch.", Diamond(1), Need(RefinedGold, 1), Need(RefinedSilver, 1)),
                Recipe("anklet", "Anklet", Anklet, 2.4d, "region", "One anklet.", Diamond(1), Need(RefinedCopper, 2)),
                Recipe("tiara", "Tiara", Tiara, 4d, "eight", "One tiara.", Diamond(3), Need(RefinedGold, 2)),
                Recipe("set", "Jewellery set", JewelSet, 8d, "set", "Uses one of each finished piece.",
                    Need(Ring, 1), Need(Bracelet, 1), Need(Necklace, 1), Need(Pendant, 1), Need(Earrings, 1),
                    Need(Bangle, 1), Need(Brooch, 1), Need(Anklet, 1), Need(Tiara, 1))
            };
        }

        static CharacterDef Person(string id, string name, string role, string summary, string unlock, bool free, int helmet, int cloth, int accent, int skin, int hair, string accessory)
        {
            return new CharacterDef
            {
                Id = id,
                Name = name,
                Role = role,
                Summary = summary,
                UnlockText = unlock,
                Free = free,
                Helmet = helmet,
                Cloth = cloth,
                Accent = accent,
                Skin = skin,
                Hair = hair,
                Accessory = accessory
            };
        }

        static CharacterDef[] BuildCharacters()
        {
            return new[]
            {
                Person("tavi", "Tavi Flintstep", "Explorer", "Shows the yard and the five free bunkers.", "Free", true, 0x1F8A8A, 0x2E6B8A, 0xE07020, 0x8D5524, 0x1A1A1A, "scarf"),
                Person("mira", "Mira Mosswheel", "Cart engineer", "Explains routes and how much you can carry.", "Free", true, 0x2F6B45, 0xD4652F, 0xE8C56B, 0xE0AC69, 0x6B3A2A, "pouch"),
                Person("oren", "Oren Pebblewick", "Geology guide", "Explains rocks and what a bunker holds.", "Free", true, 0xF0E2A0, 0x7A4A78, 0xD8C07A, 0xF1C27D, 0x4A3728, "tubes"),
                Person("sola", "Sola Shelfstone", "Shopkeeper", "Explains selling and customer orders.", "Free", true, 0xF3C2A8, 0x8AA878, 0xE7B24A, 0xC68642, 0x2B2118, "badge"),
                Person("zela", "Zela Copperfern", "Excavation engineer", "Likes a full crew and another bunker.", "Own 6 bunkers", false, 0x4A5568, 0x2A4E9A, 0xC4784A, 0x5C3317, 0x111111, "gloves"),
                Person("pavi", "Pavi Dewstone", "Washing specialist", "Unlocks after the first wash.", "Wash one batch", false, 0xF4E1A8, 0x2BB8B0, 0xE07A6A, 0xFFDBAC, 0xC45C26, "goggles"),
                Person("nilo", "Nilo Quartzpatch", "Sorting specialist", "Unlocks after five rough diamonds.", "Recover 5 diamonds", false, 0xA8E0C8, 0x3D3A8C, 0xF2D48A, 0xA56B45, 0x3A3A3A, "tray"),
                Person("etta", "Etta Facetbrook", "Gem cutter", "Unlocks after the first cut.", "Cut a diamond", false, 0xE7D7C8, 0xF0A06A, 0x6A4A9A, 0x6B3E26, 0xE6D2B0, "glasses"),
                Person("rumi", "Rumi Glowgrain", "Polishing specialist", "Unlocks after the first polish.", "Polish a diamond", false, 0xF7F1E8, 0xC8B4E0, 0xD6E8F5, 0xE8B89A, 0x5A463A, "cloth"),
                Person("kavi", "Kavi Loopforge", "Jewellery maker", "Unlocks after the first ring.", "Craft a ring", false, 0xE6D2B5, 0x8C3048, 0xD4A017, 0x3C2415, 0x1C1C1C, "loop"),
                Person("beni", "Beni Trailcart", "Delivery worker", "Unlocks after three orders.", "Finish 3 orders", false, 0xE0A040, 0x7EB6E0, 0xF4E7C8, 0xD4A574, 0x6A4226, "cart"),
                Person("daro", "Daro Rootlamp", "Workshop planner", "Unlocks after a new region.", "Find another region", false, 0xD6A423, 0x6B7040, 0x2AAAA0, 0x7A4B32, 0xB7B7B7, "blueprint")
            };
        }

        static YardTaskDef[] BuildTasks()
        {
            return new[]
            {
                new YardTaskDef { Id = "ore20", Title = "Collect 20 ore", Detail = "Any ore counts.", Reward = 15 },
                new YardTaskDef { Id = "visit5", Title = "Visit 5 bunkers", Detail = "Touch each starter door.", Reward = 15 },
                new YardTaskDef { Id = "wash1", Title = "Wash one batch", Detail = "Use the wash tank.", Reward = 20 },
                new YardTaskDef { Id = "rough5", Title = "Find 5 diamonds", Detail = "Sort washed grit.", Reward = 25 },
                new YardTaskDef { Id = "polish1", Title = "Polish a diamond", Detail = "Finish one stone.", Reward = 20 },
                new YardTaskDef { Id = "silver1", Title = "Refine silver", Detail = "Make a silver bar.", Reward = 20 },
                new YardTaskDef { Id = "ring1", Title = "Craft a ring", Detail = "Use the bench.", Reward = 30 },
                new YardTaskDef { Id = "orders3", Title = "Finish 3 orders", Detail = "Help shop visitors.", Reward = 40 },
                new YardTaskDef { Id = "bunker6", Title = "Buy a bunker", Detail = "The next door after the free five.", Reward = 30 },
                new YardTaskDef { Id = "region2", Title = "Find a region", Detail = "Open the region gate.", Reward = 20 }
            };
        }

        static CustomerOrderDef[] BuildOrders()
        {
            return new[]
            {
                new CustomerOrderDef { Id = "stone_bag", Title = "A bag of stone", ItemId = Stone, Amount = 5, Pay = 25 },
                new CustomerOrderDef { Id = "first_ring", Title = "A ring for a gift", ItemId = Ring, Amount = 1, Pay = 150 },
                new CustomerOrderDef { Id = "silver_bar", Title = "One silver bar", ItemId = RefinedSilver, Amount = 1, Pay = 40 }
            };
        }
    }

    public sealed class PoseInput
    {
        public bool Moving;
        public bool Sprint;
        public bool Carrying;
        public bool Mining;
        public bool Collecting;
        public bool UsingMachine;
        public bool Crafting;
        public double CelebrateSeconds;
        public double WaveSeconds;
    }

    public static class PuppetPose
    {
        public static readonly string[] Required =
        {
            "idle", "walk", "run", "mine", "collect", "carry", "machine", "craft", "celebrate", "wave"
        };

        public static string Resolve(PoseInput input)
        {
            if (input == null)
                return "idle";
            if (input.CelebrateSeconds > 0d)
                return "celebrate";
            if (input.WaveSeconds > 0d)
                return "wave";
            if (input.Crafting)
                return "craft";
            if (input.UsingMachine)
                return "machine";
            if (input.Mining)
                return "mine";
            if (input.Collecting)
                return "collect";
            if (input.Moving && input.Sprint)
                return "run";
            if (input.Moving && input.Carrying)
                return "carry";
            if (input.Moving)
                return "walk";
            if (input.Carrying)
                return "carry";
            return "idle";
        }
    }
}
