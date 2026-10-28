using System;
using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>Imported, skinned character. The authoritative game state stays in GameSession.</summary>
    public sealed class CrewActor : MonoBehaviour
    {
        [SerializeField] string characterId = "";
        [SerializeField] Animator animator = null;
        [SerializeField] Transform head = null;
        [SerializeField] Transform arm = null;
        [SerializeField] Transform socket = null;
        Renderer[] roleProps;
        Renderer[] miningTool;
        Renderer[] carryLoad;
        string shownPose = "";
        bool frozen;

        public string CharacterId { get { return characterId; } }
        public Animator Animator { get { return animator; } }
        public Transform Head { get { return head; } }
        public Transform Arm { get { return arm; } }
        public Transform ToolSocket { get { return socket; } }

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            roleProps = Renderers("RoleProp");
            miningTool = Renderers("MiningTool");
            carryLoad = Renderers("CarryLoad");
            Present("idle", false);
        }

        Renderer[] Renderers(string name)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.GetComponentsInChildren<Renderer>(true);
            return new Renderer[0];
        }

        static void SetVisible(Renderer[] renderers, bool visible)
        { if (renderers != null) foreach (Renderer renderer in renderers) renderer.enabled = visible; }

        public void Present(string pose, bool reducedMotion)
        {
            // Remote scene contexts stay inactive until opened. Their animator is
            // initialized by Awake on first activation, not by background updates.
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
            string next = string.IsNullOrEmpty(pose) ? "idle" : pose;
            string clip = char.ToUpperInvariant(next[0]) + next.Substring(1);
            if (shownPose != next || frozen != reducedMotion)
            {
                int hash = Animator.StringToHash("Base Layer." + clip);
                if (!animator.HasState(0, hash))
                {
                    Debug.LogError("Missing character animation state " + clip + " for " + characterId);
                    return;
                }
                animator.speed = reducedMotion ? 0 : 1;
                if (shownPose.Length == 0 || reducedMotion)
                { animator.Play(hash, 0, reducedMotion ? .35f : 0f); animator.Update(0); }
                else animator.CrossFadeInFixedTime(hash, .12f, 0);
                shownPose = next;
                frozen = reducedMotion;
            }
            SetVisible(miningTool, next == "mine");
            SetVisible(carryLoad, next == "carry" || next == "collect");
            SetVisible(roleProps, next == "idle" || next == "craft" || next == "machine");
        }

        public static bool TryCreate(CharacterDef person, Transform parent, out YardArt.Crew crew)
        {
            crew = default;
            if (person == null) return false;
            GameObject prefab = Resources.Load<GameObject>("Characters/" + person.Id);
            if (prefab == null) return false;
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = person.Id;
            CrewActor actor = instance.GetComponent<CrewActor>();
            if (actor == null)
            {
                Debug.LogError("Imported character prefab has no CrewActor: " + person.Id);
                Destroy(instance);
                return false;
            }
            crew = new YardArt.Crew { Root = instance.transform, Arm = actor.Arm, Head = actor.Head,
                Helmet = actor.Head, Cloth = instance.transform, ToolSocket = actor.ToolSocket, Actor = actor };
            return true;
        }
    }
}
