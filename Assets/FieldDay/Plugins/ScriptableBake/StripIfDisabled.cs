using System;
using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Strips the GameObject if it's disabled.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Strip If Disabled"), DisallowMultipleComponent]
    public sealed class StripIfDisabled : MonoBehaviour, IBaked {

        public const int Order = FlattenHierarchy.Order - 400;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if (isActiveAndEnabled) {
                Baking.Destroy(this, true);
            } else {
                Baking.Destroy(gameObject);
            }
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}