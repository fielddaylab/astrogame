

using FieldDay;
using FieldDay.SharedState;

namespace Astro {
    public class RefGuideState : SharedStateComponent {
        public ReferenceEntry SelectedRefEntry;

    }

    public static partial class ReferenceUtility {
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