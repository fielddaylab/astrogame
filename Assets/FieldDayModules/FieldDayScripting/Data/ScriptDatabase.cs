#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#define HOT_RELOAD_SUPPORTED
#endif // DEVELOPMENT

using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.IO;
using BeauUtil.Streaming;
using BeauUtil.Variants;
using FieldDay.Data;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf;
using Leaf.Compiler;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent, SharedStateInitOrder(-10)]
    public sealed class ScriptDatabase : ISharedState, ISceneLoadDependency, IRegistrationCallbacks {
        public const int MaxLoadedPackages = 32;

        // loaded
        internal HashSet<ScriptNodePackage> RegisteredPackages = new HashSet<ScriptNodePackage>(MaxLoadedPackages);
        internal ScriptNodePackage[] LoadedHandleMap = new ScriptNodePackage[MaxLoadedPackages];

        // loaded nodes
        internal Dictionary<StringHash32, NodeBucket<ScriptNode>> LoadedNodeBuckets = MapUtils.Create<StringHash32, NodeBucket<ScriptNode>>(64);
        internal Dictionary<StringHash32, ScriptNode> LoadedExposedNodes = MapUtils.Create<StringHash32, ScriptNode>(16);

        // loading
        internal UniqueIdAllocator16 HandleGenerator = new UniqueIdAllocator16(MaxLoadedPackages);
        internal RingBuffer<ScriptDatabaseLoadRequest> LoadQueue = new RingBuffer<ScriptDatabaseLoadRequest>();
        internal ScriptDatabaseLoadRequest CurrentLoadRequest;

        // unload
        internal RingBuffer<UniqueId16> UnloadQueue = new RingBuffer<UniqueId16>();

        #region ISceneLoadDepencency

        public bool IsLoaded(SceneLoadPhase loadPhase) {
            if ((loadPhase & SceneLoadPhase.BeforeReady) != 0) {
                return LoadQueue.Count == 0 && !CurrentLoadRequest.ParseHandle.IsRunning();
            }
            return !CurrentLoadRequest.ParseHandle.IsRunning();
        }

        #endregion // ISceneLoadDependency

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        #endregion // IRegistrationCallbacks
    }

    internal struct ScriptDatabaseLoadRequest {
        public LeafAsset Asset;
        public ScriptNodePackage Package;
        public AsyncHandle ParseHandle;
        public UniqueId16 Handle;
    }

    static public class ScriptDBUtility {
        #region Loading

        static public UniqueId16 Load(LeafAsset asset) {
            if (!asset) {
                return UniqueId16.Invalid;
            }

            var db = ScriptUtility.DB;
            if (db.CurrentLoadRequest.Asset == asset) {
                return db.CurrentLoadRequest.Handle;
            }

            foreach(var req in db.LoadQueue) {
                if (req.Asset == asset) {
                    return req.Handle;
                }
            }

            foreach(var package in db.RegisteredPackages) {
                if (package.WasFromSource(asset)) {
                    return package.m_LoadId;
                }
            }

            if (db.HandleGenerator.InUse >= ScriptDatabase.MaxLoadedPackages) {
                Log.Error("[ScriptDBUtility] Already hit maximum number of loaded scripts ({0})", ScriptDatabase.MaxLoadedPackages);
                return UniqueId16.Invalid;
            }

            UniqueId16 id = db.HandleGenerator.Alloc();
            ScriptDatabaseLoadRequest newReq;
            newReq.Asset = asset;
            newReq.Handle = id;
            newReq.Package = null;
            newReq.ParseHandle = default;
            db.LoadQueue.PushBack(newReq);
            return id;
        }

        static public void Unload(UniqueId16 id) {
            if (!ScriptUtility.DB.HandleGenerator.IsValid(id)) {
                Log.Trace("[ScriptDBUtility] Invalid/expired handle");
                return;
            }

            if (CancelCurrentLoad(ScriptUtility.DB, id)) {
                return;
            }

            ScriptUtility.DB.UnloadQueue.PushBack(id);
        }

        static internal bool CancelCurrentLoad(ScriptDatabase db, UniqueId16 loadId) {
            if (db.CurrentLoadRequest.Handle != loadId) {
                return false;
            }

            ref var req = ref db.CurrentLoadRequest;
            req.ParseHandle.Cancel();
            req.Package.Clear();
            db.HandleGenerator.Free(loadId);
            db.LoadedHandleMap[loadId.Index] = null;
            req = default;
            Log.Msg("[ScriptDBUtility] Cancelled in-flight load request");
            return true;
        }

        #endregion // Loading

        #region Package Registration

        static internal void RegisterPackage(ScriptDatabase db, ScriptNodePackage package, LeafAsset sourceAsset) {
            package.SetActive(true);

            if (!db.RegisteredPackages.Add(package)) {
                return;
            }

            BindPackage(db, package);
            package.AssignSource(sourceAsset);
        }

        static internal void DeregisterPackage(ScriptDatabase db, ScriptNodePackage package) {
            package.SetActive(false);

            if (!db.RegisteredPackages.Remove(package)) {
                return;
            }

            UnbindPackage(db, package);
            package.Clear();
        }

        static internal void BindPackage(ScriptDatabase db, ScriptNodePackage package) {
            foreach (ScriptNode node in package) {
                if ((node.Flags & ScriptNodeFlags.Exposed) != 0) {
                    db.LoadedExposedNodes.Add(node.Id(), node);
                }

                if ((node.Flags & ScriptNodeFlags.Trigger) != 0) {
                    if (!db.LoadedNodeBuckets.TryGetValue(node.TriggerOrFunctionId, out NodeBucket<ScriptNode> bucket)) {
                        bucket = db.LoadedNodeBuckets[node.TriggerOrFunctionId] = NodeBucket<ScriptNode>.Create(32, 32);
                    }

                    if (NodeBucketUtility.AddSorted(ref bucket, node, node.SortingScore)) {
                        db.LoadedNodeBuckets[node.TriggerOrFunctionId] = bucket;
                    }
                } else if ((node.Flags & ScriptNodeFlags.Function) != 0) {
                    if (!db.LoadedNodeBuckets.TryGetValue(node.TriggerOrFunctionId, out NodeBucket<ScriptNode> bucket)) {
                        bucket = NodeBucket<ScriptNode>.Create(32, 32);
                        db.LoadedNodeBuckets[node.TriggerOrFunctionId] = bucket;
                    }
                    NodeBucketUtility.AddUnsorted(ref bucket, node);
                }
            }

            if (VoxUtility.DB != null) {
                using (PooledList<KeyValuePair<StringHash32, string>> customLineNames = PooledList<KeyValuePair<StringHash32, string>>.Create()) {
                    package.GatherAllLinesWithCustomNames(customLineNames);
                    foreach (var kv in customLineNames) {
                        VoxUtility.AddHumanReadableMapping(kv.Key, kv.Value);
                    }
                }
            }
        }

        static internal void UnbindPackage(ScriptDatabase db, ScriptNodePackage package) {
            foreach (ScriptNode node in package) {
                if ((node.Flags & ScriptNodeFlags.Exposed) != 0) {
                    db.LoadedExposedNodes.Remove(node.Id());
                }

                if ((node.Flags & ScriptNodeFlags.Trigger) != 0) {
                    if (db.LoadedNodeBuckets.TryGetValue(node.TriggerOrFunctionId, out NodeBucket<ScriptNode> bucket)) {
                        if (NodeBucketUtility.RemoveSorted(ref bucket, node)) {
                            db.LoadedNodeBuckets[node.TriggerOrFunctionId] = bucket;
                        }
                    }
                } else if ((node.Flags & ScriptNodeFlags.Function) != 0) {
                    if (db.LoadedNodeBuckets.TryGetValue(node.TriggerOrFunctionId, out NodeBucket<ScriptNode> bucket)) {
                        NodeBucketUtility.RemoveUnsorted(ref bucket, node);
                    }
                }
            }

            if (VoxUtility.DB != null) {
                using (PooledList<KeyValuePair<StringHash32, string>> customLineNames = PooledList<KeyValuePair<StringHash32, string>>.Create()) {
                    package.GatherAllLinesWithCustomNames(customLineNames);
                    foreach (var kv in customLineNames) {
                        VoxUtility.RemoveHumanReadableMapping(kv.Key, kv.Value);
                    }
                }
            }
        }

        static internal void HotReload(ScriptDatabase db, ScriptNodePackage package, UniqueId16 loadId, LeafAsset asset, HotReloadAssetRemapArgs<LeafAsset> args, HotReloadOperation operation) {
            if (operation == HotReloadOperation.Deleted) {
                DeregisterPackage(db, package);
                db.HandleGenerator.Free(loadId);
                db.LoadedHandleMap[loadId.Index] = null;
                return;
            }

            UnbindPackage(ScriptUtility.DB, package);
            BlockParser.Parse(ref package, CharStreamParams.FromBytes(asset.Bytes(), asset, package.Name()), BlockParsingRules.Default, ScriptNodePackage.Parser.Instance);
            BindPackage(ScriptUtility.DB, package);
        }

        #endregion // Package Registration

        #region Lookups

        static private readonly WeightedSet<ScriptNode> s_WeightedWorkList = new WeightedSet<ScriptNode>(16);

        /// <summary>
        /// Finds a node with the given id, searching from an existing node's scope first,
        /// if provided, and then to any nodes marked with `@exposed`
        /// </summary>
        static public bool TryLookupNode(ScriptDatabase db, ScriptNode scope, StringHash32 nodeId, out ScriptNode node) {
            if (scope != null && scope.Package().TryGetNode(nodeId, out node)) {
                return true;
            }

            return db.LoadedExposedNodes.TryGetValue(nodeId, out node);
        }

        /// <summary>
        /// Finds a random valid trigger for the given bucket and request.
        /// </summary>
        static public unsafe ScriptNode FindRandomTrigger(ScriptDatabase db, StringHash32 bucketId, ScriptNodeLookupArgs request) {
            NodeBucket<ScriptNode> bucket;
            if (db.LoadedNodeBuckets.TryGetValue(bucketId, out bucket)) {
                if (NodeBucketUtility.EnsureSorted(ref bucket)) {
                    db.LoadedNodeBuckets[bucketId] = bucket;
                }
                using(PooledList<ScriptNode> lookupList = PooledList<ScriptNode>.Create()) {
#if ENABLE_IL2CPP
                    int count = NodeBucketUtility.GetHighestScoringSorted(ref bucket, request, &IsValidTrigger, lookupList);
#else
                    int count = NodeBucketUtility.GetHighestScoringSorted(ref bucket, request, IsValidTriggerCached, lookupList);
#endif // ENABLE_IL2CPP
                    if (count == 1) {
                        return lookupList[0];
                    } else if (count > 1) {
                        bool weightedSelection = false;
                        foreach(var node in lookupList) {
                            if ((node.Flags & ScriptNodeFlags.IsWeighted) != 0) {
                                weightedSelection = true;
                                break;
                            }
                        }

                        if (weightedSelection) {
                            s_WeightedWorkList.Clear();
                            foreach(var node in lookupList) {
                                s_WeightedWorkList.Add(node, node.SelectionWeight);
                            }
                            s_WeightedWorkList.Cache();
                            ScriptNode weightedChoice = s_WeightedWorkList.GetItem(request.Randomizer);
                            s_WeightedWorkList.Clear();

                            return weightedChoice;
                        } else {
                            return request.Randomizer.Choose(lookupList);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all valid functions for the given bucket and request.
        /// </summary>
        static public unsafe int FindAllFunctions(ScriptDatabase db, StringHash32 bucketId, ScriptNodeLookupArgs request, ICollection<ScriptNode> functions) {
            NodeBucket<ScriptNode> bucket;
            if (db.LoadedNodeBuckets.TryGetValue(bucketId, out bucket)) {
#if ENABLE_IL2CPP
                return NodeBucketUtility.GetAllUnsorted(ref bucket, request, &IsValidFunction, functions);
#else
                return NodeBucketUtility.GetAllUnsorted(ref bucket, request, IsValidFunctionCached, functions);
#endif // ENABLE_IL2CPP
            } else {
                return 0;
            }
        }

        #endregion // Lookups

        #region Checking

#if !ENABLE_IL2CPP
        static private readonly Predicate<ScriptNode, ScriptNodeLookupArgs> IsValidTriggerCached = IsValidTrigger;
        static private readonly Predicate<ScriptNode, ScriptNodeLookupArgs> IsValidFunctionCached = IsValidFunction;
#endif // !ENABLE_IL2CPP

        static private bool IsValidTrigger(ScriptNode node, ScriptNodeLookupArgs request) {
            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("[ScriptDatabase] Evaluating trigger node '{0}'...", node.FullName);
            }

            if (!node.Package().IsActive()) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node package '{0}' is unloading/inactive", node.Package().Name());
                }
                return false;
            }

            // if not the right target
            if ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 && !request.TargetId.IsEmpty && request.TargetId != node.TargetId) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has mismatched target '{0}' vs desired '{1}'", node.TargetId.ToDebugString(), request.TargetId.ToDebugString());
                }
                return false;
            }

            if (request.CurrentlyInCutsceneOrBlockingState) {
                // if set to ignore during a cutscene while cutscene is ongoing
                if ((node.Flags & ScriptNodeFlags.IgnoreDuringCutscene) != 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing");
                    }
                    return false;
                }

                // if cutscene, and cutscene playing, but interrupt not set
                if ((node.Flags & ScriptNodeFlags.Cutscene) != 0 && (node.Flags & ScriptNodeFlags.InterruptSamePriority) == 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing and cutscene node is not able to interrupt");
                    }
                    return false;
                }
            }

            // if set to be once
            if ((node.Flags & ScriptNodeFlags.Once) != 0 && request.History.HasSeen(node.Id(), node.PersistenceScope)) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has already been visited");
                }
                return false;
            }

            if ((node.Flags & ScriptNodeFlags.HasTimeCooldown) == 0) {
                // if has been seen within the repeat cooldown
                if (node.RepeatPeriod.NodeWindow > 0 && request.History.HasRecentlySeen(node.Id(), node.RepeatPeriod.NodeWindow)) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node has already been visited within a {0}-node window", node.RepeatPeriod.NodeWindow);
                    }
                    return false;
                }
            } else {
                // if has been seen within the repeat time cooldown
                if (node.RepeatPeriod.TimeWindow > 0 && request.History.HasRecentlySeenAfterTimestamp(node.Id(), request.CurrentTime - node.RepeatPeriod.TimeWindow)) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node has already been visited within a {0}-second window", node.RepeatPeriod.TimeWindow);
                    }
                    return false;
                }
            }

            // if target is playing a node that this node cannot interrupt
            StringHash32 targetId = request.TargetId.IsEmpty ? ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 ? node.TargetId : default(StringHash32)) : request.TargetId;
            if (!targetId.IsEmpty) {
                ScriptThread currentThread = request.ThreadMap.GetThread(targetId);
                if (currentThread != null) {
                    bool canInterrupt;
                    if ((node.Flags & ScriptNodeFlags.InterruptSamePriority) != 0) {
                        canInterrupt = currentThread.Priority() <= node.Priority;
                    } else {
                        canInterrupt = currentThread.Priority() < node.Priority;
                    }

                    if (!canInterrupt) {
                        if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                            Log.Trace("...higher-priority node '{0}' is executing for target '{1}'", currentThread.InitialNodeId().ToDebugString(), targetId.ToDebugString());
                        }
                        return false;
                    }
                }
            }

            // conditions
            if (node.Conditions.Count > 0) {
                LeafExpression failure;
                Variant result = node.Conditions.Evaluate(request.EvalContext, out failure);
                if (!result.AsBool()) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node condition '{0}' failed", failure.ToDebugString(node));
                    }
                    return false;
                }
            }

            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("...node passed!");
            }
            return true;
        }

        static private bool IsValidFunction(ScriptNode node, ScriptNodeLookupArgs request) {
            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("[ScriptDatabase] Evaluating function node '{0}'...", node.FullName);
            }

            // unloading package
            if (!node.Package().IsActive()) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node package '{0}' is unloading/inactive", node.Package().Name());
                }
                return false;
            }

            // if not the right target
            if ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 && !request.TargetId.IsEmpty && request.TargetId != node.TargetId) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has mismatched target '{0}' vs desired '{1}'", node.TargetId.ToDebugString(), request.TargetId.ToDebugString());
                }
                return false;
            }

            if (request.CurrentlyInCutsceneOrBlockingState) {
                // if set to ignore during a cutscene while cutscene is ongoing
                if ((node.Flags & ScriptNodeFlags.IgnoreDuringCutscene) != 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing");
                    }
                    return false;
                }
            }

            // if set to be once
            if ((node.Flags & ScriptNodeFlags.Once) != 0 && request.History.HasSeen(node.Id(), node.PersistenceScope)) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has already been visited");
                }
                return false;
            }

            // conditions
            if (node.Conditions.Count > 0) {
                LeafExpression failure;
                Variant result = node.Conditions.Evaluate(request.EvalContext, out failure);
                if (!result.AsBool()) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node condition '{0}' failed", failure.ToDebugString(node));
                    }
                    return false;
                }
            }

            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("...node passed!");
            }
            return true;
        }

        #endregion // Checking
    }

    public struct ScriptNodeLookupArgs {
        public LeafEvalContext EvalContext;
        public StringHash32 TargetId;
        public bool CurrentlyInCutsceneOrBlockingState;
        public ScriptThreadMap ThreadMap;
        public ScriptHistoryData History;
        public float CurrentTime;

        public System.Random Randomizer;
    }
}