using UnityEngine;
using ScriptableBake;
using BeauUtil;

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
            if (TryRevert()) {
                Debug.LogWarningFormat("[NoOverrides] GameObject '{0}' had overrides reverted", UnityHelper.FullPath(gameObject, true));
            }
            Baking.Destroy(this, true);
            return true;
        }

#endif // UNITY_EDITOR
    }
}