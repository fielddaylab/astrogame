using UnityEngine;
using ScriptableBake;

namespace FieldDay.Scenes {
    
    /// <summary>
    /// Disallows any prefab overrides for this hierarchy.
    /// </summary>
    public sealed class NoOverrides : MonoBehaviour, IBaked {

#if UNITY_EDITOR

        private bool TryRevert() {
            return Baking.TryRevertPrefabOverrides(gameObject);
        }

        int IBaked.Order { get { return FlattenHierarchy.Order - 100; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            TryRevert();
            Baking.Destroy(this);
            return true;
        }

#endif // UNITY_EDITOR
    }
}