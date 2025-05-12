using BeauUtil;
using FieldDay.Assets;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro.Reference {
    public sealed class RefGuideControlPage : MonoBehaviour {
        [AssetName(typeof(ReferencePageAsset))] public StringHash32 PageId;
        public RefGuideControl[] Regions;
        public Collider[] Colliders;

#if UNITY_EDITOR
        private void Reset() {
            Regions = GetComponentsInChildren<RefGuideControl>();
            Colliders = GetComponentsInChildren<Collider>();
        }
#endif // UNITY_EDITOR
    }

    static public partial class ReferenceUtility {
    }
}