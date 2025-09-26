using System;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay.Assets;
using UnityEngine;
using FieldDay.HID;
using UnityEngine.EventSystems;

namespace FieldDay.UI {
    [DisallowMultipleComponent]
    public class CursorHint : PointerListener {
        #region Inspector

        [Header("Tooltip")]
        [AssetName(typeof(CursorType))] public StringHash32 CursorType;
        public string Tooltip;

        #endregion // Inspector

        /// <summary>
        /// Invoked when hovering starts or ends.
        /// </summary>
        public readonly CastableEvent<CursorHint, bool> OnHover = new CastableEvent<CursorHint, bool>();

        #region Unity Events

        protected virtual void Awake() {
            onPointerEnter.AddListener(OnEnter);
            onPointerExit.AddListener(OnExit);
        }

        protected override void OnDisable() {
            if (ReferenceEquals(this, s_Pointer)) {
                s_Pointer = null;
            }
            if (ReferenceEquals(this, s_Locked)) {
                s_Locked = null;
            }
            if (ReferenceEquals(this, s_Effective)) {
                UpdateEffectiveCursor();
            }

            base.OnDisable();
        }

        private void OnEnter(EventData evtData) {
            if (!ReferenceEquals(this, s_Pointer)) {
                s_Pointer = this;
                UpdateEffectiveCursor();
            }
        }

        private void OnExit(EventData evtData) {
            if (ReferenceEquals(this, s_Pointer)) {
                s_Pointer = null;
                UpdateEffectiveCursor();
            }
        }

        #endregion // Unity Events

        #region Current Tracking

        static private StringHash32 s_DefaultOverride;
        static private CursorHint s_Pointer;
        static private CursorHint s_Locked;
        static private CursorHint s_Effective;

        /// <summary>
        /// The current cursor hint under the pointer.
        /// This ignores the locked cursor hint.
        /// </summary>
        static public CursorHint Pointer {
            get { return s_Pointer; }
        }
        
        /// <summary>
        /// The currently active cursor hint.
        /// </summary>
        static public CursorHint Current {
            get { return s_Effective; }
        }

        /// <summary>
        /// Default cursor type.
        /// </summary>
        static public StringHash32 DefaultCursor {
            get { return s_DefaultOverride; }
            set { s_DefaultOverride = value; }
        }

        static private void UpdateEffectiveCursor() {
            CursorHint desiredEffective = s_Locked ? s_Locked : s_Pointer;
            CursorHint prev = s_Effective;
            if (desiredEffective != prev) {
                if (prev) {
                    prev.OnHover.Invoke(prev, false);
                    OnHoverStop.Invoke(prev);
                }

                s_Effective = desiredEffective;
                if (desiredEffective) {
                    desiredEffective.OnHover.Invoke(desiredEffective, true);
                    OnHoverStart.Invoke(desiredEffective);
                }

                Log.Debug("[CursorHint] Updated effective focus from '{0}' to '{1}'", prev, desiredEffective);
            }
        }

        /// <summary>
        /// Invoked when a CursorHint is activated as the current hover.
        /// </summary>
        static public readonly CastableEvent<CursorHint> OnHoverStart = new CastableEvent<CursorHint>();

        /// <summary>
        /// Invoked when a CursorHint is deactivated as the current hover.
        /// </summary>
        static public readonly CastableEvent<CursorHint> OnHoverStop = new CastableEvent<CursorHint>();

        #endregion // Current Tracking

        #region Locks

        /// <summary>
        /// Returns if the given hint is the currently locked hint.
        /// </summary>
        static public bool IsLocked(CursorHint hint) {
            return hint == s_Locked;
        }

        /// <summary>
        /// Attempts to lock focus on the given cursor hint.
        /// </summary>
        static public bool TryLock(CursorHint hint) {
            if (hint) { 
                if (hint.isActiveAndEnabled) {
                    if (s_Locked != hint) {
                        if (s_Locked != null) {
                            Log.Warn("[CursorHint] Attempting to switch lock while '{0}' is currently locked", s_Locked.name);
                        }
                        s_Locked = hint;
                        Log.Debug("[CursorHint] Locked focus on '{0}'", hint.name);
                        UpdateEffectiveCursor();
                        return true;
                    }
                } else {
                    Log.Warn("[CursorHint] Hint '{0}' is currently inactive, and cannot be locked", hint.name);
                }
            }

            return false;
        }

        /// <summary>
        /// Releases the given cursor hint from being locked.
        /// </summary>
        static public bool Unlock(CursorHint hint) {
            if (hint) {
                if (s_Locked == hint) {
                    s_Locked = null;
                    Log.Debug("[CursorHint] Unlocked focus from '{0}'", hint.name);
                    UpdateEffectiveCursor();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Releases the current cursor hint from being locked.
        /// </summary>
        static public bool Unlock() {
            CursorHint prev = s_Locked;
            if (prev != null) {
                s_Locked = null;
                Log.Debug("[CursorHint] Unlocked focus from '{0}'", prev.name);
                UpdateEffectiveCursor();
                return true;
            }

            return false;
        }

        #endregion // Locks
    }
}