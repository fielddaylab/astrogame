using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.IL2CPP.CompilerServices;
using System.Collections;
using BeauUtil.IO;
using UnityEngine;
using BeauPools;

using GlobalAssetIndex = BeauUtil.TypeIndex<FieldDay.Assets.IGlobalAsset>;
using LiteAssetIndex = BeauUtil.TypeIndex<FieldDay.Assets.ILiteAsset>;
using NamedAssetIndex = BeauUtil.TypeIndex<FieldDay.Assets.INamedAsset>;
using NamedAssetCollection = FieldDay.Assets.AssetCollection<FieldDay.Assets.INamedAsset>;

namespace FieldDay.Assets {
    /// <summary>
    /// Asset manager.
    /// </summary>
    public sealed class AssetMgr {
        private readonly IGlobalAsset[] m_GlobalAssetTable = new IGlobalAsset[GlobalAssetIndex.Capacity];
        private readonly IAssetCollection[] m_LiteAssetTable = new IAssetCollection[LiteAssetIndex.Capacity];
        private readonly NamedAssetCollection[] m_NamedAssetTable = new NamedAssetCollection[NamedAssetIndex.Capacity];
        private readonly HashSet<IAssetPackage> m_LoadedPackages = new HashSet<IAssetPackage>(16);
        private readonly RingBuffer<IAssetPackage> m_UnloadQueue = new RingBuffer<IAssetPackage>(16, RingBufferMode.Expand);

        private readonly CastableAction<INamedAsset>[] m_NamedAssetPostLoadCallbackTable = new CastableAction<INamedAsset>[NamedAssetIndex.Capacity];
        private readonly CastableAction<INamedAsset>[] m_NamedAssetUnloadCallbackTable = new CastableAction<INamedAsset>[NamedAssetIndex.Capacity];

        private readonly HotReloadBatcher m_ReloadBatcher = new HotReloadBatcher();

        #region Events

        internal void Shutdown() {
            for (int i = 0; i < LiteAssetIndex.Count; i++) {
                if (m_LiteAssetTable[i] != null) {
                    m_LiteAssetTable[i].Clear();
                }
            }

            for (int i = 0; i < NamedAssetIndex.Count; i++) {
                if (m_NamedAssetTable[i] != null) {
                    m_NamedAssetTable[i].Clear();
                }
            }

            for (int i = 0; i < GlobalAssetIndex.Count; i++) {
                if (m_GlobalAssetTable[i] != null) {
                    m_GlobalAssetTable[i].Unmount();
                }
            }

            Array.Clear(m_LiteAssetTable, 0, m_LiteAssetTable.Length);
            Array.Clear(m_NamedAssetTable, 0, m_NamedAssetTable.Length);
            Array.Clear(m_GlobalAssetTable, 0, m_GlobalAssetTable.Length);
        }

        #endregion // Events

        #region Registration

        #region Global

        /// <summary>
        /// Registers the given global asset.
        /// </summary>
        public void Register(IGlobalAsset asset) {
            Assert.NotNull(asset);

            Type assetType = asset.GetType();
            int index = GlobalAssetIndex.Get(assetType);

            Assert.True(m_GlobalAssetTable[index] == null, "[AssetMgr] Global asset of type '{0}' already registered", assetType);
            m_GlobalAssetTable[index] = asset;

            RegistrationCallbacks.InvokeRegister(asset);
            asset.Mount();
            Log.Msg("[AssetMgr] Global asset '{0}' registered", assetType.FullName);
        }

        /// <summary>
        /// Deregisters the given global asset.
        /// </summary>
        public void Deregister(IGlobalAsset asset) {
            Assert.NotNull(asset);

            Type assetType = asset.GetType();
            int index = GlobalAssetIndex.Get(assetType);

            if (m_GlobalAssetTable[index] == asset) {
                m_GlobalAssetTable[index] = null;

                asset.Unmount();
                RegistrationCallbacks.InvokeDeregister(asset);
                Log.Msg("[AssetMgr] Global asset '{0}' deregistered", assetType.FullName);
            }
        }

        #endregion // Global

        #region Named

        /// <summary>
        /// Adds a named asset.
        /// </summary>
        public void AddNamed(StringHash32 id, INamedAsset asset) {
            Assert.NotNull(asset);
            Assert.False(id.IsEmpty);

            Type assetType = asset.GetType();
            var typeIndices = NamedAssetIndex.GetAll(assetType);
            foreach (var index in typeIndices) {
                GetNamedCollection(index, true).Register(id, asset);
            }

            RegistrationCallbacks.InvokeRegister(asset);
            InvokeNamedCallbacks(m_NamedAssetPostLoadCallbackTable, assetType, asset);
            Log.Msg("[AssetMgr] Named asset '{0}' of type '{1}' registered", id.ToDebugString(), assetType.FullName);
        }

        /// <summary>
        /// Removes a named asset.
        /// </summary>
        public void RemoveNamed(StringHash32 id, INamedAsset asset) {
            Assert.NotNull(asset);
            Assert.False(id.IsEmpty);

            Type assetType = asset.GetType();
            var typeIndices = NamedAssetIndex.GetAll(assetType);
            foreach (var index in typeIndices) {
                GetNamedCollection(index, false)?.Deregister(id);
            }

            InvokeNamedCallbacks(m_NamedAssetUnloadCallbackTable, assetType, asset);
            RegistrationCallbacks.InvokeDeregister(asset);
            Log.Msg("[AssetMgr] Named asset '{0}' of type '{1}' deregistered", id.ToDebugString(), assetType.FullName);
        }

        #endregion // Named

        #region Packages

        /// <summary>
        /// Loads the given package into the asset manager.
        /// </summary>
        public void LoadPackage(IAssetPackage package) {
            if (!AssetUtility.AddReference(package) || !m_LoadedPackages.Add(package)) {
                return;
            }

            Log.Msg("[AssetMgr] Loading package '{0}'...", AssetUtility.NameOf(package));
            package.Mount(this);
            Log.Msg("[AssetMgr] ...finished loading package '{0}'", AssetUtility.NameOf(package));
        }

        /// <summary>
        /// Unloads the given package from the asset manager.
        /// </summary>
        public void UnloadPackage(IAssetPackage package) {
            if (!AssetUtility.RemoveReference(package) || !m_LoadedPackages.Remove(package)) {
                return;
            }

            Log.Msg("[AssetMgr] Unloading package '{0}'...", AssetUtility.NameOf(package));
            package.Unmount(this);
            Log.Msg("[AssetMgr] ...finished unloading package '{0}'", AssetUtility.NameOf(package));
        }

        #endregion // Packages

        #region Lite

        /// <summary>
        /// Registers the given lightweight asset to be looked up.
        /// </summary>
        public void AddLite<T>(StringHash32 id, T data) where T : struct, ILiteAsset {
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            typedCollection.Register(id, data);
        }

        /// <summary>
        /// Registers the given set of lightweight assets to be looked up.
        /// </summary>
        public void AddLite<T>(T[] data, AssetKeyFunction<T> keyFunc) where T : struct, ILiteAsset {
            if (keyFunc == null) {
                throw new ArgumentNullException("keyFunc");
            }
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            for (int i = 0; i < data.Length; i++) {
                typedCollection.Register(keyFunc(data[i]), data[i]);
            }
        }

        /// <summary>
        /// Registers the given set of lightweight assets to be looked up.
        /// </summary>
        public void AddLite<T>(IEnumerable<T> data, AssetKeyFunction<T> keyFunc) where T : struct, ILiteAsset {
            if (keyFunc == null) {
                throw new ArgumentNullException("keyFunc");
            }
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            foreach (var asset in data) {
                typedCollection.Register(keyFunc(asset), asset);
            }
        }

        /// <summary>
        /// Deregisters the given lightweight asset with the given key.
        /// </summary>
        public void RemoveLite<T>(StringHash32 id) where T : struct, ILiteAsset {
            AssetCollection<T> typedCollection = GetLiteCollection<T>(false);
            typedCollection?.Deregister(id);
        }

        /// <summary>
        /// Deregisters the given set of lightweight assets.
        /// </summary>
        public void RemoveLite<T>(T[] data, AssetKeyFunction<T> keyFunc) where T : struct, ILiteAsset {
            if (keyFunc == null) {
                throw new ArgumentNullException("keyFunc");
            }
            AssetCollection<T> typedCollection = GetLiteCollection<T>(false);
            if (typedCollection != null) {
                for (int i = 0; i < data.Length; i++) {
                    typedCollection.Deregister(keyFunc(data[i]));
                }
            }
        }

        /// <summary>
        /// Deregisters the given set of lightweight assets.
        /// </summary>
        public void RemoveLite<T>(IEnumerable<T> data, AssetKeyFunction<T> keyFunc) where T : struct, ILiteAsset {
            if (keyFunc == null) {
                throw new ArgumentNullException("keyFunc");
            }
            AssetCollection<T> typedCollection = GetLiteCollection<T>(false);
            if (typedCollection != null) {
                foreach (var asset in data) {
                    typedCollection.Deregister(keyFunc(asset));
                }
            }
        }

        #endregion // Lite

        #endregion // Registration

        #region Lookup

        #region Global

        /// <summary>
        /// Returns the global asset of the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IGlobalAsset GetGlobal(Type type) {
            int index = GlobalAssetIndex.Get(type);
            IGlobalAsset asset = m_GlobalAssetTable[index];
#if DEVELOPMENT
            if (asset == null) {
                Assert.Fail("No global asset found for type '{0}'", type.FullName);
            }
#endif // DEVELOPMENT
            return asset;
        }

        /// <summary>
        /// Returns the global asset of the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetGlobal<T>() where T : class, IGlobalAsset {
            int index = GlobalAssetIndex.Get<T>();
            IGlobalAsset asset = m_GlobalAssetTable[index];
#if DEVELOPMENT
            if (asset == null) {
                Assert.Fail("No global asset found for type '{0}'", typeof(T).FullName);
            }
#endif // DEVELOPMENT
            return (T) asset;
        }

        /// <summary>
        /// Attempts to return the global asset of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGlobal(Type type, out IGlobalAsset asset) {
            int index = GlobalAssetIndex.Get(type);
            asset = index < m_GlobalAssetTable.Length ? m_GlobalAssetTable[index] : null;
            return asset != null;
        }

        /// <summary>
        /// Attempts to return the global asset of the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGlobal<T>(out T asset) where T : class, IGlobalAsset {
            int index = GlobalAssetIndex.Get<T>();
            asset = (T) (index < m_GlobalAssetTable.Length ? m_GlobalAssetTable[index] : null);
            return asset != null;
        }

        #endregion // Global

        #region Named

        /// <summary>
        /// Looks up the named asset with the given id.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        public T GetNamed<T>(StringHash32 id) where T : class, INamedAsset {
            NamedAssetCollection typedCollection = GetNamedCollection<T>(true);
            return (T) typedCollection.Lookup(id);
        }

        /// <summary>
        /// Attempts to look up the named asset with the given id.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetNamed<T>(StringHash32 id, out T asset) where T : class, INamedAsset {
            NamedAssetCollection typedCollection = GetNamedCollection<T>(true);
            bool found = typedCollection.TryLookup(id, out INamedAsset interfaceAsset);
            asset = (T) interfaceAsset;
            return found;
        }

        /// <summary>
        /// Looks up all named assets of the given type.
        /// </summary>
        public NamedAssetIterator<T> GetAllNamed<T>() where T : class, INamedAsset {
            NamedAssetCollection typedCollection = GetNamedCollection<T>(true);
            return new NamedAssetIterator<T>(typedCollection.GetAll());
        }

        #endregion // Named

        #region Lite

        /// <summary>
        /// Looks up the lightweight asset with the given id.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        public T GetLite<T>(StringHash32 id) where T : struct, ILiteAsset {
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            return typedCollection.Lookup(id);
        }

        /// <summary>
        /// Attempts to look up the lightweight asset with the given id.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetLite<T>(StringHash32 id, out T asset) where T : struct, ILiteAsset {
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            return typedCollection.TryLookup(id, out asset);
        }

        /// <summary>
        /// Looks up all lightweight assets of the given type.
        /// </summary>
        public LiteAssetIterator<T> GetAllLite<T>() where T : struct, ILiteAsset {
            AssetCollection<T> typedCollection = GetLiteCollection<T>(true);
            return new LiteAssetIterator<T>(typedCollection.GetAll());
        }

        #endregion // Lite

        #endregion // Lookup

        #region Internal

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        private AssetCollection<T> GetLiteCollection<T>(bool create) where T : struct, ILiteAsset {
            int index = LiteAssetIndex.Get<T>();
            AssetCollection<T> typedCollection;
            ref IAssetCollection collection = ref m_LiteAssetTable[index];
            if (collection == null && create) {
                collection = typedCollection = new AssetCollection<T>();
            } else {
                typedCollection = (AssetCollection<T>) collection;
            }
            return typedCollection;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        private NamedAssetCollection GetNamedCollection<T>(bool create) where T : class, INamedAsset {
            int index = NamedAssetIndex.Get<T>();
            NamedAssetCollection typedCollection;
            ref NamedAssetCollection collection = ref m_NamedAssetTable[index];
            if (collection == null && create) {
                collection = typedCollection = new NamedAssetCollection();
            } else {
                typedCollection = collection;
            }
            return typedCollection;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        private NamedAssetCollection GetNamedCollection(int index, bool create) {
            NamedAssetCollection typedCollection;
            ref NamedAssetCollection collection = ref m_NamedAssetTable[index];
            if (collection == null && create) {
                collection = typedCollection = new NamedAssetCollection();
            } else {
                typedCollection = collection;
            }
            return typedCollection;
        }

        #endregion // Internal

        #region Callbacks

        /// <summary>
        /// Sets load and unload handlers for a given asset type.
        /// </summary>
        public void SetNamedAssetLoadCallbacks<T>(Action<T> onLoad, Action<T> onUnload) where T : INamedAsset {
            int index = NamedAssetIndex.Get<T>();
            if (onLoad != null) {
                m_NamedAssetPostLoadCallbackTable[index] = CastableAction<INamedAsset>.Create(onLoad);
            } else {
                m_NamedAssetPostLoadCallbackTable[index] = default;
            }

            if (onUnload != null) {
                m_NamedAssetUnloadCallbackTable[index] = CastableAction<INamedAsset>.Create(onUnload);
            } else {
                m_NamedAssetUnloadCallbackTable[index] = default;
            }
        }

        static private void InvokeNamedCallbacks(CastableAction<INamedAsset>[] assets, Type assetType, INamedAsset asset) {
            var typeIndices = NamedAssetIndex.GetAll(assetType);
            foreach (var index in typeIndices) {
                var action = assets[index];
                if (!action.IsEmpty) {
                    action.Invoke(asset);
                }
            }
        }

        #endregion // Callbacks

        #region Hot Reload

        /// <summary>
        /// Registers a hot-reloadable asset.
        /// </summary>
        public void RegisterHotReloadable(IHotReloadable reloadable) {
            if (m_ReloadBatcher.Add(reloadable)) {
                Log.Debug("[AssetMgr] Registered hot-reloadable asset '{0}'", reloadable.Id);
            }
        }

        /// <summary>
        /// Registers a hot-reloadable asset.
        /// </summary>
        public IHotReloadable RegisterHotReloadCallbacks<T>(T asset, HotReloadAssetDelegate<T> callback) where T : UnityEngine.Object {
#if UNITY_EDITOR
            if (asset != null && asset.IsPersistent()) {
                var reloadable = new HotReloadableAssetProxy<T>(asset, callback);
                RegisterHotReloadable(reloadable);
                return reloadable;
            } else {
                return null;
            }
#else
            return null;
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// Registers a hot-reloadable asset.
        /// </summary>
        public void DeregisterHotReloadable(IHotReloadable reloadable) {
            if (m_ReloadBatcher.Remove(reloadable)) {
                Log.Debug("[AssetMgr] Unregistered hot-reloadable asset '{0}'", reloadable.Id);
            }
        }

        private void TryHotReloadAll() {
            using (var res = PooledSet<HotReloadResult>.Create()) {
                m_ReloadBatcher.TryReloadAll(res, false);
                LogHotReloadResults(res);
            }
        }

        static private void LogHotReloadResults(ICollection<HotReloadResult> res) {
            if (res.Count > 0) {
                using (var str = PooledStringBuilder.Create(1024)) {
                    str.Builder.Append("[AssetMgr] Hot-reloaded ").AppendNoAlloc(res.Count).Append(" assets");
                    foreach (var result in res) {
                        str.Builder.Append("\n - ").Append(result.ToDebugString());
                    }
                    Log.Msg(str.Builder.Flush());
                }
            } else {
                Log.Trace("[AssetMgr] Hot-reloaded no assets");
            }
        }

        #endregion // Hot Reload

#if UNITY_EDITOR
        private class EditorReloadCallback : UnityEditor.AssetPostprocessor {
            static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
                if (!Application.isPlaying || Game.IsShuttingDown)
                    return;

                UnityEditor.EditorApplication.delayCall += () => Game.Assets.TryHotReloadAll();
            }
        }
#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Named asset enumerator.
    /// </summary>
    public struct NamedAssetIterator<T> : IEnumerable<T>, IEnumerator<T>, IDisposable where T : class, INamedAsset {
        private Dictionary<StringHash32, INamedAsset>.ValueCollection.Enumerator m_Source;

        internal NamedAssetIterator(Dictionary<StringHash32, INamedAsset>.ValueCollection source) {
            m_Source = source.GetEnumerator();
        }

        public bool MoveNext() {
            return m_Source.MoveNext();
        }

        public T Current {
            get { return (T) m_Source.Current; }
        }

        public NamedAssetIterator<T> GetEnumerator() {
            return this;
        }

        #region Interfaces

        public void Dispose() {
            m_Source.Dispose();
            m_Source = default;
        }

        object IEnumerator.Current {
            get { return Current; }
        }

        void IEnumerator.Reset() {
            throw new NotSupportedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion // Interfaces
    }

    /// <summary>
    /// Lite asset enumerator.
    /// </summary>
    public struct LiteAssetIterator<T> : IEnumerable<T>, IEnumerator<T>, IDisposable where T : struct, ILiteAsset {
        private Dictionary<StringHash32, T>.ValueCollection.Enumerator m_Source;

        internal LiteAssetIterator(Dictionary<StringHash32, T>.ValueCollection source) {
            m_Source = source.GetEnumerator();
        }

        public bool MoveNext() {
            return m_Source.MoveNext();
        }

        public T Current {
            get { return m_Source.Current; }
        }

        public LiteAssetIterator<T> GetEnumerator() {
            return this;
        }

        #region Interfaces

        public void Dispose() {
            m_Source.Dispose();
            m_Source = default;
        }

        object IEnumerator.Current {
            get { return Current; }
        }

        void IEnumerator.Reset() {
            throw new NotSupportedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion // Interfaces
    }
}