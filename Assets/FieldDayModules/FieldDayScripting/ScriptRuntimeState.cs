using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf;
using Leaf.Runtime;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace FieldDay.Scripting {
    [SharedStateInitOrder(-10)]
    public class ScriptRuntimeState : ISharedState, IRegistrationCallbacks, ISceneLoadDependency {
        #region State

        // Thread Tracking
        internal LeafThreadHandle Cutscene;
        internal readonly ScriptThreadMap ActorThreadMap = new ScriptThreadMap(32);
        internal readonly RingBuffer<LeafThreadHandle> ActiveThreads = new RingBuffer<LeafThreadHandle>(16, RingBufferMode.Expand);

        // Actor Tracking
        internal readonly ScriptActorMap<ScriptActor> Actors = new ScriptActorMap<ScriptActor>(16);

        // Signals
        internal readonly EventDispatcher<Variant> SignalMap = new EventDispatcher<Variant>(8, 8, 4);

        // Plugin
        internal ScriptPlugin Plugin;
        internal MethodCache<LeafMember> MethodCache;

        // Tag String
        internal CustomTagParserConfig TagParserConfig;
        internal TagStringEventHandler TagEventHandler;
        internal HashSet<StringHash32> SkippableTagEvents;
        internal HashSet<StringHash32> TextOutputTagEvents;
        internal TagStringParser TagParser;

        // Pools
        internal IPool<ScriptThread> ThreadPool;
        internal IPool<VariantTable> TablePool;

        // Variable Resolvers
        internal CustomVariantResolver Resolver;
        internal CustomVariantResolver ResolverOverride;

        // Randomization
        internal System.Random Random = new System.Random();

        // Execution State
        internal int PauseDepth;

        // current history buffer
        internal ScriptHistoryData CurrentHistoryBuffer;

        // temporary script table
        internal VariantTable SceneLocalTable;

        // skipping
        internal Routine SkipCutsceneRoutine;
        internal bool IsSkippingCutscene;

        private Routine m_BootRoutine;

        #endregion // State

        #region Callbacks

        public readonly CastableEvent<ScriptThread, TagString> OnTaggedLineProcessed = new CastableEvent<ScriptThread, TagString>();

        #endregion // Callbacks

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Resolver = new CustomVariantResolver();
            MethodCache = LeafUtils.CreateMethodCache(typeof(IScriptActorComponent));

            ResolverOverride = new CustomVariantResolver();
            ResolverOverride.Base = Resolver;

            TagParserConfig = new CustomTagParserConfig();
            TagEventHandler = new TagStringEventHandler();

            ThreadPool = new DynamicPool<ScriptThread>(16, (p) => {
                return new ScriptThread(p, Plugin);
            });

            TablePool = new FixedPool<VariantTable>(16, Pool.DefaultConstructor<VariantTable>());
            TablePool.Config.RegisterOnAlloc((p, t) => t.Name = "temp");
            TablePool.Config.RegisterOnFree((p, t) => t.Reset());
            TablePool.Prewarm();

            TagParser = new TagStringParser(TagStringParser.CurlyBraceDelimiters);
            TagParser.EventProcessor = TagParserConfig;
            TagParser.ReplaceProcessor = TagParserConfig;

            CurrentHistoryBuffer = new ScriptHistoryData(64);

            Plugin = new ScriptPlugin(this, ScriptUtility.DB);
            ThreadPool.Prewarm();

            TagEvents.ConfigureParsers(TagParserConfig, Plugin);
            TagEvents.ConfigureHandlers(TagEventHandler, Plugin);
            DefaultLeaf.ConfigureDefaultVariables(Resolver);

            SceneLocalTable = new VariantTable("temp");
            SceneLocalTable.Capacity = 64;
            ScriptUtility.BindTable("temp", SceneLocalTable);

            Game.Scenes.OnMainSceneLateEnable.Register(() => {
                SceneLocalTable.Clear();
            });
            Game.Scenes.OnMainSceneUnloaded.Register(() => {
                SignalMap.CleanupDeadReferences();
            });

            Game.Scenes.QueueOnEnable(InitialMethodCache);

            SmokeTestMgr.RegisterResetHandler(() => {
                ScriptUtility.KillAllThreads();
                CurrentHistoryBuffer.RecentlyViewedNodeIds.Clear();
                foreach(var buff in CurrentHistoryBuffer.VisitedNodesMap) {
                    buff.Clear();
                }
                SceneLocalTable.Clear();
            });
        }
        // TODO: Figure out why this needs to be called later in the scene loading process
        // when in WebGL. Also why LoadStaticAsync is broken
        private void InitialMethodCache() {
            MethodCache.Load(typeof(ScriptActor));
            MethodCache.LoadStatic();
            GC.Collect();
        }

        #endregion // IRegistrationCallbacks

        #region ISceneLoadDependency

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            return loadPhase != SceneLoadPhase.BeforeLateEnable || !m_BootRoutine;
        }

        #endregion // ISceneLoadDependency
    }

    internal struct QueuedScriptEvent {
        public int Order;
        public int Id;

        public Action OnStart;
        public Action OnComplete;

        public StringHash32 TriggerId;
        public TempVarTable Vars;
        public Future<LeafThreadHandle> Return;

        #region Id

        static private int s_CurrentId = 0;

        static internal int NextId() {
            return Interlocked.Increment(ref s_CurrentId) - 1;
        }

        static internal void ResetIds() {
            Interlocked.Exchange(ref s_CurrentId, 0);
        }

        #endregion // Id
    }

    static public partial class ScriptUtility {
        public const int RuntimeUpdateMask = 0x7FFFFFFF;

        [SharedStateReference] static public ScriptRuntimeState Runtime { get; private set; }
        [SharedStateReference] static public ScriptDatabase DB { get; private set; }

        [InvokePreBoot]
        static private void Initialize() {
            Game.SharedState.Register(new ScriptDatabase());
            Game.SharedState.Register(new ScriptRuntimeState());
            Game.Systems.Register(new ScriptLoadingSystem());
            Game.Systems.Register(new ScriptRuntimeTickSystem());
        }

        #region Tables

        /// <summary>
        /// Binds a named variable table to the runtime.
        /// </summary>
        static public void BindTable(StringHash32 id, VariantTable table) {
            Runtime.Resolver.SetTable(id, table);
        }

        /// <summary>
        /// Removes a named variable table from the runtime.
        /// </summary>
        static public void UnbindTable(StringHash32 id) {
            Runtime.Resolver.ClearTable(id);
        }

        #endregion // Tables

        #region Variables

        /// <summary>
        /// Binds a named variable to the runtime.
        /// </summary>
        static public void BindVariable(TableKeyPair keyPair, CustomVariantResolver.GetVarDelegate resolver) {
            Runtime.Resolver.SetVar(keyPair, resolver);
        }

        /// <summary>
        /// Removes a named variable from the runtime.
        /// </summary>
        static public void UnbindVariable(TableKeyPair keyPair) {
            Runtime.Resolver.ClearVar(keyPair);
        }

        /// <summary>
        /// Reads the variable at the given location.
        /// </summary>
        static public Variant ReadVariable(TableKeyPair keyPair, Variant defaultVal = default) {
            Variant result;
            if (!Runtime.Resolver.TryResolve(null, keyPair, out result)) {
                result = defaultVal;
            }
            return result;
        }

        /// <summary>
        /// Writes a variable to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void WriteVariable(TableKeyPair keyPair, Variant value) {
            Runtime.Resolver.TryModify(null, keyPair, VariantModifyOperator.Set, value);
        }

        #endregion // Variables

        #region Tag Parsing

        /// <summary>
        /// Parses the given string into the given TagString.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void ParseTag(ref TagString tagString, StringSlice line, object context = null) {
            Runtime.TagParser.Parse(ref tagString, line, context);
        }

        /// <summary>
        /// Returns the character id embedded in the given line.
        /// </summary>
        static public StringHash32 GetCharacterId(TagString tagString) {
            tagString.TryFindEvent(LeafUtils.Events.Character, out var evtData);
            return evtData.Argument0.AsStringHash();
        }

        #endregion // Tag Parsing

        #region Actors

        /// <summary>
        /// Locates the actor for the given id.
        /// </summary>
        static public ScriptActor FindActor(StringHash32 actorId) {
            Runtime.Actors.TryGet(actorId, out ScriptActor actor);
            return actor;
        }

        /// <summary>
        /// Returns the actor for the given GameObject.
        /// </summary>
        static public ScriptActor Actor(GameObject go) {
            if (go && go.TryGetComponent<ScriptActor>(out var actor)) {
                return actor;
            }
            return null;
        }

        /// <summary>
        /// Returns the actor id for the given GameObject.
        /// </summary>
        static public StringHash32 ActorId(GameObject go) {
            if (go && go.TryGetComponent<ScriptActor>(out var actor)) {
                return actor.Id;
            }
            return default;
        }

        /// <summary>
        /// Returns the actor for the given Component.
        /// </summary>
        static public ScriptActor Actor(Component comp) {
            if (comp && comp.TryGetComponent<ScriptActor>(out var actor)) {
                return actor;
            }
            return null;
        }


        /// <summary>
        /// Returns the actor id for the given Component.
        /// </summary>
        static public StringHash32 ActorId(Component comp) {
            if (comp && comp.TryGetComponent<ScriptActor>(out var actor)) {
                return actor.Id;
            }
            return default;
        }

        /// <summary>
        /// Returns the actor id for the given actor component.
        /// </summary>
        static public StringHash32 ActorId(ScriptActorComponent comp) {
            return comp.Actor.Id;
        }

        /// <summary>
        /// Returns the actor id for the given actor.
        /// </summary>
        static public StringHash32 ActorId(ScriptActor actor) {
            return actor.Id;
        }

        /// <summary>
        /// Returns the actor type for the given Component.
        /// </summary>
        static public StringHash32 ActorType(Component comp) {
            if (comp && comp.TryGetComponent<ScriptActor>(out var actor)) {
                return actor.ClassName;
            }
            return default;
        }

        /// <summary>
        /// Returns the actor type for the given actor component.
        /// </summary>
        static public StringHash32 ActorType(ScriptActorComponent comp) {
            return comp.Actor.ClassName;
        }

        /// <summary>
        /// Returns the actor type for the given actor.
        /// </summary>
        static public StringHash32 ActorType(ScriptActor actor) {
            return actor.ClassName;
        }

        /// <summary>
        /// Writes actor parameters to the given temporary variable table.
        /// </summary>
        static public void ActorInfo(this TempVarTable table, ScriptActor actor) {
            table.Set("objectId", actor?.Id ?? StringHash32.Null);
            table.Set("objectType", actor?.ClassName ?? StringHash32.Null);
        }

        /// <summary>
        /// Writes actor parameters to the given temporary variable table.
        /// </summary>
        static public void ActorInfo(this TempVarTable table, ScriptActor actor, string idField, string typeField) {
            if (!string.IsNullOrEmpty(idField)) {
                table.Set(idField, actor?.Id ?? StringHash32.Null);
            }
            if (!string.IsNullOrEmpty(typeField)) {
                table.Set(typeField, actor?.ClassName ?? StringHash32.Null);
            }
        }

        #endregion // Actors

        #region Context

        static private LeafEvalContext GetEvalContext(ILeafActor actor, VariantTable table) {
            if (table == null || table.Count == 0) {
                return LeafEvalContext.FromResolver(Runtime.Plugin, Runtime.Resolver, actor);
            }

            Runtime.ResolverOverride.SetDefaultTable(table);
            return LeafEvalContext.FromResolver(Runtime.Plugin, Runtime.ResolverOverride, actor);
        }

        #endregion // Context

        #region Functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Invoke(StringHash32 functionId, VariantTable vars = null) {
            Invoke(functionId, default, null, vars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Invoke(StringHash32 functionId, ILeafActor actor, VariantTable vars = null) {
            Invoke(functionId, actor?.Id ?? StringHash32.Null, actor, vars);
        }

        static public void Invoke(StringHash32 functionId, StringHash32 targetId, ILeafActor actor, VariantTable vars = null) {
            using (PooledList<ScriptNode> funcNodes = PooledList<ScriptNode>.Create()) {
                ScriptNodeLookupArgs lookup;
                lookup.TargetId = targetId;
                lookup.History = Runtime.CurrentHistoryBuffer;
                lookup.Randomizer = Runtime.Random;
                lookup.ThreadMap = Runtime.ActorThreadMap;
                lookup.CurrentlyInCutsceneOrBlockingState = Runtime.Cutscene.IsRunning();
                lookup.CurrentTime = Time.time;
                lookup.EvalContext = GetEvalContext(actor, vars);
                ScriptDBUtility.FindAllFunctions(DB, functionId, lookup, funcNodes);
                foreach (var node in funcNodes) {
                    Runtime.Plugin.Run(node, targetId, actor, vars, "Function Invokation", true);
                }
                Log.Msg("[ScriptUtility] Invoked '{0}', {1} response(s)", functionId.ToDebugString(), funcNodes.Count.ToStringLookup());
            }
        }

        #endregion // Functions

        #region Trigger

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public LeafThreadHandle Trigger(StringHash32 triggerId, VariantTable vars = null) {
            return Trigger(triggerId, default, null, vars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public LeafThreadHandle Trigger(StringHash32 triggerId, ILeafActor actor, VariantTable vars = null) {
            return Trigger(triggerId, actor?.Id ?? StringHash32.Null, actor, vars);
        }

        static public LeafThreadHandle Trigger(StringHash32 triggerId, StringHash32 targetId, ILeafActor actor, VariantTable vars = null) {
            Invoke(triggerId, targetId, actor, vars);

            ScriptNodeLookupArgs lookup;
            lookup.TargetId = targetId;
            lookup.History = Runtime.CurrentHistoryBuffer;
            lookup.Randomizer = Runtime.Random;
            lookup.ThreadMap = Runtime.ActorThreadMap;
            lookup.CurrentlyInCutsceneOrBlockingState = Runtime.Cutscene.IsRunning();
            lookup.CurrentTime = Time.time;
            lookup.EvalContext = GetEvalContext(actor, vars);

            ScriptNode node = ScriptDBUtility.FindRandomTrigger(DB, triggerId, lookup);
            if (node != null) {
                Log.Msg("[ScriptUtility] Triggered '{0}', found response '{1}'", triggerId.ToDebugString(), node.FullName);
                return Runtime.Plugin.Run(node, targetId, actor, vars, "Trigger Invokation", true);
            }

            Log.Msg("[ScriptUtility] Triggered '{0}', no response", triggerId.ToDebugString());
            return default;
        }

        #endregion // Trigger

        #region Spawn

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public LeafThreadHandle SpawnThread(StringHash32 nodeId, VariantTable vars = null) {
            return SpawnThread(nodeId, null, vars);
        }

        static public LeafThreadHandle SpawnThread(StringHash32 nodeId, ILeafActor actor, VariantTable vars = null) {
            if (ScriptDBUtility.TryLookupExposedNode(DB, nodeId, out ScriptNode node)) {
                return Runtime.Plugin.Run(node, actor?.Id ?? StringHash32.Null, actor, vars, "Spawn Directly", true);
            }

            Log.Warn("[ScriptUtility] No exposed node with id '{0}' found", nodeId.ToDebugString());
            return default;
        }

        #endregion // Spawn

        #region Vox

        static internal VoxPriority ScriptPriorityToVoxPriority(ScriptNodePriority priority) {
            return (VoxPriority) priority;
        }

        #endregion // Vox

        #region Stopping

        /// <summary>
        /// Kills all running threads associated with the given actor.
        /// </summary>
        static public int KillThreads(ILeafActor actor) {
            int killed = 0;
            var table = Runtime.ActiveThreads;
            for(int i = table.Count - 1; i >= 0; i--) {
                var thread = table[i].GetThread();
                if (thread != null && thread.Actor == actor) {
                    table[i].Kill();
                    killed++;
                }
            }
            return killed;
        }

        /// <summary>
        /// Kills all running threads.
        /// </summary>
        static public int KillAllThreads() {
            int killed = 0;
            var table = Runtime.ActiveThreads;
            for (int i = table.Count - 1; i >= 0; i--) {
                var thread = table[i].GetThread();
                if (thread != null) {
                    table[i].Kill();
                    killed++;
                }
            }
            return killed;
        }

        /// <summary>
        /// Kills all running threads with a lower priority than the given priority.
        /// </summary>
        static public int KillLowPriorityThreads(ScriptNodePriority threshold = ScriptNodePriority.Cutscene, bool killFunctions = false) {
            int killed = 0;
            var table = Runtime.ActiveThreads;
            for (int i = table.Count - 1; i >= 0; i--) {
                var thread = table[i].GetThread<ScriptThread>();
                if (thread != null && (thread.Priority() < threshold) && (killFunctions || !thread.IsFunction())) {
                    table[i].Kill();
                    killed++;
                }
            }
            return killed;
        }

        #endregion // Stopping

        #region Who

        /// <summary>
        /// Resolves the target id for a specific thread.
        /// </summary>
        static public StringHash32 ResolveThreadTarget(StringHash32 targetId, ScriptNode node) {
            return targetId.IsEmpty ? ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 ? node.TargetId : default(StringHash32)) : targetId;
        }

        #endregion // Who

        #region Active Threads

        /// <summary>
        /// Handle for the currently playing cutscene.
        /// </summary>
        static public LeafThreadHandle CurrentCutscene {
            [Il2CppSetOption(Option.NullChecks, false)]
            get { return Runtime.Cutscene; }
        }

        /// <summary>
        /// Handle for the currently playing cutscene.
        /// </summary>
        static public RingBuffer<LeafThreadHandle>.Enumerator CurrentThreads {
            [Il2CppSetOption(Option.NullChecks, false)]
            get { return Runtime.ActiveThreads.GetEnumerator(); }
        }

        /// <summary>
        /// Performs an action on each thread handle.
        /// </summary>
        static public int ForEachThreadHandle(Action<LeafThreadHandle> action) {
            using(PooledList<LeafThreadHandle> threads = PooledList<LeafThreadHandle>.Create()) {
                threads.AddRange(Runtime.ActiveThreads);
                int count = 0;
                foreach(var handle in threads) {
                    if (handle.IsRunning()) {
                        action(handle);
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Performs an action on each thread.
        /// </summary>
        static public int ForEachThread(Action<ScriptThread> action) {
            using (PooledList<LeafThreadHandle> threads = PooledList<LeafThreadHandle>.Create()) {
                threads.AddRange(Runtime.ActiveThreads);
                int count = 0;
                foreach (var handle in threads) {
                    ScriptThread thread = handle.GetThread<ScriptThread>();
                    if (thread != null) {
                        action(thread);
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion // Active Threads

        #region Cutscenes

        /// <summary>
        /// Returns if a cutscene is currently being skipped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public bool IsSkippingCutscene() {
            return Runtime.IsSkippingCutscene;
        }

        #endregion // Cutscenes

        #region Signals

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void RegisterForSignal(StringHash32 signalId, Action action, UnityEngine.Object context = null) {
            Runtime.SignalMap.Register(signalId, action, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DeregisterFromSignal(StringHash32 signalId, Action action) {
            Runtime.SignalMap.Deregister(signalId, action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void RegisterForSignal(StringHash32 signalId, Action<Variant> action, UnityEngine.Object context = null) {
            Runtime.SignalMap.Register(signalId, action, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DeregisterFromSignal(StringHash32 signalId, Action<Variant> action) {
            Runtime.SignalMap.Deregister(signalId, action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DeregisterAllSignalsForContext(UnityEngine.Object context) {
            Runtime.SignalMap.DeregisterAllForContext(context);
        }

        #endregion // Signals
    }
}