

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
using UnityEngine;

namespace Astro.Reference {
    public class RefGuideState : SharedStateComponent, IRegistrationCallbacks, IScenePreload {

        #region Inspector

        [Header("Game Objects")]
        public SubmitButton SubmitButton;

        #endregion // Inspector

        [NonSerialized] public bool IsAvailableOnNode;
        [NonSerialized] public ReferenceClassification SelectedRefClassification;
        [NonSerialized] public SpectrographMaterialMask SelectedMaterials;
        [NonSerialized] public ReferencePageAsset CurrentPage;
        [NonSerialized] public int CurrentPageNum;
        [NonSerialized] public ReferencePageList PageList;
        [NonSerialized] public bool SubmissionActive = true;

        [NonSerialized] public bool AllowPageChanges = true;

        [NonSerialized] public Routine TransitionRoutine;
        [NonSerialized] public RefGuideInteractionState CurrentState;

        [NonSerialized] public ViewNode OwnedNode;

        #region Registration

        private Action m_EnableSubmission;
        private Action m_DisableSubmission;

        public void OnRegister() {
            m_EnableSubmission = () => { SubmissionActive = true; };
            m_DisableSubmission = () => { SubmissionActive = false; };

            Game.Events.Register(GameEvents.StartOpenMode, m_EnableSubmission);
            Game.Events.Register(GameEvents.StopOpenMode, m_DisableSubmission);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartOpenMode, m_EnableSubmission);
            Game.Events.Deregister(GameEvents.StopOpenMode, m_DisableSubmission);
        }

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
            var state = Find.State<RefGuideState>();

            switch (control.ControlType) {
                case RefGuideControlType.PrevPage: {
                    if (state.AllowPageChanges) {
                        LoadPreviousPage(state);
                    }
                    break;
                }
                case RefGuideControlType.NextPage: {
                    if (state.AllowPageChanges) {
                        LoadNextPage(state);
                    }
                    break;
                }

                case RefGuideControlType.Bookmark: {
                    if (state.AllowPageChanges) {
                        LoadPage(control.GetComponentInParent<RefGuideBookmark>().Page, state);
                    }
                    break;
                }

                case RefGuideControlType.Classification: {
                    ToggleControl(control, state);
                    break;
                }

                case RefGuideControlType.MaterialClassification: {
                    ToggleRadioControl(control, state);
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

            PopulateReferenceColliders(state.CurrentPage, rig);

            yield return 0.15f;
            yield return Tween.ZeroToOne(SetRefGuideCoverAngle, 0.45f).Ease(Curve.Smooth);
            rig.CoverRenderer.enabled = false;

            foreach (Collider collider in rig.OpenControls) {
                GameObject ctrlObject = collider.gameObject;
                RefGuideControl ctrl = ctrlObject.GetComponent<RefGuideControl>();

                bool isPageTurn = ctrl.ControlType == RefGuideControlType.NextPage || ctrl.ControlType == RefGuideControlType.PrevPage;
                if (isPageTurn && !Find.State<RefGuideState>().AllowPageChanges) continue;
                ctrl.gameObject.SetActive(true);
            }

            yield return rig.RootTransform.MoveTo(rig.OpenPosition.position, 0.12f).Ease(Curve.Smooth);
            SetGuideInteraction(rig, RefGuideInteractionState.Open);
            state.CurrentState = RefGuideInteractionState.Open;
        }

        static private IEnumerator TransitionToClose(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            yield return rig.RootTransform.MoveTo(rig.IntermediatePosition.position, 0.12f).Ease(Curve.BackOut);

            foreach (Collider ctrl in rig.OpenControls) {
                ctrl.gameObject.SetActive(false);
            }

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
            rgs.CurrentPage = newPage;
            rgs.SelectedMaterials &= ~rgs.SelectedMaterials;
            PopulateContents(rig.Contents, newPage);
            PopulateReferenceColliders(newPage, rig);
            ToggleControl(null, rgs);
            //AdjustAllBookmarkPositions(rig, rgs.CurrentPageNum, rgs.PageList.Pages.Count - 1);
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

        static public void SetPageTurnLocked(bool locked) {
            var state = Find.State<RefGuideState>();
            var rig = Find.State<RefGuideRig>();

            state.AllowPageChanges = !locked;
            rig.PageControls.SetActive(!locked);
        }

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
                        if (ctrlPage.Regions[i].ControlType == RefGuideControlType.MaterialClassification) {
                            ctrlPage.Regions[i].Material = Enum.Parse<SpectrographMaterialMask>(page.Classifications[i].AssetId.ToDebugString());
                        }
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

            for (int i = 0; i < rig.SelectionPool.childCount; i++) {
                GameObject child = rig.SelectionPool.GetChild(i).gameObject;
                child.SetActive(false);
            }
        }

        public static void ToggleControl(RefGuideControl region, RefGuideState rgs) {
            RefGuideRig rig = Find.State<RefGuideRig>();

            if (region == null) {
                rgs.SelectedRefClassification = null;
                for (int i = 0; i < rig.SelectionPool.childCount; i++) {
                    GameObject child = rig.SelectionPool.GetChild(i).gameObject;
                    child.SetActive(false);
                }
                // rgs.SubmitButton.Root.SetActive(false);
                Routine.Start( rgs.SubmitButton.SetButtonActive(false) );
                return;
            }

            if (rgs.SelectedRefClassification != region.Classification) {
                rgs.SelectedRefClassification = region.Classification;

                Collider c = region.GetComponent<Collider>();
                Bounds b = PhysicsUtils.GetLocalBounds(c);
                Vector2 off = region.transform.localPosition + region.transform.parent.localPosition;

                Transform highlight = rig.SelectionPool.GetChild(0);

                highlight.SetPosition(b.center + (Vector3) off, Axis.XY, Space.Self);
                highlight.SetScale(b.size, Axis.XY);
                highlight.gameObject.SetActive(true);
            } else {
                rgs.SelectedRefClassification = null;
                Transform highlight = rig.SelectionPool.GetChild(0);
                highlight.gameObject.SetActive(false);
            }

            TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        }

        public static void ToggleRadioControl(RefGuideControl region, RefGuideState rgs) {
            RefGuideRig rig = Find.State<RefGuideRig>();

            if (region == null) {
                rgs.SelectedRefClassification = null;
                for (int i = 0; i < rig.SelectionPool.childCount; i++) {
                    GameObject child = rig.SelectionPool.GetChild(i).gameObject;
                    child.SetActive(false);
                }
                // rgs.SubmitButton.Root.SetActive(false);
                Routine.Start( rgs.SubmitButton.SetButtonActive(false) );
                return;
            }

            if (rgs.SelectedMaterials.HasFlag(region.Material)) {
                rgs.SelectedMaterials &= ~region.Material;
            } else {
                rgs.SelectedMaterials |= region.Material;
            }

            RefGuideControlPage page = Array.Find(rig.ControlPages, p => Array.IndexOf(p.Regions, region) != -1);

            // Update highlights
            for (int i = 0; i < rig.SelectionPool.childCount; i++) {
                GameObject child = rig.SelectionPool.GetChild(i).gameObject;
                child.SetActive(false);
            }
            for (int i = 0; i < page.Regions.Length; i++) {
                RefGuideControl r = page.Regions[i];

                if (!rgs.SelectedMaterials.HasFlag(r.Material)) continue;

                Collider c = r.GetComponent<Collider>();
                Bounds b = PhysicsUtils.GetLocalBounds(c);
                Vector2 off = r.transform.localPosition + r.transform.parent.localPosition;

                Transform highlight = rig.SelectionPool.GetChild(i);

                highlight.SetPosition(b.center + (Vector3) off, Axis.XY, Space.Self);
                highlight.SetScale(b.size, Axis.XY);
                highlight.gameObject.SetActive(true);
            }

            TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        }

        public static void TryEnableIDSubmit(bool focusActive) {
            RefGuideState rgs = Find.State<RefGuideState>();

            if (!rgs.SubmissionActive) return;

            bool refGuideselection = rgs.SelectedRefClassification != null || rgs.SelectedMaterials != 0;

            bool buttonActive = focusActive && refGuideselection && !ReviewUtility.ReviewInProgress();
            Routine.Start( rgs.SubmitButton.SetButtonActive(buttonActive) );
        }

        #region Leaf

        [LeafMember("SetRefGuideActive")]
        private static void LeafSetRefGuideActive(bool active) {
            SetReferenceActive(active);
        }

        [LeafMember("SetRefGuidePagesLocked")]
        private static void LeafSetRefGuidePagesLocked(bool locked) {
            SetPageTurnLocked(locked);
        }

        [LeafMember("SetRefGuidePage")]
        private static void LeafSetRefGuidePage(StringHash32 pageId) {
            LoadPage(pageId);
        }

        [LeafMember("OpenRefGuidePage")]
        private static void LeafOpenRefGuidePage(StringHash32 pageId) {
            SetReferenceActive(true);
            LoadPage(pageId);
        }

        #endregion // Leaf
    }
}