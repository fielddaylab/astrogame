using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(CursorHint))]
    public sealed class LabInteractable : BatchedComponent {
        public bool IsDraggable = false;
        [NonSerialized] public bool InteractReceived = false;
        [NonSerialized] public bool InteractEnded = false;
        [NonSerialized] public bool IsDragging = false;
        [NonSerialized] public CursorHint Cursor;

        //private string m_CachedPath;

        private void Awake() {
            Cursor = GetComponent<CursorHint>();
        }

        protected override void OnDisable() {
            base.OnDisable();

            InteractReceived = false;
        }

        //void IRegistrationCallbacks.OnDeregister() {
        //    if (!this) {
        //        Log.Error("Hey WTF I was deleted {0}", m_CachedPath);
        //    } else {
        //        Log.Msg("[LabInteractable] Deregistering {0}", m_CachedPath);
        //    }
        //}

        //void IRegistrationCallbacks.OnRegister() {
        //    Log.Msg("[LabInteractable] Registering {0}", m_CachedPath ?? (m_CachedPath = UnityHelper.FullPath(gameObject)));
        //}
    }
}