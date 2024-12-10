

using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public class RefGuideState : SharedStateComponent {
        public ReferenceEntry SelectedRefEntry;
        public SpriteRenderer SelectionSprite;
        public ReferencePageAsset CurrentPage;

        public SubmitButton SubmitButton;

        [Header("Colliders")]
        public RefGuideRegion[] LeftRegions;
        public RefGuideRegion BackRegion;
        public RefGuideRegion[] RightRegions;
        public RefGuideRegion ForwardRegion;
    }

    public static partial class ReferenceUtility {

        public static void LoadPage(StringHash32 pageId, RefGuideState rgs = null) {
            if (rgs == null) {
                rgs = Find.State<RefGuideState>();
            }
            ReferencePageAsset newPage = Find.NamedAsset<ReferencePageAsset>(pageId);
            if (rgs.CurrentPage == newPage) return;
            PopulateReferenceCanvas(newPage);
            PopulateReferenceColliders(newPage, rgs);
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
            RefGuideState rgs = Find.State<RefGuideState>();
            if (region == null) {
                rgs.SelectedRefEntry = null;
                rgs.SelectionSprite.enabled = false;
                return;
            }
            rgs.SelectedRefEntry = region.ConnectedEntry;
            rgs.SelectionSprite.enabled = true;
            rgs.SelectionSprite.transform.SetParent(region.transform, true);
            rgs.SelectionSprite.transform.localPosition = Vector3.zero;

            rgs.SubmitButton.gameObject.SetActive(Find.State<FocusState>().CurrentFocus != null && !PointsUtility.ReviewInProgress());         
        }

        public static void TryEnableIDSubmit(bool focusActive) {
            RefGuideState rgs = Find.State<RefGuideState>();
            rgs.SubmitButton.gameObject.SetActive(focusActive && rgs.SelectedRefEntry != null && !PointsUtility.ReviewInProgress());
        }

        public static ReferenceEntry GetSelectedRef() {
            return Find.State<RefGuideState>().SelectedRefEntry;
        }

        public static bool RefMatchesCelestialAsset(ReferenceEntry refEntry, CelestialAsset asset) {
            return asset.ReferenceId.Equals(refEntry.AssetId);
        }

        public static bool CurrentRefMatchesFocus() {
            ReferenceEntry refEntry = Find.State<RefGuideState>().SelectedRefEntry;
            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            if (refEntry == null || focus == null) {
                return false;
            }
            return RefMatchesCelestialAsset(refEntry, focus.TargetData);

        }
    }
}