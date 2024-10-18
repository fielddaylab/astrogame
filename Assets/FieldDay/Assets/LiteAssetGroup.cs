using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Asset {
    /// <summary>
    /// Group of lightweight assets.
    /// </summary>
    public abstract class LiteAssetGroup : ScriptableObject {
        internal abstract void RegisterAssets(AssetMgr mgr);
        internal abstract void DeregisterAssets(AssetMgr mgr);
    }

    /// <summary>
    /// Group of typed lightweight assets.
    /// </summary>
    public abstract class LiteAssetGroup<T> : LiteAssetGroup where T : struct, ILiteAsset {
        public T[] Assets;

        internal override void RegisterAssets(AssetMgr mgr) {
            mgr.AddLite<T>(Assets, s_CachedAssetKeyFunc ?? (s_CachedAssetKeyFunc = GetAssetKeyFunction()));
        }

        internal override void DeregisterAssets(AssetMgr mgr) {
            mgr.RemoveLite<T>(Assets, s_CachedAssetKeyFunc ?? (s_CachedAssetKeyFunc = GetAssetKeyFunction()));
        }

        /// <summary>
        /// Override to specify the asset key function.
        /// </summary>
        protected abstract AssetKeyFunction<T> GetAssetKeyFunction();

        static private AssetKeyFunction<T> s_CachedAssetKeyFunc;
    }
}