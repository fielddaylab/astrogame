using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Data {
    /// <summary>
    /// Set of nodes, both sorted and unsorted.
    /// </summary>
    public struct NodeBucket<TNode> {
        public RingBuffer<PrioritizedNode<TNode>> Ordered;
        public HashSet<TNode> Unordered;
        public bool OrderedDirty;

        #region Factory

        static public NodeBucket<TNode> Create(int orderedCapacity, int unorderedCapacity) {
            NodeBucket<TNode> bucket;
            bucket.Ordered = new RingBuffer<PrioritizedNode<TNode>>(orderedCapacity, RingBufferMode.Expand);
            bucket.Unordered = new HashSet<TNode>(unorderedCapacity);
            bucket.OrderedDirty = false;
            return bucket;
        }

        static public NodeBucket<TNode> Create() {
            NodeBucket<TNode> bucket;
            bucket.Ordered = new RingBuffer<PrioritizedNode<TNode>>();
            bucket.Unordered = new HashSet<TNode>();
            bucket.OrderedDirty = false;
            return bucket;
        }

        #endregion // Factory
    }

    public struct PrioritizedNode<TNode> {
        public int Score;
        public TNode Node;

        public PrioritizedNode(TNode node, int score) {
            Node = node;
            Score = score;
        }

        static public readonly Comparison<PrioritizedNode<TNode>> ScoreSorter = (a, b) => {
            return b.Score - a.Score;
        };
    }

    static public class NodeBucketUtility {
        #region Unsorted

        /// <summary>
        /// Adds an unsorted node to the bucket.
        /// </summary>
        static public void AddUnsorted<TNode>(ref NodeBucket<TNode> bucket, TNode node) {
            bucket.Unordered.Add(node);
        }

        /// <summary>
        /// Removes an unsorted node from the bucket.
        /// </summary>
        static public void RemoveUnsorted<TNode>(ref NodeBucket<TNode> bucket, TNode node) {
            bucket.Unordered.Remove(node);
        }

        /// <summary>
        /// Retrieves all valid unsorted nodes that pass the given predicate.
        /// </summary>
        static public int GetAllUnsorted<TNode, TArgs>(ref NodeBucket<TNode> bucket, in TArgs args, Predicate<TNode, TArgs> predicate, ICollection<TNode> validNodes) where TArgs : struct {
            int count = 0;
            foreach (var node in bucket.Unordered) {
                if (!predicate(node, args)) {
                    continue;
                }

                validNodes.Add(node);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Retrieves all valid unsorted nodes that pass the given predicate.
        /// </summary>
        static public unsafe int GetAllUnsorted<TNode, TArgs>(ref NodeBucket<TNode> bucket, in TArgs args, delegate*<TNode, TArgs, bool> predicate, ICollection<TNode> validNodes) where TArgs : struct {
            int count = 0;
            foreach (var node in bucket.Unordered) {
                if (!predicate(node, args)) {
                    continue;
                }

                validNodes.Add(node);
                count++;
            }

            return count;
        }

        #endregion // Unsorted

        #region Sorted

        /// <summary>
        /// Adds a sorted node to the bucket with the given score.
        /// Returns if the bucket needs sorting.
        /// </summary>
        static public bool AddSorted<TNode>(ref NodeBucket<TNode> bucket, TNode node, int score) {
            bucket.Ordered.PushBack(new PrioritizedNode<TNode>(node, score));
            int sortedItemCount = bucket.Ordered.Count;
            if (!bucket.OrderedDirty && sortedItemCount > 1 && bucket.Ordered[sortedItemCount - 2].Score < score) {
                bucket.OrderedDirty = true;
                return true;
            }

            return bucket.OrderedDirty;
        }

        /// <summary>
        /// Removes a sorted node from the bucket.
        /// Returns if the bucket needs sorting.
        /// </summary>
        static public bool RemoveSorted<TNode>(ref NodeBucket<TNode> bucket, TNode node) {
            var equality = CompareUtils.DefaultEquals<TNode>();
            int idx = bucket.Ordered.FindIndex((i, n) => equality.Equals(i.Node, n), node);
            if (idx >= 0) {
                bucket.Ordered.FastRemoveAt(idx);
                bucket.OrderedDirty = true;
                return true;
            }

            return bucket.OrderedDirty;
        }

        /// <summary>
        /// Ensures that a bucket's sorted nodes are sorted.
        /// </summary>
        static public bool EnsureSorted<TNode>(ref NodeBucket<TNode> bucket) {
            if (!bucket.OrderedDirty) {
                return false;
            }

            bucket.Ordered.Sort(PrioritizedNode<TNode>.ScoreSorter);
            bucket.OrderedDirty = false;
            return true;
        }

        /// <summary>
        /// Outputs the highest scoring set of valid nodes that pass the given predicate.
        /// Returns the number of nodes that passed.
        /// </summary>
        static public int GetHighestScoringSorted<TNode, TArgs>(ref NodeBucket<TNode> bucket, in TArgs args, Predicate<TNode, TArgs> predicate, ICollection<TNode> validNodes) where TArgs : struct {
            Assert.False(bucket.OrderedDirty, "EnsureSorted must be called before GetHighestScoringSorted");

            int count = 0;
            int minScore = int.MinValue;
            foreach (var scoreNodePair in bucket.Ordered) {
                if (scoreNodePair.Score < minScore) {
                    break;
                }

                if (!predicate(scoreNodePair.Node, args)) {
                    continue;
                }

                minScore = scoreNodePair.Score;
                validNodes.Add(scoreNodePair.Node);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Outputs the highest scoring set of valid nodes that pass the given predicate.
        /// Returns the number of nodes that passed.
        /// </summary>
        static public unsafe int GetHighestScoringSorted<TNode, TArgs>(ref NodeBucket<TNode> bucket, in TArgs args, delegate*<TNode, TArgs, bool> predicate, ICollection<TNode> validNodes) where TArgs : struct {
            Assert.False(bucket.OrderedDirty, "EnsureSorted must be called before GetHighestScoringSorted");

            int count = 0;
            int minScore = int.MinValue;
            foreach (var scoreNodePair in bucket.Ordered) {
                if (scoreNodePair.Score < minScore) {
                    break;
                }

                if (!predicate(scoreNodePair.Node, args)) {
                    continue;
                }

                minScore = scoreNodePair.Score;
                validNodes.Add(scoreNodePair.Node);
                count++;
            }

            return count;
        }

        #endregion // Sorted

        #region Clear
        
        /// <summary>
        /// Clears all nodes from the given bucket.
        /// </summary>
        static public void Clear<TNode>(ref NodeBucket<TNode> bucket) {
            bucket.Ordered.Clear();
            bucket.Unordered.Clear();
            bucket.OrderedDirty = false;
        }

        #endregion // Clear
    }
}