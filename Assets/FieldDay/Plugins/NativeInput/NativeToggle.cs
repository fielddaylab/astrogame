using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NativeUtils {
    /// <summary>
    /// Toggle that responds to native input callbacks.
    /// </summary>
    [AddComponentMenu("UI/Toggle (NativeInput)", 30)]
    public class NativeToggle : Toggle, INativePointerDownHandler, INativePointerClickHandler {
        public override void OnPointerClick(PointerEventData eventData) {
        }

        public virtual void OnNativePointerClick(PointerEventData eventData) {
            base.OnPointerClick(eventData);
        }

        public virtual void OnNativePointerDown(PointerEventData eventData) {
        }
    }
}