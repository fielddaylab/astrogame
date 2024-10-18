using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    /// <summary>
    /// Build configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Build System/Build Configuration")]
    public class BuildConfig : ScriptableObject {
        public string[] BranchNamePatterns;
        public bool DevelopmentBuild;
        public ManagedStrippingLevel StrippingLevel = ManagedStrippingLevel.Medium;

        [Multiline]
        public string CustomDefines;

        public int Order;
    }
}