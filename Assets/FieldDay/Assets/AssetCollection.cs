#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.IL2CPP.CompilerServices;

namespace FieldDay.Assets {
    internal interface IAssetCollection {
        void Register(StringHash32 id, object asset);
        void Deregister(StringHash32 id);
        object Lookup(StringHash32 id);
        void Clear();
        IEnumerable GetAll();
    }

    internal class AssetCollection<T> : IAssetCollection {
        private readonly Dictionary<StringHash32, T> m_Lookup;

        internal AssetCollection() {
            m_Lookup = new Dictionary<StringHash32, T>(64, CompareUtils.DefaultEquals<StringHash32>());
        }

        /// <summary>
        /// Looks up the asset with the given id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public T Lookup(StringHash32 id) {
#if DEVELOPMENT
            if (!m_Lookup.TryGetValue(id, out T asset)) {
                Assert.Fail("No asset of type {0} with name '{1}' located", typeof(T).FullName, id.ToDebugString());
            }
            return asset;
#else
            m_Lookup.TryGetValue(id, out T asset);
            return asset;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Attempts to look up the asset with the given id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryLookup(StringHash32 id, out T asset) {
            return m_Lookup.TryGetValue(id, out asset);
        }

        /// <summary>
        /// Gets all the assets of this type.
        /// </summary>
        public Dictionary<StringHash32, T>.ValueCollection GetAll() {
            return m_Lookup.Values;
        }

        #region Modifications

        /// <summary>
        /// Registers the given asset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Register(StringHash32 id, T asset) {
#if DEVELOPMENT
            if (m_Lookup.ContainsKey(id)) {
                Assert.Fail("Multiple assets of type {0} with name '{1}'", typeof(T).FullName, id.ToDebugString());
            }
#endif // DEVELOPMENT
            m_Lookup.Add(id, asset);
        }

        /// <summary>
        /// Deregisters the asset for the given idl.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Deregister(StringHash32 id) {
#if DEVELOPMENT
            if (!m_Lookup.ContainsKey(id)) {
                Assert.Fail("No assets of type {0} with name '{1}' found", typeof(T).FullName, id.ToDebugString());
            }
#endif // DEVELOPMENT
            m_Lookup.Remove(id);
        }

        /// <summary>
        /// Clears all assets from the lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Clear() {
            m_Lookup.Clear();
        }

        #endregion // Modifications

        #region IAssetCollection

        void IAssetCollection.Register(StringHash32 id, object asset) {
            Register(id, (T) asset);
        }

        object IAssetCollection.Lookup(StringHash32 id) {
            return Lookup(id);
        }
        
        IEnumerable IAssetCollection.GetAll() {
            return GetAll();
        }

        #endregion // IAssetCollection
    }
}