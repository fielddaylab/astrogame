#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;
using BeauRoutine;
using FieldDay.Rendering;
using FieldDay.Assets;
using System.Runtime.CompilerServices;
using EasyAssetStreaming;
using FieldDay.Debugging;
using FieldDay.Threading;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Scenes {
    public sealed class SceneMgr {
        #region Operations

        private struct LoadSceneArgs {
            public string ScenePath;
            public StringHash32 Tag;
            public SceneImportFlags Flags;
            public SceneType Type;
            public Matrix4x4? Transform;
            public SceneDataExt Parent;
            public RingBuffer<SceneDataExt> Queue;
            public CounterHandle Counter;
        }

        private struct TransformSceneArgs {
            public SceneDataExt Data;
            public Scene Scene;
            public Matrix4x4 Transform;
            public CounterHandle Counter;
        }

        private struct QueueSubScenesArgs {
            public SceneDataExt Data;
            public RingBuffer<SceneDataExt> Queue;
            public CounterHandle Counter;
        }

        private struct ImportLightingArgs {
            public SceneDataExt Data;
            public CounterHandle Counter;
        }

        private struct PreloadArgs {
            public PreloadManifest[] Manifests;
            public CounterHandle Counter;
        }

        private struct LateEnableArgs {
            public SceneDataExt Data;
            public CounterHandle Counter;
        }

        private struct UnloadSceneArgs {
            public SceneDataExt Data;
            public UnloadSceneOptions Options;
            public bool UnloadTree;
            public CounterHandle Counter;
        }

        private struct OperationSlot<T> where T : struct {
            public T Args;
            public AsyncOperation UnityOp;
            public bool Active;

            public bool TryFill(RingBuffer<T> queue) {
                if (Active = queue.TryPopFront(out Args)) {
                    UnityOp = null;
                    return true;
                }
                return false;
            }

            public void Fill(T args) {
                Active = true;
                Args = args;
                UnityOp = null;
            }

            public void Clear() {
                Args = default;
                UnityOp = null;
                Active = false;
            }
        }

        private struct PreloadOperationSlot {
            public PreloadManifest.Reader Reader;
            public WorkSlicer.EnumeratedState WorkState;
            public RingBuffer<IScenePreload> Preloads;
            public CounterHandle Counter;
            public bool Active;

            public void Create() {
                Reader = new PreloadManifest.Reader();
                Preloads = new RingBuffer<IScenePreload>(64, RingBufferMode.Expand);
            }

            public void Clear() {
                Reader.Clear();
                WorkState.Clear();
                Preloads.Clear();
                Counter = default;
                Active = false;
            }
        }

        #endregion // Operations

        #region Types

        private struct LoadProcessArgs {
            public string Path;
            public StringHash32 Tag;
            public SceneType Type;
            public SceneImportFlags Flags;
            public Matrix4x4? Transform;
        }

        private struct UninitializedSceneCallback {
            public Scene Scene;
            public Action Action;

            public UninitializedSceneCallback(Scene scene, Action action) {
                Scene = scene;
                Action = action;
            }
        }

        #endregion // Types

        #region State

        // current state
        private SceneDataExt m_MainScene;
        private readonly RingBuffer<SceneDataExt> m_AuxScenes = new RingBuffer<SceneDataExt>(16, RingBufferMode.Expand);
        private readonly RingBuffer<SceneDataExt> m_PersistentScenes = new RingBuffer<SceneDataExt>(16, RingBufferMode.Expand);

        // queues
        private readonly RingBuffer<LoadProcessArgs> m_LoadProcessQueue = new RingBuffer<LoadProcessArgs>();
        private readonly RingBuffer<LoadSceneArgs> m_LoadQueue = new RingBuffer<LoadSceneArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<TransformSceneArgs> m_TransformRootsQueue = new RingBuffer<TransformSceneArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<QueueSubScenesArgs> m_SubSceneQueue = new RingBuffer<QueueSubScenesArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ImportLightingArgs> m_LightingCopyQueue = new RingBuffer<ImportLightingArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<PreloadArgs> m_PreloadQueue = new RingBuffer<PreloadArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<UnloadSceneArgs> m_UnloadQueue = new RingBuffer<UnloadSceneArgs>(8, RingBufferMode.Expand);
        private readonly RingBuffer<LateEnableArgs> m_LateEnableQueue = new RingBuffer<LateEnableArgs>(8, RingBufferMode.Expand);

        private readonly WorkSlicer.StepOperation CachedUpdateStep;
        private float m_UpdateStepTimeSlice = 2;
        private LightingImportFlags m_LightImportFlags = LightingImportFlags.All;

        // operation slots
        private OperationSlot<LoadSceneArgs> m_CurrentLoadOperation;
        private OperationSlot<UnloadSceneArgs> m_CurrentUnloadOperation;
        private PreloadOperationSlot m_CurrentPreloadOperation;

        // ongoing loads
        private Routine m_MainSceneLoadProcess;
        private Routine m_AdditionalSceneLoadProcess;
        private Routine m_MainSceneTransition;

        // handlers
        private SceneTransitionHandler m_MainTransitionUnload;
        private SceneTransitionHandler m_MainTransitionLoad;

        // dependencies
        private RingBuffer<ISceneLoadDependency> m_Dependencies = new RingBuffer<ISceneLoadDependency>(8, RingBufferMode.Expand);
        private RingBuffer<AsyncHandle> m_DependencyHandles = new RingBuffer<AsyncHandle>(8, RingBufferMode.Expand);

        // temp queues
        private RingBuffer<UninitializedSceneCallback> m_TempOnLateEnableQueue = new RingBuffer<UninitializedSceneCallback>(4, RingBufferMode.Expand);
        private RingBuffer<UninitializedSceneCallback> m_TempOnLoadQueue = new RingBuffer<UninitializedSceneCallback>(4, RingBufferMode.Expand);
        private RingBuffer<UninitializedSceneCallback> m_TempOnUnloadQueue = new RingBuffer<UninitializedSceneCallback>(4, RingBufferMode.Expand);

        #endregion // State

        #region Exposed Events

        public readonly CastableEvent<SceneEventArgs> OnPrepareScene = new CastableEvent<SceneEventArgs>();
        public readonly CastableEvent<SceneEventArgs> OnScenePreload = new CastableEvent<SceneEventArgs>();
        public readonly CastableEvent<SceneEventArgs> OnSceneReady = new CastableEvent<SceneEventArgs>();
        public readonly ActionEvent OnMainSceneLateEnable = new ActionEvent();
        public readonly ActionEvent OnMainSceneReady = new ActionEvent();
        public readonly ActionEvent OnMainSceneUnloading = new ActionEvent();
        public readonly CastableEvent<SceneEventArgs> OnSceneUnload = new CastableEvent<SceneEventArgs>();
        public readonly ActionEvent OnAnySceneUnloaded = new ActionEvent();
        public readonly ActionEvent OnAnySceneEnabled = new ActionEvent();

        #endregion // Exposed Events

        internal SceneMgr() {
            CachedUpdateStep = UpdateStep;
            m_CurrentPreloadOperation.Create();
        }

        #region Public API

        /// <summary>
        /// Amount of time, in millisecs, that scene loading operations are allowed
        /// to operate per frame.
        /// </summary>
        public float TimeSlice {
            get { return m_UpdateStepTimeSlice; }
            set {
                if (value <= 0) {
                    throw new ArgumentOutOfRangeException("value", "Time slice cannot be set to 0 or less");
                }
                m_UpdateStepTimeSlice = value;
            }
        }

        /// <summary>
        /// Flags to apply for lighting import.
        /// </summary>
        public LightingImportFlags LightingImport {
            get { return m_LightImportFlags; }
            set { m_LightImportFlags = value; }
        }

        #region Checks

        /// <summary>
        /// Returns if the main scene is currently loading.
        /// </summary>
        public bool IsMainLoading() {
            return m_MainSceneLoadProcess || IsLoadQueued(SceneType.Main);
        }

        /// <summary>
        /// Returns if the main scene is currently loaded.
        /// </summary>
        public bool IsMainLoaded() {
            return m_MainScene && m_MainScene.IsVisited(SceneDataExt.VisitFlags.Loaded) && !m_MainScene.IsVisited(SceneDataExt.VisitFlags.Unloaded);
        }

        /// <summary>
        /// Returns if the given scene is loading.
        /// </summary>
        public bool IsLoading(SceneReference scene) {
            return IsLoading(scene.Path);
        }

        /// <summary>
        /// Returns if the given scene is loading.
        /// </summary>
        public bool IsLoading(string scenePath) {
            SceneDataExt data = SceneDataExt.GetByPath(scenePath);
            if (data && !data.IsVisited(SceneDataExt.VisitFlags.Loaded)) {
                return true;
            }

            for(int i = 0; i < m_LoadProcessQueue.Count; i++) {
                if (m_LoadProcessQueue[i].Path == scenePath) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given scene is loaded.
        /// </summary>
        public bool IsLoaded(SceneReference scene) {
            return IsLoaded(scene.Path);
        }

        /// <summary>
        /// Returns if the given scene is loaded.
        /// </summary>
        public bool IsLoaded(string scenePath) {
            SceneDataExt data = SceneDataExt.GetByPath(scenePath);
            return data && data.IsVisited(SceneDataExt.VisitFlags.Loaded);
        }

        /// <summary>
        /// Returns if a load of the given type is queued.
        /// </summary>
        public bool IsLoadQueued(SceneType sceneType) {
            foreach(var process in m_LoadProcessQueue) {
                if (process.Type == sceneType) {
                    return true;
                }
            }

            return false;
        }

        #endregion // Checks

        #region Main Load

        public void LoadMainScene(string scenePath) {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot load main during main scene loading");
            QueueMainLoadInternal(scenePath, true, false);
        }

        public void LoadMainScene(string scenePath, bool forceReload) {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot load main during main scene loading");
            QueueMainLoadInternal(scenePath, true, forceReload);
        }

        public void LoadMainScene(SceneReference scene) {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot load main during main scene loading");
            QueueMainLoadInternal(scene.Path, true, false);
        }

        public void LoadMainScene(SceneReference scene, bool forceReload) {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot load main during main scene loading");
            QueueMainLoadInternal(scene.Path, true, forceReload);
        }

        public void ReloadMainScene() {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot load main during main scene loading");
            QueueMainLoadInternal(m_MainScene.Scene.path, true, true);
        }

        /// <summary>
        /// Returns a reference to the main scene.
        /// </summary>
        public SceneBinding MainScene() {
            if (m_MainScene != null) {
                return m_MainScene.SceneBinding;
            } else {
                return default(SceneBinding);
            }
        }

        #endregion // Main Load

        #region Aux Load

        public void LoadAuxScene(string scenePath, StringHash32 tag, Matrix4x4? transformBy = null, SceneImportFlags flags = 0) {
            QueueSceneLoadInternal(scenePath, tag, SceneType.Aux, flags, transformBy, SceneLoadPriority.Default);
        }

        public void LoadAuxScene(string scenePath, StringHash32 tag, Matrix4x4? transformBy = null) {
            QueueSceneLoadInternal(scenePath, tag, SceneType.Aux, 0, transformBy, SceneLoadPriority.Default);
        }

        public void LoadAuxScene(SceneReference scene, StringHash32 tag, Matrix4x4? transformBy = null) {
            QueueSceneLoadInternal(scene.Path, tag, SceneType.Aux, 0, transformBy, SceneLoadPriority.Default);
        }

        #endregion // Aux Load

        #region Persistent Load

        public void LoadPersistentScene(string scenePath, StringHash32 tag = default) {
            QueueSceneLoadInternal(scenePath, tag, SceneType.Persistent, SceneImportFlags.Persistent, null, SceneLoadPriority.High);
        }

        public void LoadPersistentScene(SceneReference reference, StringHash32 tag = default) {
            QueueSceneLoadInternal(reference.Path, tag, SceneType.Persistent, SceneImportFlags.Persistent, null, SceneLoadPriority.High);
        }

        #endregion // Persistent Load

        #region Unload

        // TODO: Handle unloading a scene that is currently being loaded

        /// <summary>
        /// Unloads the given scene.
        /// </summary>
        public void UnloadScene(SceneReference reference, bool unloadTree = true) {
            UnloadScene(reference.Path, unloadTree);
        }

        /// <summary>
        /// Unloads the given scene.
        /// </summary>
        public void UnloadScene(string scenePath, bool unloadTree = true) {
            SceneDataExt data = SceneDataExt.GetByPath(scenePath);

            if (data != null) {
                switch (data.SceneType) {
                    case SceneType.Main: {
                        Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot unload main scene during main scene load");
                        if (m_MainScene == data) {
                            m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                                Data = m_MainScene,
                                Options = 0,
                                UnloadTree = unloadTree
                            });
                            m_MainScene = null;
                        }
                        break;
                    }

                    case SceneType.Aux: {
                        if (m_AuxScenes.FastRemove(data)) {
                            m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                                Data = data,
                                UnloadTree = unloadTree,
                                Options = 0,
                            });
                        }
                        break;
                    }

                    case SceneType.Persistent: {
                        if (m_PersistentScenes.FastRemove(data)) {
                            m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                                Data = data,
                                UnloadTree = unloadTree,
                                Options = 0,
                            });
                        }
                        break;
                    }
                }
            }

            // TODO: account for scene loads that are in progress but not yet SceneDataExt

            for (int i = m_LoadProcessQueue.Count - 1; i >= 0; i--) {
                if (m_LoadProcessQueue[i].Path == scenePath) {
                    Log.Warn("[SceneMgr] Cancelling scene load '{0}'", scenePath);
                    m_LoadProcessQueue.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Unloads all scenes with the given tag.
        /// </summary>
        public void UnloadScenesByTag(StringHash32 tag) {
            Assert.False(m_MainSceneLoadProcess.Exists(), "Cannot unload by tag during main scene loading");
            if (m_MainScene != null && m_MainScene.SceneTag == tag) {
                m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                    Data = m_MainScene,
                    Options = 0,
                    UnloadTree = false
                });
                m_MainScene.TryVisit(SceneDataExt.VisitFlags.Unloading);
                m_MainScene = null;
            }

            for(int i = m_AuxScenes.Count - 1; i >= 0; i--) {
                var aux = m_AuxScenes[i];
                if (aux.SceneTag == tag) {
                    m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                        Data = aux,
                        UnloadTree = false,
                        Options = 0,
                    });
                    aux.TryVisit(SceneDataExt.VisitFlags.Unloading);
                    m_AuxScenes.FastRemoveAt(i);
                }
            }

            for (int i = m_PersistentScenes.Count - 1; i >= 0; i--) {
                var persist = m_PersistentScenes[i];
                if (persist.SceneTag == tag) {
                    m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                        Data = persist,
                        UnloadTree = false,
                        Options = 0,
                    });
                    persist.TryVisit(SceneDataExt.VisitFlags.Unloading);
                    m_PersistentScenes.FastRemoveAt(i);
                }
            }

            // TODO: account for scene loads that are in progress but not yet SceneDataExt

            for (int i = m_LoadProcessQueue.Count - 1; i >= 0; i--) {
                if (m_LoadProcessQueue[i].Tag == tag) {
                    Log.Warn("[SceneMgr] Cancelling scene load '{0}'", m_LoadProcessQueue[i].Path);
                    m_LoadProcessQueue.RemoveAt(i);
                }
            }
        }

        #endregion // Unload

        #region Callbacks

        /// <summary>
        /// Queues a callback for when the main scene is enabled.
        /// </summary>
        public void QueueOnEnable(Action action) {
            SceneDataExt data = m_MainScene;
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.LateEnabled)) {
                    action();
                } else {
                    data.LateEnableCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnLateEnableQueue.PushBack(new UninitializedSceneCallback(default, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the given scene is enabled.
        /// </summary>
        public void QueueOnEnable(Scene scene, Action action) {
            SceneDataExt data = SceneDataExt.Get(scene);
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.LateEnabled)) {
                    action();
                } else {
                    data.LateEnableCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnLateEnableQueue.PushBack(new UninitializedSceneCallback(scene, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnEnable(GameObject gameObject, Action action) {
            QueueOnEnable(gameObject.scene, action);
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnEnable(Component component, Action action) {
            QueueOnEnable(component.gameObject.scene, action);
        }

        /// <summary>
        /// Queues a callback for when the main scene is loaded.
        /// </summary>
        public void QueueOnLoad(Action action) {
            SceneDataExt data = m_MainScene;
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.Readied)) {
                    action();
                } else {
                    data.LoadedCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnLoadQueue.PushBack(new UninitializedSceneCallback(default, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the given scene is loaded.
        /// </summary>
        public void QueueOnLoad(Scene scene, Action action) {
            SceneDataExt data = SceneDataExt.Get(scene);
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.Readied)) {
                    action();
                } else {
                    data.LoadedCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnLoadQueue.PushBack(new UninitializedSceneCallback(scene, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is loaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnLoad(GameObject gameObject, Action action) {
            QueueOnLoad(gameObject.scene, action);
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is loaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnLoad(Component component, Action action) {
            QueueOnLoad(component.gameObject.scene, action);
        }

        /// <summary>
        /// Queues a callback for when the main scene is unloaded.
        /// </summary>
        public void QueueOnUnload(Action action) {
            SceneDataExt data = m_MainScene;
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.Unloading)) {
                    action();
                } else {
                    data.UnloadingCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnUnloadQueue.PushBack(new UninitializedSceneCallback(default, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the given scene is unloaded.
        /// </summary>
        public void QueueOnUnload(Scene scene, Action action) {
            SceneDataExt data = SceneDataExt.Get(scene);
            if (data != null) {
                if (data.IsVisited(SceneDataExt.VisitFlags.Unloading)) {
                    action();
                } else {
                    data.UnloadingCallbackQueue.PushBack(action);
                }
            } else {
                m_TempOnUnloadQueue.PushBack(new UninitializedSceneCallback(scene, action));
            }
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is unloaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnUnload(GameObject gameObject, Action action) {
            QueueOnUnload(gameObject.scene, action);
        }

        /// <summary>
        /// Queues a callback for when the scene for the given object is unloaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueueOnUnload(Component component, Action action) {
            QueueOnUnload(component.gameObject.scene, action);
        }

        /// <summary>
        /// Registers handlers for dealing with transitions.
        /// </summary>
        public void RegisterTransitionHandlers(SceneTransitionHandler unload, SceneTransitionHandler load) {
            m_MainTransitionUnload = unload;
            m_MainTransitionLoad = load;
        }

        #endregion // Callbacks

        #endregion // Public API

        #region Events

        internal void Prepare() {
            if (m_MainScene == null && !m_MainSceneLoadProcess) {
                QueueMainLoadInternal(SceneManager.GetActiveScene().path, false, true);
            }

            // need to ensure we still have a scene remaining when unloading,
            // even if it's just an empty dummy scene
            Scene dummyScene = SceneManager.CreateScene("__DummyScene");
        }

        internal void Update() {
            CleanLists();
            ProcessLoadProcessQueue();
            WorkSlicer.TimeSliced(CachedUpdateStep, m_UpdateStepTimeSlice);
        }

        private void CleanLists() {
            for (int i = m_AuxScenes.Count - 1; i >= 0; i--) {
                if (!m_AuxScenes[i]) {
                    m_AuxScenes.FastRemoveAt(i);
                }
            }

            for (int i = m_PersistentScenes.Count - 1; i >= 0; i--) {
                if (!m_PersistentScenes[i]) {
                    m_PersistentScenes.FastRemoveAt(i);
                }
            }
        }

        private WorkSlicer.Result UpdateStep() {
            if (ProcessUnloadQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessLoadQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessTransformSceneQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessImportLightingQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessSubSceneImportQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessPreloadQueue()) {
                return WorkSlicer.Result.Processed;
            }

            if (ProcessLateEnableQueue()) {
                return WorkSlicer.Result.Processed;
            }

            return WorkSlicer.Result.OutOfData;

        }

        internal void Shutdown() {
            m_LateEnableQueue.Clear();
            m_LightingCopyQueue.Clear();
            m_LoadProcessQueue.Clear();
            m_LoadQueue.Clear();
            m_PreloadQueue.Clear();
            m_SubSceneQueue.Clear();
            m_TransformRootsQueue.Clear();
            m_UnloadQueue.Clear();

            m_CurrentLoadOperation.Clear();
            m_CurrentPreloadOperation.Clear();
            m_CurrentUnloadOperation.Clear();

            m_MainSceneLoadProcess.Stop();
            m_AdditionalSceneLoadProcess.Stop();

            OnScenePreload.Clear();
            OnSceneReady.Clear();
            OnSceneUnload.Clear();
            OnAnySceneEnabled.Clear();
            OnAnySceneUnloaded.Clear();
            OnMainSceneReady.Clear();
            OnPrepareScene.Clear();
        }

        #endregion // Events

        #region Internal

        static private bool IsLoadingOrLoaded(Scene scene) {
            SceneHelper.LoadingState loadingState = scene.GetLoadingState();
            return loadingState == SceneHelper.LoadingState.Loading || loadingState == SceneHelper.LoadingState.Loaded;
        }

        static private bool IsDoneLoading(AsyncOperation operation, in LoadSceneArgs args, out Scene scene) {
            if (ScenesUtility.Editor.AreDelayedSceneProcessorsRunning()) {
                scene = default;
                return false;
            }

            if (operation != null) {
                if (operation.isDone) {
                    scene = SafeGetSceneByPath(args.ScenePath);
                    return true;
                } else {
                    scene = default;
                    return false;
                }
            } else {
                scene = SafeGetSceneByPath(args.ScenePath);
                return scene.isLoaded;
            }
        }

        private void QueueMainLoadInternal(string path, bool killNonPersistentLoads, bool force) {
            SceneDataExt data = SceneDataExt.GetByPath(path);

            if (!force && data != null && data.IsVisited(SceneDataExt.VisitFlags.Loaded)) {
                return;
            }

            LoadProcessArgs args = new LoadProcessArgs() {
                Path = path,
                Type = SceneType.Main,
                Flags = force ? SceneImportFlags.ForceReload : 0,
                Tag = default,
                Transform = null
            };

            if (killNonPersistentLoads) {
                ClearNonPersistentLoadProcesses();
            }
            m_LoadProcessQueue.PushFront(args);

            if (!killNonPersistentLoads) {
                DebugFlags.MarkNewSceneLoaded();
            }
        }

        private void QueueSceneLoadInternal(string path, StringHash32 tag, SceneType type, SceneImportFlags flags, Matrix4x4? transform, SceneLoadPriority priority) {
            Assert.True(type != SceneType.Main);
            SceneDataExt data = SceneDataExt.GetByPath(path);

            if (data != null && data.IsVisited(SceneDataExt.VisitFlags.Loaded)) {
                return;
            }

            LoadProcessArgs args = new LoadProcessArgs() {
                Path = path,
                Type = type,
                Flags = flags,
                Tag = tag,
                Transform = transform
            };

            if (priority == SceneLoadPriority.High) {
                m_LoadProcessQueue.PushFront(args);
            } else {
                m_LoadProcessQueue.PushBack(args);
            }
        }

        private void FlushSceneLoadCallbacks(SceneDataExt ext, Scene scene, bool isMain) {
            FlushSceneLoadCallbacks(ext, scene, isMain, m_TempOnLateEnableQueue, ext.LateEnableCallbackQueue);
            FlushSceneLoadCallbacks(ext, scene, isMain, m_TempOnLoadQueue, ext.LoadedCallbackQueue);
            FlushSceneLoadCallbacks(ext, scene, isMain, m_TempOnUnloadQueue, ext.UnloadingCallbackQueue);
        }

        static private void FlushSceneLoadCallbacks(SceneDataExt ext, Scene scene, bool isMain, RingBuffer<UninitializedSceneCallback> src, RingBuffer<Action> dest) {
            int count = src.Count;
            while (count-- > 0) {
                var callback = src.PopFront();
                if ((isMain && callback.Scene == default) || ext.Scene == scene) {
                    dest.PushBack(callback.Action);
                } else {
                    src.PushBack(callback);
                }
            }
        }

        static private Scene SafeGetSceneByPath(string path) {
            SceneBinding scene = SceneHelper.FindSceneByPath(path, SceneCategories.Build);
            Assert.True(scene.IsValid(), "Scene '{0}' is not valid", path);
            return scene;
        }

        #endregion // Internal

        #region Operations

        private void ProcessLoadProcessQueue() {
            if (m_LoadProcessQueue.TryPeekFront(out var args)) {
                Assert.True(!string.IsNullOrEmpty(args.Path), "Empty path provided to scene loader");
                Assert.True(SceneHelper.FindSceneByPath(args.Path, SceneCategories.Build).IsValid(), "No scene with path '{0}' found", args.Path);

                if (args.Type == SceneType.Main) {
                    if (m_MainSceneLoadProcess) {
                        Log.Error("Multiple main scene load processes at once.");
                        m_LoadProcessQueue.PopFront();
                    } else {
                        m_MainSceneLoadProcess = Routine.Start(SceneLoadProcess(args));
                        m_LoadProcessQueue.PopFront();
                    }
                } else {
                    if (!m_AdditionalSceneLoadProcess) {
                        SceneDataExt data = SceneDataExt.GetByPath(args.Path);
                        if (data == null || !data.IsVisited(SceneDataExt.VisitFlags.Unloading)) {
                            m_AdditionalSceneLoadProcess = Routine.Start(SceneLoadProcess(args));
                            m_LoadProcessQueue.PopFront();
                        }
                    }
                }
            }
        }

        private bool ProcessLoadQueue() {
            if (m_CurrentLoadOperation.Active) {
                LoadSceneArgs args = m_CurrentLoadOperation.Args;
                if (IsDoneLoading(m_CurrentLoadOperation.UnityOp, args, out Scene scene)) {
                    Log.Msg("[SceneMgr] Additive load of '{0}' complete", args.ScenePath);
                    EnqueueSceneProcessors(scene, args);
                    m_CurrentLoadOperation.Clear();
                    return true;
                } else {
                    return false;
                }
            } else if (m_CurrentLoadOperation.TryFill(m_LoadQueue)) {
                LoadSceneArgs args = m_CurrentLoadOperation.Args;
                Scene currentScene = SafeGetSceneByPath(args.ScenePath);
                if (!IsLoadingOrLoaded(currentScene)) {
                    Log.Msg("[SceneMgr] Starting additive load of '{0}'", args.ScenePath);
#if UNITY_EDITOR
                    m_CurrentLoadOperation.UnityOp = EditorSceneManager.LoadSceneAsyncInPlayMode(args.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
                    m_CurrentLoadOperation.UnityOp = SceneManager.LoadSceneAsync(args.ScenePath, LoadSceneMode.Additive);
#endif // UNITY_EDITOR
                } else if (currentScene.isLoaded) {
                    Log.Msg("[SceneMgr] Scene '{0}' already loaded", args.ScenePath);
                    EnqueueSceneProcessors(currentScene, args);
                    m_CurrentLoadOperation.Clear();
                }
                return true;
            } else {
                return false;
            }
        }

        private void EnqueueSceneProcessors(Scene scene, in LoadSceneArgs args) {
            SceneDataExt data = SceneDataExt.Get(scene);
            Assert.True(data);
            Assert.NotNull(args.Queue);

            if (!data.TryVisit(SceneDataExt.VisitFlags.Loaded)) {
                args.Counter.Decrement();
                return;
            }

            data.SceneTag = args.Tag;
            data.SceneType = args.Type;

            if (args.Parent != null && args.Type != SceneType.Persistent && (args.Flags & SceneImportFlags.AttachAsChild) != 0) {
                args.Parent.Children.PushBack(data);
            }

            args.Queue.PushBack(data);

            switch (args.Type) {
                case SceneType.Main: {
                    m_MainScene = data;
                    SceneManager.SetActiveScene(scene);
                    break;
                }

                case SceneType.Aux: {
                    m_AuxScenes.PushBack(data);;
                    break;
                }

                case SceneType.Persistent: {
                    m_PersistentScenes.PushBack(data);
                    break;
                }
            }

            if (!data.IsVisited(SceneDataExt.VisitFlags.LightCopied)) {
                if ((args.Flags & SceneImportFlags.ImportLightingSettings) != 0) {
                    m_LightingCopyQueue.PushBack(new ImportLightingArgs() {
                        Counter = args.Counter,
                        Data = data
                    });
                    args.Counter.Increment();
                } else {
                    data.TryVisit(SceneDataExt.VisitFlags.LightCopied);
                }
            }

            if (!data.IsVisited(SceneDataExt.VisitFlags.Transformed)) {
                if (args.Transform.HasValue) {
                    m_TransformRootsQueue.PushBack(new TransformSceneArgs() {
                        Counter = args.Counter,
                        Scene = scene,
                        Data = data,
                        Transform = args.Transform.Value
                    });
                    args.Counter.Increment();
                } else {
                    data.TryVisit(SceneDataExt.VisitFlags.Transformed);
                }
            }

            if (!data.IsVisited(SceneDataExt.VisitFlags.Subscenes)) {
                if (data.DynamicSubscenes.Length + data.SubScenes.Length > 0) {
                    m_SubSceneQueue.PushBack(new QueueSubScenesArgs() {
                        Data = data,
                        Counter = args.Counter,
                        Queue = args.Queue
                    });
                    args.Counter.Increment();
                } else {
                    data.TryVisit(SceneDataExt.VisitFlags.Subscenes);
                }
            }

            FlushSceneLoadCallbacks(data, scene, args.Type == SceneType.Main);

            args.Counter.Decrement();

            if (!OnPrepareScene.IsEmpty) {
                OnPrepareScene.Invoke(new SceneEventArgs() {
                    LoadType = args.Type,
                    Scene = scene
                });
            }
        }

        private bool ProcessUnloadQueue() {
            if (m_CurrentUnloadOperation.Active) {
                if (m_CurrentUnloadOperation.UnityOp == null || m_CurrentUnloadOperation.UnityOp.isDone) {
                    Log.Msg("[SceneMgr] Unload complete");
                    m_CurrentUnloadOperation.Args.Counter.Decrement();

                    if (!OnAnySceneUnloaded.IsEmpty) {
                        OnAnySceneUnloaded.Invoke();
                    }

                    m_CurrentUnloadOperation.Clear();
                    Game.Events?.CleanupDeadReferences();
                    return true;
                } else {
                    return false;
                }
            } else if (m_UnloadQueue.TryPopFront(out UnloadSceneArgs args)) { 
                if (args.Data) {
                    // if the scene hasn't finished loading, then push this off until later
                    if (!args.Data.IsVisited(SceneDataExt.VisitFlags.Readied)) {
                        Log.Warn("[SceneMgr] Delaying unload of scene '{0}' until scene is finished with load process", args.Data.Scene.path);
                        m_UnloadQueue.PushBack(args);
                        return false;
                    } else if (args.Data.TryVisit(SceneDataExt.VisitFlags.Unloaded)) { // otherwise, if it hasn't already been unloaded
                        m_CurrentUnloadOperation.Fill(args);
                        var scene = args.Data.Scene;
                        FlushCallbacks(args.Data.UnloadingCallbackQueue);
                        foreach (ISceneCustomData custom in args.Data.CustomData) {
                            custom.OnUnload();
                        }
                        SceneHelper.OnUnload(scene);
                        if (!OnSceneUnload.IsEmpty) {
                            OnSceneUnload.Invoke(new SceneEventArgs() {
                                LoadType = args.Data.SceneType,
                                Scene = args.Data.Scene
                            });
                        }

                        // if the whole tree should be unloaded
                        if (args.UnloadTree) {
                            foreach (var child in args.Data.Children) {
                                m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                                    Data = child,
                                    Options = args.Options,
                                    UnloadTree = args.UnloadTree,
                                    Counter = args.Counter
                                });
                                args.Counter.Increment();
                            }
                        }
                        Log.Msg("[SceneMgr] Unloading '{0}'", args.Data.Scene.path);
                        m_CurrentUnloadOperation.UnityOp = SceneManager.UnloadSceneAsync(scene, args.Options);
                    } else { // otherwise, it's already done and we can move on
                        m_CurrentUnloadOperation.Clear();
                        args.Counter.Decrement();
                    }
                } else {
                    m_CurrentUnloadOperation.Clear();
                    args.Counter.Decrement();
                }
                return true;
            } else {
                return false;
            }
        }

        private bool ProcessImportLightingQueue() {
            if (m_LightingCopyQueue.TryPopFront(out var args)) {
                if (args.Data.TryVisit(SceneDataExt.VisitFlags.LightCopied)) {
                    LightUtility.CopySettingsToActive(args.Data.Scene, m_LightImportFlags);
                    Log.Msg("[SceneMgr] Copied lighting settings from '{0}'", args.Data.Scene.path);
                }
                args.Counter.Decrement();
                return true;
            } else {
                return false;
            }
        }

        private bool ProcessTransformSceneQueue() {
            if (m_TransformRootsQueue.TryPopFront(out var args)) {
                if (args.Data.TryVisit(SceneDataExt.VisitFlags.Transformed)) {
                    ImportScene.TransformRoots(args.Scene, args.Transform);
                    Log.Msg("[SceneMgr] Transformed roots of '{0}'", args.Data.Scene.path);
                }
                args.Counter.Decrement();
                return true;
            } else {
                return false;
            }
        }

        private bool ProcessSubSceneImportQueue() {
            if (m_SubSceneQueue.TryPopFront(out var args)) {
                if (args.Data.TryVisit(SceneDataExt.VisitFlags.Subscenes)) {
                    // queue subscenes
                    foreach (var subscene in args.Data.SubScenes) {
                        SceneImportSettings import = subscene.GetImportSettings();
                        m_LoadQueue.PushBack(new LoadSceneArgs() {
                            Counter = args.Counter,
                            Flags = import.Flags,
                            Type = args.Data.SceneType == SceneType.Persistent ? SceneType.Persistent : import.LoadType,
                            Parent = args.Data,
                            Tag = import.Tag,
                            ScenePath = import.Path,
                            Queue = args.Queue
                        });
                        args.Counter.Increment();
                    }

                    // queue dynamic subscenes
                    foreach (IDynamicSceneImport resolver in args.Data.DynamicSubscenes) {
                        foreach (var import in resolver.GetSubscenes()) {
                            m_LoadQueue.PushBack(new LoadSceneArgs() {
                                Counter = args.Counter,
                                Flags = import.Flags,
                                Type = args.Data.SceneType == SceneType.Persistent ? SceneType.Persistent : import.LoadType,
                                Parent = args.Data,
                                Tag = import.Tag.IsEmpty ? args.Data.tag : import.Tag,
                                ScenePath = import.Path,
                                Queue = args.Queue
                            });
                            args.Counter.Increment();
                        }
                    }

                    Log.Msg("[SceneMgr] Subscenes from '{0}' evaluated", args.Data.Scene.path);
                }

                args.Counter.Decrement();
                return true;
            } else {
                return false;
            }
        }

        private bool ProcessPreloadQueue() {
            if (m_CurrentPreloadOperation.Active) {
                var result = WorkSlicer.Step(m_CurrentPreloadOperation.Preloads, PreloadManifest.ExecutePreloader, ref m_CurrentPreloadOperation.WorkState);
                if (result == WorkSlicer.Result.OutOfData) {
                    int nextCount = m_CurrentPreloadOperation.Reader.Read(m_CurrentPreloadOperation.Preloads);
                    if (nextCount == 0) {
                        m_CurrentPreloadOperation.Counter.Decrement();
                        m_CurrentPreloadOperation.Clear();
                        return false;
                    }
                }
                return true;
            } else {
                if (m_PreloadQueue.TryPopFront(out var args)) {
                    m_CurrentPreloadOperation.WorkState.Clear();
                    m_CurrentPreloadOperation.Preloads.Clear();
                    m_CurrentPreloadOperation.Reader.Init(args.Manifests);
                    Log.Msg("[SceneMgr] Starting preload");
                    m_CurrentPreloadOperation.Counter = args.Counter;
                    m_CurrentPreloadOperation.Active = true;
                    return true;
                }
                return false;
            }
        }

        private bool ProcessLateEnableQueue() {
            if (m_LateEnableQueue.Count > 0) {
                while (m_LateEnableQueue.TryPopFront(out var args)) {
                    if (args.Data.TryVisit(SceneDataExt.VisitFlags.LateEnabled)) {
                        foreach (var obj in args.Data.LateEnable) {
                            obj.SetActive(true);
                        }
                        foreach (ISceneCustomData custom in args.Data.CustomData) {
                            custom.OnLateEnable();
                        }
                        FlushCallbacks(args.Data.LateEnableCallbackQueue);
                        if (!OnAnySceneEnabled.IsEmpty) {
                            OnAnySceneEnabled.Invoke();
                        }
                        Log.Msg("[SceneMgr] LateEnable processed for '{0}'", args.Data.Scene.path);
                    }
                    args.Counter.Decrement();
                }
                return true;
            }

            return false;
        }

        private void FlushCallbacks(RingBuffer<Action> callbacks) {
            while(callbacks.TryPopFront(out Action act)) {
                act();
            }
        }

        #endregion // Operations

        #region Routines

        private void ClearNonPersistentLoadProcesses() {
            for(int i = m_LoadProcessQueue.Count - 1; i >= 0; i--) {
                if (m_LoadProcessQueue[i].Type != SceneType.Persistent) {
                    m_LoadProcessQueue.RemoveAt(i);
                }
            }
        }

        private IEnumerator SceneLoadProcess(LoadProcessArgs args) {
            using(CounterHandle counter = CounterHandle.Alloc()) {

                // validate
                SceneDataExt existing = SceneDataExt.GetByPath(args.Path);
                if ((args.Flags & SceneImportFlags.ForceReload) == 0 && existing != null && existing.IsVisited(SceneDataExt.VisitFlags.Loaded)) {
                    yield break;
                }

                while(ScenesUtility.Editor.AreDelayedSceneProcessorsRunning()) {
                    yield return null;
                }

                RingBuffer<SceneDataExt> linearizedScenes = new RingBuffer<SceneDataExt>(4, RingBufferMode.Expand);

                // unloading

                if (args.Type == SceneType.Main) {
                    m_MainSceneTransition.Stop();

                    Game.Events.Dispatch(ScenesUtility.Events.PreUnload);
                    OnMainSceneUnloading.Invoke();

                    if (m_MainTransitionUnload != null) {
                        if (m_MainScene != null || m_AuxScenes.Count > 0) {
                            Scene targetScene = SafeGetSceneByPath(args.Path);
                            IEnumerator wait = m_MainTransitionUnload(targetScene, args.Tag);
                            if (wait != null) {
                                yield return wait;
                            }
                        }
                    }

                    if (m_MainScene != null) {
                        m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                            Data = m_MainScene,
                            Options = UnloadSceneOptions.None,
                            Counter = counter
                        });
                        counter.Increment();

                        m_MainScene = null;
                    }

                    while (m_AuxScenes.TryPopFront(out var aux)) {
                        m_UnloadQueue.PushBack(new UnloadSceneArgs() {
                            Data = aux,
                            Options = UnloadSceneOptions.None,
                            Counter = counter
                        });
                        counter.Increment();
                    }

                    while (!counter.IsDone()) {
                        yield return null;
                    }
                }

                // load main scene and traverse graph

                counter.Reset();

                m_LoadQueue.PushBack(new LoadSceneArgs() {
                    Counter = counter,
                    Flags = args.Flags,
                    Parent = null,
                    ScenePath = args.Path,
                    Type = args.Type,
                    Transform = args.Transform,
                    Tag = args.Tag,
                    Queue = linearizedScenes
                });
                counter.Increment();

                while(!counter.IsDone()) {
                    yield return null;
                }

                // preloads

                counter.Reset();

                PreloadManifest[] manifests = new PreloadManifest[linearizedScenes.Count];

                for(int i = 0; i < manifests.Length; i++) {
                    manifests[i] = linearizedScenes[i].Preload;
                    if (!OnScenePreload.IsEmpty) {
                        OnScenePreload.Invoke(new SceneEventArgs() {
                            Scene = linearizedScenes[i].Scene,
                            LoadType = linearizedScenes[i].SceneType
                        });
                    }
                }

                m_PreloadQueue.PushBack(new PreloadArgs() {
                    Manifests = manifests,
                    Counter = counter
                });
                counter.Increment();

                while (!counter.IsDone()) {
                    yield return null;
                }

                // dependencies

                while(!AreDependenciesAndStreamingLoaded(SceneLoadPhase.BeforeLateEnable)) {
                    yield return null;
                }

                // unload unused assets

                yield return AssetUtility.UnloadUnused();
                Streaming.UnloadUnusedAsync();

                if (args.Type == SceneType.Main) {
                    using (Profiling.Time("gc collect", ProfileTimeUnits.Microseconds)) {
                        GC.Collect();
                    }
                    
                    while(Streaming.IsUnloading()) {
                        yield return null;
                    }
                }

                // late enable

                counter.Reset();

                foreach(var data in linearizedScenes) {
                    if (data.LateEnable.Length > 0 && !data.IsVisited(SceneDataExt.VisitFlags.LateEnabled)) {
                        m_LateEnableQueue.PushBack(new LateEnableArgs() {
                            Data = data,
                            Counter = counter
                        });
                        counter.Increment();
                    } else {
                        data.TryVisit(SceneDataExt.VisitFlags.LateEnabled);
                        FlushCallbacks(data.LateEnableCallbackQueue);
                        if (!OnAnySceneEnabled.IsEmpty) {
                            OnAnySceneEnabled.Invoke();
                        }
                    }
                }

                while(!counter.IsDone()) {
                    yield return null;
                }

                if (args.Type == SceneType.Main) {
                    OnMainSceneLateEnable.Invoke();
                }

                // one more check for dependencies

                while (!AreDependenciesAndStreamingLoaded(SceneLoadPhase.BeforeReady)) {
                    yield return null;
                }

                // broadcast ready

                Log.Msg("[SceneMgr] Scene is ready");

                foreach (var data in linearizedScenes) {
                    data.TryVisit(SceneDataExt.VisitFlags.Readied);
                    OnSceneReady.Invoke(new SceneEventArgs() {
                        Scene = data.Scene,
                        LoadType = data.SceneType
                    });
                    foreach (ISceneCustomData custom in data.CustomData) {
                        custom.OnReady();
                    }
                    FlushCallbacks(data.LoadedCallbackQueue);
                }

                if (args.Type == SceneType.Main) {
                    OnMainSceneReady.Invoke();

                    if (m_MainTransitionLoad != null) {
                        Scene targetScene = SafeGetSceneByPath(args.Path);
                        IEnumerator wait = m_MainTransitionLoad(targetScene, args.Tag);
                        m_MainSceneTransition.Replace(wait);
                    }
                }

                Game.Events.Dispatch(ScenesUtility.Events.Ready);
            }
        }

        #endregion // Routines

        #region Dependencies

        private bool AreDependenciesAndStreamingLoaded(SceneLoadPhase phase) {
            for(int i = 0; i < m_Dependencies.Count; i++) {
                if (!m_Dependencies[i].IsLoaded(phase)) {
                    return false;
                }
            }

            while(m_DependencyHandles.TryPeekFront(out AsyncHandle handle)) {
                if (handle.IsRunning()) {
                    return false;
                }
                m_DependencyHandles.PopFront();
            }

            if (Streaming.IsLoading()) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns if all load dependencies loaded.
        /// </summary>
        public bool AreLoadDependenciesLoaded(SceneLoadPhase phase = SceneLoadPhase.Any) {
            for (int i = 0; i < m_Dependencies.Count; i++) {
                if (!m_Dependencies[i].IsLoaded(phase)) {
                    return false;
                }
            }

            while (m_DependencyHandles.TryPeekFront(out AsyncHandle handle)) {
                if (handle.IsRunning()) {
                    return false;
                }
                m_DependencyHandles.PopFront();
            }

            if (Streaming.IsLoading()) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers a dependency, which must be completed before scenes can be late-enabled and readied.
        /// </summary>
        public void RegisterLoadDependency(ISceneLoadDependency loadDependency) {
            Assert.NotNull(loadDependency);
            if (!m_Dependencies.Contains(loadDependency)) {
                m_Dependencies.PushBack(loadDependency);
                Log.Msg("[SceneMgr] Registered scene load dependency '{0}'", AssetUtility.NameOf(loadDependency));
            }
        }

        /// <summary>
        /// Registers a dependency, which must be completed before scenes can be late-enabled and readied.
        /// </summary>
        public void RegisterLoadDependency(AsyncHandle loadDependency) {
            if (loadDependency.IsRunning() && !m_DependencyHandles.Contains(loadDependency)) {
                m_DependencyHandles.PushBack(loadDependency);
                Log.Msg("[SceneMgr] Registered scene load dependency async handle");
            }
        }

        /// <summary>
        /// Deregisters a load dependency.
        /// </summary>
        public void DeregisterLoadDependency(ISceneLoadDependency loadDependency) {
            Assert.NotNull(loadDependency);
            if (m_Dependencies.FastRemove(loadDependency)) {
                Log.Msg("[SceneMgr] Deregistered scene load dependency async handle");
            }
        }

        #endregion // Dependencies
    }

    /// <summary>
    /// Scene event arguments.
    /// </summary>
    public struct SceneEventArgs {
        public Scene Scene;
        public SceneType LoadType;
    }

    /// <summary>
    /// Load priority for non-main scene loads.
    /// </summary>
    public enum SceneLoadPriority {
        Default,
        High
    }

    /// <summary>
    /// Scene utility methods.
    /// </summary>
    static public class ScenesUtility {
        static public class Events {
            static public readonly StringHash32 Ready = "SceneMgr::Ready";
            static public readonly StringHash32 PreUnload = "SceneMgr::PreUnload";
        }

        /// <summary>
        /// Returns the active scene's build index.
        /// </summary>
        static public int ActiveSceneIndex() {
            return SceneManager.GetActiveScene().buildIndex;
        }

        /// <summary>
        /// Returns the active scene's name.
        /// </summary>
        static public string ActiveSceneName() {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Retrieves additional baked scene data for the current scene.
        /// </summary>
        static public TData GetActiveSceneBakedData<TData>() where TData : MonoBehaviour, ISceneCustomData {
            var sceneData = SceneDataExt.Get(SceneManager.GetActiveScene());
            if (sceneData) {
                return sceneData.GetComponent<TData>();
            } else {
                return null;
            }
        }

        /// <summary>
        /// Returns if the given GameObject will persist across scenes.
        /// </summary>
        static public bool IsPersistent(GameObject gameObject) {
            return gameObject.TryGetComponent(out Persist _);
        }

        /// <summary>
        /// Returns if the given component's GameObject will persist across scenes.
        /// </summary>
        static public bool IsPersistent(Component component) {
            return component.TryGetComponent(out Persist _);
        }

        static public class Editor {
#if UNITY_EDITOR
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public bool AreDelayedSceneProcessorsRunning() {
                return s_DelayedSceneProcessorsRunning;
            }

            static private bool s_DelayedSceneProcessorsRunning;

            static internal void SetDelayedSceneProcessorsRunning(bool running) {
                s_DelayedSceneProcessorsRunning = running;
            }
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public bool AreDelayedSceneProcessorsRunning() {
                return false;
            }
#endif // UNITY_EDITOR
        }
    }

    /// <summary>
    /// Delegate for handling scene transitions.
    /// </summary>
    public delegate IEnumerator SceneTransitionHandler(Scene scene, StringHash32 tag);

    /// <summary>
    /// Scene loading dependency.
    /// </summary>
    public interface ISceneLoadDependency {
        bool IsLoaded(SceneLoadPhase loadPhase);
    }

    /// <summary>
    /// Scene load phase.
    /// </summary>
    [Flags]
    public enum SceneLoadPhase {
        Any = 0,
        BeforeLateEnable = 0x1,
        BeforeReady = 0x2,
    }
}