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
        Light yardSun;
        TimberBunkerArea timberArea;
        GameplayEnvironmentSet environments;
        bool shown;
        bool cartWasRiding;
        Vector3[] haulRoute;
        Vector3 previousCartPosition;
        Transform[] cartWheels;
        bool areaFraming;
        public TimberBunkerArea TimberArea { get { return timberArea; } }
        public Transform TransportCart { get { return cart; } }
        public Transform Player { get { return player; } }

        // Also used by the art review camera. This changes presentation only.
        public void FrameTimberArea(bool enabled) { areaFraming = enabled; cameraReady = false; }
        string screenMode = "";
        public void SetScreenMode(string nextMode)
        {
            screenMode = nextMode ?? "";
            cameraReady = false;
            if (environments != null) environments.SetMode(screenMode);
            if (screenMode.Length > 0) ClearMove();
        }
        Transform player;
        CrewActor crewActor;
        string shownCharacter = "";
        string pose = "idle";
        Vector3 cameraPosition;
        bool cameraReady;
        TextMesh[] bunkerLabels;
        Transform cart;
        bool hasMove;
        Vector3 moveTarget;
        public float Facing;

        public bool HasMove { get { return hasMove; } }
        public Vector3 MoveTarget { get { return moveTarget; } }

        public void SetMoveTarget(float x, float z)
        {
            hasMove = true;
            moveTarget = new Vector3(x, 0f, z);
        }

        public void ClearMove()
        {
            hasMove = false;
        }

        public Camera View { get { return view; } }

        void Awake()
        {
            var cameraObject = new GameObject("YardCamera");
            cameraObject.transform.SetParent(transform, false);
            view = cameraObject.AddComponent<Camera>();
            view.depth = 1f;
            view.cullingMask = ~(1 << MinePresenter.WorldLayer);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.55f, 0.78f, 0.92f);
            view.fieldOfView = 42f;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 80f;
            view.enabled = false;

            var sunObject = new GameObject("YardLight");
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            yardSun = sun;
            sun.cullingMask = ~(1 << MinePresenter.WorldLayer);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.12f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .80f;
            sun.shadowBias = .035f;
            sun.shadowNormalBias = .16f;
            sun.enabled = false;
            environments = GameplayEnvironmentSet.Create(transform);
            timberArea = environments != null ? environments.TimberArea : null;
            bunkerLabels = environments != null ? environments.BunkerLabels : null;
            cart = TimberBunkerArea.CreateCart(transform);
            if (cart != null)
            {
                var wheels = new System.Collections.Generic.List<Transform>();
                foreach (Transform part in cart.GetComponentsInChildren<Transform>())
                    if (part.name.StartsWith("Wheel_",System.StringComparison.Ordinal)) wheels.Add(part);
                cartWheels = wheels.ToArray();
                cart.gameObject.SetActive(false);
            }
        }

        public void SetShown(bool shown)
        {
            this.shown = shown;
            if (view != null)
                view.enabled = shown;
            if (yardSun != null) yardSun.enabled = shown;
            if (environments != null) environments.SetShown(shown);
            if (player != null) player.gameObject.SetActive(shown && (environments == null || environments.InYard));
            if (cart != null && !shown) cart.gameObject.SetActive(false);
            if (timberArea != null) timberArea.Present(cartWasRiding, shown);
            if (shown)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.53f,.62f,.73f);
                RenderSettings.ambientEquatorColor = new Color(.40f,.42f,.40f);
                RenderSettings.ambientGroundColor = new Color(.23f,.20f,.16f);
                QualitySettings.pixelLightCount = Mathf.Max(2, QualitySettings.pixelLightCount);
            }
        }

        public void Present(GameSession session)
        {
            if (session == null || !session.Ready || session.State == null || session.State.Adventure == null)
                return;
            AdventureState adventure = session.State.Adventure;
            if (player == null || shownCharacter != (adventure.SelectedCharacter ?? ""))
                ApplyLook(adventure);
            if (player == null)
                return;
            bool inYard = environments != null && environments.InYard;
            player.gameObject.SetActive(shown && inYard);
            player.position = new Vector3((float)adventure.PlayerX, 0f, (float)adventure.PlayerZ);
            player.rotation = Quaternion.Euler(0f, Facing, 0f);
            PaintBunkers(adventure);
            PlaceCart(adventure);
            PoseInput input = BuildPose(adventure);
            pose = PuppetPose.Resolve(input);
            float dt = session.State.Settings.ReducedMotion ? 0f : Time.unscaledDeltaTime;
            Animate(dt <= 0f);
            if (environments != null) environments.Present(adventure, session.State.Settings.ReducedMotion);
            if (environments == null || !environments.Frame(view))
                FollowCamera(player.position, session.State.Settings.ReducedMotion);
            if (bunkerLabels != null)
                foreach (TextMesh label in bunkerLabels)
                    if (label != null) label.transform.rotation = view.transform.rotation;
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
            string nextCharacter = adventure.SelectedCharacter ?? "";
            CharacterDef person = JourneyCatalog.FindCharacter(nextCharacter) ?? JourneyCatalog.AllCharacters[0];
            YardArt.Crew crew = YardArt.BuildCrew(person, transform);
            if (crew.Root == null) return;
            Vector3 position = player != null ? player.position : Vector3.zero;
            float yaw = player != null ? player.eulerAngles.y : Facing;
            if (player != null) Destroy(player.gameObject);
            player = crew.Root;
            crewActor = crew.Actor;
            shownCharacter = nextCharacter;
            player.position = position;
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void Animate(bool frozen)
        {
            if (crewActor != null) crewActor.Present(pose, frozen);
        }

        void FollowCamera(Vector3 target, bool reducedMotion)
        {
            view.backgroundColor = new Color(.55f,.78f,.92f);
            // ScreenMode == "" or general Explore / Tasks / Rank / Characters
            YardPoint entrance = YardLayout.Entrance(0);
            Vector3 timberFocus = new Vector3((float)entrance.X - 0.15f, 1.1f, (float)entrance.Z - 0.28f);
            bool nearTimber = areaFraming || Vector2.Distance(new Vector2(target.x, target.z),
                new Vector2((float)entrance.X, (float)entrance.Z)) < 2.8f;
            if (nearTimber)
            {
                view.orthographic = true;
                view.orthographicSize = Mathf.Max(2.65f, 2.65f / Mathf.Max(0.3f, view.aspect));
                Vector3 position = timberFocus + new Vector3(4.8f, 3.7f, -7.2f);
                PositionCamera(position, timberFocus, reducedMotion);
                return;
            }
            view.orthographic = false;
            view.fieldOfView = 42f;
            Vector3 desiredYard = target + new Vector3(-6.5f, 9.5f, -7.2f);
            desiredYard.x = Mathf.Clamp(desiredYard.x, -6f, 6f);
            desiredYard.z = Mathf.Clamp(desiredYard.z, -8f, 12f);
            PositionCamera(desiredYard, target + new Vector3(0f, 0.8f, 0f), reducedMotion);
        }

        void PositionCamera(Vector3 desired, Vector3 lookTarget, bool reducedMotion)
        {
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
            view.transform.LookAt(lookTarget);
        }

        static string BunkerWord(int plot)
        {
            if (plot == 0)
                return "Stone";
            if (plot == 1)
                return "Copper";
            if (plot == 2)
                return "Diamond";
            if (plot == 3)
                return "Silver";
            if (plot == 4)
                return "Mixed";
            return "Buy";
        }

        void PaintBunkers(AdventureState adventure)
        {
            if (bunkerLabels == null)
                return;
            for (int plot = 0; plot < bunkerLabels.Length; plot++)
            {
                bool owned = adventure.Owned.Get(plot);
                if (bunkerLabels[plot] != null)
                {
                    string word = plot < WorldGenerator.StarterCount ? BunkerWord(plot) : (owned ? "Yours" : "Buy");
                    bunkerLabels[plot].text = (plot + 1) + "\n" + word;
                }
            }
        }

        void PlaceCart(AdventureState adventure)
        {
            if (cart == null)
                return;
            bool riding = adventure.CartLoad > 0d && !adventure.CartGranted;
            if (timberArea != null) timberArea.Present(riding && adventure.FocusPlotId == 0, shown);
            cart.gameObject.SetActive(riding && shown && environments != null && environments.InYard);
            if (!riding)
            {
                cartWasRiding = false;
                return;
            }
            if (!cartWasRiding)
            {
                BuildHaulRoute(adventure);
                previousCartPosition = RoutePoint(0f);
            }
            cartWasRiding = true;
            float span = (float)JourneyCatalog.CartTripSeconds;
            float along = span <= 0f ? 1f : 1f - Mathf.Clamp01((float)adventure.CartSeconds / span);
            Vector3 next = RoutePoint(along);
            Vector3 ahead = RoutePoint(Mathf.Min(1f,along+.005f))-next;
            if (ahead.sqrMagnitude>.00001f) cart.rotation=Quaternion.LookRotation(ahead.normalized,Vector3.up);
            float distance=Vector3.Distance(previousCartPosition,next);
            if(cartWheels!=null) foreach(Transform wheel in cartWheels)
                wheel.Rotate(cart.right,distance/.125f*Mathf.Rad2Deg,Space.World);
            cart.position = next;
            previousCartPosition = next;
        }

        void BuildHaulRoute(AdventureState adventure)
        {
            var points = new System.Collections.Generic.List<Vector3>();
            if (adventure.FocusPlotId == 0)
            {
                YardPoint door=YardLayout.Entrance(0);
                float x=(float)door.X,z=(float)door.Z;
                points.Add(new Vector3(x,0,z-.98f));
                points.Add(new Vector3(x,0,z-1.09f));
                for(int i=1;i<=12;i++)
                {
                    float a=i/12f*Mathf.PI*.5f;
                    points.Add(new Vector3(x+.85f-.85f*Mathf.Cos(a),0,z-1.09f-.85f*Mathf.Sin(a)));
                }
                points.Add(new Vector3(0,0,z-1.94f));
            }
            else points.Add(new Vector3((float)adventure.PlayerX,0,(float)adventure.PlayerZ));
            points.Add(new Vector3(0,0,.8f));
            haulRoute=points.ToArray();
        }

        Vector3 RoutePoint(float t)
        {
            if(haulRoute==null || haulRoute.Length<2) return Vector3.zero;
            float total=0;
            for(int i=1;i<haulRoute.Length;i++) total+=Vector3.Distance(haulRoute[i-1],haulRoute[i]);
            float remaining=Mathf.Clamp01(t)*total;
            for(int i=1;i<haulRoute.Length;i++)
            {
                float length=Vector3.Distance(haulRoute[i-1],haulRoute[i]);
                if(remaining<=length) return Vector3.Lerp(haulRoute[i-1],haulRoute[i],length<.0001f?1:remaining/length);
                remaining-=length;
            }
            return haulRoute[haulRoute.Length-1];
        }

    }
}
