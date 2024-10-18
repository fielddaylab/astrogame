using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.UI;
using UnityEngine;

namespace FieldDay {
    /// <summary>
    /// Object lookup shortcuts.
    /// </summary>
    public class Find {
        #region Assets

        /// <summary>
        /// Looks up the global asset of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public T GlobalAsset<T>() where T : class, IGlobalAsset {
            return Game.Assets.GetGlobal<T>();
        }

        /// <summary>
        /// Looks up the named asset of the given type.
        /// </summary>
        static public T NamedAsset<T>(StringHash32 id) where T : class, INamedAsset {
            return Game.Assets.GetNamed<T>(id);
        }

        /// <summary>
        /// Looks up the lite asset with the given id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public T LiteAsset<T>(StringHash32 id) where T : struct, ILiteAsset {
            return Game.Assets.GetLite<T>(id);
        }

        #endregion // Assets

        #region Shared State

        /// <summary>
        /// Looks up the shared state of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public T State<T>() where T : class, ISharedState {
            return Game.SharedState.Get<T>();
        }

        #endregion // Shared State

        #region Gui

        /// <summary>
        /// Looks up the shared gui panel of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public T Panel<T>() where T : class, ISharedGuiPanel {
            return Game.Gui.GetShared<T>();
        }

        /// <summary>
        /// Looks up the named RectTransform of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public RectTransform NamedRectTransform(StringHash32 name) {
            return Game.Gui.FindNamed(name);
        }

        #endregion // Gui

        #region Components

        /// <summary>
        /// Looks up the list of components of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public ComponentIterator<T> Components<T>() where T : class, IComponentData {
            return Game.Components.ComponentsOfType<T>();
        }

        /// <summary>
        /// Looks up the first component of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public T FirstComponent<T>() where T : class, IComponentData {
            return Game.Components.FirstComponentOfType<T>();
        }

        #endregion // Components

        #region Unity

        /// <summary>
        /// Finds a Unity object from its asset id.
        /// </summary>
        static public UnityEngine.Object FromId(int instanceId) {
            return UnityHelper.Find(instanceId);
        }

        /// <summary>
        /// Finds a Unity object from its asset id.
        /// </summary>
        static public T FromId<T>(int instanceId) where T : UnityEngine.Object {
            return UnityHelper.Find<T>(instanceId);
        }

        /// <summary>
        /// Finds an instance of the given type.
        /// </summary>
        static public T Any<T>() where T : UnityEngine.Object {
            return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Finds an instance of the given type.
        /// </summary>
        static public T Any<T>(FindObjectsInactive findInactive) where T : UnityEngine.Object {
            return Object.FindAnyObjectByType<T>(findInactive);
        }

        #endregion // Unity
    }
}