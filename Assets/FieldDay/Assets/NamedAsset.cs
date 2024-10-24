using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Base class for a ScriptableObject named asset.
    /// </summary>
    [NonIndexed]
    public abstract class NamedAsset : ScriptableObject, INamedAsset {
        [NonSerialized] private StringHash32 m_CachedId;

        public StringHash32 AssetId {
            get { return AssetUtility.CacheNameHash(ref m_CachedId, this); }
        }
    }
}