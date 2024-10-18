using UnityEngine;
using ScriptableBake;

namespace FieldDay.Scenes {
    
    /// <summary>
    /// Strips all colliders from this hierarchy.
    /// </summary>
    public sealed class StripColliders : MonoBehaviour, IBaked {

        [Tooltip("If 3d colliders will be destroyed")]
        public bool Destroy3D = true;

        [Tooltip("If 2d colliders will be destroyed")]
        public bool Destroy2D = true;

        #if UNITY_EDITOR

        public int Order { get { return ScriptableBake.FlattenHierarchy.Order - 1; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            if (Destroy3D) {
                Collider[] colliders = GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders) {
                    Baking.Destroy(collider);
                }
            }

            if (Destroy2D) {
                Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
                foreach (var collider in colliders) {
                    Baking.Destroy(collider);
                }
            }

            Baking.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}