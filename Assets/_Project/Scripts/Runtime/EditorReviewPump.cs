#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DiamondMine.Runtime
{
    // Absent from every player compilation, including development APKs.
    public sealed class EditorReviewPump : MonoBehaviour
    {
        public Action TickAction;
        void Update() { TickAction?.Invoke(); }
    }
}
#endif
