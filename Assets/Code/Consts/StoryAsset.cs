using UnityEngine;
using FieldDay.Assets;
using BeauUtil;
using FieldDay.Data;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/StoryAsset")]
    public sealed class StoryAsset : GlobalAsset, IEditorOnlyData {
        [AssetName(typeof(DayConfigAsset))] public StringHash32[] Days;
        public DayConfigAsset DEBUG_SandboxDay;

#if UNITY_EDITOR
        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            if (!isDevelopmentBuild) {
                DEBUG_SandboxDay = null;
            }
        }
#endif // UNITY_EDITOR
    }
}