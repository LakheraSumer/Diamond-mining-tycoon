using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Local yard for the starter bunkers. Distant plots stay as data until a page is opened.
    /// </summary>
    public sealed class YardWorld : MonoBehaviour
    {
        Camera view;
        Transform player;
        Transform arm;
        Transform toolSocket;
        Transform helmet;
        Transform cloth;
        Transform accent;
        Transform head;
        Renderer[] accessoryRenderers;
        string shownCharacter = "";
        string pose = "idle";
        float poseTime;
        Vector3 cameraPosition;
        bool cameraReady;

        public Camera View { get { return view; } }

        void Awake()
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var cameraObject = new GameObject("YardCamera");
            cameraObject.transform.SetParent(transform, false);
            view = cameraObject.AddComponent<Camera>();
            view.depth = 1f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.45f, 0.62f, 0.42f);
            view.fieldOfView = 42f;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 80f;
            view.enabled = false;

            var sunObject = new GameObject("YardLight");
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.None;

            BuildBlock(shader, "Ground", new Vector3(0f, -0.2f, 6f), new Vector3(18f, 0.4f, 24f), new Color(0.55f, 0.62f, 0.38f));
            BuildBlock(shader, "Factory", new Vector3(0f, 0.7f, 0.8f), new Vector3(2.2f, 1.4f, 1.6f), new Color(0.35f, 0.4f, 0.48f));
            BuildBlock(shader, "Shop", new Vector3(4.2f, 0.55f, -1.2f), new Vector3(1.6f, 1.1f, 1.4f), new Color(0.62f, 0.45f, 0.32f));
            BuildBlock(shader, "Gate", new Vector3(-6.2f, 0.8f, -1.2f), new Vector3(0.4f, 1.6f, 1.8f), new Color(0.85f, 0.72f, 0.35f));
            for (int i = 0; i < YardLayout.RockCount; i++)
            {
                YardPoint rock = YardLayout.Rock(i);
                BuildBlock(shader, "Rock", new Vector3((float)rock.X, 0.35f, (float)rock.Z), new Vector3(0.9f, 0.7f, 0.9f), new Color(0.45f, 0.42f, 0.4f));
            }
            for (int plot = 0; plot < WorldGenerator.VisibleYardPlots; plot++)
            {
                YardPoint door = YardLayout.Entrance(plot);
                Color color = plot < WorldGenerator.StarterCount
                    ? new Color(0.2f, 0.55f, 0.52f)
                    : new Color(0.35f, 0.32f, 0.3f);
                BuildBlock(shader, "Bunker" + plot, new Vector3((float)door.X, 0.55f, (float)door.Z), new Vector3(1.1f, 1.1f, 1.1f), color);
            }
            BuildPlayer(shader);
        }

        public void SetShown(bool shown)
        {
            if (view != null)
                view.enabled = shown;
        }

        public void Present(GameSession session)
        {
            if (session == null || !session.Ready || session.State == null || session.State.Adventure == null || player == null)
                return;
            AdventureState adventure = session.State.Adventure;
            player.position = new Vector3((float)adventure.PlayerX, 0f, (float)adventure.PlayerZ);
            ApplyLook(adventure);
            PoseInput input = BuildPose(adventure);
            pose = PuppetPose.Resolve(input);
            float dt = session.State.Settings.ReducedMotion ? 0f : Time.unscaledDeltaTime;
            poseTime += dt;
            Animate(dt <= 0f);
            FollowCamera(player.position, session.State.Settings.ReducedMotion);
        }

        static PoseInput BuildPose(AdventureState adventure)
        {
            return new PoseInput
            {
                Moving = adventure.PoseHint == "move",
                Sprint = adventure.PoseHint == "run",
                Carrying = adventure.HandsAmount > 0d,
                Mining = adventure.PoseHint == "mine",
                Collecting = adventure.PoseHint == "collect",
                UsingMachine = adventure.PoseHint == "machine",
                Crafting = adventure.PoseHint == "craft" || HasOpenJob(adventure),
                CelebrateSeconds = adventure.PoseCelebrate,
                WaveSeconds = adventure.PoseWave
            };
        }

        static bool HasOpenJob(AdventureState adventure)
        {
            for (int i = 0; i < adventure.Jobs.Count; i++)
            {
                if (!adventure.Jobs[i].OutputGranted)
                    return true;
            }
            return false;
        }

        void ApplyLook(AdventureState adventure)
        {
            if (shownCharacter == adventure.SelectedCharacter)
                return;
            shownCharacter = adventure.SelectedCharacter ?? "";
            CharacterDef person = JourneyCatalog.FindCharacter(shownCharacter);
            if (person == null)
                person = JourneyCatalog.AllCharacters[0];
            Paint(helmet, person.Helmet);
            Paint(cloth, person.Cloth);
            Paint(head, person.Skin);
            Paint(accent, person.Accent);
            if (accessoryRenderers != null)
            {
                for (int i = 0; i < accessoryRenderers.Length; i++)
                    accessoryRenderers[i].enabled = accessoryRenderers[i].gameObject.name == person.Accessory;
            }
        }

        void Animate(bool frozen)
        {
            float swing = frozen ? 0f : Mathf.Sin(poseTime * 6f) * 25f;
            if (arm == null)
                return;
            if (pose == "mine" || pose == "machine" || pose == "craft")
                arm.localRotation = Quaternion.Euler(frozen ? -40f : -40f + swing, 0f, 0f);
            else if (pose == "walk" || pose == "run" || pose == "carry")
                arm.localRotation = Quaternion.Euler(frozen ? 0f : swing * 0.4f, 0f, 0f);
            else if (pose == "wave" || pose == "celebrate")
                arm.localRotation = Quaternion.Euler(-120f, 0f, frozen ? 20f : 20f + swing);
            else
                arm.localRotation = Quaternion.identity;
        }

        void FollowCamera(Vector3 target, bool reducedMotion)
        {
            Vector3 desired = target + new Vector3(-4.2f, 8.4f, -5.4f);
            desired.x = Mathf.Clamp(desired.x, -6f, 6f);
            desired.z = Mathf.Clamp(desired.z, -8f, 12f);
            if (!cameraReady || reducedMotion)
            {
                cameraPosition = desired;
                cameraReady = true;
            }
            else
            {
                cameraPosition = Vector3.Lerp(cameraPosition, desired, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            }
            view.transform.position = cameraPosition;
            view.transform.LookAt(target + new Vector3(0f, 0.8f, 0f));
        }

        void BuildPlayer(Shader shader)
        {
            player = BuildBlock(shader, "Miner", Vector3.zero, new Vector3(0.55f, 0.9f, 0.4f), new Color(0.3f, 0.45f, 0.55f)).transform;
            cloth = player;
            head = BuildBlock(shader, "Head", new Vector3(0f, 0.72f, 0f), new Vector3(0.38f, 0.38f, 0.38f), new Color(0.8f, 0.64f, 0.45f)).transform;
            head.SetParent(player, false);
            helmet = BuildBlock(shader, "Helmet", new Vector3(0f, 0.22f, 0f), new Vector3(0.46f, 0.22f, 0.46f), Color.cyan).transform;
            helmet.SetParent(head, false);
            arm = BuildBlock(shader, "Arm", new Vector3(0.38f, 0.15f, 0.1f), new Vector3(0.16f, 0.45f, 0.16f), new Color(0.8f, 0.64f, 0.45f)).transform;
            arm.SetParent(player, false);
            toolSocket = new GameObject("ToolSocket").transform;
            toolSocket.SetParent(arm, false);
            toolSocket.localPosition = new Vector3(0f, -0.28f, 0.1f);
            accent = BuildBlock(shader, "Tool", Vector3.zero, new Vector3(0.12f, 0.28f, 0.12f), Color.gray).transform;
            accent.SetParent(toolSocket, false);
            string[] names = { "scarf", "pouch", "pads", "tubes", "gloves", "goggles", "tray", "glasses", "cloth", "loop", "badge", "cart", "blueprint" };
            accessoryRenderers = new Renderer[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                GameObject piece = BuildBlock(shader, names[i], new Vector3(0f, 0.2f, 0.28f), new Vector3(0.22f, 0.12f, 0.08f), Color.white);
                piece.transform.SetParent(player, false);
                accessoryRenderers[i] = piece.GetComponent<Renderer>();
                accessoryRenderers[i].enabled = false;
            }
        }

        static void Paint(Transform target, int hex)
        {
            if (target == null)
                return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Hex(hex);
        }

        static Color Hex(int hex)
        {
            return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);
        }

        static GameObject BuildBlock(Shader shader, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            Collider collider = block.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            block.transform.position = position;
            block.transform.localScale = scale;
            Renderer renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(shader);
            renderer.sharedMaterial.color = color;
            return block;
        }
    }
}
