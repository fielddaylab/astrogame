using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Changes the transform parent.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Change Parent"), DisallowMultipleComponent]
    public sealed class ChangeParent : MonoBehaviour, IBaked {

        public const int Order = FlattenHierarchy.Order - 10;

        public Transform NewParent;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Baking.UnpackPrefabIfNecessary(transform);
            transform.SetParent(NewParent);
            Baking.Destroy(this, true);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}