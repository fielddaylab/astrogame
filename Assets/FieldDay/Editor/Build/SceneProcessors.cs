#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using FieldDay.Debugging;
using FieldDay.Perf;
using FieldDay.Rendering;
using FieldDay.Scenes;
using ScriptableBake;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Editor {
    #region Build Pipeline Only

    /// <summary>
    /// Strips editor-only data from scene objects.
    /// </summary>
    public class StripEditorDataSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10000; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (!BuildPipeline.isBuildingPlayer) {
                return;
            }

            List<IEditorOnlyData> toStrip = new List<IEditorOnlyData>(256);
            scene.GetAllComponents(true, toStrip);

            if (toStrip.Count > 0) {
                Debug.LogFormat("[StripEditorDataSceneProcessor] Found {0} objects with editor-only data...", toStrip.Count);
                using(Profiling.Time("stripping editor-only data")) {
                    foreach(var obj in toStrip) {
                        obj.ClearEditorData(EditorUserBuildSettings.development);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Strips GameLoop GameObjects, from all scenes except for the initial scene.
    /// </summary>
    public class StripGameLoopSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (!BuildPipeline.isBuildingPlayer) {
                return;
            }

            if (scene.buildIndex == 0) {
                return;
            }

            GameLoop[] toRemove = GameObject.FindObjectsOfType<GameLoop>();
            if (toRemove.Length > 0) {
                Debug.LogFormat("[StripGameLoopSceneProcessor] Removing GameLoop GameObjects from scene '{0}'", scene.name);
                foreach(var obj in toRemove) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Strips DebugDraw components, and DebugConsole GameObjects, from non-development builds.
    /// </summary>
    public class StripDebugSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (!BuildPipeline.isBuildingPlayer || EditorUserBuildSettings.development) {
                return;
            }

            DebugDraw[] toRemoveDraw = GameObject.FindObjectsOfType<DebugDraw>();
            if (toRemoveDraw.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing DebugDraw objects from scene '{0}'", scene.name);
                foreach (var obj in toRemoveDraw) {
                    GameObject.DestroyImmediate(obj);
                }
            }

            DebugConsole[] toRemoveConsole = GameObject.FindObjectsOfType<DebugConsole>();
            if (toRemoveConsole.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing DebugConsole GameObjects from scene '{0}'", scene.name);
                foreach (var obj in toRemoveConsole) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }

            DevModeOnly[] toRemoveDevMode = GameObject.FindObjectsOfType<DevModeOnly>();
            if (toRemoveDevMode.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing {0} DevModeOnly GameObjects from scene '{1}'", toRemoveDevMode.Length, scene.name);
                foreach (var obj in toRemoveDevMode) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }

#if DISABLE_FRAMERATE_COUNTER
            FramerateDisplay[] toRemoveFramerates = GameObject.FindObjectsOfType<FramerateDisplay>();
            if (toRemoveFramerates.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing {0} FramerateDisplay GameObjects from scene '{1}'", toRemoveFramerates.Length, scene.name);
                foreach(var obj in toRemoveFramerates) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }
#endif // DISABLE_FRAMERATE_COUNTER

            List<IDevModeOnly> devModeOnlyComponents = new List<IDevModeOnly>(256);
            scene.GetAllComponents(true, devModeOnlyComponents);
            if (devModeOnlyComponents.Count > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing {0} IDevModeOnly components from scene '{1}'", devModeOnlyComponents.Count, scene.name);
                foreach (var obj in devModeOnlyComponents) {
                    GameObject.DestroyImmediate(obj as UnityEngine.Component);
                }
            }
        }
    }

    #endregion // Build Pipeline Only

    /// <summary>
    /// Executes any custom bake processes for scene objects.
    /// </summary>
    public class BakeSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 20; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (PlayModeDelayedSceneProcessor.IsQueued()) {
                PlayModeDelayedSceneProcessor.Queue(this, scene, report, null);
                return;
            }

            using(Profiling.Time("baking objects")) {
                Debug.LogFormat("baking '{0}'", scene.path);
                Baking.BakeScene(scene, 0);
            }
        }
    }

    /// <summary>
    /// Generates the SceneDataExt object.
    /// </summary>
    public class GenerateSceneDataExtProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 30; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (PlayModeDelayedSceneProcessor.IsQueued()) {
                PlayModeDelayedSceneProcessor.Queue(this, scene, report, null);
                return;
            }

            using (Profiling.Time("generating SceneDataExt")) {

                List<SceneDataExt> list = new List<SceneDataExt>();
                scene.GetAllComponents(true, list);
                SceneDataExt ext = list.Count > 0 ? list[0] : null;
                if (ext == null) {
                    GameObject extGO = new GameObject("__SceneDataExt");
                    SceneManager.MoveGameObjectToScene(extGO, scene);
                    ext = extGO.AddComponent<SceneDataExt>();
                }

                // preload manifest
                ext.Preload = PreloadManifest.Generate(scene);

                // late enabled objects
                List<LateEnable> allLateEnables = new List<LateEnable>(32);
                scene.GetAllComponents(true, allLateEnables);
                allLateEnables.Sort((a, b) => a.Order - b.Order);
                ext.LateEnable = new GameObject[allLateEnables.Count];
                for (int i = 0; i < allLateEnables.Count; i++) {
                    ext.LateEnable[i] = allLateEnables[i].gameObject;
                    ext.LateEnable[i].SetActive(false);
                    Baking.Destroy(allLateEnables[i]);
                }

                // remaining ImportScene objects
                List<ImportScene> allSubscenes = new List<ImportScene>(4);
                scene.GetAllComponents(true, allSubscenes);
                ext.SubScenes = allSubscenes.ToArray();

                // dynamic scene import components
                List<IDynamicSceneImport> dynamicImports = new List<IDynamicSceneImport>();
                SceneHelper.GetAllComponents(scene, dynamicImports);
                ext.DynamicSubscenes = new Component[dynamicImports.Count];
                for (int i = 0; i < dynamicImports.Count; i++) {
                    ext.DynamicSubscenes[i] = (Component) dynamicImports[i];
                }

                ext.CustomData = Array.Empty<Component>();
            }
        }
    }

    /// <summary>
    /// Merges in any scenes that should be merged into the main scene.
    /// </summary>
    public class ImportMergeScenesSceneProcessor : IProcessSceneWithReport {
        static private readonly FieldInfo PersistIdField = typeof(Persist).GetField("m_UniqueID", BindingFlags.NonPublic | BindingFlags.Instance);

        private HashSet<string> visitedScenes;
        private List<ImportScene> importBuffer;
        private RingBuffer<ImportScene> importQueue;
        private RingBuffer<DelayedImportArgs> delayedImports;
        private HashSet<string> persists;

        public int callbackOrder { get { return -10; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (PlayModeDelayedSceneProcessor.IsQueued()) {
                PlayModeDelayedSceneProcessor.Queue(this, scene, report, null);
                return;
            }

            using (Profiling.Time("importing scenes")) {
                importQueue = new RingBuffer<ImportScene>();
                delayedImports = new RingBuffer<DelayedImportArgs>();

                visitedScenes = new HashSet<string>();
                visitedScenes.Add(scene.path);

                if (!EditorApplication.isPlaying) {
                    persists = new HashSet<string>();
                    HandlePersists(scene);
                }

                importBuffer = new List<ImportScene>();
                scene.GetAllComponents(true, importBuffer);

                foreach(var import in importBuffer) {
                    importQueue.PushBack(import);
                }

                EvaluateImportQueue(scene, report);
            }
        }

        #region Merge

        private void AttemptMerge(ImportScene import, Scene scene, BuildReport report) {
            if (!import.Merge) {
                return;
            }

            if (!visitedScenes.Add(import.Scene.Path)) {
                return;
            }

            SceneImportSettings settings = import.GetImportSettings();

            if (import.DestroyGameObject && Baking.IsEmptyLeaf(import.transform, 1)) {
                Baking.Destroy(import.gameObject);
            } else {
                Baking.Destroy(import);
            }

            Scene subsceneRef = EditorSceneManager.GetSceneByPath(settings.Path);

            if (EditorApplication.isPlaying) {
                subsceneRef = SceneManager.GetSceneByPath(settings.Path);
                if (!subsceneRef.IsValid()) {
                    subsceneRef = EditorSceneManager.LoadSceneInPlayMode(settings.Path, new LoadSceneParameters(LoadSceneMode.Additive));
                } else {
                    Debug.LogFormat("[ImportMergeScenesSceneProcessor] Scene '{0}' already present", settings.Path);
                }
            } else {
                subsceneRef = EditorSceneManager.OpenScene(settings.Path, OpenSceneMode.Additive);
            }

            Assert.True(subsceneRef.IsValid(), "Scene '{0}' is not valid", settings.Path);

            if (subsceneRef.isLoaded) {
                FinishMerge(scene, report, subsceneRef, settings);
            } else {
                delayedImports.PushBack(new DelayedImportArgs() {
                    Scene = subsceneRef,
                    Settings = settings
                });
                PlayModeDelayedSceneProcessor.IgnoreScene(subsceneRef);
            }
        }

        private void FinishMerge(Scene scene, BuildReport report, Scene subsceneRef, SceneImportSettings settings) {
            Assert.True(subsceneRef.isLoaded, "Scene '{0}' is not loaded", settings.Path);

            if (!EditorApplication.isPlaying) {
                HandlePersists(subsceneRef);
            }

            foreach (var root in subsceneRef.GetRootGameObjects()) {
                root.GetComponentsInChildren(true, importBuffer);
                foreach (var subImport in importBuffer) {
                    importQueue.PushBack(subImport);
                }

                SceneManager.MoveGameObjectToScene(root, scene);
                ImportScene.TransformRoot(root.transform, settings.RootMatrix);
            }

            if ((settings.Flags & SceneImportFlags.ImportLightingSettings) != 0) {
                LightUtility.CopySettingsToScene(subsceneRef, scene, LightingImportFlags.All);
            }

            if (EditorApplication.isPlaying) {
#pragma warning disable CS0618
                EditorSceneManager.UnloadScene(subsceneRef);
#pragma warning restore CS0618
            } else {
                EditorSceneManager.CloseScene(subsceneRef, true);
            }
        }

        private void HandlePersists(Scene scene) {
            List<Persist> persistsInScene = new List<Persist>(8);
            scene.GetAllComponents(true, persistsInScene);

            foreach (var persist in persistsInScene) {
                if (!persist) {
                    continue;
                }

                string id = (string) PersistIdField.GetValue(persist);
                if (string.IsNullOrEmpty(id)) {
                    continue;
                }

                if (!persists.Add(id)) {
                    Debug.LogFormat("[ImportMergeScenesSceneProcessor] Removing duplicate persist object '{0}' ('{1}') from scene '{2}'", persist.name, id, scene.path);
                    Baking.Destroy(persist.gameObject);
                }
            }
        }

        #endregion // Merge

        #region Queues

        private struct DelayedImportArgs {
            public Scene Scene;
            public SceneImportSettings Settings;
        }

        private void DelayedFlushQueues(Scene scene, BuildReport report, object arg) {
            EvaluateDelayedImportedScenes(scene, report);
            EvaluateImportQueue(scene, report);
        }

        private void EvaluateDelayedImportedScenes(Scene scene, BuildReport report) {
            while(delayedImports.TryPopFront(out DelayedImportArgs args)) {
                FinishMerge(scene, report, args.Scene, args.Settings);
                Debug.LogFormat("[ImportMergeScenesSceneProcessor] Finished importing '{0}'", args.Settings.Path);
            }
        }

        private void EvaluateImportQueue(Scene scene, BuildReport report) {
            while(importQueue.TryPopFront(out ImportScene import)) {
                AttemptMerge(import, scene, report);
            }

            if (delayedImports.Count > 0) {
                PlayModeDelayedSceneProcessor.QueueFront(DelayedFlushQueues, scene, report, null);
            }
        }

        #endregion // Queues
    }

    /// <summary>
    /// Cleans up missing components on gameobjects.
    /// </summary>
    public class CleanUpMissingComponentsSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 30; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (PlayModeDelayedSceneProcessor.IsQueued()) {
                PlayModeDelayedSceneProcessor.Queue(this, scene, report, null);
                return;
            }

            if (Baking.CleanUpMissingComponents(scene.GetRootGameObjects())) {
                Debug.LogWarningFormat("[CleanUpMissingComponentsSceneProcessor] Missing component types cleaned up in '{0}'", scene.path);
            }
        }
    }

    /// <summary>
    /// Gathers additional custom scene data.
    /// </summary>
    public class GatherCustomSceneDataSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 32000; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (PlayModeDelayedSceneProcessor.IsQueued()) {
                PlayModeDelayedSceneProcessor.Queue(this, scene, report, null);
                return;
            }

            using (Profiling.Time("generating custom scene data")) {
                List<SceneDataExt> list = new List<SceneDataExt>();
                scene.GetAllComponents(true, list);
                SceneDataExt ext = list.Count > 0 ? list[0] : null;
                if (ext == null) {
                    Debug.LogErrorFormat("[GatherCustomSceneDataSceneProcessor] No SceneDataExt able to be found");
                    return;
                }

                foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ISceneCustomData))) {
                    if (type.IsAbstract || type.IsInterface) {
                        continue;
                    }

                    if (!typeof(MonoBehaviour).IsAssignableFrom(type)) {
                        Debug.LogErrorFormat("[GatherCustomSceneDataSceneProcessor] Custom type '{0}' is not derived from MonoBehaviour", type.FullName);
                        continue;
                    }

                    ISceneCustomData c = (ISceneCustomData) ext.gameObject.AddComponent(type);
                    if (!c.Build(scene)) {
                        GameObject.DestroyImmediate((UnityEngine.Object) c);
                    }
                }

                ext.CustomData = ext.GetComponents(typeof(ISceneCustomData));
            }
        }
    }
}