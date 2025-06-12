using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    /// <summary>
    /// Build configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Build Configuration", order = -260)]
    public class BuildConfig : ScriptableObject {
        public string[] BranchNamePatterns;
        public bool DevelopmentBuild;
        public ManagedStrippingLevel StrippingLevel = ManagedStrippingLevel.Medium;

        [Multiline]
        public string CustomDefines;

        public int Order;
    }
}