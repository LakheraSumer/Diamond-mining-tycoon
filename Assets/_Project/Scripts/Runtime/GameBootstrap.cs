using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Scene entry. Builds the session, mine view, audio, and interface in code
    /// so the player does not depend on a hand-placed prefab.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Ensure()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null)
                return;
            var root = new GameObject("Game");
            root.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            var session = gameObject.AddComponent<GameSession>();
            session.Configure(new SystemGameClock());
            var audio = gameObject.AddComponent<GameAudio>();
            var worldObject = new GameObject("MineWorld");
            worldObject.transform.SetParent(transform, false);
            var world = worldObject.AddComponent<MinePresenter>();
            var ui = gameObject.AddComponent<GameUi>();
            ui.Bind(session, audio, world);
        }
    }
}
