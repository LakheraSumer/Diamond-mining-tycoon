using System;
using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>Authored context scenes. This class changes presentation, never the simulation.</summary>
    public sealed class GameplayEnvironmentSet : MonoBehaviour
    {
        public const string Resource = "Environments/GameplayEnvironmentSet";
        [SerializeField] Transform explore = null;
        [SerializeField] Transform mine = null;
        [SerializeField] Transform factory = null;
        [SerializeField] Transform workshop = null;
        [SerializeField] Transform boutique = null;
        [SerializeField] Transform regionMap = null;
        [SerializeField] TimberBunkerArea timberArea = null;
        [SerializeField] TextMesh[] bunkerLabels = null;
        CrewActor mineActor;
        string mineCharacter = "";
        string mode = "";
        bool shown;

        public TimberBunkerArea TimberArea { get { return timberArea; } }
        public TextMesh[] BunkerLabels { get { return bunkerLabels; } }
        public Transform ExploreRoot { get { return explore; } }
        public bool InYard { get { return mode != "mine" && mode != "factory" && mode != "craft" && mode != "shop" && mode != "map"; } }

        public static GameplayEnvironmentSet Create(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(Resource);
            if (prefab == null)
            {
                Debug.LogError("Authored gameplay environments missing: Resources/" + Resource + ". Run Diamond Mine > Art > Import gameplay environments.");
                return null;
            }
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = "GameplayEnvironments_Authored";
            return instance.GetComponent<GameplayEnvironmentSet>();
        }

        public void SetMode(string next)
        {
            mode = next ?? "";
            ApplyVisibility();
        }

        public void SetShown(bool value)
        {
            shown = value;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (explore != null) explore.gameObject.SetActive(shown && InYard);
            if (mine != null) mine.gameObject.SetActive(shown && mode == "mine");
            if (factory != null) factory.gameObject.SetActive(shown && mode == "factory");
            if (workshop != null) workshop.gameObject.SetActive(shown && mode == "craft");
            if (boutique != null) boutique.gameObject.SetActive(shown && mode == "shop");
            if (regionMap != null) regionMap.gameObject.SetActive(shown && mode == "map");
        }

        public void Present(AdventureState state, bool reducedMotion)
        {
            if (state == null) return;
            if (mine != null && mode == "mine" && (mineActor == null || mineCharacter != state.SelectedCharacter))
            {
                CharacterDef person = JourneyCatalog.FindCharacter(state.SelectedCharacter) ?? JourneyCatalog.AllCharacters[0];
                if (mineActor != null) Destroy(mineActor.gameObject);
                YardArt.Crew crew = YardArt.BuildCrew(person, mine);
                mineActor = crew.Actor;
                mineCharacter = state.SelectedCharacter;
                if (crew.Root != null)
                {
                    crew.Root.name = "SelectedMiner";
                    crew.Root.localPosition = new Vector3(.50f, 0, -.60f);
                    crew.Root.localRotation = Quaternion.Euler(0, -65, 0);
                }
            }
            if (mineActor != null && mode == "mine")
                mineActor.Present(state.PoseHint == "mine" ? "mine" : state.HandsAmount > 0 ? "carry" : "idle", reducedMotion);
            foreach (CrewActor actor in GetComponentsInChildren<CrewActor>(false))
            {
                if (actor == mineActor) continue;
                bool busy = false;
                if (mode == "factory") foreach (MachineState machine in state.Machines) if (machine.BusySeconds > 0) { busy = true; break; }
                if (mode == "craft") foreach (CraftJob job in state.Jobs) if (!job.OutputGranted) { busy = true; break; }
                actor.Present(busy ? mode == "craft" ? "craft" : "machine" : "idle", reducedMotion);
            }
        }

        /// <returns>True when a dedicated context owns the camera; false for the normal yard follow camera.</returns>
        public bool Frame(Camera camera)
        {
            if (camera == null || InYard) return false;
            Vector3 origin;
            Vector3 focus;
            Vector3 offset;
            float field;
            switch (mode)
            {
                case "mine": origin = mine.position; focus = new Vector3(0, 1.3f, .1f); offset = new Vector3(3.9f, 2.4f, -6.8f); field = 40; break;
                case "factory": origin = factory.position; focus = new Vector3(0, 1.0f, .65f); offset = new Vector3(-6.6f, 7.5f, -10.7f); field = 44; break;
                case "craft": origin = workshop.position; focus = new Vector3(.05f, 1.12f, .65f); offset = new Vector3(3.9f, 3.3f, -6.2f); field = 42; break;
                case "shop": origin = boutique.position; focus = new Vector3(0, 1.20f, .6f); offset = new Vector3(-4.0f, 2.8f, -7.1f); field = 42; break;
                case "map": origin = regionMap.position; focus = new Vector3(0, .1f, 2.0f); offset = new Vector3(-7, 16.6f, -15); field = 44; break;
                default: return false;
            }
            camera.orthographic = false;
            camera.fieldOfView = field;
            camera.transform.position = origin + focus + offset;
            camera.transform.LookAt(origin + focus);
            camera.backgroundColor = mode == "mine" ? new Color(.13f, .105f, .075f) : mode == "map" ? new Color(.38f,.69f,.79f) : new Color(.70f,.79f,.76f);
            return true;
        }
    }
}
