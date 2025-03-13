

using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Linq;
using UnityEngine;

namespace Astro {
    public class RefGuideState : SharedStateComponent, IRegistrationCallbacks {
        [Header("Data")]
        [NonSerialized] public ReferenceClassification SelectedRefClassification;
        [NonSerialized] public ReferencePageAsset CurrentPage;
        [NonSerialized] public int CurrentPageNum;
        [NonSerialized] public ReferencePageList PageList;
        [NonSerialized] public bool SubmissionActive = true;

        [Header("Game Objects")]
        public Transform RefGuideRoot;
        public SpriteRenderer SelectionSprite;
        public SubmitButton SubmitButton;

        [Header("Colliders")]
        public RefGuideRegion[] LeftRegions;
        public RefGuideRegion BackRegion;
        public RefGuideRegion[] RightRegions;
        public RefGuideRegion ForwardRegion;

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
    }

    public static partial class ReferenceUtility {

        public static void InitializeRefGuide(RefGuideState rgs) {
            rgs.PageList = Find.GlobalAsset<ReferencePageList>();
            LoadPage(0, rgs);
        }

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
            if (rgs.CurrentPage == newPage) return;
            if (overrideNum < 0) {
                rgs.CurrentPageNum = rgs.PageList.Pages.BinarySearch(newPage);
            } else {
                rgs.CurrentPageNum = overrideNum;
            }
            PopulateReferenceCanvas(newPage);
            PopulateReferenceColliders(newPage, rgs);
        }

        public static void LoadNextPage(RefGuideState rgs) {
            if (rgs.CurrentPageNum >= rgs.PageList.Pages.Count - 1) {
                LoadPage(0, rgs);
                return;
            }
            LoadPage(rgs.CurrentPageNum + 1, rgs);
        }

        public static void LoadPreviousPage(RefGuideState rgs) {
            if (rgs.CurrentPageNum <= 0) {
                LoadPage(rgs.PageList.Pages.Count - 1, rgs);
                return;
            }
            LoadPage(rgs.CurrentPageNum - 1, rgs);
        }

        private static void PopulateReferenceColliders(ReferencePageAsset page, RefGuideState rgs = null) {
            if (rgs == null) {
                rgs = Find.State<RefGuideState>();
            }
            
            if (page == null) {
                ClearReferenceColliders(rgs);
                return;
            }

            for (int i = 0; i < rgs.LeftRegions.Length; i++) {
                if (i < page.EntriesLeft.Length) {
                    rgs.LeftRegions[i].ConnectedEntry = page.EntriesLeft[i];
                }
                if (i < page.EntriesRight.Length) {
                    rgs.RightRegions[i].ConnectedEntry = page.EntriesRight[i];
                }
            }
        }

        private static void ClearReferenceColliders(RefGuideState rgs) {
            for (int i = 0; i < rgs.LeftRegions.Length; i++) {
                rgs.LeftRegions[i].ConnectedEntry = null;
                rgs.RightRegions[i].ConnectedEntry = null;
            }
        }

        public static void SelectRegion(RefGuideRegion region) {
            // Disallow new selections if we already have an object in review
            if (Find.State<PlayerPointsState>().SubmittedObject) {
                return;
            }

            RefGuideState rgs = Find.State<RefGuideState>();
            if (region == null) {
                rgs.SelectedRefClassification = null;
                rgs.SelectionSprite.enabled = false;
                return;
            }

            rgs.SelectedRefClassification = region.ConnectedEntry;

            if (region.PageChange == RefGuidePageChange.Previous) {
                LoadPreviousPage(rgs);
                rgs.SelectionSprite.enabled = false;
            } else if (region.PageChange == RefGuidePageChange.Next) {
                LoadNextPage(rgs);
                rgs.SelectionSprite.enabled = false;
            } else {
                rgs.SelectionSprite.enabled = true;
                rgs.SelectionSprite.transform.SetParent(region.transform, true);
                rgs.SelectionSprite.transform.localPosition = Vector3.zero;
            }

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

        public static bool RefClassMatchesAsset(ReferenceClassification refClass, CelestialAsset asset) {
            for (int i = 0; i < asset.ClassIds.Length; i++) {
                if (asset.ClassificationsCompleted[i]) {
                    // Already completed!
                    return false;
                }
                if (asset.ClassIds[i].Equals(refClass.name)) {
                    asset.ClassificationsCompleted[i] = true;
                    return true;
                }
            }
            return false;
        }

        public static bool CurrentRefMatchesFocus() {
            ReferenceClassification refClass = Find.State<RefGuideState>().SelectedRefClassification;
            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            if (refClass == null || focus == null) {
                return false;
            }
            return RefClassMatchesAsset(refClass, focus.TargetData);

        }

        public static bool CurrentRefInNeutrinoEvent()
        {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[state.DayIndex]);

            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            return day.NeutrinoEvent.RelevantObjectIds.Contains(focus.TargetData.AssetId);
        }

        public static void ToggleReferenceActive() {
            RefGuideState guide = Find.State<RefGuideState>();
            if (guide.PageList == null) {
                InitializeRefGuide(guide);
            }
            guide.RefGuideRoot.gameObject.SetActive(!guide.RefGuideRoot.gameObject.activeSelf);
        }
    }
}