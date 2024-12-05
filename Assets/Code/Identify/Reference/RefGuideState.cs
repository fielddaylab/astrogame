

using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public class RefGuideState : SharedStateComponent {
        public ReferenceEntry SelectedRefEntry;
        public SpriteRenderer SelectionSprite;
    }

    public static partial class ReferenceUtility {

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