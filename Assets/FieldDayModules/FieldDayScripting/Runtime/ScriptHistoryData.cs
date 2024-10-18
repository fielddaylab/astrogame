using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting history data.
    /// </summary>
    public sealed class ScriptHistoryData {
        public const int ScopeCount = 3;
        public const int HistorySize = 64;

        /// <summary>
        /// Record of visited nodes and when they were visited.
        /// </summary>
        public struct TimestampedNodeRecord {
            public StringHash32 Id;
            public float Timestamp;
        }

        /// <summary>
        /// Circular buffer of visited nodes.
        /// </summary>
        public RingBuffer<TimestampedNodeRecord> RecentlyViewedNodeIds;

        /// <summary>
        /// Set of visited nodes.
        /// </summary>
        public HashSet<StringHash32>[] VisitedNodesMap = new HashSet<StringHash32>[ScopeCount];

        public ScriptHistoryData(int visitedNodeCapacity) {
            RecentlyViewedNodeIds = new RingBuffer<TimestampedNodeRecord>(HistorySize, RingBufferMode.Overwrite);
            for (int i = 0; i < ScopeCount; i++) {
                VisitedNodesMap[i] = SetUtils.Create<StringHash32>(visitedNodeCapacity);
            }
        }

        #region Queries

        /// <summary>
        /// Returns if the given node has been recently seen in a specific window.
        /// </summary>
        public bool HasRecentlySeen(StringHash32 nodeId, int window) {
            window = Math.Min(window, RecentlyViewedNodeIds.Count);
            for(int i = 0; i < window; i++) {
                if (RecentlyViewedNodeIds[i].Id == nodeId) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given node has been recently seen in a specific time window.
        /// </summary>
        public bool HasRecentlySeenAfterTimestamp(StringHash32 nodeId, float minTimestamp) {
            for (int i = 0; i < RecentlyViewedNodeIds.Count; i++) {
                var pair = RecentlyViewedNodeIds[i];
                if (pair.Timestamp < minTimestamp) {
                    return false;
                }

                if (pair.Id == nodeId) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given node has been seen in a specific scope.
        /// </summary>
        public bool HasSeen(StringHash32 nodeId, ScriptNodeMemoryScope scope) {
            switch (scope) {
                case ScriptNodeMemoryScope.Untracked: {
                    for(int i = 0; i < RecentlyViewedNodeIds.Count; i++) {
                        if (RecentlyViewedNodeIds[i].Id == nodeId) {
                            return true;
                        }
                    }

                    return false;
                }

                default: {
                    int idx = (int) scope - 1;
                    Assert.True(idx >= 0 && idx < ScopeCount, "Invalid memory scope value '{0}'", scope);
                    return VisitedNodesMap[idx].Contains(nodeId);
                }
            }
        }

        #endregion // Queries

        #region Actions

        /// <summary>
        /// Clears the recent node history.
        /// </summary>
        public void ClearRecentHistory() {
            RecentlyViewedNodeIds.Clear();
        }

        /// <summary>
        /// Clears a given scope.
        /// </summary>
        public void ClearScope(ScriptNodeMemoryScope scope) {
            int idx = (int) scope - 1;
            Assert.True(idx >= 0 && idx < ScopeCount, "Invalid memory scope value '{0}'", scope);
            VisitedNodesMap[idx].Clear();
        }

        /// <summary>
        /// Records a visited node and stores it in the appropriate scopes.
        /// </summary>
        public void RecordVisit(StringHash32 id, ScriptNodeMemoryScope scope, float timestamp) {
            RecentlyViewedNodeIds.PushFront(new TimestampedNodeRecord() {
                Id = id,
                Timestamp = timestamp
            });

            if (scope > 0) {
                int length = (int) scope;
                Assert.True(length > 0 && length <= ScopeCount, "Invalid memory target value '{0}'", scope);
                for(int i = 0; i < length; i++) {
                    VisitedNodesMap[i].Add(id);
                }
            }
        }

        #endregion // Actions
    }
}