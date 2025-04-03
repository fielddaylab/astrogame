using System;
using BeauPools;
using UnityEngine;

namespace FieldDay.Rendering {
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed class CameraOverlayLayer : MonoBehaviour {
        [NonSerialized] public Canvas Canvas;

        #region Construction

        static private readonly Type[] CreateComponentTypes = new Type[] {
            typeof(Canvas), typeof(CameraOverlayLayer)
        };

        static public CameraOverlayLayer Create() {
            GameObject go = new GameObject("OverlayLayer", CreateComponentTypes);
            DontDestroyOnLoad(go);
            return go.GetComponent<CameraOverlayLayer>();
        }

        #endregion // Construction
    }
}