using FieldDay.Scenes;
using ScriptableBake;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay {
    /// <summary>
    /// Base class for a component used to coordinate the setup of
    /// several components. It will be deleted in builds.
    /// </summary>
    public abstract class ComponentKit : MonoBehaviour, IEditorOnly {

        /// <summary>
        /// Called during OnValidate.
        /// </summary>
        protected virtual void ApplyChanges() {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static protected void PrepareChange(UnityEngine.Object obj) {
#if UNITY_EDITOR
            Baking.PrepareUndo(obj, "modified by ComponentKit");
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Frame.IsActive(this)) {
                ApplyChanges();
            }
        }
#else
        private ComponentKit() {
            throw new Exception("ComponentKits cannot exist outside of editor.");
        }
#endif // UNITY_EDITOR
    }
}