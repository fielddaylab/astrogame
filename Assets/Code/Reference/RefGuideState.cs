

using Astro.Reference;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Astro.Reference {
    public class RefGuideState : SharedStateComponent, IRegistrationCallbacks, IScenePreload {

        #region Inspector

        [Header("Game Objects")]
        public SubmitButton SubmitButton;

        #endregion // Inspector

        [NonSerialized] public bool IsAvailableOnNode;
        [NonSerialized] public ReferenceClassification SelectedRefClassification;
        [NonSerialized] public ReferencePageAsset CurrentPage;
        [NonSerialized] public int CurrentPageNum;
        [NonSerialized] public ReferencePageList PageList;
        [NonSerialized] public bool SubmissionActive = true;

        [NonSerialized] public Routine TransitionRoutine;
        [NonSerialized] public RefGuideInteractionState CurrentState;

        [NonSerialized] public ViewNode OwnedNode;

        #region Registration

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartOpenMode, () => {
                SubmissionActive = true;
            });
            Game.Events.Register(GameEvents.StopOpenMode, () => {
                SubmissionActive = false;
                //ReferenceUtility.SelectRegion(null);
            });
        }

        public void OnDeregister(){ return; }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            PageList = Find.GlobalAsset<ReferencePageList>();
            ReferenceUtility.LoadPage(0, this);

            yield return null;

            OwnedNode = ViewNavUtility.GetNodeById("Right");
            OwnedNode.OnExit.Register(() => {
                ReferenceUtility.SetReferenceActive(false);
                IsAvailableOnNode = false;
            });

            OwnedNode.OnEnter.Register(() => {
                IsAvailableOnNode = true;
            });
        }

        #endregion // Registration
    }

    public enum RefGuideInteractionState {
        Closed,
        Open,
        Transitioning,
    }

    public static partial class ReferenceUtility {

        #region Controls

        static public void HandleControl(RefGuideControl control) {
            // Disallow new selections if we already have an object in review
            if (Find.State<PlayerPointsState>().SubmittedObject) {
                return;
            }

            switch (control.ControlType) {
                case RefGuideControlType.PrevPage: {
                    LoadPreviousPage(Find.State<RefGuideState>());
                    break;
                }
                case RefGuideControlType.NextPage: {
                    LoadNextPage(Find.State<RefGuideState>());
                    break;
                }

                case RefGuideControlType.Bookmark: {
                    LoadPage(control.GetComponentInParent<RefGuideBookmark>().Page, Find.State<RefGuideState>());
                    break;
                }

                case RefGuideControlType.Classification: {
                    SelectControl(control);
                    break;
                }

                case RefGuideControlType.ToggleActive: {
                    ToggleReferenceActive();
                    break;
                }
            }
        }

        #endregion // Controls

        #region Transitions

        public static void ToggleReferenceActive() {
            RefGuideState guide = Find.State<RefGuideState>();
            RefGuideRig rig = Find.State<RefGuideRig>();
            if (guide.CurrentState == RefGuideInteractionState.Transitioning) {
                return;
            }

            if (guide.CurrentState == RefGuideInteractionState.Closed) {
                guide.TransitionRoutine.Replace(guide, TransitionToOpen(guide, rig)).TryManuallyUpdate(0);
                ScriptUtility.Trigger(ScriptEvents.OnRefGuideOpened);
            } else {
                guide.TransitionRoutine.Replace(guide, TransitionToClose(guide, rig)).TryManuallyUpdate(0);
                ScriptUtility.Trigger(ScriptEvents.OnRefGuideClosed);
            }
        }

        public static void SetReferenceActive(bool active) {
            RefGuideState guide = Find.State<RefGuideState>();
            RefGuideRig rig = Find.State<RefGuideRig>();
            if (guide.CurrentState == RefGuideInteractionState.Transitioning) {
                // TODO: handle interrupting the transition
                Log.Warn("[ReferenceUtility] Attempting to set ref guide state while transitioning");
                return;
            }

            if (active) {
                if (guide.CurrentState == RefGuideInteractionState.Closed) {
                    guide.TransitionRoutine.Replace(guide, TransitionToOpen(guide, rig)).TryManuallyUpdate(0);
                    ScriptUtility.Trigger(ScriptEvents.OnRefGuideOpened);
                }
            } else {
                if (guide.CurrentState == RefGuideInteractionState.Open) {
                    guide.TransitionRoutine.Replace(guide, TransitionToClose(guide, rig)).TryManuallyUpdate(0);
                    ScriptUtility.Trigger(ScriptEvents.OnRefGuideClosed);
                }
            }
        }

        static private IEnumerator TransitionToOpen(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);

            rig.IntermediatePosition.GetPositionAndRotation(out var p, out var r);
            rig.RootTransform.SetPositionAndRotation(p, r);
            SetGuideOpenVisibility(rig, true);

            yield return Routine.Inline(rig.RootTransform.MoveTo(rig.RootTransform.localPosition.y + 0.1f, 0.3f, Axis.Y, Space.Self).Ease(Curve.BackOut).From());

            yield return 0.15f;
            yield return Tween.ZeroToOne(SetRefGuideCoverAngle, 0.45f).Ease(Curve.Smooth);
            rig.CoverRenderer.enabled = false;

            yield return rig.RootTransform.MoveTo(rig.OpenPosition.position, 0.12f).Ease(Curve.Smooth);
            SetGuideInteraction(rig, RefGuideInteractionState.Open);
            state.CurrentState = RefGuideInteractionState.Open;
        }

        static private IEnumerator TransitionToClose(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            yield return rig.RootTransform.MoveTo(rig.IntermediatePosition.position, 0.12f).Ease(Curve.BackOut);

            rig.CoverRenderer.enabled = true;
            yield return Tween.OneToZero(SetRefGuideCoverAngle, 0.25f).Ease(Curve.Smooth);
            yield return 0.27f;

            rig.ClosedPosition.GetPositionAndRotation(out var p, out var r);
            rig.RootTransform.SetPositionAndRotation(p, r);
            SetGuideOpenVisibility(rig, false);

            yield return Routine.Inline(rig.RootTransform.MoveTo(rig.RootTransform.localPosition.y + 0.05f, 0.12f, Axis.Y, Space.Self).Ease(Curve.CubeOut).From().ForceOnCancel(false));
            SetGuideInteraction(rig, RefGuideInteractionState.Closed);
            state.CurrentState = RefGuideInteractionState.Closed;
        }

        static private readonly Action<float> SetRefGuideCoverAngle = (f) => {
            RefGuideRig rig = Find.State<RefGuideRig>();
            rig.CoverAnchor.SetRotation(f * 360, Axis.Y, Space.Self);

            float coverOffsetAngleRad = Mathf.Deg2Rad * Mathf.Lerp(rig.CoverClosedAngle, rig.CoverOpenAngle, f);
            rig.CoverOffset.SetPosition(new Vector3(Mathf.Cos(coverOffsetAngleRad) * rig.CoverRingRadius, 0, Mathf.Sin(coverOffsetAngleRad) * rig.CoverRingRadius), Axis.XZ, Space.Self);

            rig.CoverRenderer.enabled = f < 1;
        };

        #endregion // Transitions

        #region Page Loading

        public static void LoadPage(StringHash32 pageId, RefGuideState rgs = null) {
            if (rgs == null) {
                rgs = Find.State<RefGuideState>();
            }
            LoadPage(Find.NamedAsset<ReferencePageAsset>(pageId), rgs);
        }

        public static void LoadPage(int pageNum, RefGuideState rgs = null) {
            if (rgs == null) {
                rgs = Find.State<RefGuideState>();
            }
            if (pageNum < 0 || pageNum >= rgs.PageList.Pages.Count) {
                throw new IndexOutOfRangeException("[ReferenceUtility.LoadPage] Index out of bounds!");
            }
            LoadPage(rgs.PageList.Pages[pageNum], rgs, pageNum);
        }

        public static void LoadPage(ReferencePageAsset newPage, RefGuideState rgs, int overrideNum = -1) {
            if (rgs.CurrentPage == newPage)
                return;

            if (overrideNum < 0) {
                rgs.CurrentPageNum = rgs.PageList.Pages.IndexOf(newPage);
            } else {
                rgs.CurrentPageNum = overrideNum;
            }

            RefGuideRig rig = Find.State<RefGuideRig>();
            PopulateContents(rig.Contents, newPage);
            PopulateReferenceColliders(newPage, rig);
            SelectControl(null);
        }

        public static void LoadNextPage(RefGuideState rgs) {
            int totalPages = rgs.PageList.Pages.Count;
            int pageIdx = (rgs.CurrentPageNum + 1) % totalPages;
            LoadPage(pageIdx, rgs);
        }

        public static void LoadPreviousPage(RefGuideState rgs) {
            int totalPages = rgs.PageList.Pages.Count;
            int pageIdx = (rgs.CurrentPageNum - 1 + totalPages) % totalPages;
            LoadPage(pageIdx, rgs);
        }

        #endregion // Page Loading

        private static void PopulateReferenceColliders(ReferencePageAsset page, RefGuideRig rig) {
            if (page == null) {
                ClearReferenceColliders(rig);
                return;
            }

            foreach(var ctrlPage in rig.ControlPages) {
                if (ctrlPage.PageId == page.AssetId) {
                    for(int i = 0; i < ctrlPage.Regions.Length; i++) {
                        ctrlPage.Colliders[i].enabled = true;
                        ctrlPage.Regions[i].Classification = page.Classifications[i];
                    }
                } else {
                    foreach(var ctrl in ctrlPage.Colliders) {
                        ctrl.enabled = false;
                    }
                }
            }
        }

        private static void ClearReferenceColliders(RefGuideRig rig) {
            foreach(var page in rig.ControlPages) {
                foreach(var c in page.Colliders) {
                    c.enabled = false;
                }
            }

            rig.SelectionGraphic.gameObject.SetActive(false);
        }

        public static void SelectControl(RefGuideControl region) {
            // Disallow new selections if we already have an object in review
            if (Find.State<PlayerPointsState>().SubmittedObject) {
                return;
            }

            RefGuideState rgs = Find.State<RefGuideState>();
            RefGuideRig rig = Find.State<RefGuideRig>();

            if (region == null) {
                rgs.SelectedRefClassification = null;
                rig.SelectionGraphic.gameObject.SetActive(false);
                rgs.SubmitButton.gameObject.SetActive(false);
                return;
            }

            rgs.SelectedRefClassification = region.Classification;

            Collider c = region.GetComponent<Collider>();
            Bounds b = PhysicsUtils.GetLocalBounds(c);
            Vector2 off = region.transform.localPosition;

            rig.SelectionGraphic.SetPosition(b.center + (Vector3) off, Axis.XY, Space.Self);
            rig.SelectionGraphic.SetScale(b.size, Axis.XY);
            rig.SelectionGraphic.gameObject.SetActive(true);

            TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        }

        public static void TryEnableIDSubmit(bool focusActive) {
            RefGuideState rgs = Find.State<RefGuideState>();

            if (!rgs.SubmissionActive) return;

            rgs.SubmitButton.gameObject.SetActive(focusActive && rgs.SelectedRefClassification != null && !PointsUtility.ReviewInProgress());
        }

        public static ReferenceClassification GetSelectedRef() {
            return Find.State<RefGuideState>().SelectedRefClassification;
        }

        public static bool RefEntryMatchesAsset(ReferenceEntry refEntry, CelestialAsset asset) {
            return asset.ReferenceId.Equals(refEntry.AssetId);
        }

        public static bool RefClassMatchesAsset(ReferenceClassification refClass, CelestialAsset asset, PlayerProgressState progress) {
            progress.Classifications.TryGetValue(asset.AssetId, out BitSet32 completed);
            for (int i = 0; i < asset.ClassIds.Length; i++) {
                if (completed[i]) {
                    // Already completed!
                    return false;
                }
                if (asset.ClassIds[i].Equals(refClass.name)) {
                    completed[i] = true;
                    progress.Classifications[asset.AssetId] = completed;
                    return true;
                }
            }
            return false;
        }

        public static bool CurrentRefMatchesFocus() {
            ReferenceClassification refClass = Find.State<RefGuideState>().SelectedRefClassification;
            PlayerProgressState progress = Find.State<PlayerProgressState>();
            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            if (refClass == null || focus == null) {
                return false;
            }
            return RefClassMatchesAsset(refClass, focus.TargetData, progress);

        }

        public static bool CurrentRefInNeutrinoEvent()
        {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[state.DayIndex]);

            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            return day.NeutrinoEvent.RelevantObjectIds.Contains(focus.TargetData.AssetId);
        }

        public static bool AssetSubmissionCompleted() {
            ReferenceClassification refClass = Find.State<RefGuideState>().SelectedRefClassification;
            PlayerProgressState progress = Find.State<PlayerProgressState>();
            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            if (refClass == null || focus == null) {
                return false;
            }
            progress.Classifications.TryGetValue(focus.TargetData.AssetId, out BitSet32 completed);
            for (int i = 0; i < focus.TargetData.ClassIds.Length; i++) {
                if (completed[i]) {
                    return true;
                }
            }
            return false;
        }

        [LeafMember("SetRefGuideActive")]
        private static void LeafSetRefGuideActive(bool active) {
            SetReferenceActive(active);
        }
    }
}