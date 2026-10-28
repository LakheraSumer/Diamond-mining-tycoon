using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Draws the mine from simulation state. Moving these objects does not mine, haul, or sell anything.
    /// </summary>
    public sealed class MinePresenter : MonoBehaviour
    {
        const int CartPool = 8;
        public const int WorldLayer = 29;

        Camera view;
        Camera backdrop;
        Light sun;
        Transform facePile;
        Transform processPile;
        Transform roughPile;
        Transform vaultFill;
        Transform jaw;
        Transform minerArm;
        Transform miner;
        Transform hauler;
        Transform sorterWorker;
        Transform clerk;
        Transform saleGem;
        Transform[] carts;
        Transform[] loads;
        GameObject warmDress;
        GameObject crystalDress;
        GameObject deepDress;
        Material wallMat;
        Material rockMat;
        Material metalMat;
        Material woodMat;
        Material diamondMat;
        Material coinMat;
        Material emberMat;
        Mesh gemMesh;
        Material[] gemFacets;
        string palette = "";
        double lastSold = -1d;
        float saleFlash;

        static readonly Vector3 FacePoint = new Vector3(-1.08f, 0.32f, 0f);
        static readonly Vector3 CrusherPoint = new Vector3(-0.02f, 0.32f, 0f);

        public Camera View { get { return view; } }

        public void SetRendering(bool enabled)
        {
            if (sun != null) sun.enabled = enabled;
            if (view != null)
                view.enabled = enabled;
            if (backdrop != null)
                backdrop.enabled = enabled;
        }

        void Awake()
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            wallMat = Make(shader, new Color(0.28f, 0.18f, 0.13f));
            rockMat = Make(shader, new Color(0.55f, 0.34f, 0.2f));
            metalMat = Make(shader, new Color(0.32f, 0.34f, 0.38f));
            woodMat = Make(shader, new Color(0.45f, 0.28f, 0.14f));
            diamondMat = Make(shader, new Color(0.35f, 0.86f, 0.92f));
            coinMat = Make(shader, new Color(0.93f, 0.76f, 0.28f));
            emberMat = Make(shader, new Color(0.75f, 0.28f, 0.12f));

            // Cameras must not move the scenery they observe. A full-screen clear
            // also prevents old overlay UI pixels surviving outside the mine viewport.
            var backdropObject = new GameObject("BackdropCamera");
            backdropObject.transform.SetParent(transform, false);
            backdrop = backdropObject.AddComponent<Camera>();
            backdrop.depth = -2;
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = new Color(0.09f, 0.07f, 0.08f);
            backdrop.cullingMask = 0;
            backdrop.allowMSAA = false;
            var cameraObject = new GameObject("MineCamera");
            cameraObject.transform.SetParent(transform, false);
            view = cameraObject.AddComponent<Camera>();
            view.depth = -1;
            view.cullingMask = 1 << WorldLayer;
            if (FindAnyObjectByType<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.2f, 0.13f, 0.1f);
            view.fieldOfView = 48f;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 40f;
            view.orthographic = false;
            view.allowMSAA = false;

            var lightObject = new GameObject("MineLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            sun = lightObject.AddComponent<Light>();
            sun.cullingMask = 1 << WorldLayer;
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.75f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Hard;

            BuildScenery();
            // Works and the authored yard occupy overlapping coordinates. Keep
            // their geometry and shadows on separate camera/light layers.
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = WorldLayer;
        }

        public void SetViewport(Rect rect)
        {
            if (view == null)
                return;
            view.rect = rect;
            float aspect = Mathf.Max(0.25f, view.aspect);
            float vertical = view.fieldOfView * Mathf.Deg2Rad;
            float horizontal = 2f * Mathf.Atan(Mathf.Tan(vertical * 0.5f) * aspect);
            float distance = Mathf.Max(3.3f, 2.05f / Mathf.Tan(horizontal * 0.5f));
            view.transform.position = new Vector3(0.12f, 1.62f, -distance);
            view.transform.LookAt(new Vector3(0.12f, 0.78f, 0f));
        }

        public void ApplyQuality(int quality)
        {
            if (sun == null)
                return;
            if (quality <= 0)
                sun.shadows = LightShadows.None;
            else if (quality == 1)
                sun.shadows = LightShadows.Hard;
            else
                sun.shadows = LightShadows.Soft;
        }

        public void Present(GameSession session)
        {
            if (session == null || !session.Ready || session.State == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            GameState state = session.State;
            MineState mine = state.FindMine(state.ActiveMineId);
            MineDefinition def = session.Config.FindMine(state.ActiveMineId);
            if (mine == null || def == null)
                return;

            ApplyQuality(state.Settings.Quality);
            Paint(def.Palette);
            MineRates rates = MineStats.Compute(def, mine, session.Config, state.Mastery, state.BoostRemainingSeconds > 0d);
            bool reduced = state.Settings.ReducedMotion;

            SetHeight(facePile, Fill(mine.OreAtFace, rates.MineBuffer), 0.7f);
            SetHeight(processPile, Fill(mine.ProcessInput, rates.ProcessBuffer), 0.55f);
            SetHeight(roughPile, Fill(mine.Rough, rates.RoughBuffer), 0.4f);
            SetHeight(vaultFill, Fill(mine.Storage, rates.StorageCap), 0.62f);

            bool mining = rates.AutoUnlocked || state.TotalPlaySeconds - mine.LastTapTime < 0.55d;
            float bob = reduced ? 0f : Mathf.Sin(Time.unscaledTime * 5.5f) * 0.035f;
            miner.localPosition = new Vector3(-1.28f, 0.42f + (mining ? bob : 0f), -0.68f);
            hauler.localPosition = new Vector3(-0.55f, 0.4f + (reduced ? 0f : bob), -0.42f);
            sorterWorker.localPosition = new Vector3(0.62f, 0.4f + (mine.Rough > 0.2d && !reduced ? bob : 0f), 0.38f);
            clerk.localPosition = new Vector3(1.48f, 0.4f + (mine.Storage > 0.2d && !reduced ? bob : 0f), -0.28f);
            if (minerArm != null)
            {
                float swing = mining && !reduced ? Mathf.Sin(Time.unscaledTime * 8f) * 28f : -12f;
                minerArm.localRotation = Quaternion.Euler(swing, 0f, 0f);
            }
            if (jaw != null)
            {
                float chew = mine.ProcessInput > 0.05d && !reduced ? Mathf.Sin(Time.unscaledTime * 7f) * 8f : 0f;
                jaw.localRotation = Quaternion.Euler(0f, 0f, chew);
            }

            PlaceCarts(mine, rates);
            if (lastSold < 0d)
                lastSold = mine.DiamondsSold;
            if (mine.DiamondsSold > lastSold + 0.001d)
                saleFlash = 1f;
            lastSold = mine.DiamondsSold;
            saleFlash = Mathf.MoveTowards(saleFlash, 0f, Time.unscaledDeltaTime * (reduced ? 4f : 1.6f));
            if (saleGem != null)
            {
                saleGem.gameObject.SetActive(saleFlash > 0.05f || mine.Storage > 0.2d);
                float scale = 0.12f + saleFlash * 0.18f;
                saleGem.localScale = new Vector3(scale, scale, scale);
            }
        }

        void PlaceCarts(MineState mine, MineRates rates)
        {
            float trip = Mathf.Max(1.1f, (float)rates.TripSeconds);
            int count = Mathf.Min(CartPool, mine.Carts.Count);
            for (int i = 0; i < CartPool; i++)
            {
                bool visible = i < count;
                carts[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                CartState cart = mine.Carts[i];
                float along = 0f;
                if (cart.Phase == CartPhase.Outbound)
                    along = 1f - Mathf.Clamp01((float)(cart.Timer / trip));
                else if (cart.Phase == CartPhase.Unloading)
                    along = 1f;
                else if (cart.Phase == CartPhase.Returning)
                    along = Mathf.Clamp01((float)(cart.Timer / trip));
                Vector3 pos = Vector3.Lerp(FacePoint, CrusherPoint, along);
                pos.z = (i - (count - 1) * 0.5f) * 0.22f;
                carts[i].localPosition = pos;
                float load = rates.CartCapacity <= 0d ? 0f : Mathf.Clamp01((float)(cart.Load / rates.CartCapacity));
                loads[i].localScale = new Vector3(0.16f, 0.08f + load * 0.14f, 0.12f);
                loads[i].gameObject.SetActive(cart.Load > 0.05d || cart.Phase == CartPhase.Outbound || cart.Phase == CartPhase.Unloading);
            }
        }

        void Paint(string next)
        {
            if (palette == next)
                return;
            palette = next;
            Color wall = new Color(0.28f, 0.18f, 0.13f);
            Color rock = new Color(0.55f, 0.34f, 0.2f);
            Color fog = new Color(0.2f, 0.13f, 0.1f);
            if (next == "crystal")
            {
                wall = new Color(0.28f, 0.24f, 0.4f);
                rock = new Color(0.48f, 0.42f, 0.62f);
                fog = new Color(0.16f, 0.14f, 0.24f);
            }
            else if (next == "deepcore")
            {
                wall = new Color(0.14f, 0.13f, 0.16f);
                rock = new Color(0.22f, 0.2f, 0.24f);
                fog = new Color(0.08f, 0.07f, 0.1f);
            }

            wallMat.color = wall;
            rockMat.color = rock;
            view.backgroundColor = fog;
            warmDress.SetActive(next == "warmstone" || string.IsNullOrEmpty(next));
            crystalDress.SetActive(next == "crystal");
            deepDress.SetActive(next == "deepcore");
        }

        void BuildScenery()
        {
            Box("Floor", new Vector3(0.15f, -0.08f, 0f), new Vector3(3.8f, 0.16f, 1.8f), wallMat);
            Box("BackWall", new Vector3(0.15f, 1.15f, 0.7f), new Vector3(3.8f, 2.2f, 0.18f), wallMat);
            Box("Face", new Vector3(-1.45f, 0.85f, 0f), new Vector3(0.55f, 1.5f, 1.1f), rockMat);
            facePile = Box("FaceOre", new Vector3(-1.05f, 0.2f, 0.15f), new Vector3(0.28f, 0.2f, 0.28f), rockMat);
            Box("Rail", new Vector3(-0.55f, 0.16f, 0f), new Vector3(1.15f, 0.05f, 0.08f), metalMat);

            Box("CrusherBase", new Vector3(-0.02f, 0.28f, 0.15f), new Vector3(0.42f, 0.36f, 0.42f), metalMat);
            jaw = Box("CrusherJaw", new Vector3(-0.02f, 0.58f, 0.15f), new Vector3(0.36f, 0.12f, 0.36f), metalMat);
            processPile = Box("ProcessOre", new Vector3(-0.02f, 0.42f, 0.15f), new Vector3(0.2f, 0.12f, 0.2f), rockMat);

            Box("SorterTable", new Vector3(0.58f, 0.28f, 0f), new Vector3(0.46f, 0.28f, 0.4f), woodMat);
            roughPile = Gem("RoughGems", new Vector3(0.58f, 0.5f, 0f), new Vector3(0.16f, 0.12f, 0.16f));

            Box("Vault", new Vector3(1.08f, 0.4f, 0.05f), new Vector3(0.36f, 0.7f, 0.36f), metalMat);
            vaultFill = Box("VaultFill", new Vector3(1.08f, 0.22f, 0.05f), new Vector3(0.18f, 0.12f, 0.18f), diamondMat);

            Box("SalesDesk", new Vector3(1.5f, 0.24f, -0.15f), new Vector3(0.4f, 0.28f, 0.32f), woodMat);
            saleGem = Gem("SaleGem", new Vector3(1.5f, 0.48f, -0.15f), new Vector3(0.12f, 0.12f, 0.12f));

            miner = Worker("Miner", new Vector3(-1.28f, 0.42f, -0.68f), woodMat);
            minerArm = Box("Pick", new Vector3(0.12f, 0.18f, 0f), new Vector3(0.28f, 0.05f, 0.05f), metalMat, miner);
            hauler = Worker("Hauler", new Vector3(-0.55f, 0.4f, -0.42f), metalMat);
            sorterWorker = Worker("Sorter", new Vector3(0.62f, 0.4f, 0.38f), diamondMat);
            clerk = Worker("Clerk", new Vector3(1.48f, 0.4f, -0.28f), coinMat);

            carts = new Transform[CartPool];
            loads = new Transform[CartPool];
            for (int i = 0; i < CartPool; i++)
            {
                var cartObject = new GameObject("Cart" + i);
                cartObject.transform.SetParent(transform, false);
                carts[i] = cartObject.transform;
                carts[i].localPosition = FacePoint;
                Box("CartBody", Vector3.zero, new Vector3(0.24f, 0.14f, 0.2f), woodMat, carts[i]);
                loads[i] = Box("Load" + i, new Vector3(0f, 0.12f, 0f), new Vector3(0.16f, 0.1f, 0.12f), rockMat, carts[i]);
                for (int axle = -1; axle <= 1; axle += 2)
                    for (int side = -1; side <= 1; side += 2)
                        Box("Wheel", new Vector3(axle * 0.08f, -0.09f, side * 0.11f), new Vector3(0.07f, 0.07f, 0.04f), metalMat, carts[i]);
            }

            warmDress = new GameObject("WarmDress");
            warmDress.transform.SetParent(transform, false);
            Box("BeamL", new Vector3(-0.9f, 1.15f, 0.45f), new Vector3(0.08f, 1.3f, 0.08f), woodMat, warmDress.transform);
            Box("BeamR", new Vector3(0.9f, 1.15f, 0.45f), new Vector3(0.08f, 1.3f, 0.08f), woodMat, warmDress.transform);

            crystalDress = new GameObject("CrystalDress");
            crystalDress.transform.SetParent(transform, false);
            Box("ShardA", new Vector3(-1.2f, 1.35f, 0.5f), new Vector3(0.08f, 0.28f, 0.08f), diamondMat, crystalDress.transform);
            Box("ShardB", new Vector3(0.2f, 1.45f, 0.52f), new Vector3(0.06f, 0.22f, 0.06f), diamondMat, crystalDress.transform);
            crystalDress.SetActive(false);

            deepDress = new GameObject("DeepDress");
            deepDress.transform.SetParent(transform, false);
            Box("Ember", new Vector3(1.35f, 0.08f, 0.4f), new Vector3(0.2f, 0.06f, 0.12f), emberMat, deepDress.transform);
            Box("Pillar", new Vector3(-0.7f, 1.1f, 0.48f), new Vector3(0.12f, 1.4f, 0.12f), metalMat, deepDress.transform);
            deepDress.SetActive(false);
        }

        Transform Gem(string name, Vector3 position, Vector3 scale)
        {
            if (gemMesh == null)
            {
                gemMesh = new Mesh { name = "Original faceted diamond" };
                gemMesh.vertices = new[] { new Vector3(0, 0.9f, 0), new Vector3(1, 0.1f, 0),
                    new Vector3(0, 0.1f, 1), new Vector3(-1, 0.1f, 0), new Vector3(0, 0.1f, -1), new Vector3(0, -1, 0) };
                gemMesh.subMeshCount = 8;
                int[][] faces = { new[] {0,2,1}, new[] {0,3,2}, new[] {0,4,3}, new[] {0,1,4},
                    new[] {5,1,2}, new[] {5,2,3}, new[] {5,3,4}, new[] {5,4,1} };
                gemFacets = new Material[8];
                Material bright = Make(diamondMat.shader, new Color(0.65f, 0.98f, 1f));
                Material dark = Make(diamondMat.shader, new Color(0.12f, 0.57f, 0.72f));
                for (int i = 0; i < faces.Length; i++)
                {
                    gemMesh.SetTriangles(faces[i], i);
                    gemFacets[i] = i % 3 == 0 ? bright : i % 3 == 1 ? diamondMat : dark;
                }
                gemMesh.RecalculateNormals();
                gemMesh.RecalculateBounds();
            }
            var gem = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gem.transform.SetParent(transform, false);
            gem.transform.localPosition = position;
            gem.transform.localScale = scale;
            gem.GetComponent<MeshFilter>().sharedMesh = gemMesh;
            gem.GetComponent<MeshRenderer>().sharedMaterials = gemFacets;
            return gem.transform;
        }

        Transform Worker(string name, Vector3 position, Material shirt)
        {
            var worker = new GameObject(name);
            worker.transform.SetParent(transform, false);
            worker.transform.localPosition = position;
            Transform root = worker.transform;
            Box("Shirt", Vector3.zero, new Vector3(0.17f, 0.25f, 0.13f), shirt, root);
            Box("Head", new Vector3(0f, 0.21f, 0f), new Vector3(0.14f, 0.14f, 0.14f), new Material(wallMat) { color = new Color(0.93f, 0.78f, 0.62f) }, root);
            Box("Helmet", new Vector3(0f, 0.3f, 0f), new Vector3(0.18f, 0.06f, 0.17f), coinMat, root);
            Box("LeftLeg", new Vector3(-0.05f, -0.2f, 0f), new Vector3(0.065f, 0.18f, 0.1f), metalMat, root);
            Box("RightLeg", new Vector3(0.05f, -0.2f, 0f), new Vector3(0.065f, 0.18f, 0.1f), metalMat, root);
            return root;
        }

        Transform Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            return Box(name, position, scale, material, transform);
        }

        static Transform Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go.transform;
        }

        static Material Make(Shader shader, Color color)
        {
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        static void SetHeight(Transform target, float fill, float fullHeight)
        {
            if (target == null)
                return;
            Vector3 scale = target.localScale;
            scale.y = 0.04f + fullHeight * Mathf.Clamp01(fill);
            target.localScale = scale;
        }

        static float Fill(double amount, double capacity)
        {
            if (capacity <= 0d)
                return 0f;
            return Mathf.Clamp01((float)(amount / capacity));
        }
    }
}
