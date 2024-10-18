using BeauUtil;
using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Base class for a ScriptableObject global asset.
    /// </summary>
    [NonIndexed]
    public abstract class GlobalAsset : ScriptableObject, IGlobalAsset {

        #region IGlobalAsset

        /// <summary>
        /// Invoked when the asset is loaded.
        /// </summary>
        public virtual void Mount() { }

        /// <summary>
        /// Invoked when the asset is unloaded.
        /// </summary>
        public virtual void Unmount() { }

        #endregion // IGlobalAsset
    }
}