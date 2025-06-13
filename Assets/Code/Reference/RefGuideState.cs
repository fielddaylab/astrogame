

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
        // value used to control the first page the player opens to 
        [NonSerialized] public int StickyFirstPage = -1;
        [NonSerialized] public int CurrentPageNum;
        [NonSerialized] public Dictionary<int, List<ReferenceClassification>> SelectedRegionsPerPage = new Dictionary<int, List<ReferenceClassification>>();
        [NonSerialized] public ReferencePageList PageList;
        [NonSerialized] public BitSet32 ActivePages = new BitSet32();
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
            // All pages start enabled
            for (int i = 0; i < PageList.Pages.Count; i++) {
                ActivePages[i] = true;
            } 

            ReferenceUtility.LoadPage(0, this);

            yield return null;

            OwnedNode = ViewNavUtility.GetNodeById("Right");
            OwnedNode.OnExit.Register(() => {
                var viewState = Find.State<ViewState>();
                if (viewState.ActiveNode != null) {
                    if (viewState.ActiveNode.Id.Hash() != "GuideFocus") {
                        ReferenceUtility.SetReferenceActive(false);
                    }
                } else {
                    ReferenceUtility.SetReferenceActive(false);
                }
                IsAvailableOnNode = false;
            });

            OwnedNode.OnEnter.Register(() => {
                if (Find.State<RefGuideState>().CurrentState == RefGuideInteractionState.Open) {
                    // Enable zoom and disable minimize icon
                    ReferenceUtility.SetControlIconActive(3, true);
                    ReferenceUtility.SetControlIconActive(4, false);
                }
                IsAvailableOnNode = true;
            });

            OwnedNode = ViewNavUtility.GetNodeById("GuideFocus");
            OwnedNode.OnEnter.Register(() => {
                // Disable zoom and enable minimize icon
                ReferenceUtility.SetControlIconActive(3, false);
                ReferenceUtility.SetControlIconActive(4, true);
            });
        }

        #endregion // Registration
    }

    public enum RefGuideInteractionState {
        Closed,
        Open,
        Transitioning,
        Zoomed
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

                case RefGuideControlType.Zoom: {
                        ToggleReferenceZoom();
                    break;
                }
            }
        }

        static public void SetControlIconActive(int index, bool value, RefGuideRig rig = null) {
            if (rig == null) rig = Find.State<RefGuideRig>();
            rig.ControlIcons[index].SetActive(value);
        }

        static public void ClearControls(RefGuideState rgs)
        {
            ToggleControl(null, rgs);
            rgs.SelectedMaterials &= ~rgs.SelectedMaterials;
            rgs.SelectedRegionsPerPage.Clear();
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
                // check if we are overriding the top page
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

        static private void ToggleReferenceZoom() {
            RefGuideState guide = Find.State<RefGuideState>();
            RefGuideRig rig = Find.State<RefGuideRig>();           
            if (guide.CurrentState == RefGuideInteractionState.Transitioning) {
                return;
            }
            if (guide.CurrentState == RefGuideInteractionState.Zoomed) {
                guide.TransitionRoutine.Replace(guide, TransitionFromZoomed(guide, rig)).TryManuallyUpdate(0);
                InputUtility.SetClickableMaskDefault(Find.State<InputState>());
            } else {
                guide.TransitionRoutine.Replace(guide, TransitionToZoomed(guide, rig)).TryManuallyUpdate(0);
                InputUtility.SetClickableMaskCustom(Find.State<InputState>(), LayerMasks.ReferenceInteract_Mask);
            }
        }

        static private IEnumerator TransitionToZoomed(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);

            rig.ZoomPosition.GetPositionAndRotation(out var p, out var r);

            yield return Routine.Combine(rig.RootTransform.MoveTo(p, 0.4f).Ease(Curve.CubeInOut), rig.RootTransform.RotateQuaternionTo(r, 0.4f).Ease(Curve.CubeInOut));
            //yield return null;
            SetControlIconActive(3, true);
            SetControlIconActive(4, false);

            rig.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
            SetGuideInteraction(rig, RefGuideInteractionState.Zoomed);
            state.CurrentState = RefGuideInteractionState.Zoomed;
        }

        static private IEnumerator TransitionFromZoomed(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            rig.transform.SetParent(null, true);

            rig.OpenPosition.GetPositionAndRotation(out var p, out var r);

            yield return Routine.Combine(rig.RootTransform.MoveTo(p, 0.4f).Ease(Curve.CubeInOut), rig.RootTransform.RotateQuaternionTo(r, 0.4f).Ease(Curve.CubeInOut));
            SetGuideInteraction(rig, RefGuideInteractionState.Open);
            state.CurrentState = RefGuideInteractionState.Open;

        }

        static private IEnumerator TransitionToOpen(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);

            rig.IntermediatePosition.GetPositionAndRotation(out var p, out var r);
            rig.RootTransform.SetPositionAndRotation(p, r);
            SetGuideOpenVisibility(rig, true);

            yield return Routine.Inline(rig.RootTransform.MoveTo(rig.RootTransform.localPosition.y + 0.1f, 0.3f, Axis.Y, Space.Self).Ease(Curve.BackOut).From());

            PopulateReferenceColliders(state.CurrentPage, rig);
            RefGuideControlPage ctrlPage = Array.Find(rig.ControlPages, p => p.PageId.Equals(state.CurrentPage.AssetId));
            RefreshSelectedControls(rig, ctrlPage);

            yield return 0.15f;
            if (state.StickyFirstPage > -1) {
                LoadPage(state.StickyFirstPage, state);
            }
            yield return Tween.ZeroToOne(SetRefGuideCoverAngle, 0.45f).Ease(Curve.Smooth);
            rig.CoverRenderer.enabled = false;

            foreach (Collider collider in rig.OpenControls) {
                GameObject ctrlObject = collider.gameObject;
                RefGuideControl ctrl = ctrlObject.GetComponent<RefGuideControl>();

                bool isPageTurn = ctrl.ControlType == RefGuideControlType.NextPage || ctrl.ControlType == RefGuideControlType.PrevPage;
                if (isPageTurn && !Find.State<RefGuideState>().AllowPageChanges) continue;
            }

            foreach (GameObject icon in rig.ControlIcons) {
                icon.gameObject.SetActive(true); 
            }
            // Enable zoom and disable minimize icon
            SetControlIconActive(3, true);
            SetControlIconActive(4, false);

            SetGuideInteraction(rig, RefGuideInteractionState.Open);
            yield return rig.RootTransform.MoveTo(rig.OpenPosition.position, 0.12f).Ease(Curve.Smooth);
            state.CurrentState = RefGuideInteractionState.Open;
        }

        static private IEnumerator TransitionToClose(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            // clear data
            rig.transform.SetParent(null, true);
            state.SelectedMaterials = 0;
            state.SelectedRefClassification = null;
            TryEnableIDSubmit(false);
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            yield return rig.RootTransform.MoveTo(rig.IntermediatePosition.position, 0.12f).Ease(Curve.BackOut);

            foreach (GameObject ctrl in rig.ControlIcons) {
                ctrl.SetActive(false);
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
            // rgs.SelectedMaterials &= ~rgs.SelectedMaterials;
            PopulateContents(rig.Contents, newPage);
            PopulateReferenceColliders(newPage, rig);
            RefGuideControlPage ctrlPage = Array.Find(rig.ControlPages, p => p.PageId.Equals(newPage.AssetId));
            RefreshSelectedControls(rig, ctrlPage);
            //AdjustAllBookmarkPositions(rig, rgs.CurrentPageNum, rgs.PageList.Pages.Count - 1);
        }

        public static void LoadNextPage(RefGuideState rgs) {
            int totalPages = rgs.PageList.Pages.Count;
            int pageIdx = (rgs.CurrentPageNum + 1) % totalPages;
            // Keep incrementing till we find an active page
            while (!rgs.ActivePages[pageIdx]) {
                pageIdx = (pageIdx + 1) % totalPages;
            }
            LoadPage(pageIdx, rgs);
        }

        public static void LoadPreviousPage(RefGuideState rgs) {
            int totalPages = rgs.PageList.Pages.Count;
            int pageIdx = (rgs.CurrentPageNum - 1 + totalPages) % totalPages;
            // Keep decrementing till we find an active page
            while (!rgs.ActivePages[pageIdx]) {
                pageIdx = (pageIdx - 1 + totalPages) % totalPages;
            }
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
                            ctrlPage.Regions[i].Material = Enum.Parse<SpectrographMaterialMask>(ctrlPage.Regions[i].gameObject.name);
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
                DisableHighlights(rig, rgs);
                // rgs.SubmitButton.Root.SetActive(false);
                Routine.Start( rgs.SubmitButton.SetButtonActive(false) );

                // update selections per page
                if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification>();
                }
                else {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Clear();
                }
                return;
            }

            if (rgs.SelectedRefClassification != region.Classification) {
                rgs.SelectedRefClassification = region.Classification;

                // update selections per page
                if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification> {
                        region.Classification
                    };
                }
                else {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Clear();
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Add(region.Classification);
                }
            }
            else {
                // update selections per page
                if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification>();
                }
                else {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Clear();
                }
            }

            RefGuideControlPage page = Array.Find(rig.ControlPages, p => Array.IndexOf(p.Regions, region) != -1);

            RefreshSelectedControls(rig, page);

            TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        }

        public static void ToggleRadioControl(RefGuideControl region, RefGuideState rgs) {
            RefGuideRig rig = Find.State<RefGuideRig>();

            if (region == null) {
                rgs.SelectedRefClassification = null;
                DisableHighlights(rig, rgs);
                // rgs.SubmitButton.Root.SetActive(false);
                Routine.Start( rgs.SubmitButton.SetButtonActive(false) );

                // update selections per page
                if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification>();
                }
                else {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Clear();
                }
                return;
            }

            if (rgs.SelectedMaterials.HasFlag(region.Material)) {
                rgs.SelectedMaterials &= ~region.Material;
            } else {
                rgs.SelectedMaterials |= region.Material;
            }

            // temporarily clear selections per page
            if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification>();
            }
            else {
                rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Clear();
            }

            RefGuideControlPage page = Array.Find(rig.ControlPages, p => Array.IndexOf(p.Regions, region) != -1);

            // update selections per page
            for (int i = 0; i < page.Regions.Length; i++)
            {
                RefGuideControl r = page.Regions[i];

                if (!rgs.SelectedMaterials.HasFlag(r.Material)) continue;

                if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum] = new List<ReferenceClassification>();
                }
                else {
                    rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].Add(r.Classification);
                }
            }

            RefreshSelectedControls(rig, page);
        }

        private static void RefreshSelectedControls(RefGuideRig rig, RefGuideControlPage page)
        {
            var rgs = Find.State<RefGuideState>();

            DisableHighlights(rig, rgs);

            if (!rgs.SelectedRegionsPerPage.ContainsKey(rgs.CurrentPageNum)) {
                TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
                return;
            }

            for (int i = 0; i < page.Regions.Length; i++) {
                RefGuideControl r = page.Regions[i];

                if (rgs.SelectedRegionsPerPage[rgs.CurrentPageNum].IndexOf(r.Classification) == -1) continue;

                Collider c = r.GetComponent<Collider>();
                Bounds b = PhysicsUtils.GetLocalBounds(c);
                Vector2 off = r.transform.localPosition + r.transform.parent.localPosition;

                Transform highlight = rig.SelectionPool.GetChild(i);

                highlight.SetPosition(b.center + (Vector3)off, Axis.XY, Space.Self);
                highlight.SetScale(b.size, Axis.XY);
                highlight.gameObject.SetActive(true);

                if (r.ControlType.Equals(RefGuideControlType.MaterialClassification)) {
                    Transform checkbox = rig.CheckboxPool.GetChild(i);

                    Vector3 checkPos = new Vector3(b.center.x + 0.4f * b.size.x, b.center.y + 0.0035f, b.center.z);
                    checkbox.SetPosition(checkPos + (Vector3)off, Axis.XY, Space.Self);
                    checkbox.gameObject.SetActive(true);
                }

                // set selections data
                if (r.ControlType.Equals(RefGuideControlType.Classification)) {
                    // Classification
                    rgs.SelectedRefClassification = r.Classification;
                }
                else if (r.ControlType.Equals(RefGuideControlType.MaterialClassification)) {
                    // Materials
                    if (rgs.SelectedMaterials.HasFlag(r.Material)) {
                        rgs.SelectedMaterials &= ~r.Material;
                    }
                    else {
                        rgs.SelectedMaterials |= r.Material;
                    }
                }
            }

            TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        }

        private static void DisableHighlights(RefGuideRig rig, RefGuideState rgs) {
            for (int i = 0; i < Math.Max(rig.SelectionPool.childCount, rig.CheckboxPool.childCount); i++) {
                if (i < rig.SelectionPool.childCount) {
                    rig.SelectionPool.GetChild(i).gameObject.SetActive(false);
                }
                if (i < rig.CheckboxPool.childCount) {
                    rig.CheckboxPool.GetChild(i).gameObject.SetActive(false);
                }
            }

            // clear data
            rgs.SelectedMaterials = 0;
            rgs.SelectedRefClassification = null;
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

        [LeafMember("SetRefPaceActive")]
        private static void LeafSetRefGuideActive(int pageNum, bool active) {
            RefGuideState state = Find.State<RefGuideState>();
            state.ActivePages[pageNum] = active;
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

        [LeafMember("SetStickyFirstPage")]
        private static void LeafSetStickyFirstPage(int pageNum) {
            RefGuideState refGuideState = Find.State<RefGuideState>();
            refGuideState.StickyFirstPage = pageNum;
        }

        #endregion // Leaf
    }
}