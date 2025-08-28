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

        #region TimeScale Adjustments

#if DEVELOPMENT
        static private uint s_TimeScaleLock;
#endif // DEVELOPMENT

        /// <summary>
        /// Returns if time controls are allowed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool AllowTimeControl() {
#if DEVELOPMENT
            return s_TimeScaleLock == 0;
#else
            return false;
#endif // DEVELOPMENT
        }

        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void BlockTimeControl() {
#if DEVELOPMENT
            s_TimeScaleLock++;
#endif // DEVELOPMENT
        }

        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void UnblockTimeControl() {
#if DEVELOPMENT
            Assert.True(s_TimeScaleLock > 0);
            s_TimeScaleLock--;
#endif // DEVELOPMENT
        }

        #endregion // TimeScale Adjustments

        #region Flags

#if DEVELOPMENT
        private struct FlagGroup256 {
            public BitSet256 Flags;
            public BitSet256 QueuedDisable;
            public BitSet256 QueuedSingleFrame;
        }

        private const int MaxFlagGroups = 128;
        private const int MaxToggleGroups = 16;

        static private FlagGroup256 s_GlobalFlags;
        static private FlagGroup256[] s_FlagGroups = new FlagGroup256[MaxFlagGroups];
        static private volatile int s_FlagGroupCount;

        static private int GetNextGroupIndex() {
            Assert.True(s_FlagGroupCount < MaxFlagGroups);
            return Interlocked.Increment(ref s_FlagGroupCount) - 1;
        }

        static private class EnumFlagGroup<T> where T : unmanaged, Enum {
            static internal int Index = GetNextGroupIndex();

            static internal BitSet256[] ToggleGroups = new BitSet256[MaxToggleGroups];
            static internal int ToggleGroupCount;

            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
            [Il2CppSetOption(Option.NullChecks, false)]
            static internal void AddToggleGroup(BitSet256 group) {
                for(int i = 0; i < ToggleGroupCount; i++) {
                    if ((ToggleGroups[i] & group) == group) {
                        ToggleGroups[i] = group;
                        break;
                    }
                }

                Assert.True(ToggleGroupCount < MaxToggleGroups, "Too many toggle groups for enum '{0}' - max allowed {1}", typeof(T).FullName, MaxToggleGroups);
                ToggleGroups[ToggleGroupCount++] = group;
            }

            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
            [Il2CppSetOption(Option.NullChecks, false)]
            static internal void SetToggleGroupAware(ref BitSet256 value, int index) {
                for(int i = 0; i < ToggleGroupCount; i++) {
                    if (ToggleGroups[i].IsSet(index)) {
                        value &= ~ToggleGroups[i];
                    }
                }
                value.Set(index);
            }
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
            return s_FlagGroups[EnumFlagGroup<T>.Index].Flags.IsSet(Enums.ToInt(index));
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
            int type = EnumFlagGroup<T>.Index;
            int idx = Enums.ToInt(index);
            bool val = s_FlagGroups[type].Flags.IsSet(idx);
            if (value) {
                EnumFlagGroup<T>.SetToggleGroupAware(ref s_FlagGroups[type].Flags, idx);
            } else {
                s_FlagGroups[type].Flags.Unset(idx);
            }
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
            int type = EnumFlagGroup<T>.Index;
            int idx = Enums.ToInt(index);
            EnumFlagGroup<T>.SetToggleGroupAware(ref s_FlagGroups[type].Flags, idx);
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
            int type = EnumFlagGroup<T>.Index;
            int idx = Enums.ToInt(index);
            s_FlagGroups[type].Flags.Unset(idx);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag to its opposite. Returns the new value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public bool ToggleFlag<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            int type = EnumFlagGroup<T>.Index;
            int idx = Enums.ToInt(index);
            bool val = s_FlagGroups[type].Flags.IsSet(idx);
            if (!val) {
                EnumFlagGroup<T>.SetToggleGroupAware(ref s_FlagGroups[type].Flags, idx);
            } else {
                s_FlagGroups[type].Flags.Unset(idx);
            }
            return !val;
#else
            return false;
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

        /// <summary>
        /// Sets the given debug flag to its opposite. Returns the new value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ToggleFlag(int index) {
#if DEVELOPMENT
            bool val = s_GlobalFlags.Flags.IsSet(index);
            s_GlobalFlags.Flags.Set(index, !val);
            return !val;
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets a mutually exclusive set of debug flags.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddToggleGroup<T>(params T[] values) where T : unmanaged, Enum {
#if DEVELOPMENT
            BitSet256 bits = default;
            foreach(var value in values) {
                bits.Set(Enums.ToInt(value));
            }
            EnumFlagGroup<T>.AddToggleGroup(bits);
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
            int type = EnumFlagGroup<T>.Index;
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
            int type = EnumFlagGroup<T>.Index;
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
            int type = EnumFlagGroup<T>.Index;
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
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
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

        static private void ProcessQueue(ref FlagGroup256 group) {
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

        #region Testing

#if DEVELOPMENT
        static private bool s_IsRunningAutomatedTest;
#endif // DEVELOPMENT

        /// <summary>
        /// Returns if time controls are allowed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsAutomatedTestActive() {
#if DEVELOPMENT
            return s_IsRunningAutomatedTest;
#else
            return false;
#endif // DEVELOPMENT
        }

        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static internal void SetAutomatedTestActive(bool active) {
#if DEVELOPMENT
            s_IsRunningAutomatedTest = active;
#endif // DEVELOPMENT
        }

        #endregion // Testing

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
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddFlagToggle(DMInfo menu, string name, int index, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
                menu.AddToggle(name, () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to set/unset a flag.
            /// </summary>
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddFlagToggle<T>(DMInfo menu, string name, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddToggle(name, () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to set/unset a flag.
            /// </summary>
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddFlagToggle<T>(DMInfo menu, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddToggle(ReflectionCache.InspectorName(index.ToString()), () => IsFlagSet(index), (b) => SetFlag(index, b), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddSingleFrameFlagButton(DMInfo menu, string name, int index, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
                menu.AddButton(name, () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddSingleFrameFlagButton<T>(DMInfo menu, string name, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddButton(name, () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }

            /// <summary>
            /// Adds a toggle to queue a flag for a single frame.
            /// </summary>
            [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
            static public void AddSingleFrameFlagButton<T>(DMInfo menu, T index, DMPredicate predicate = null, int indent = 0) where T : unmanaged, Enum {
#if DEVELOPMENT
                menu.AddButton(ReflectionCache.InspectorName(index.ToString()), () => QueueFlagSingleFrame(index), predicate, indent);
#endif // DEVELOPMENT
            }
        }

        #endregion // Menu
    }
}