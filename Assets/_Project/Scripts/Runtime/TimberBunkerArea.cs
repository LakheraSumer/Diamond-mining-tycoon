using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>Imported, authored vertical slice for plot 0. Never builds fallback geometry.</summary>
    public sealed class TimberBunkerArea : MonoBehaviour
    {
        public const string AreaResource = "Yard/TimberBunker";
        public const string CartResource = "Yard/OreCart";
        [SerializeField] Transform parkedCart = null;
        [SerializeField] Light lantern = null;

        public Transform ParkedCart { get { return parkedCart; } }

        public static TimberBunkerArea Create(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(AreaResource);
            if (prefab == null)
            {
                Debug.LogError("Timber bunker prefab missing: Resources/" + AreaResource +
                    ". Run Diamond Mine > Art > Import timber bunker after exporting the Blender assets.");
                return null;
            }
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = "TimberBunker_01_Authored";
            YardPoint point = YardLayout.Entrance(0);
            instance.transform.localPosition = new Vector3((float)point.X, 0f, (float)point.Z);
            return instance.GetComponent<TimberBunkerArea>();
        }

        public static Transform CreateCart(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(CartResource);
            if (prefab == null)
            {
                Debug.LogError("Imported ore cart prefab missing: Resources/" + CartResource);
                return null;
            }
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = "HaulCart_Authored";
            return instance.transform;
        }

        public void Present(bool haulingFromThisBunker, bool shown)
        {
            if (parkedCart != null) parkedCart.gameObject.SetActive(!haulingFromThisBunker);
            if (lantern != null) lantern.enabled = shown;
        }

    }
}
