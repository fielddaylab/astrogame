#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Components;
using FieldDay.Debugging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEditor;
using UnityEngine;

using ComponentIndex = BeauUtil.TypeIndex<FieldDay.Components.IComponentData>;
using SystemIndex = BeauUtil.TypeIndex<FieldDay.Systems.ISystem>;

namespace FieldDay.Systems {
    /// <summary>
    /// Manages game system updates.
    /// </summary>
    public sealed class SystemsMgr {
        #region Types

        // initialization info
        private struct SystemInitInfo {
            public int Order;
            public ISystem System;

            static public SystemInitInfo Create(ISystem system) {
                SystemInitInfo info;
                info.System = system;

                SysInitOrderAttribute orderAttr = Reflect.GetAttribute<SysInitOrderAttribute>(system.GetType(), true);
                info.Order = orderAttr != null ? orderAttr.Order : 0;
                return info;
            }

            static public readonly Predicate<SystemInitInfo, ISystem> FindPredicate = (i, s) => i.System == s;
        }

        private struct UpdateRecord : IEquatable<UpdateRecord> {
            public ISystem System;
            public int UpdateOrder;
            public int CategoryMask;
            public bool AllowDuringLoad;

            public UpdateRecord(ISystem system) {
                System = system;
                UpdateOrder = 0;
                CategoryMask = 0;
                AllowDuringLoad = false;
            }

            public UpdateRecord(ISystem system, SysUpdateAttribute info) {
                System = system;
                UpdateOrder = info.Order;
                CategoryMask = info.CategoryMask;
                AllowDuringLoad = info.AllowExecutionDuringLoad;
            }

            public bool Equals(UpdateRecord other) {
                return System == other.System;
            }

            static public readonly Predicate<UpdateRecord, ISystem> FindPredicate = (u, s) => u.System == s;
            static public readonly Comparison<UpdateRecord> Comparer = (a, b) => {
                if (a.UpdateOrder < b.UpdateOrder) {
                    return -1;
                } else if (a.UpdateOrder > b.UpdateOrder) {
                    return 1;
                } else {
                    return CompareUtils.Compare(a.System.GetType().FullName, b.System.GetType().FullName);
                }
            };
        }

        public delegate void SystemCallback(ISystem system);

        #endregion // Types

        internal SystemsMgr() { }

        #region System Lists

        private readonly RingBuffer<ISystem> m_AllSystems = new RingBuffer<ISystem>(32, RingBufferMode.Expand);
        private readonly RingBuffer<SystemInitInfo> m_InitList = new RingBuffer<SystemInitInfo>(32, RingBufferMode.Expand);

        private PhaseBuckets<UpdateRecord> m_Updates = new PhaseBuckets<UpdateRecord>(4);

        /// <summary>
        /// Callback for when a system is registered.
        /// </summary>
        public event SystemCallback OnSystemRegistered;

        /// <summary>
        /// Callback for when a system is deregistered.
        /// </summary>
        public event SystemCallback OnSystemDeregistered;

        /// <summary>
        /// Queues the given system for registration.
        /// </summary>
        public void Register(ISystem system) {
            Assert.NotNull(system);
            Assert.False(m_AllSystems.Contains(system), "System already registered");

            if (!m_InitList.Exists(SystemInitInfo.FindPredicate, system)) {
                m_InitList.PushBack(SystemInitInfo.Create(system));
            }
        }

        /// <summary>
        /// Immediately deregisters the given system.
        /// </summary>
        public void Deregister(ISystem system) {
            Assert.NotNull(system);

            if (m_InitList.RemoveWhere(SystemInitInfo.FindPredicate, system) > 0) {
                return;
            }

            bool removed = m_AllSystems.FastRemove(system);
            Assert.True(removed, "System already deregistered");

            SysUpdateAttribute updateInfo = GetUpdateInfo(system.GetType());
            if (updateInfo != null && updateInfo.PhaseMask != 0) {
                foreach(var phase in new PhaseBuckets.PhaseEnumerator(updateInfo.PhaseMask)) {
                    m_Updates[phase].RemoveWhere(UpdateRecord.FindPredicate, system);
                }
                m_Updates.MarkBucketsDirty(updateInfo.PhaseMask);
            }

            IComponentSystem componentSystem = system as IComponentSystem;
            if (componentSystem != null) {
                DeregisterComponentSystem(componentSystem);
            }

            system.Shutdown();
            Log.Msg("[SystemsMgr] Manager '{0}' shutdown", system.GetType().FullName);

            if (OnSystemDeregistered != null) {
                OnSystemDeregistered.Invoke(system);
            }
        }

        /// <summary>
        /// Processes the system initialization queue.
        /// </summary>
        internal void ProcessInitQueue() {
            if (m_InitList.Count == 0) {
                return;
            }

            m_InitList.Sort((a, b) => a.Order - b.Order);
            while (m_InitList.TryPopFront(out SystemInitInfo info)) {
                FinishSystemInit(info.System);
            }
        }

        /// <summary>
        /// Finishes initializating the given system.
        /// </summary>
        private void FinishSystemInit(ISystem system) {
            m_AllSystems.PushBack(system);

            IComponentSystem componentSystem = system as IComponentSystem;
            if (componentSystem != null) {
                RegisterComponentSystem(componentSystem);
            }

            system.Initialize();
            Log.Msg("[SystemsMgr] System '{0}' initialized", system.GetType().FullName);

            SysUpdateAttribute updateInfo = CacheUpdateInfo(system.GetType());
            if (updateInfo != null && updateInfo.PhaseMask != 0) {
                Assert.True(PhaseBuckets.IsTracked(updateInfo.PhaseMask), "System '{0}' has an invalid update phase '{1}'", system.GetType().FullName, updateInfo.PhaseMask);
                UpdateRecord record = new UpdateRecord(system, updateInfo);
                foreach (var phase in new PhaseBuckets.PhaseEnumerator(updateInfo.PhaseMask)) {
                    m_Updates[phase].PushBack(record);
                }
                m_Updates.MarkBucketsDirty(updateInfo.PhaseMask);
            }

            if (OnSystemRegistered != null) {
                OnSystemRegistered.Invoke(system);
            }
        }

        #endregion // System Lists

        #region Component Mapping

        private readonly List<IComponentSystem>[] m_SystemComponentTypeMap = new List<IComponentSystem>[ComponentIndex.Capacity];
        private readonly List<IComponentSystem>[] m_RelevantSystemsMap = new List<IComponentSystem>[ComponentIndex.Capacity];

        /// <summary>
        /// Looks up systems for the given component type.
        /// </summary>
        public int LookupSystemsForComponent(Type componentType, List<IComponentSystem> systems) {
            List<IComponentSystem> relevantSystems = GetRelevantSystems(componentType, true);
            if (relevantSystems != null) {
                systems.AddRange(relevantSystems);
                return relevantSystems.Count;
            }

            return 0;
        }

        /// <summary>
        /// Looks up systems for the given component type.
        /// </summary>
        public int LookupSystemsForComponent<T>(List<IComponentSystem<T>> systems) where T : class, IComponentData {
            List<IComponentSystem> relevantSystems = GetRelevantSystems(typeof(T), true);
            if (relevantSystems != null) {
                for(int i = 0; i < relevantSystems.Count; i++) {
                    systems.Add((IComponentSystem<T>) relevantSystems[i]);
                }
                return relevantSystems.Count;
            }

            return 0;
        }

        /// <summary>
        /// Adds the given component to all relevant systems.
        /// </summary>
        internal void AddComponent(IComponentData component) {
            Type componentType = component.GetType();

            List<IComponentSystem> relevant = GetRelevantSystems(componentType, true);
            if (relevant != null && relevant.Count > 0) {
                for(int i = 0; i < relevant.Count; i++) {
                    relevant[i].Add(component);
                }
            } else {
                //Log.Warn("[SystemsMgr] Component of type '{0}' does not have any corresponding systems", componentType.FullName);
            }
        }

        /// <summary>
        /// Removes the given component from all relevant systems.
        /// </summary>
        internal void RemoveComponent(IComponentData component) {
            Type componentType = component.GetType();

            List<IComponentSystem> relevant = GetRelevantSystems(componentType, false);
            if (relevant != null && relevant.Count > 0) {
                for (int i = 0; i < relevant.Count; i++) {
                    relevant[i].Remove(component);
                }
            }
        }

        /// <summary>
        /// Adds the given component system to component system tracking.
        /// </summary>
        private void RegisterComponentSystem(IComponentSystem componentSystem) {
            Type componentType = componentSystem.ComponentType;
            int index = ComponentIndex.Get(componentType);

            // direct mapping of component type to systems
            List<IComponentSystem> directList = m_SystemComponentTypeMap[index];
            if (directList == null) {
                directList = new List<IComponentSystem>(1);
                m_SystemComponentTypeMap[index] = directList;
            }
            directList.Add(componentSystem);

            // mapping of type to all systems that handle that type
            List<IComponentSystem> relevantList = m_RelevantSystemsMap[index];
            if (relevantList == null) {
                relevantList = new List<IComponentSystem>(2);
                m_RelevantSystemsMap[index] = relevantList;
            }
            relevantList.Add(componentSystem);
        }

        /// <summary>
        /// Removes the given component system from component system tracking.
        /// </summary>
        private void DeregisterComponentSystem(IComponentSystem componentSystem) {
            Type componentType = componentSystem.ComponentType;
            int index = ComponentIndex.Get(componentType);

            // remove from direct list
            List<IComponentSystem> directList = m_SystemComponentTypeMap[index];
            if (directList != null) {
                directList.Remove(componentSystem);
            }

            // remove from direct relevant list
            List<IComponentSystem> relevantList = m_RelevantSystemsMap[index];
            if (relevantList != null) {
                relevantList.Remove(componentSystem);
            }
        }

        /// <summary>
        /// Retrieves the list of all systems relevant for the given component type.
        /// </summary>
        private List<IComponentSystem> GetRelevantSystems(Type componentType, bool createIfNotFound) {
            int index = ComponentIndex.Get(componentType);
            List<IComponentSystem> relevantSystems = m_RelevantSystemsMap[index];
            if (relevantSystems == null && createIfNotFound) {
                relevantSystems = new List<IComponentSystem>(Math.Max(m_AllSystems.Count / 4, 2));

                foreach(var checkedIndex in ComponentIndex.GetAll(index)) {
                    List<IComponentSystem> directList = m_SystemComponentTypeMap[checkedIndex];
                    if (directList != null) {
                        relevantSystems.AddRange(directList);
                    }
                }

                m_RelevantSystemsMap[index] = relevantSystems;
            }

            return relevantSystems;
        }

        #endregion // Component Mapping

        #region Events

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DebugUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.DebugUpdate], m_Updates.PopBucketDirty(GameLoopPhase.DebugUpdate), deltaTime, categoryMask, isLoading);

#if DEVELOPMENT
            RenderDebugInfo();
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PreUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.PreUpdate], m_Updates.PopBucketDirty(GameLoopPhase.PreUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FixedUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.FixedUpdate], m_Updates.PopBucketDirty(GameLoopPhase.FixedUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LateFixedUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.LateFixedUpdate], m_Updates.PopBucketDirty(GameLoopPhase.LateFixedUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.Update], m_Updates.PopBucketDirty(GameLoopPhase.Update), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UnscaledUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.UnscaledUpdate], m_Updates.PopBucketDirty(GameLoopPhase.UnscaledUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LateUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.LateUpdate], m_Updates.PopBucketDirty(GameLoopPhase.LateUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UnscaledLateUpdate(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.UnscaledLateUpdate], m_Updates.PopBucketDirty(GameLoopPhase.UnscaledLateUpdate), deltaTime, categoryMask, isLoading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ApplicationPreRender(float deltaTime, int categoryMask, bool isLoading) {
            ProcessUpdates(m_Updates[GameLoopPhase.ApplicationPreRender], m_Updates.PopBucketDirty(GameLoopPhase.ApplicationPreRender), deltaTime, categoryMask, isLoading);
        }

        internal void Shutdown() {
            m_Updates.Clear();

            foreach(var list in m_SystemComponentTypeMap) {
                list?.Clear();
            }
            Array.Clear(m_SystemComponentTypeMap, 0, m_SystemComponentTypeMap.Length);
            foreach(var list in m_RelevantSystemsMap) {
                list?.Clear();
            }
            Array.Clear(m_RelevantSystemsMap, 0, m_SystemComponentTypeMap.Length);
            m_InitList.Clear();

            while(m_AllSystems.TryPopBack(out ISystem sys)) {
                sys.Shutdown();
                Log.Msg("[SystemsMgr] System '{0}' has shutdown", sys.GetType().FullName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static private void ProcessUpdates(RingBuffer<UpdateRecord> systems, bool needsSort, float deltaTime, int categoryMask, bool isLoading) {
            if (needsSort) {
                systems.Sort(UpdateRecord.Comparer);
            }

            foreach(var sys in systems) {
#if DEVELOPMENT
                try {
                    if ((sys.AllowDuringLoad || !isLoading) && (categoryMask & sys.CategoryMask) != 0 && sys.System.HasWork()) {
                        sys.System.ProcessWork(deltaTime);
                    }
                } catch(Exception e) {
                    Log.Error("[SystemsMgr] Encountered exception when processing system '{0}'", sys.System.GetType().FullName);
                    Debug.LogException(e);
                }
#else
                if ((sys.AllowDuringLoad || !isLoading) && (categoryMask & sys.CategoryMask) != 0 && sys.System.HasWork()) {
                    sys.System.ProcessWork(deltaTime);
                }
#endif // DEVELOPMENT
            }
        }

#endregion // Events

        #region Cached Info

        static private readonly SysUpdateAttribute[] s_UpdateAttributeCache = new SysUpdateAttribute[SystemIndex.Capacity];
        static private readonly SysUpdateAttribute DefaultUpdateAttribute = new SysUpdateAttribute(GameLoopPhase.Update, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private SysUpdateAttribute CacheUpdateInfo(Type type) {
            int index = SystemIndex.Get(type);
            SysUpdateAttribute update = s_UpdateAttributeCache[index];
            if (update == null) {
                update = Reflect.GetAttribute<SysUpdateAttribute>(type);
                if (update == null && (HasOwnMethod(type, "ProcessWork") || HasOwnMethod(type, "ProcessWorkForComponent"))) {
                    update = DefaultUpdateAttribute;
                }
                s_UpdateAttributeCache[index] = update;
            }
            return update;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private SysUpdateAttribute GetUpdateInfo(Type type) {
            return s_UpdateAttributeCache[SystemIndex.Get(type)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private SysUpdateAttribute GetUpdateInfo(int index) {
            return s_UpdateAttributeCache[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool HasOwnMethod(Type type, string methodName) {
            MethodInfo info = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return info != null && info.DeclaringType == type;
        }

        #endregion // Cached Info

        #region Debug

        private enum DebuggingFlags {
            DisplayStats,
            DisplayOrder
        }

#if DEVELOPMENT

        static private int s_DebugBucketIndex;

        internal void RenderDebugInfo() {
            if (DebugFlags.IsFlagSet(DebuggingFlags.DisplayStats)) {
                using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    psb.Builder.Append("System Count: ").AppendNoAlloc(m_AllSystems.Count);
                    for(int i = 0; i < BucketList.Length; i++) {
                        psb.Builder.Append("\n   ").Append(BucketNames[i]).Append(": ").AppendNoAlloc(m_Updates[BucketList[i]].Count);
                    }
                    DebugDraw.AddLogText(psb, ColorBank.Azure);
                }
            }

            if (DebugFlags.IsFlagSet(DebuggingFlags.DisplayOrder)) {
                var systems = GetSortedUpdatesForPhase(BucketList[s_DebugBucketIndex]);
                bool isLoading = GameLoop.IsLoading;
                int categoryMask = GameLoop.UpdateMask;
                using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    psb.Builder.Append("Phase ").Append(BucketNames[s_DebugBucketIndex]).Append(": ").AppendNoAlloc(systems.Count);
                    if (systems.Count > 0) {
                        int prevOrder = systems[0].UpdateOrder;
                        foreach(var sys in systems) {
                            if (sys.UpdateOrder != prevOrder) {
                                psb.Builder.Append("\n ---");
                                prevOrder = sys.UpdateOrder;
                            }
                            psb.Builder.Append('\n');
                            if ((sys.AllowDuringLoad || !isLoading) && (categoryMask & sys.CategoryMask) != 0) {
                                psb.Builder.Append("[X] ");
                            } else {
                                psb.Builder.Append("[ ] ");
                            }
                            psb.Builder.Append(sys.System.GetType().FullName);
                        }
                    }
                    DebugDraw.AddViewportText(new Vector2(0, 1), new Vector2(16, -16), psb, ColorBank.Wheat, 0, TextAnchor.UpperLeft, DebugTextStyle.BackgroundDark);
                }
            }
        }

        static private readonly GameLoopPhase[] BucketList = new GameLoopPhase[] {
            GameLoopPhase.DebugUpdate, GameLoopPhase.PreUpdate,
            GameLoopPhase.FixedUpdate, GameLoopPhase.LateFixedUpdate,
            GameLoopPhase.Update, GameLoopPhase.UnscaledUpdate,
            GameLoopPhase.LateUpdate, GameLoopPhase.UnscaledLateUpdate,
            GameLoopPhase.ApplicationPreRender,
        };

        static private readonly string[] BucketNames = new string[] {
            "Debug Update", "PreUpdate",
            "Fixed Update", "Late Fixed Update",
            "Update", "Unscaled Update",
            "Late Update", "Unscaled Late Update",
            "Application PreRender"
        };
        
        private RingBuffer<UpdateRecord> GetSortedUpdatesForPhase(GameLoopPhase phase) {
            RingBuffer<UpdateRecord> systems = m_Updates[phase];
            if (m_Updates.PopBucketDirty(phase)) {
                systems.Sort(UpdateRecord.Comparer);
            }
            return systems;
        }

        [EngineMenuFactory]
        static private DMInfo CreateSystemsDebugMenu() {
            DMInfo info = new DMInfo("ECS Systems", 24);
            
            DebugFlags.Menu.AddFlagToggle(info, "Display Stats", DebuggingFlags.DisplayStats);
            info.AddDivider();
            
            DMPredicate groupPredicate = () => DebugFlags.IsFlagSet(DebuggingFlags.DisplayOrder);
            DebugFlags.Menu.AddFlagToggle(info, "Display Order", DebuggingFlags.DisplayOrder);
            info.AddSlider("Bucket Selection", () => s_DebugBucketIndex, (f) => s_DebugBucketIndex = (int)f, 0, BucketList.Length - 1, 1,
                (f) => BucketNames[(int)f], groupPredicate, 1);
            return info;
        }

#endif // DEVELOPMENT

        #endregion // Debug
    }
}