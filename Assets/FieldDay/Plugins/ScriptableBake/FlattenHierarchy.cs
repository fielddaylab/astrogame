using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Flattens a transform hierarchy.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Flatten Hierarchy"), DisallowMultipleComponent]
    public sealed class FlattenHierarchy : MonoBehaviour, IBaked {

        public const int Order = -1000000;

        [Tooltip("If true, will flatten in editor")]
        public bool Always = false;

        [Tooltip("Whether or not to destroy any inactive children of this GameObject")]
        public bool DestroyInactiveChildren = false;

        [Tooltip("If true, the full hierarchy beneath this object will be flattened.\nIf false, only the immediate children of this object will be affected")]
        public bool Recursive = false;

        [Tooltip("Whether or not to destroy the GameObject once flattened")]
        public bool DestroyGameObject = false;

        [Tooltip("If true, this will skip all objects that are a child of an Animator")]
        public bool IgnoreAnimators = true;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if (!Always && (flags & BakeFlags.IsBuild) == 0) {
                Baking.Destroy(this, true);
                return true;
            }

            FlattenFlags flattenFlags = 0;
            if (DestroyInactiveChildren) {
                flattenFlags |= FlattenFlags.DestroyInactive;
            }
            if (Recursive) {
                flattenFlags |= FlattenFlags.Recursive;
            }
            if (IgnoreAnimators) {
                flattenFlags |= FlattenFlags.SkipAnimators;
            }
            Baking.FlattenHierarchy(transform, flattenFlags);
            bool destroyGO = DestroyGameObject;

            if (destroyGO && !Baking.IsEmptyLeaf(transform, 1)) {
                Debug.LogWarningFormat("[FlattenHierarchy] DestroyGameObject enabled on non-leaf GameObject '{0}' (children or additional components found) - not destroying", gameObject.name);
                destroyGO = false;
            }
            Baking.Destroy(destroyGO ? (UnityEngine.Object) gameObject : this, true);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}