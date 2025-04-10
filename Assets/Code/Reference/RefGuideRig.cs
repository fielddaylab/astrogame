using System.Collections.Generic;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro.Reference {
    public sealed class RefGuideRig : SharedStateComponent, IScenePreload {
        #region Inspector

        public Transform RootTransform;
        public float PageThickness;

        [Header("Open State")]
        public MeshRenderer OpenRenderer;
        public GameObject OpenInteractables;
        public Collider[] OpenControls;
        public ActiveGroup OpenRenderers;
        public RefGuideContents Contents;

        [Header("Closed State")]
        public MeshRenderer CoverRenderer;
        public Transform CoverAnchor;
        public Transform CoverOffset;
        public Collider ClosedToggle;

        [Header("Pages")]
        public RefGuideBoookmarkRow TopTabs;
        public RefGuideBoookmarkRow RightTabs;

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
        static public void SetGuideOpenVisibility(RefGuideRig rig, bool isOpen) {
            foreach(var tab in rig.TopTabs.Bookmarks) {
                tab.Contents.SetActive(isOpen);
            }
            foreach (var tab in rig.RightTabs.Bookmarks) {
                tab.Contents.SetActive(isOpen);
            }

            rig.OpenRenderers.SetActive(isOpen);
            rig.OpenLight.enabled = isOpen;
        }

        static public void SetGuideInteraction(RefGuideRig rig, RefGuideInteractionState state) {
            rig.ClosedToggle.enabled = state == RefGuideInteractionState.Closed;

            foreach(var tab in rig.TopTabs.Bookmarks) {
                tab.Clickable.enabled = state == RefGuideInteractionState.Open;
            }

            foreach (var tab in rig.RightTabs.Bookmarks) {
                tab.Clickable.enabled = state == RefGuideInteractionState.Open;
            }

            foreach(var control in rig.OpenControls) {
                control.enabled = state == RefGuideInteractionState.Open;
            }

            rig.OpenInteractables.SetActive(state == RefGuideInteractionState.Open);
        }
    }
}