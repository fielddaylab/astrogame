using UnityEngine;
using ScriptableBake;

namespace FieldDay.Scenes {
    /// <summary>
    /// Behaviour marking a GameObject hierarchy as only existing during editing.
    /// </summary>
    public sealed class EditModeOnly : MonoBehaviour, IBaked {
#if UNITY_EDITOR

        public int Order { get { return FlattenHierarchy.Order - 500; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            Baking.Destroy(gameObject);
            return true;
        }

#endif // UNITY_EDITOR
    }
}