using BeauUtil;

namespace FieldDay.Mathematics {
    static public class DependencySolver {
        public struct SourceNode<TIdentifier> where TIdentifier : unmanaged {
            public TIdentifier Id;
            public UnsafeSpan<TIdentifier> Dependencies;
        }

        public struct OutputNode {
            public UnsafeSpan<uint> DependencyIndices;
        }
    }
}