using UnityEngine;

namespace FieldDay.Physics {
    static public class PhysicsExtractor {
        #region ResolveComponent

        static public TComponent ResolveComponent<TComponent>(this RaycastHit hit) {
            if (hit.colliderInstanceID == 0) {
                return default(TComponent);
            }

            Collider col = hit.collider;
            Rigidbody rb;
            TComponent comp = col.GetComponent<TComponent>();
            if (comp == null && (rb = col.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = col.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        static public TComponent ResolveComponent<TComponent>(this RaycastHit2D hit) {
            Collider2D col = hit.collider;
            if (!col) {
                return default(TComponent);
            }

            Rigidbody2D rb;
            TComponent comp = col.GetComponent<TComponent>();
            if (comp == null && (rb = col.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = col.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        static public TComponent ResolveComponent<TComponent>(this Collision collision) {
            Collider col = collision.collider;
            if (!col) {
                return default(TComponent);
            }

            Rigidbody rb;
            TComponent comp = col.GetComponent<TComponent>();
            if (comp == null && (rb = col.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = col.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        static public TComponent ResolveComponent<TComponent>(this Collision2D collision) {
            Collider2D col = collision.collider;
            if (!col) {
                return default(TComponent);
            }

            Rigidbody2D rb;
            TComponent comp = col.GetComponent<TComponent>();
            if (comp == null && (rb = col.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = col.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        static public TComponent ResolveComponent<TComponent>(this Collider collider) {
            if (!collider) {
                return default(TComponent);
            }

            Rigidbody rb;
            TComponent comp = collider.GetComponent<TComponent>();
            if (comp == null && (rb = collider.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = collider.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        static public TComponent ResolveComponent<TComponent>(this Collider2D collider) {
            if (!collider) {
                return default(TComponent);
            }

            Rigidbody2D rb;
            TComponent comp = collider.GetComponent<TComponent>();
            if (comp == null && (rb = collider.attachedRigidbody)) {
                comp = rb.GetComponent<TComponent>();
            }
            if (comp == null) {
                comp = collider.GetComponentInParent<TComponent>();
            }

            return comp;
        }

        #endregion // ResolveComponent
    }
}