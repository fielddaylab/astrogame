using BeauUtil;
using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Base class for a ScriptableObject named asset.
    /// </summary>
    [NonIndexed]
    public abstract class NamedAsset : ScriptableObject, INamedAsset {
    }
}