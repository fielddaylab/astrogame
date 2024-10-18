#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.IL2CPP.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug flags.
    /// </summary>
    static public class DebugFlags {
        #region Scene Launch

#if UNITY_EDITOR
        static private bool s_LaunchedFromScene = true;

        /// <summary>
        /// Detects whether the game was launched from this scene.
        /// </summary>
        static public bool LaunchedFromThisScene {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return s_LaunchedFromScene; }
        }
#else
        public const bool LaunchedFromThisScene = false;
#endif // UNITY_EDITOR

        [Conditional("UNITY_EDITOR")]
        static internal void MarkNewSceneLoaded() {
#if UNITY_EDITOR
            s_LaunchedFromScene = false;
#endif // UNITY_EDITOR
        }

        #endregion // Scene Launch

        #region Flags

#if DEVELOPMENT
        private struct FlagGroup64 {
            public BitSet64 Flags;
            public BitSet64 QueuedDisable;
            public BitSet64 QueuedSingleFrame;
        }

        private const int MaxFlagGroups = 128;

        static private FlagGroup64 s_GlobalFlags;
        static private FlagGroup64[] s_FlagGroups = new FlagGroup64[MaxFlagGroups];
        static private volatile int s_FlagGroupCount;

        static private int GetNextGroupIndex() {
            Assert.True(s_FlagGroupCount < MaxFlagGroups);
            return Interlocked.Increment(ref s_FlagGroupCount) - 1;
        }

        static private class FlagGroupIndex<T> where T : unmanaged, Enum {
            static internal int Index = GetNextGroupIndex();
        }
#endif // DEVELOPMENT

        #region Checking

        /// <summary>
        /// Returns if the given debug flag is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public bool IsFlagSet<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            return s_FlagGroups[FlagGroupIndex<T>.Index].Flags.IsSet(Enums.ToInt(index));
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Returns if the given debug flag is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsFlagSet(int index) {
#if DEVELOPMENT
            return s_GlobalFlags.Flags.IsSet(index);
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Checking

        #region Setting

        /// <summary>
        /// Sets the given debug flag. Returns the previous value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public bool SetFlag<T>(T index, bool value) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            bool val = s_FlagGroups[type].Flags.IsSet(idx);
            s_FlagGroups[type].Flags.Set(idx, value);
            return val;
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag to true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public void SetFlag<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].Flags.Set(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag to false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public void ClearFlag<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].Flags.Unset(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag. Returns the previous value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool SetFlag(int index, bool value) {
#if DEVELOPMENT
            bool val = s_GlobalFlags.Flags.IsSet(index);
            s_GlobalFlags.Flags.Set(index, value);
            return val;
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag to true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void SetFlag(int index) {
#if DEVELOPMENT
            s_GlobalFlags.Flags.Set(index);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag to false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void ClearFlag(int index) {
#if DEVELOPMENT
            s_GlobalFlags.Flags.Unset(index);
#endif // DEVELOPMENT
        }

        #endregion // Setting

        #region Queue

        /// <summary>
        /// Sets the given debug flag for the duration of this frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public void SetFlagSingleFrame<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].Flags.Set(idx);
            s_FlagGroups[type].QueuedDisable.Set(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Queues the given debug flag to be set for the duration of the next frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public void QueueFlagSingleFrame<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].QueuedSingleFrame.Set(idx);
            s_FlagGroups[type].QueuedDisable.Unset(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Clears the given debug flag on the next frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public void ClearFlagNextFrame<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = FlagGroupIndex<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].QueuedSingleFrame.Unset(idx);
            s_FlagGroups[type].QueuedDisable.Set(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag for the duration of this frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void SetFlagSingleFrame(int index) {
#if DEVELOPMENT
            s_GlobalFlags.Flags.Set(index);
            s_GlobalFlags.QueuedDisable.Set(index);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Queues the given debug flag to be set for the duration of the next frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void QueueFlagSingleFrame(int index) {
#if DEVELOPMENT
            s_GlobalFlags.QueuedSingleFrame.Set(index);
            s_GlobalFlags.QueuedDisable.Unset(index);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Clears the given debug flag on the next frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void ClearFlagNextFrame(int index) {
#if DEVELOPMENT
            s_GlobalFlags.QueuedSingleFrame.Unset(index);
            s_GlobalFlags.QueuedDisable.Set(index);
#endif // DEVELOPMENT
        }

        #endregion // Queue

        /// <summary>
        /// Processes single-frame queues.
        /// </summary>
        [Conditional("DEVELOPMENT")]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static internal void HandleFrameRollover() {
#if DEVELOPMENT
            ProcessQueue(ref s_GlobalFlags);
            for(int i = 0; i < s_FlagGroupCount; i++) {
                ProcessQueue(ref s_FlagGroups[i]);
            }
#endif // DEVELOPMENT
        }

#if DEVELOPMENT

        static private void ProcessQueue(ref FlagGroup64 group) {
            if (group.QueuedDisable) {
                group.Flags &= ~group.QueuedDisable;
                group.QueuedDisable.Clear();
            }

            if (group.QueuedSingleFrame) {
                group.Flags |= group.QueuedSingleFrame;
                group.QueuedDisable |= group.QueuedSingleFrame;
                group.QueuedSingleFrame.Clear();
            }
        }

#endif // DEVELOPMENT

        #endregion // Flags

        #region Object Selection

        /// <summary>
        /// Returns if the given object is selected.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsSelected(UnityEngine.Object obj) {
#if UNITY_EDITOR
            return obj && Selection.activeInstanceID == obj.GetInstanceID();
#else
            return false;
#endif // UNITY_EDITOR
        }

        #endregion // Object Selection

        #region Menu

        static public class Menu {
            /// <summary>
            /// Adds a toggle to set/unset a flag.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddFlagToggle(DMInfo menu, string name, int index, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
                menu.AddToggle(name, () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to set/unset a flag.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddFlagToggle<T>(DMInfo menu, string name, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddToggle(name, () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to set/unset a flag.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddFlagToggle<T>(DMInfo menu, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddToggle(ReflectionCache.InspectorName(index.ToString()), () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddSingleFrameFlagButton(DMInfo menu, string name, int index, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
                menu.AddButton(name, () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddSingleFrameFlagButton<T>(DMInfo menu, string name, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddButton(name, () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT")]
            static public void AddSingleFrameFlagButton<T>(DMInfo menu, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddButton(ReflectionCache.InspectorName(index.ToString()), () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }
        }

        #endregion // Menu
    }
}