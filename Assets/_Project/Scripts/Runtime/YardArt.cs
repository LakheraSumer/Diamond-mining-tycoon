using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Existing yard blockout. Plot 0 uses imported Blender assets; the remaining
    /// plots and production stations keep their current temporary presentation.
    /// </summary>
    public static class YardArt
    {
        public struct Crew
        {
            public Transform Root;
            public Transform Arm;
            public Transform ToolSocket;
            public Transform Helmet;
            public Transform Head;
            public Transform Cloth;
            public CrewActor Actor;
        }

        public static TextMesh[] BunkerLabels;

        public static TimberBunkerArea BuildSite(Transform parent)
        {
            Terrain(parent);
            TimberBunkerArea hero = TimberBunkerArea.Create(parent);
            for (int i = 0; i < 8; i++)
            {
                float x = -9f + i * 2.4f;
                Tree(parent, new Vector3(x, 0f, -3.4f), 0.7f + (i % 3) * 0.15f);
                Tree(parent, new Vector3(-8.5f + i * 2.2f, 0f, 11.5f), 0.9f);
            }
            BunkerLabels = new TextMesh[WorldGenerator.VisibleYardPlots];
            for (int plot = 0; plot < WorldGenerator.VisibleYardPlots; plot++)
            {
                // Plot 0 is the authored Blender slice. Never overwrite it with a primitive bunker/cart.
                if (plot == 0) continue;
                YardPoint door = YardLayout.Entrance(plot);
                var at = new Vector3((float)door.X, 0f, (float)door.Z);
                Color ore = plot == 2 ? new Color(0.45f, 0.82f, 0.95f) : plot == 4 ? new Color(0.92f, 0.78f, 0.28f) : new Color(0.62f, 0.58f, 0.54f);
                BunkerLabels[plot] = Bunker(parent, at, plot + 1, ore, plot >= WorldGenerator.StarterCount);
                if (plot < 6)
                    Cart(parent, at + new Vector3(0f, 0f, -1.15f), ore);
            }
            // Authored curved track joins this existing main line beyond the reviewed patch.
            Rails(parent, new Vector3(-6.1f, 0.02f, 4.86f), new Vector3(6.5f, 0.02f, 4.86f));
            BuildCaveMineVignette(parent, new Vector3(-8.2f, 0f, 6.8f));
            BuildArchipelagoMap(parent, new Vector3(26f, 0f, 26f));

            // Factory processing line with connecting conveyor
            Crusher(parent, new Vector3(-1.6f, 0f, 2.6f));
            WashTank(parent, new Vector3(0.6f, 0f, 2.6f));
            SortTable(parent, new Vector3(2.6f, 0f, 2.6f));
            CutBooth(parent, new Vector3(4.4f, 0f, 2.6f));
            PolishBench(parent, new Vector3(6.1f, 0f, 2.6f));
            Refinery(parent, new Vector3(8f, 0f, 2.4f));

            // Continuous industrial conveyor belt along processing stations
            Part(parent, "MainLineConveyor", PrimitiveType.Cube, new Vector3(2.25f, 0.40f, 2.6f), new Vector3(8.5f, 0.08f, 0.32f), Vector3.zero, new Color(0.22f, 0.24f, 0.26f), 0.35f, 0.3f);
            for (int i = 0; i < 9; i++)
                Part(parent, "MainLineRoller_" + i, PrimitiveType.Cylinder, new Vector3(-1.8f + i * 1.0f, 0.42f, 2.6f), new Vector3(0.08f, 0.15f, 0.08f), new Vector3(0f, 0f, 90f), new Color(0.65f, 0.68f, 0.70f), 0.5f, 0.5f);

            Workshop(parent, new Vector3(6.4f, 0f, 0.2f));
            Boutique(parent, new Vector3(4.2f, 0f, -1.2f));
            string[] bins = { "Stone", "Iron", "Copper", "Silver", "Gold" };
            Color[] binColors =
            {
                new Color(0.62f, 0.58f, 0.52f),
                new Color(0.55f, 0.32f, 0.26f),
                new Color(0.78f, 0.46f, 0.24f),
                new Color(0.78f, 0.8f, 0.82f),
                new Color(0.9f, 0.72f, 0.22f)
            };
            for (int i = 0; i < bins.Length; i++)
                Bin(parent, new Vector3(-7.2f + i * 1.45f, 0f, -2.5f), bins[i], binColors[i]);

            // Place role crew at stations across the yard
            PlaceCrew(parent, "pavi", new Vector3(0.9f, 0f, 1.5f), 20f);
            PlaceCrew(parent, "nilo", new Vector3(2.6f, 0f, 1.7f), 0f);
            PlaceCrew(parent, "kavi", new Vector3(6.4f, 0f, -0.6f), 0f);
            PlaceCrew(parent, "sola", new Vector3(4.8f, 0f, -1.5f), -35f);
            PlaceCrew(parent, "oren", new Vector3(-7.2f, 0f, 5.8f), 45f);
            return hero;
        }

        public static Crew BuildCrew(CharacterDef person, Transform parent)
        {
            if (person == null) person = JourneyCatalog.AllCharacters[0];
            if (CrewActor.TryCreate(person, parent, out Crew imported)) return imported;
            Debug.LogError("Authored character prefab missing: Resources/Characters/" + person.Id +
                ". Run Diamond Mine > Characters > Import modelled crew.");
            return default;
        }

        static TextMesh Bunker(Transform parent, Vector3 at, int number, Color ore, bool locked)
        {
            var root = new GameObject("Bunker" + number);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Color rock = new Color(0.48f, 0.46f, 0.43f);
            Color timber = new Color(0.5f, 0.3f, 0.16f);
            Color timberDark = new Color(0.32f, 0.18f, 0.09f);
            Part(root.transform, "RockL", PrimitiveType.Cube, new Vector3(-0.85f, 0.9f, 0.3f), new Vector3(0.7f, 1.8f, 1.1f), new Vector3(0f, 8f, 0f), rock, 0.08f, 0f);
            Part(root.transform, "RockR", PrimitiveType.Cube, new Vector3(0.85f, 0.9f, 0.3f), new Vector3(0.7f, 1.8f, 1.1f), new Vector3(0f, -8f, 0f), rock, 0.08f, 0f);
            Part(root.transform, "RockTop", PrimitiveType.Cube, new Vector3(0f, 1.85f, 0.25f), new Vector3(1.8f, 0.55f, 1.2f), Vector3.zero, rock, 0.08f, 0f);
            Part(root.transform, "Mouth", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.15f), new Vector3(0.9f, 1.15f, 0.35f), Vector3.zero, new Color(0.08f, 0.07f, 0.06f), 0.05f, 0f);
            Part(root.transform, "PostL", PrimitiveType.Cube, new Vector3(-0.48f, 0.9f, -0.05f), new Vector3(0.12f, 1.7f, 0.12f), Vector3.zero, timber, 0.2f, 0f);
            Part(root.transform, "PostR", PrimitiveType.Cube, new Vector3(0.48f, 0.9f, -0.05f), new Vector3(0.12f, 1.7f, 0.12f), Vector3.zero, timber, 0.2f, 0f);
            Part(root.transform, "Lintel", PrimitiveType.Cube, new Vector3(0f, 1.7f, -0.05f), new Vector3(1.15f, 0.14f, 0.16f), Vector3.zero, timberDark, 0.2f, 0f);
            Part(root.transform, "BraceL", PrimitiveType.Cube, new Vector3(-0.28f, 1.45f, -0.05f), new Vector3(0.5f, 0.08f, 0.1f), new Vector3(0f, 0f, 28f), timber, 0.2f, 0f);
            Part(root.transform, "BraceR", PrimitiveType.Cube, new Vector3(0.28f, 1.45f, -0.05f), new Vector3(0.5f, 0.08f, 0.1f), new Vector3(0f, 0f, -28f), timber, 0.2f, 0f);
            Part(root.transform, "Tie1", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.55f), new Vector3(0.7f, 0.06f, 0.12f), Vector3.zero, timberDark, 0.15f, 0f);
            Part(root.transform, "Tie2", PrimitiveType.Cube, new Vector3(0f, 0.06f, -1.05f), new Vector3(0.7f, 0.06f, 0.12f), Vector3.zero, timberDark, 0.15f, 0f);
            Part(root.transform, "RailL", PrimitiveType.Cube, new Vector3(-0.16f, 0.1f, -0.8f), new Vector3(0.05f, 0.04f, 1.3f), Vector3.zero, new Color(0.25f, 0.27f, 0.3f), 0.55f, 0.7f);
            Part(root.transform, "RailR", PrimitiveType.Cube, new Vector3(0.16f, 0.1f, -0.8f), new Vector3(0.05f, 0.04f, 1.3f), Vector3.zero, new Color(0.25f, 0.27f, 0.3f), 0.55f, 0.7f);
            Part(root.transform, "Ore", PrimitiveType.Cube, new Vector3(-0.7f, 0.28f, -0.35f), new Vector3(0.28f, 0.22f, 0.22f), new Vector3(0f, 18f, 8f), ore, 0.35f, 0.05f);
            Part(root.transform, "Lantern", PrimitiveType.Sphere, new Vector3(0.62f, 1.35f, -0.1f), new Vector3(0.12f, 0.12f, 0.12f), Vector3.zero, locked ? new Color(0.4f, 0.4f, 0.42f) : new Color(1f, 0.85f, 0.4f), 0.6f, 0f);
            return Plate(root.transform, number.ToString(), new Vector3(0f, 2.15f, -0.15f));
        }

        public static Transform Cart(Transform parent, Vector3 at, Color ore)
        {
            var root = new GameObject("Cart");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Color wood = new Color(0.45f, 0.26f, 0.12f);
            Part(root.transform, "Bed", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.22f, 0.38f), Vector3.zero, wood, 0.2f, 0f);
            Part(root.transform, "Lip", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0.16f), new Vector3(0.58f, 0.08f, 0.06f), Vector3.zero, wood, 0.2f, 0f);
            Part(root.transform, "Load", PrimitiveType.Cube, new Vector3(0f, 0.46f, -0.02f), new Vector3(0.36f, 0.16f, 0.24f), new Vector3(8f, 12f, 0f), ore, 0.3f, 0.05f);
            Wheel(root.transform, new Vector3(-0.22f, 0.12f, 0.14f));
            Wheel(root.transform, new Vector3(0.22f, 0.12f, 0.14f));
            Wheel(root.transform, new Vector3(-0.22f, 0.12f, -0.14f));
            Wheel(root.transform, new Vector3(0.22f, 0.12f, -0.14f));
            return root.transform;
        }

        static void Wheel(Transform parent, Vector3 at)
        {
            Part(parent, "Wheel", PrimitiveType.Cylinder, at, new Vector3(0.12f, 0.04f, 0.12f), new Vector3(0f, 0f, 90f), new Color(0.12f, 0.12f, 0.13f), 0.25f, 0.2f);
        }

        static void Crusher(Transform parent, Vector3 at)
        {
            var root = new GameObject("Crusher");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Color teal = new Color(0.16f, 0.55f, 0.62f);
            Color tealDark = new Color(0.1f, 0.38f, 0.46f);
            Part(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.7f, 0.9f), Vector3.zero, teal, 0.4f, 0.25f);
            Part(root.transform, "Housing", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(1f, 0.55f, 0.7f), Vector3.zero, tealDark, 0.45f, 0.3f);
            Part(root.transform, "Drum", PrimitiveType.Cylinder, new Vector3(0.15f, 0.85f, 0f), new Vector3(0.42f, 0.38f, 0.42f), new Vector3(0f, 0f, 90f), new Color(0.55f, 0.58f, 0.62f), 0.6f, 0.7f);
            Part(root.transform, "Hopper", PrimitiveType.Cube, new Vector3(-0.45f, 1.15f, 0f), new Vector3(0.4f, 0.35f, 0.4f), new Vector3(0f, 0f, 18f), teal, 0.35f, 0.2f);
            Conveyor(root.transform, new Vector3(0.95f, 0.42f, 0f));
            Plate(root.transform, "Crush", new Vector3(0f, 1.35f, -0.2f));
        }

        static void WashTank(Transform parent, Vector3 at)
        {
            var root = new GameObject("Wash");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Color teal = new Color(0.2f, 0.62f, 0.7f);
            Part(root.transform, "Tank", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.35f, 0.7f), Vector3.zero, teal, 0.5f, 0.2f);
            Part(root.transform, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.92f, 0f), new Vector3(0.55f, 0.04f, 0.55f), Vector3.zero, new Color(0.45f, 0.75f, 0.9f), 0.7f, 0.05f);
            Part(root.transform, "Pipe", PrimitiveType.Cylinder, new Vector3(0.45f, 0.7f, 0f), new Vector3(0.08f, 0.28f, 0.08f), new Vector3(0f, 0f, 70f), new Color(0.7f, 0.74f, 0.78f), 0.55f, 0.6f);
            Plate(root.transform, "Wash", new Vector3(0f, 1.25f, 0f));
        }

        static void SortTable(Transform parent, Vector3 at)
        {
            var root = new GameObject("Sort");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Table", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.12f, 0.7f), Vector3.zero, new Color(0.55f, 0.58f, 0.6f), 0.35f, 0.3f);
            Part(root.transform, "Leg", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.4f, 0.5f), Vector3.zero, new Color(0.16f, 0.48f, 0.55f), 0.3f, 0.15f);
            Part(root.transform, "Tray", PrimitiveType.Cube, new Vector3(0.15f, 0.66f, 0f), new Vector3(0.35f, 0.06f, 0.28f), Vector3.zero, new Color(0.75f, 0.78f, 0.82f), 0.4f, 0.2f);
            Plate(root.transform, "Sort", new Vector3(0f, 1.05f, 0f));
        }

        static void CutBooth(Transform parent, Vector3 at)
        {
            var root = new GameObject("Cut");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Booth", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 1.2f, 0.7f), Vector3.zero, new Color(0.18f, 0.5f, 0.58f), 0.35f, 0.2f);
            Part(root.transform, "Window", PrimitiveType.Cube, new Vector3(0f, 0.85f, -0.32f), new Vector3(0.4f, 0.45f, 0.06f), Vector3.zero, new Color(0.7f, 0.9f, 0.95f), 0.8f, 0.1f);
            Plate(root.transform, "Cut", new Vector3(0f, 1.45f, 0f));
        }

        static void PolishBench(Transform parent, Vector3 at)
        {
            var root = new GameObject("Polish");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Bench", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.7f, 0.6f), Vector3.zero, new Color(0.25f, 0.58f, 0.64f), 0.4f, 0.2f);
            Part(root.transform, "Wheel", PrimitiveType.Cylinder, new Vector3(0.2f, 0.95f, 0f), new Vector3(0.28f, 0.06f, 0.28f), new Vector3(0f, 0f, 90f), new Color(0.85f, 0.86f, 0.88f), 0.7f, 0.4f);
            Plate(root.transform, "Polish", new Vector3(0f, 1.3f, 0f));
        }

        static void Refinery(Transform parent, Vector3 at)
        {
            var root = new GameObject("Refinery");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Furnace", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(0.9f, 1.3f, 0.8f), Vector3.zero, new Color(0.45f, 0.28f, 0.22f), 0.2f, 0.15f);
            Part(root.transform, "Glow", PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.36f), new Vector3(0.35f, 0.28f, 0.08f), Vector3.zero, new Color(1f, 0.55f, 0.15f), 0.6f, 0f);
            Part(root.transform, "Stack", PrimitiveType.Cylinder, new Vector3(0.2f, 1.6f, 0.1f), new Vector3(0.16f, 0.4f, 0.16f), Vector3.zero, new Color(0.35f, 0.35f, 0.38f), 0.3f, 0.4f);
            Plate(root.transform, "Refine", new Vector3(0f, 2.1f, 0f));
        }

        static void Workshop(Transform parent, Vector3 at)
        {
            var root = new GameObject("Workshop");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Wall", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 1.5f, 1.2f), Vector3.zero, new Color(0.93f, 0.86f, 0.7f), 0.15f, 0f);
            Part(root.transform, "Roof", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0f), new Vector3(1.8f, 0.18f, 1.4f), new Vector3(0f, 0f, 8f), new Color(0.55f, 0.28f, 0.22f), 0.15f, 0f);
            Part(root.transform, "Bench", PrimitiveType.Cube, new Vector3(0.15f, 0.55f, -0.45f), new Vector3(1.1f, 0.14f, 0.55f), Vector3.zero, new Color(0.48f, 0.32f, 0.18f), 0.25f, 0f);

            // Stacks of Gold Ingots
            Color gold = new Color(0.98f, 0.82f, 0.18f);
            Part(root.transform, "GoldBar1", PrimitiveType.Cube, new Vector3(0.45f, 0.66f, -0.45f), new Vector3(0.24f, 0.08f, 0.14f), new Vector3(0f, 15f, 0f), gold, 0.85f, 0.9f);
            Part(root.transform, "GoldBar2", PrimitiveType.Cube, new Vector3(0.45f, 0.74f, -0.45f), new Vector3(0.22f, 0.08f, 0.12f), new Vector3(0f, 20f, 0f), gold, 0.85f, 0.9f);

            // Velvet Gem Tray with Cut Diamonds
            Part(root.transform, "VelvetTray", PrimitiveType.Cube, new Vector3(-0.25f, 0.64f, -0.45f), new Vector3(0.36f, 0.04f, 0.28f), Vector3.zero, new Color(0.12f, 0.12f, 0.22f), 0.2f, 0f);
            EmissivePart(root.transform, "SolitaireGem", PrimitiveType.Cube, new Vector3(-0.25f, 0.70f, -0.45f), new Vector3(0.10f, 0.10f, 0.10f), new Vector3(45f, 45f, 0f), new Color(0.25f, 0.85f, 1.0f), new Color(0.2f, 0.8f, 1f) * 1.5f, 0.95f, 0.1f);

            // Warm task spotlight
            PointLight(root.transform, "ArtisanLamp", new Vector3(0.1f, 1.35f, -0.4f), new Color(1f, 0.88f, 0.62f), 2.0f, 3.8f);

            Plate(root.transform, "Workshop", new Vector3(0f, 1.95f, -0.2f));
        }

        static void Boutique(Transform parent, Vector3 at)
        {
            var root = new GameObject("Boutique");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Shop", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(2.0f, 1.7f, 1.5f), Vector3.zero, new Color(0.96f, 0.92f, 0.82f), 0.15f, 0f);
            Part(root.transform, "Awning", PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.75f), new Vector3(2.2f, 0.08f, 0.65f), new Vector3(14f, 0f, 0f), new Color(0.18f, 0.58f, 0.66f), 0.25f, 0f);

            // Polished Glass Showcase Counter
            Part(root.transform, "CaseBase", PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.65f), new Vector3(1.5f, 0.42f, 0.45f), Vector3.zero, new Color(0.22f, 0.16f, 0.12f), 0.3f, 0.1f);
            Part(root.transform, "GlassTop", PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.65f), new Vector3(1.45f, 0.22f, 0.42f), Vector3.zero, new Color(0.75f, 0.92f, 0.96f), 0.95f, 0.05f);

            // Display Bust with Diamond Necklace
            Part(root.transform, "NecklaceBust", PrimitiveType.Cylinder, new Vector3(-0.35f, 0.75f, -0.65f), new Vector3(0.16f, 0.15f, 0.16f), Vector3.zero, new Color(0.15f, 0.12f, 0.22f), 0.2f, 0f);
            EmissivePart(root.transform, "NecklaceGem", PrimitiveType.Sphere, new Vector3(-0.35f, 0.82f, -0.62f), new Vector3(0.08f, 0.08f, 0.08f), Vector3.zero, new Color(0.2f, 0.85f, 1f), new Color(0.2f, 0.85f, 1f) * 1.8f, 0.95f, 0.1f);

            // Open Ring Box with Diamond Ring
            Part(root.transform, "RingBox", PrimitiveType.Cube, new Vector3(0.32f, 0.72f, -0.65f), new Vector3(0.14f, 0.08f, 0.14f), new Vector3(0f, 20f, 0f), new Color(0.55f, 0.12f, 0.18f), 0.3f, 0f);
            EmissivePart(root.transform, "SolitaireRing", PrimitiveType.Sphere, new Vector3(0.32f, 0.78f, -0.65f), new Vector3(0.07f, 0.07f, 0.07f), Vector3.zero, new Color(0.98f, 0.82f, 0.2f), new Color(0.3f, 0.85f, 1f) * 1.5f, 0.95f, 0.3f);

            // Warm Interior Boutique Light
            PointLight(root.transform, "BoutiqueGlow", new Vector3(0f, 1.4f, -0.5f), new Color(1f, 0.92f, 0.75f), 2.2f, 4.5f);

            Plate(root.transform, "Boutique", new Vector3(0f, 2.05f, -0.3f));
        }

        static void Bin(Transform parent, Vector3 at, string label, Color fill)
        {
            var root = new GameObject(label + "Bin");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Crate", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(1.05f, 0.45f, 0.75f), Vector3.zero, new Color(0.42f, 0.26f, 0.14f), 0.15f, 0f);
            Part(root.transform, "Fill", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.85f, 0.18f, 0.55f), Vector3.zero, fill, 0.25f, 0.05f);
            Plate(root.transform, label, new Vector3(0f, 0.8f, 0f));
        }

        static void Conveyor(Transform parent, Vector3 at)
        {
            Part(parent, "Belt", PrimitiveType.Cube, at, new Vector3(0.9f, 0.08f, 0.28f), Vector3.zero, new Color(0.2f, 0.22f, 0.24f), 0.3f, 0.2f);
            for (int i = 0; i < 4; i++)
                Part(parent, "Roller", PrimitiveType.Cylinder, at + new Vector3(-0.3f + i * 0.2f, 0.02f, 0f), new Vector3(0.08f, 0.12f, 0.08f), new Vector3(0f, 0f, 90f), new Color(0.65f, 0.68f, 0.7f), 0.5f, 0.5f);
        }

        static void Rails(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 mid = (from + to) * 0.5f;
            Vector3 scale = new Vector3(Mathf.Abs(to.x - from.x), 0.04f, 0.05f);
            Part(parent, "RunL", PrimitiveType.Cube, mid + new Vector3(0f, 0.08f, -0.16f), scale, Vector3.zero, new Color(0.3f, 0.32f, 0.34f), 0.55f, 0.7f);
            Part(parent, "RunR", PrimitiveType.Cube, mid + new Vector3(0f, 0.08f, 0.16f), scale, Vector3.zero, new Color(0.3f, 0.32f, 0.34f), 0.55f, 0.7f);
        }

        static void Terrain(Transform parent)
        {
            const int nx = 40;
            const int nz = 48;
            const float sizeX = 26f;
            const float sizeZ = 30f;
            int vx = nx + 1;
            int vz = nz + 1;
            var vertices = new Vector3[vx * vz];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            for (int z = 0; z < vz; z++)
            {
                for (int x = 0; x < vx; x++)
                {
                    float fx = (x / (float)nx - 0.5f) * sizeX;
                    float fz = (z / (float)nz) * sizeZ - 8f;
                    float h = 0.06f * Mathf.Sin(fx * 0.55f) + 0.05f * Mathf.Cos(fz * 0.45f);
                    if (fz > 7.6f)
                        h += (fz - 7.6f) * 0.9f;
                    // The imported contoured trail owns this surface; keep the old grid below it.
                    if (fx > -10.4f && fx < -6.0f && fz > 3.35f && fz < 8.6f)
                        h = -.30f;
                    int index = z * vx + x;
                    vertices[index] = new Vector3(fx, h, fz);
                    uv[index] = new Vector2(x / (float)nx, z / (float)nz);
                    bool path = Mathf.Abs(fx + 1f) < 1.6f && fz < 7.2f && fz > -2f;
                    bool cliff = fz > 7.8f;
                    colors[index] = cliff
                        ? new Color(0.5f, 0.48f, 0.44f)
                        : path ? new Color(0.62f, 0.48f, 0.3f) : new Color(0.32f, 0.62f, 0.3f);
                }
            }
            var triangles = new int[nx * nz * 6];
            int t = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int i = z * vx + x;
                    triangles[t++] = i;
                    triangles[t++] = i + vx;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + vx;
                    triangles[t++] = i + vx + 1;
                }
            }
            var mesh = new Mesh();
            mesh.name = "YardTerrain";
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            var ground = new GameObject("Terrain");
            ground.transform.SetParent(parent, false);
            var filter = ground.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = ground.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("DiamondMine/VertexLit");
            if (shader == null)
                shader = Shader.Find("Diffuse");
            renderer.sharedMaterial = new Material(shader);
            var water = Part(parent, "Stream", PrimitiveType.Cube, new Vector3(-10.5f, -0.15f, 1f), new Vector3(1.4f, 0.2f, 8f), Vector3.zero, new Color(0.25f, 0.55f, 0.72f), 0.7f, 0.05f);
            water.gameObject.isStatic = false;
        }

        static void Tree(Transform parent, Vector3 at, float scale)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            root.transform.localScale = Vector3.one * scale;
            Part(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.18f, 0.45f, 0.18f), Vector3.zero, new Color(0.38f, 0.24f, 0.12f), 0.1f, 0f);
            Part(root.transform, "LeafA", PrimitiveType.Sphere, new Vector3(0f, 1.15f, 0f), new Vector3(0.8f, 0.7f, 0.8f), Vector3.zero, new Color(0.18f, 0.48f, 0.22f), 0.05f, 0f);
            Part(root.transform, "LeafB", PrimitiveType.Sphere, new Vector3(0.2f, 1.45f, 0.1f), new Vector3(0.55f, 0.5f, 0.55f), Vector3.zero, new Color(0.28f, 0.58f, 0.26f), 0.05f, 0f);
        }

        static void PlaceCrew(Transform parent, string id, Vector3 at, float yaw)
        {
            Crew crew = BuildCrew(JourneyCatalog.FindCharacter(id), parent);
            if (crew.Root == null) return;
            crew.Root.position = at;
            crew.Root.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        static TextMesh Plate(Transform parent, string text, Vector3 local)
        {
            var board = new GameObject("Sign");
            board.transform.SetParent(parent, false);
            board.transform.localPosition = local;
            board.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh mesh = board.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = 0.045f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.12f, 0.08f, 0.05f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                mesh.font = font;
            return mesh;
        }

        static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Vector3 euler, Color color, float smooth, float metal)
        {
            GameObject block = GameObject.CreatePrimitive(type);
            block.name = name;
            Collider collider = block.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }
            block.transform.SetParent(parent, false);
            block.transform.localPosition = local;
            block.transform.localScale = scale;
            block.transform.localRotation = Quaternion.Euler(euler);
            Renderer renderer = block.GetComponent<Renderer>();
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Diffuse");
            var material = new Material(shader);
            material.color = color;
            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Glossiness", smooth);
                material.SetFloat("_Metallic", metal);
            }
            renderer.sharedMaterial = material;
            return block.transform;
        }

        public static void BuildCaveMineVignette(Transform parent, Vector3 at)
        {
            var root = new GameObject("CaveMineVignette");
            root.transform.SetParent(parent, false);
            root.transform.position = at;

            Color slateDark = new Color(0.22f, 0.22f, 0.25f);
            Color slateMid = new Color(0.32f, 0.32f, 0.36f);
            Color timber = new Color(0.38f, 0.22f, 0.12f);
            Color diamondCyan = new Color(0.20f, 0.85f, 1.0f);
            Color cyanGlow = new Color(0.15f, 0.85f, 1.0f) * 2.2f;

            // Cavern Rock Formations behind the timber entrance
            Part(root.transform, "CaveBackRock1", PrimitiveType.Cube, new Vector3(-1.6f, 1.6f, 1.8f), new Vector3(2.6f, 3.2f, 1.8f), new Vector3(8f, 15f, -6f), slateDark, 0.15f, 0f);
            Part(root.transform, "CaveBackRock2", PrimitiveType.Cube, new Vector3(1.4f, 1.8f, 1.6f), new Vector3(2.4f, 3.4f, 1.6f), new Vector3(-6f, -12f, 8f), slateDark, 0.15f, 0f);
            Part(root.transform, "CaveCeiling", PrimitiveType.Cube, new Vector3(0f, 3.2f, 0.8f), new Vector3(4.2f, 1.4f, 3.2f), new Vector3(12f, 5f, 0f), slateDark, 0.15f, 0f);
            Part(root.transform, "CaveWallL", PrimitiveType.Cube, new Vector3(-2.4f, 1.4f, 0.2f), new Vector3(1.8f, 2.8f, 2.8f), new Vector3(0f, 25f, 0f), slateMid, 0.15f, 0f);

            // Glowing Diamond Crystal Cluster (hero focal point)
            Transform cluster = new GameObject("DiamondCluster").transform;
            cluster.SetParent(root.transform, false);
            cluster.localPosition = new Vector3(-1.45f, 1.15f, 0.45f);

            EmissivePart(cluster, "Crystal_Main", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.32f, 0.75f, 0.32f), new Vector3(32f, 24f, 45f), diamondCyan, cyanGlow, 0.95f, 0.1f);
            EmissivePart(cluster, "Crystal_Spike1", PrimitiveType.Cube, new Vector3(0.18f, 0.22f, -0.08f), new Vector3(0.24f, 0.55f, 0.24f), new Vector3(15f, 65f, 35f), diamondCyan, cyanGlow, 0.95f, 0.1f);
            EmissivePart(cluster, "Crystal_Spike2", PrimitiveType.Cube, new Vector3(-0.16f, 0.14f, 0.12f), new Vector3(0.22f, 0.48f, 0.22f), new Vector3(-25f, -40f, 55f), diamondCyan, cyanGlow, 0.95f, 0.1f);
            EmissivePart(cluster, "Crystal_Shard1", PrimitiveType.Cube, new Vector3(0.12f, -0.18f, 0.14f), new Vector3(0.18f, 0.36f, 0.18f), new Vector3(45f, 15f, -30f), diamondCyan, cyanGlow, 0.95f, 0.1f);
            EmissivePart(cluster, "Crystal_Shard2", PrimitiveType.Cube, new Vector3(-0.14f, -0.12f, -0.12f), new Vector3(0.16f, 0.32f, 0.16f), new Vector3(-35f, 45f, 20f), diamondCyan, cyanGlow, 0.95f, 0.1f);

            PointLight(cluster, "CyanDiamondGlow", new Vector3(0f, 0.2f, 0f), diamondCyan, 2.4f, 5.5f);

            // Additional timber shoring posts
            Part(root.transform, "ShoringPost", PrimitiveType.Cube, new Vector3(-1.1f, 1.1f, -0.2f), new Vector3(0.18f, 2.2f, 0.18f), new Vector3(0f, 0f, 4f), timber, 0.2f, 0f);
            Part(root.transform, "ShoringCross", PrimitiveType.Cube, new Vector3(0f, 2.05f, -0.2f), new Vector3(2.4f, 0.18f, 0.22f), Vector3.zero, timber, 0.2f, 0f);

            // Warm hanging lantern
            Transform lantern = Part(root.transform, "LanternHousing", PrimitiveType.Cylinder, new Vector3(0.85f, 1.75f, -0.15f), new Vector3(0.14f, 0.22f, 0.14f), Vector3.zero, new Color(0.18f, 0.18f, 0.2f), 0.5f, 0.8f);
            EmissivePart(lantern, "LanternBulb", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.10f, 0.10f, 0.10f), Vector3.zero, new Color(1f, 0.85f, 0.4f), new Color(1f, 0.82f, 0.35f) * 2.0f, 0.9f, 0f);
            PointLight(lantern, "WarmLanternLight", Vector3.zero, new Color(1f, 0.82f, 0.45f), 1.8f, 4.5f);
        }

        public static void BuildArchipelagoMap(Transform parent, Vector3 at)
        {
            var root = new GameObject("ArchipelagoMap");
            root.transform.SetParent(parent, false);
            root.transform.position = at;

            Color seaBlue = new Color(0.14f, 0.58f, 0.74f);
            Color grassGreen = new Color(0.32f, 0.68f, 0.30f);
            Color sandYellow = new Color(0.88f, 0.82f, 0.60f);
            Color rockGray = new Color(0.48f, 0.48f, 0.50f);
            Color snowWhite = new Color(0.95f, 0.96f, 0.98f);
            Color cloudWhite = new Color(0.98f, 0.98f, 1.0f);
            Color cyanPlot = new Color(0.20f, 0.85f, 1.0f);

            // Ocean Water Basin
            Part(root.transform, "OceanPlane", PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(24f, 0.2f, 24f), Vector3.zero, seaBlue, 0.85f, 0.05f);

            // Main Central Island
            Part(root.transform, "MainIsland_Sand", PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0f), new Vector3(10.5f, 0.25f, 8.5f), new Vector3(0f, 25f, 0f), sandYellow, 0.15f, 0f);
            Part(root.transform, "MainIsland_Grass", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(9.2f, 0.35f, 7.5f), new Vector3(0f, 25f, 0f), grassGreen, 0.2f, 0f);
            Part(root.transform, "Mountain_Base", PrimitiveType.Cube, new Vector3(-0.5f, 1.0f, 0.8f), new Vector3(3.5f, 1.8f, 3.0f), new Vector3(0f, 35f, 0f), rockGray, 0.15f, 0f);
            Part(root.transform, "Mountain_Peak", PrimitiveType.Cube, new Vector3(-0.5f, 2.1f, 0.8f), new Vector3(1.8f, 1.4f, 1.6f), new Vector3(0f, 35f, 0f), snowWhite, 0.3f, 0f);

            // Surrounding Islands
            Vector3[] islands = {
                new Vector3(-6.2f, 0.1f, -4.5f),
                new Vector3(6.5f, 0.1f, -3.8f),
                new Vector3(-5.8f, 0.1f, 5.2f),
                new Vector3(5.5f, 0.1f, 4.8f),
                new Vector3(0f, 0.1f, -6.8f)
            };
            for (int i = 0; i < islands.Length; i++)
            {
                Part(root.transform, "SubIsland_" + i, PrimitiveType.Cylinder, islands[i], new Vector3(3.8f, 0.3f, 3.2f), new Vector3(0f, i * 42f, 0f), grassGreen, 0.2f, 0f);
                Part(root.transform, "SubRock_" + i, PrimitiveType.Cube, islands[i] + new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.7f, 1.2f), new Vector3(5f, i * 30f, -5f), rockGray, 0.15f, 0f);
            }

            // 6 Outposts with Plot 6 Highlighted
            Vector3[] outposts = {
                new Vector3(-2.8f, 0.45f, -1.5f),
                new Vector3(2.5f, 0.45f, -1.2f),
                new Vector3(-6.2f, 0.35f, -4.5f),
                new Vector3(6.5f, 0.35f, -3.8f),
                new Vector3(-5.8f, 0.35f, 5.2f),
                new Vector3(2.2f, 0.5f, 1.8f) // Plot 6 "Crystal Ridge Mine"
            };

            for (int i = 0; i < outposts.Length; i++)
            {
                bool isPlot6 = i == 5;
                Color roofColor = isPlot6 ? cyanPlot : new Color(0.78f, 0.35f, 0.22f);
                Part(root.transform, "Cabin_" + (i + 1), PrimitiveType.Cube, outposts[i] + new Vector3(0f, 0.25f, 0f), new Vector3(0.65f, 0.45f, 0.65f), Vector3.zero, new Color(0.48f, 0.32f, 0.20f), 0.2f, 0f);
                Part(root.transform, "Roof_" + (i + 1), PrimitiveType.Cube, outposts[i] + new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.22f, 0.8f), new Vector3(0f, 0f, 15f), roofColor, 0.3f, 0f);
                Part(root.transform, "FlagPole_" + (i + 1), PrimitiveType.Cylinder, outposts[i] + new Vector3(0.45f, 0.6f, 0f), new Vector3(0.04f, 0.7f, 0.04f), Vector3.zero, new Color(0.3f, 0.3f, 0.35f), 0.4f, 0.6f);

                if (isPlot6)
                {
                    // Glowing cyan highlight ring and crystal spire on Plot 6
                    EmissivePart(root.transform, "Plot6_Ring", PrimitiveType.Cylinder, outposts[i] + new Vector3(0f, 0.05f, 0f), new Vector3(1.8f, 0.04f, 1.8f), Vector3.zero, cyanPlot, cyanPlot * 2.0f, 0.9f, 0.2f);
                    EmissivePart(root.transform, "Plot6_Crystal", PrimitiveType.Cube, outposts[i] + new Vector3(0f, 0.95f, 0f), new Vector3(0.22f, 0.48f, 0.22f), new Vector3(25f, 45f, 25f), cyanPlot, cyanPlot * 2.5f, 0.95f, 0.1f);
                    PointLight(root.transform, "Plot6_Light", outposts[i] + new Vector3(0f, 1.2f, 0f), cyanPlot, 2.2f, 4.0f);
                }
            }

            // Stylized Puffy White Clouds
            Vector3[] clouds = {
                new Vector3(-4.5f, 4.2f, -3.5f),
                new Vector3(5.2f, 4.6f, 2.8f),
                new Vector3(-2.8f, 4.0f, 4.5f),
                new Vector3(3.8f, 4.4f, -4.8f)
            };
            for (int i = 0; i < clouds.Length; i++)
            {
                Transform cloud = new GameObject("Cloud_" + i).transform;
                cloud.SetParent(root.transform, false);
                cloud.localPosition = clouds[i];
                Part(cloud, "CloudPuff1", PrimitiveType.Sphere, Vector3.zero, new Vector3(1.4f, 0.7f, 0.9f), Vector3.zero, cloudWhite, 0.1f, 0f);
                Part(cloud, "CloudPuff2", PrimitiveType.Sphere, new Vector3(0.5f, 0.15f, 0f), new Vector3(1.0f, 0.6f, 0.8f), Vector3.zero, cloudWhite, 0.1f, 0f);
                Part(cloud, "CloudPuff3", PrimitiveType.Sphere, new Vector3(-0.45f, 0.1f, 0f), new Vector3(1.1f, 0.65f, 0.85f), Vector3.zero, cloudWhite, 0.1f, 0f);
            }
        }

        public static Transform EmissivePart(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Vector3 euler, Color color, Color emission, float smooth, float metal)
        {
            GameObject block = GameObject.CreatePrimitive(type);
            block.name = name;
            Collider collider = block.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }
            block.transform.SetParent(parent, false);
            block.transform.localPosition = local;
            block.transform.localScale = scale;
            block.transform.localRotation = Quaternion.Euler(euler);
            Renderer renderer = block.GetComponent<Renderer>();
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            var material = new Material(shader);
            material.color = color;
            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Glossiness", smooth);
                material.SetFloat("_Metallic", metal);
                if (emission.maxColorComponent > 0.01f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                }
            }
            renderer.sharedMaterial = material;
            return block.transform;
        }

        public static Light PointLight(Transform parent, string name, Vector3 local, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
    }
}
}
