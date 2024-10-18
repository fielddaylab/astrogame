using System.Collections.Generic;
using BeauUtil;

namespace FieldDay.Assets {
    [NonIndexed]
    public interface IAsset {
    }

    /// <summary>
    /// Interface for a named asset.
    /// </summary>
    [TypeIndexCapacity(256)]
    public interface INamedAsset : IAsset {
    }

    /// <summary>
    /// Interface for a global configuration asset.
    /// </summary>
    [TypeIndexCapacity(128)]
    public interface IGlobalAsset : IAsset {
        void Mount();
        void Unmount();
    }

    /// <summary>
    /// Lightweight asset data.
    /// Useful for assets whose data can be contained with a struct.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface ILiteAsset {
    }

    /// <summary>
    /// Interface for a reference-counted asset.
    /// </summary>
    public interface IRefCountedAsset {
        bool AddRef();
        bool RemoveRef();
        bool IsReferenced();
    }

    /// <summary>
    /// Interface for an asset package.
    /// </summary>
    public interface IAssetPackage : IRefCountedAsset {
        void Mount(AssetMgr mgr);
        void Unmount(AssetMgr mgr);
    }
}