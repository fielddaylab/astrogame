

using Astro.Reference;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Astro.Reference {
    public class RefGuideState : SharedStateComponent, IRegistrationCallbacks, IScenePreload {

        #region Inspector

        [Header("Game Objects")]
        public SpriteRenderer SelectionSprite;
        public SubmitButton SubmitButton;

        #endregion // Inspector

        [Header("Data")]
        [NonSerialized] public ReferenceClassification SelectedRefClassification;
        [NonSerialized] public ReferencePageAsset CurrentPage;
        [NonSerialized] public int CurrentPageNum;
        [NonSerialized] public ReferencePageList PageList;
        [NonSerialized] public bool SubmissionActive = true;

        [NonSerialized] public Routine TransitionRoutine;
        [NonSerialized] public RefGuideInteractionState CurrentState;

        #region Registration

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartOpenMode, () => {
                SubmissionActive = true;
            });
            Game.Events.Register(GameEvents.StopOpenMode, () => {
                SubmissionActive = false;
                ReferenceUtility.SelectRegion(null);
            });
        }

        public void OnDeregister(){ return; }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            PageList = Find.GlobalAsset<ReferencePageList>();
            ReferenceUtility.LoadPage(0, this);
            return null;
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
                    // TODO: classification
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
            } else {
                guide.TransitionRoutine.Replace(guide, TransitionToClose(guide, rig)).TryManuallyUpdate(0);
            }
        }

        static private IEnumerator TransitionToOpen(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            yield return rig.RootTransform.MoveAlong(rig.ClosedSpline, 0.15f).Ease(Curve.Smooth);
            rig.RootTransform.rotation = rig.OpenPosition.rotation;
            SetGuideOpenVisibility(rig, true);
            yield return rig.RootTransform.MoveAlong(rig.OpenSpline, 0.15f).Ease(Curve.Smooth).From();
            SetGuideInteraction(rig, RefGuideInteractionState.Open);
            state.CurrentState = RefGuideInteractionState.Open;
        }

        static private IEnumerator TransitionToClose(RefGuideState state, RefGuideRig rig) {
            state.CurrentState = RefGuideInteractionState.Transitioning;
            SetGuideInteraction(rig, RefGuideInteractionState.Transitioning);
            yield return rig.RootTransform.MoveAlong(rig.OpenSpline, 0.15f).Ease(Curve.Smooth);
            rig.RootTransform.rotation = rig.ClosedPosition.rotation;
            SetGuideOpenVisibility(rig, false);
            yield return rig.RootTransform.MoveAlong(rig.ClosedSpline, 0.15f).Ease(Curve.Smooth).From();
            SetGuideInteraction(rig, RefGuideInteractionState.Closed);
            state.CurrentState = RefGuideInteractionState.Closed;
        }

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
            //PopulateReferenceColliders(newPage, rgs);
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

        private static void PopulateReferenceColliders(ReferencePageAsset page, RefGuideState rgs = null) {
            if (rgs == null) {
                rgs = Find.State<RefGuideState>();
            }
            
            if (page == null) {
                ClearReferenceColliders(rgs);
                return;
            }

            //for (int i = 0; i < rgs.LeftRegions.Length; i++) {
            //    if (i < page.EntriesLeft.Length) {
            //        rgs.LeftRegions[i].ConnectedEntry = page.EntriesLeft[i];
            //    }
            //    if (i < page.EntriesRight.Length) {
            //        rgs.RightRegions[i].ConnectedEntry = page.EntriesRight[i];
            //    }
            //}
        }

        private static void ClearReferenceColliders(RefGuideState rgs) {
            //for (int i = 0; i < rgs.LeftRegions.Length; i++) {
            //    rgs.LeftRegions[i].ConnectedEntry = null;
            //    rgs.RightRegions[i].ConnectedEntry = null;
            //}
        }

        //public static void SelectRegion(RefGuideRegion region) {
        //    // Disallow new selections if we already have an object in review
        //    if (Find.State<PlayerPointsState>().SubmittedObject) {
        //        return;
        //    }

        //    RefGuideState rgs = Find.State<RefGuideState>();
        //    if (region == null) {
        //        rgs.SelectedRefClassification = null;
        //        rgs.SelectionSprite.enabled = false;
        //        return;
        //    }

        //    rgs.SelectedRefClassification = region.ConnectedEntry;

        //    if (region.PageChange == RefGuidePageChange.Previous) {
        //        LoadPreviousPage(rgs);
        //        rgs.SelectionSprite.enabled = false;
        //    } else if (region.PageChange == RefGuidePageChange.Next) {
        //        LoadNextPage(rgs);
        //        rgs.SelectionSprite.enabled = false;
        //    } else {
        //        rgs.SelectionSprite.enabled = true;
        //        rgs.SelectionSprite.transform.SetParent(region.transform, true);
        //        rgs.SelectionSprite.transform.localPosition = Vector3.zero;
        //    }

        //    TryEnableIDSubmit(Find.State<FocusState>().CurrentFocus != null);
        //}

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

        //public static void PopulateFlexibleReferenceCanvas(FlexReferencePageAsset asset, RefGuideRenderState rgrs) {
        //    if (asset.Layout == PageLayout.None) {
        //        return;
        //    }

        //    FlexPage page = rgrs.Page;
        //    switch (asset.Layout) {
        //        case PageLayout.ImageOnly: {
        //            page.Table.gameObject.SetActive(false);
        //            page.BackgroundImage.gameObject.SetActive(true);
        //            page.BackgroundImage.sprite = asset.Background;
        //            page.Body.SetTextAndActive("");
        //            page.Title.SetTextAndActive("");
        //            break;
        //        }
        //        case PageLayout.TitleBody: {
        //            page.Table.gameObject.SetActive(false);
        //            page.BackgroundImage.gameObject.SetActive(false);
        //            page.Title.SetTextAndActive(asset.TextData.TitleText);
        //            page.Body.SetTextAndActive(asset.TextData.BodyText);
        //            break;
        //        }
        //        case PageLayout.Table: {
        //            page.Table.gameObject.SetActive(true);
        //            page.BackgroundImage.gameObject.SetActive(false);
        //            page.Title.SetTextAndActive("");
        //            page.Body.SetTextAndActive("");
        //            PopulateFlexTable(asset.TableData, page);
        //            break;
        //        }
        //    }


        //}

        //public static void PopulateFlexTable(TableData data, FlexPage page) {

        //}

    }
}