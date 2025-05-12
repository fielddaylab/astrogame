using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Rendering {
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed class CameraOverlayLayer : MonoBehaviour {
        public const short DefaultSortingOrder = short.MaxValue * 3 / 4;
        public const float DefaultPlaneDistance = 3;
        
        [NonSerialized] public Canvas Canvas;

        #region Setup

        public void SetupGlobal() {
            SetupGlobal(DefaultSortingOrder);
        }

        public void SetupGlobal(short sortingOrder) {
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = sortingOrder;
            Canvas.worldCamera = null;
            DontDestroyOnLoad(gameObject);
        }

        public void SetupTarget(Camera target) {
            SetupTarget(target, DefaultPlaneDistance, 0, DefaultSortingOrder);
        }

        public void SetupTarget(Camera target, short sortingOrder) {
            SetupTarget(target, DefaultPlaneDistance, 0, sortingOrder);
        }

        public void SetupTarget(Camera target, int sortingLayerId, short sortingOrder) {
            SetupTarget(target, DefaultPlaneDistance, sortingLayerId, sortingOrder);
        }

        public void SetupTarget(Camera target, float planeDistance, int sortingLayerId, short sortingOrder) {
            Assert.NotNull(target);
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.sortingLayerID = sortingLayerId;
            Canvas.sortingOrder = sortingOrder;
            Canvas.worldCamera = target;
            Canvas.planeDistance = planeDistance;
            SceneManager.MoveGameObjectToScene(gameObject, target.scene);
        }

        #endregion // Setup

        public void AddChild(Transform child) {
            child.SetParent(transform, false);
        }

        #region Events

        private void Awake() {
            this.CacheComponent(ref Canvas);
        }

        #endregion // Events

        #region Construction

        static private readonly Type[] CreateComponentTypes = new Type[] {
            typeof(Canvas), typeof(CameraOverlayLayer)
        };

        static public CameraOverlayLayer Create() {
            GameObject go = new GameObject("OverlayLayer", CreateComponentTypes);
            return go.GetComponent<CameraOverlayLayer>();
        }

        #endregion // Construction
    }
}