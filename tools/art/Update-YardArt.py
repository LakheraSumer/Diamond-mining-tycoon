import re

path = 'Assets/_Project/Scripts/Runtime/YardArt.cs'
with open(path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update BuildSite
old_buildsite = """            // Authored curved track joins this existing main line beyond the reviewed patch.
            Rails(parent, new Vector3(-6.1f, 0.02f, 4.86f), new Vector3(6.5f, 0.02f, 4.86f));
            Crusher(parent, new Vector3(-1.6f, 0f, 2.6f));
            WashTank(parent, new Vector3(0.6f, 0f, 2.6f));
            SortTable(parent, new Vector3(2.6f, 0f, 2.6f));
            CutBooth(parent, new Vector3(4.4f, 0f, 2.6f));
            PolishBench(parent, new Vector3(6.1f, 0f, 2.6f));
            Refinery(parent, new Vector3(8f, 0f, 2.4f));
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
            PlaceCrew(parent, "pavi", new Vector3(0.9f, 0f, 1.5f), 20f);
            PlaceCrew(parent, "sola", new Vector3(5.5f, 0f, -1.6f), -30f);
            return hero;"""

new_buildsite = """            // Authored curved track joins this existing main line beyond the reviewed patch.
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
            return hero;"""

# Normalize line endings for replacement
code_lf = code.replace('\r\n', '\n')
old_buildsite_lf = old_buildsite.replace('\r\n', '\n')
new_buildsite_lf = new_buildsite.replace('\r\n', '\n')

assert old_buildsite_lf in code_lf, "Could not find old_buildsite in YardArt.cs"
code_lf = code_lf.replace(old_buildsite_lf, new_buildsite_lf)

# 2. Update Workshop
old_workshop = """        static void Workshop(Transform parent, Vector3 at)
        {
            var root = new GameObject("Workshop");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Wall", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 1.4f, 1f), Vector3.zero, new Color(0.93f, 0.86f, 0.7f), 0.15f, 0f);
            Part(root.transform, "Roof", PrimitiveType.Cube, new Vector3(0f, 1.6f, 0f), new Vector3(1.5f, 0.16f, 1.2f), new Vector3(0f, 0f, 8f), new Color(0.55f, 0.28f, 0.22f), 0.15f, 0f);
            Part(root.transform, "Bench", PrimitiveType.Cube, new Vector3(0.2f, 0.55f, -0.35f), new Vector3(0.7f, 0.12f, 0.3f), Vector3.zero, new Color(0.5f, 0.32f, 0.18f), 0.2f, 0f);
            Plate(root.transform, "Workshop", new Vector3(0f, 1.9f, -0.2f));
        }"""

new_workshop = """        static void Workshop(Transform parent, Vector3 at)
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
        }"""

code_lf = code_lf.replace(old_workshop.replace('\r\n', '\n'), new_workshop.replace('\r\n', '\n'))

# 3. Update Boutique
old_boutique = """        static void Boutique(Transform parent, Vector3 at)
        {
            var root = new GameObject("Boutique");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            Part(root.transform, "Shop", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(1.8f, 1.6f, 1.4f), Vector3.zero, new Color(0.96f, 0.9f, 0.78f), 0.15f, 0f);
            Part(root.transform, "Awning", PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.7f), new Vector3(1.9f, 0.08f, 0.55f), new Vector3(12f, 0f, 0f), new Color(0.2f, 0.55f, 0.62f), 0.2f, 0f);
            Part(root.transform, "Case", PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.55f), new Vector3(1.2f, 0.5f, 0.3f), Vector3.zero, new Color(0.75f, 0.88f, 0.9f), 0.65f, 0.05f);
            Part(root.transform, "Gem", PrimitiveType.Sphere, new Vector3(0.15f, 0.75f, -0.55f), new Vector3(0.12f, 0.12f, 0.12f), Vector3.zero, new Color(0.55f, 0.9f, 0.95f), 0.8f, 0.1f);
            Plate(root.transform, "Boutique", new Vector3(0f, 1.9f, -0.3f));
        }"""

new_boutique = """        static void Boutique(Transform parent, Vector3 at)
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
        }"""

code_lf = code_lf.replace(old_boutique.replace('\r\n', '\n'), new_boutique.replace('\r\n', '\n'))

# 4. Add CaveMine, ArchipelagoMap, EmissivePart, PointLight before end of YardArt class
extra_methods = """
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
"""

last_brace = code_lf.rfind('}')
code_lf = code_lf[:last_brace] + extra_methods + '\n}\n'

with open(path, 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(code_lf)
print('Successfully updated YardArt.cs')
