using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Mathematics {
    static public class DependencySolver {
        public enum Result {
            Success = 0,
            CycleDetected = 1,
            MissingNode = 2
        }

        public struct Node<T> where T : unmanaged {
            public T Id;
            public OffsetLengthU16 Edges;
        }

        public struct Edge<T> where T : unmanaged {
            public T Endpoint;
        }

        public struct OutputNode<T> where T : unmanaged {
            public T Id;
            public int OriginalIndex;
        }

        private struct MultiEdgeSolverData<T> where T : unmanaged {
            public UnsafeSpan<Node<T>> Nodes;
            public UnsafeSpan<Edge<T>> Edges;
            public UnsafeSpan<OutputNode<T>> Output;
            public UnsafeBitSet VisitBits;
            public UnsafeBitSet CycleDetectionBits;
            public int OutputCount;
        }

        static public unsafe Result Solve<T>(UnsafeSpan<Node<T>> nodes, UnsafeSpan<Edge<T>> edges, UnsafeSpan<OutputNode<T>> output) where T : unmanaged {
            Assert.True(output.Length >= nodes.Length);

            int nodeCount = nodes.Length;
            int requiredBackingMemSize = Unsafe.AlignUp32(nodeCount) / 32;
            uint* visitMem = stackalloc uint[requiredBackingMemSize];
            uint* cycleMem = stackalloc uint[requiredBackingMemSize];

            UnsafeBitSet visited = new UnsafeBitSet(visitMem, requiredBackingMemSize);
            UnsafeBitSet cycle = new UnsafeBitSet(cycleMem, requiredBackingMemSize);

            MultiEdgeSolverData<T> solverData = new MultiEdgeSolverData<T>() {
                Nodes = nodes,
                Edges = edges,
                Output = output,
                VisitBits = visited,
                CycleDetectionBits = cycle
            };

            Result result = Result.Success;

            for(int i = 0; i < nodeCount; i++) {
                if (!visited.IsSet(i)) {
                    result = Traverse(ref solverData, i);
                    if (result != Result.Success) {
                        break;
                    }
                }
            }

            Assert.True(result != Result.Success || solverData.OutputCount == nodes.Length);
            return result;
        }

        static private Result Traverse<T>(ref MultiEdgeSolverData<T> solverData, int nodeIndex) where T : unmanaged {
            if (solverData.VisitBits.IsSet(nodeIndex)) {
                return Result.Success;
            }
            if (solverData.CycleDetectionBits.IsSet(nodeIndex)) {
                return Result.CycleDetected;
            }

            solverData.CycleDetectionBits.Set(nodeIndex);
            Node<T> node = solverData.Nodes[nodeIndex];

            for(int edgeIdx = node.Edges.Offset; edgeIdx < node.Edges.End; edgeIdx++) {
                int nextIdx = IndexOfNode(solverData.Nodes, solverData.Edges[edgeIdx].Endpoint);
                if (nextIdx < 0) {
                    return Result.MissingNode;
                }
                Result edgeResult = Traverse(ref solverData, nextIdx);
                if (edgeResult != Result.Success) {
                    return edgeResult;
                }
            }

            solverData.CycleDetectionBits.Unset(nodeIndex);
            solverData.VisitBits.Set(nodeIndex);

            solverData.Output[solverData.OutputCount++] = new OutputNode<T>() {
                Id = node.Id,
                OriginalIndex = nodeIndex
            };
            return Result.Success;
        }

        static private int IndexOfNode<T>(UnsafeSpan<Node<T>> nodes, in T id) where T : unmanaged {
            for(int i = 0; i < nodes.Length; i++) {
                if (CompareUtils.Equals(id, nodes[i].Id)) {
                    return i;
                }
            }
            return -1;
        }

        static private int IndexOfNode<T>(UnsafeSpan<T> nodes, in T id) where T : unmanaged {
            for (int i = 0; i < nodes.Length; i++) {
                if (CompareUtils.Equals(id, nodes[i])) {
                    return i;
                }
            }
            return -1;
        }
    }
}