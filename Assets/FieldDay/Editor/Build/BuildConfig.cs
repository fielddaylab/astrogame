using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    /// <summary>
    /// Build configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Build Configuration", order = -260)]
    public class BuildConfig : ScriptableObject {
        public string[] BranchNamePatterns;
        public int Order;

        [Header("Settings")]
        public bool DevelopmentBuild;
        [Multiline]
        public string CustomDefines;

        [Header("Optimizations")]
        public ManagedStrippingLevel StrippingLevel = ManagedStrippingLevel.Medium;
        public BuildConfigurations.CodeOptimizationFlags OptimizationFlags;
    }
}