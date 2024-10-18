using System.Collections;
using System.Collections.Generic;

namespace FieldDay.Assets {
    static public class AssetDependencyResolver {
        public struct Dependencies {
            public IEnumerable<INamedAsset> NamedAssets;
            public IEnumerable<IGlobalAsset> GlobalAssets;
        }

        static public Dependencies GetDirectDependencies(INamedAsset namedAsset) {
            // TODO: Implement
            return default;
        }

        static public Dependencies GetDirectDependencies(IGlobalAsset globalAsset) {
            // TODO: Implement
            return default;
        }
    }
}