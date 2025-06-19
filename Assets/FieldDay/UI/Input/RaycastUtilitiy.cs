using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    static public class RaycastUtility {
        /// <summary>
        /// Returns if the given GameObject is interactable via raycast.
        /// </summary>
        static public bool IsInteractableByRaycaster(GameObject gameObject, PhysicsRaycaster raycaster) {
            return ((1 << gameObject.layer) & raycaster.finalEventMask) != 0;
        }
    }
}