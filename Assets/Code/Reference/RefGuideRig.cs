using System.Collections.Generic;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Reference {
    public sealed class RefGuideRig : SharedStateComponent, IScenePreload {
        #region Inspector

        public Transform RootTransform;
        public float PageThickness;

        [Header("Open State")]
        public MeshRenderer OpenRenderer;
        public GameObject OpenInteractables;
        public GameObject[] ControlIcons;
        public Collider[] OpenControls;
        public ActiveGroup OpenRenderers;
        public RefGuideContents Contents;
        public Collider OpenRaycastBlocker;

        [Header("Closed State")]
        public MeshRenderer CoverRenderer;
        public Transform CoverAnchor;
        public Transform CoverOffset;
        public Collider ClosedToggle;

        [Header("Pages")]
        public RefGuideBoookmarkRow TopTabs;

        [Header("Positions")]
        public Transform ClosedPosition;
        public Transform OpenPosition;
        public Transform IntermediatePosition;

        [Header("Animation Params")]
        public float CoverClosedAngle;
        public float CoverOpenAngle;
        public float CoverRingRadius;

        [Header("Lighting")]
        public Light OpenLight;

        [Header("Materials")]
        public MeshRenderer BaseMesh;
        public MeshRenderer CoverMesh;

        [HideInInspector] public Material DefaultBaseMaterial;
        [HideInInspector] public Material DefaultCoverMaterial;

        public Material HighlightMaterial;

        [Header("Selection")]
        public RefGuideControlPage[] ControlPages;
        public ActiveGroup PageControls;
        public Transform SelectionPool;
        public Transform CheckboxPool;

        private void Awake() {
            DefaultBaseMaterial = BaseMesh.material; 
            DefaultCoverMaterial = CoverMesh.material; 
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ReferenceUtility.SetGuideOpenVisibility(this, false);
            ReferenceUtility.SetGuideInteraction(this, RefGuideInteractionState.Closed);

            ClosedPosition.GetPositionAndRotation(out var p, out var r);
            RootTransform.SetPositionAndRotation(p, r);

            return null;
        }

        #endregion // Inspector
    }

    static public partial class ReferenceUtility {
        [LeafMember("SetGuideHighlight")]
        static public void LeafSetGuideHighlight(bool value = false) {
            RefGuideRig rig = Find.State<RefGuideRig>();
            
            if (value) {
                rig.BaseMesh.material = rig.HighlightMaterial;
                rig.CoverMesh.material = rig.HighlightMaterial;
            } else {
                rig.BaseMesh.material = rig.DefaultBaseMaterial;
                rig.CoverMesh.material = rig.DefaultCoverMaterial;
            }
        }
        
        static public void SetGuideOpenVisibility(RefGuideRig rig, bool isOpen)
        {
            foreach (var tab in rig.TopTabs.Bookmarks)
            {
                tab.Contents.SetActive(isOpen);
            }

            rig.OpenRenderers.SetActive(isOpen);
            rig.OpenLight.enabled = isOpen;

            if (!isOpen)
            {
                for (int i = 0; i < rig.SelectionPool.childCount; i++)
                {
                    GameObject child = rig.SelectionPool.GetChild(i).gameObject;
                    child.SetActive(false);
                }
            }

            rig.OpenRaycastBlocker.enabled = isOpen;
        }

        static public void SetGuideInteraction(RefGuideRig rig, RefGuideInteractionState state) {
            rig.ClosedToggle.enabled = state == RefGuideInteractionState.Closed;

            foreach(var tab in rig.TopTabs.Bookmarks) {
                tab.Clickable.enabled = state == RefGuideInteractionState.Open;
            }

            foreach (var control in rig.OpenControls) {
                RefGuideControl ctrl = control.gameObject.GetComponent<RefGuideControl>();

                bool isPageTurn = ctrl.ControlType == RefGuideControlType.NextPage || ctrl.ControlType == RefGuideControlType.PrevPage;
                if (isPageTurn) {
                    if (Find.State<RefGuideState>().AllowPageChanges) {
                        ctrl.gameObject.SetActive(state == RefGuideInteractionState.Open);
                    }
                    else {
                        ctrl.gameObject.SetActive(false);
                    }
                    continue;
                }

                control.enabled = state == RefGuideInteractionState.Open;
            }

            if (state != RefGuideInteractionState.Open) {
                foreach(var page in rig.ControlPages) {
                    foreach(var c in page.Colliders) {
                        c.enabled = false;
                    }
                }
            }

            rig.OpenInteractables.SetActive(state == RefGuideInteractionState.Open);
        }
    }
}