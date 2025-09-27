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
            Baking.Destroy(gameObject, true);
            return true;
        }

#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Marks a component type as only existing at edit time.
    /// </summary>
    public interface IEditModeOnly { }

    /// <summary>
    /// Marks a component type as only existing while in the editor.
    /// </summary>
    public interface IEditorOnly { }
}