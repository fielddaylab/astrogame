using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NativeUtils {
    /// <summary>
    /// Button that responds to native input callbacks.
    /// </summary>
    [AddComponentMenu("UI/Button (NativeInput)", 30)]
    public class NativeButton : Button, INativePointerDownHandler, INativePointerClickHandler {
        public override void OnPointerClick(PointerEventData eventData) {
        }

        public virtual void OnNativePointerClick(PointerEventData eventData) {
            base.OnPointerClick(eventData);
        }

        public virtual void OnNativePointerDown(PointerEventData eventData) {
        }
    }
}