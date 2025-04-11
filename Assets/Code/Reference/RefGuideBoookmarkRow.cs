using FieldDay.SharedState;
using UnityEngine;

namespace Astro.Reference {
    public sealed class RefGuideBoookmarkRow : MonoBehaviour {
        public RefGuideBookmark[] Bookmarks;

#if UNITY_EDITOR
        private void Reset() {
            Bookmarks = GetComponentsInChildren<RefGuideBookmark>();
        }
#endif // UNITY_EDITOR
    }

    static public partial class ReferenceUtility {

        static public void AdjustAllBookmarkPositions(RefGuideRig rig, int currentPage, int maxPages) {
            AdjustBookmarkPositions(rig.TopTabs.Bookmarks, rig.PageThickness, currentPage, maxPages);
            AdjustBookmarkPositions(rig.RightTabs.Bookmarks, rig.PageThickness, currentPage, maxPages);
        }

        static public void AdjustBookmarkPositions(RefGuideBookmark[] bookmarks, float pageThickness, int currentPage, int maxPages) {
            for(int i = 0; i < bookmarks.Length; i++) {
                RefGuideBookmark bookmark = bookmarks[i];
                int adjustedPageNum = (bookmark.CachedAbsolutePageNum - currentPage + maxPages) % maxPages;
                Vector3 local = bookmark.Group.localPosition;
                local.z = adjustedPageNum * pageThickness;
                bookmark.Group.localPosition = local;
            }
        }
    }
}