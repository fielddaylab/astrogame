using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Scenes {
    /// <summary>
    /// Extended scene data.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-32000), AddComponentMenu("")]
    internal sealed class SceneDataExt : MonoBehaviour {
        /// <summary>
        /// Processed flags.
        /// </summary>
        [Flags]
        internal enum VisitFlags : uint {
            Loaded = 0x01,
            Transformed = 0x02,
            LightCopied = 0x04,
            Subscenes = 0x08,
            Preloaded = 0x10,
            LateEnabled = 0x20,
            Readied = 0x40,
            Unloading = 0x80,
            Unloaded = 0x100,
        }

        /// <summary>
        /// Preload data.
        /// </summary>
        public PreloadManifest Preload;

        /// <summary>
        /// Subscenes to import (but not merge).
        /// </summary>
        public ImportScene[] SubScenes;

        /// <summary>
        /// Dynamic subscenes to import.
        /// </summary>
        public Component[] DynamicSubscenes;

        /// <summary>
        /// List of objects to enable late.
        /// </summary>
        public GameObject[] LateEnable;

        /// <summary>
        /// Additional data objects.
        /// </summary>
        public Component[] CustomData;

        #region Temp State

        /// <summary>
        /// List of all immediately loaded scenes.
        /// </summary>
        [NonSerialized] public readonly RingBuffer<SceneDataExt> Children = new RingBuffer<SceneDataExt>();

        /// <summary>
        /// Current scene.
        /// </summary>
        [NonSerialized] public StringHash32 SceneTag;

        /// <summary>
        /// Scene type.
        /// </summary>
        [NonSerialized] public SceneType SceneType;

        /// <summary>
        /// Whether or not this has been visited.
        /// </summary>
        [NonSerialized] private VisitFlags m_VisitState;

        /// <summary>
        /// Reference to the current scene.
        /// </summary>
        public Scene Scene { get { return gameObject.scene; } }

        /// <summary>
        /// Reference to the current scene.
        /// </summary>
        public SceneBinding SceneBinding { get; private set; }

        #endregion // Temp State

        /// <summary>
        /// Attempts to visit for the given step.
        /// </summary>
        public bool TryVisit(VisitFlags flags) {
            if ((m_VisitState & flags) != flags) {
                m_VisitState |= flags;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if the given visit steps have completed.
        /// </summary>
        public bool IsVisited(VisitFlags flags) {
            return (m_VisitState & flags) == flags;
        }

        /// <summary>
        /// Queue of callbacks for when the scene is marked as late-enabled.
        /// </summary>
        public RingBuffer<Action> LateEnableCallbackQueue = new RingBuffer<Action>(32, RingBufferMode.Expand);

        /// <summary>
        /// Queue of callbacks for when the scene is marked loaded.
        /// </summary>
        public RingBuffer<Action> LoadedCallbackQueue = new RingBuffer<Action>(32, RingBufferMode.Expand);
        
        /// <summary>
        /// Queues of callbacks for when the scene is marked as being unloaded.
        /// </summary>
        public RingBuffer<Action> UnloadingCallbackQueue = new RingBuffer<Action>(32, RingBufferMode.Expand);

        #region Tracking

        static private readonly List<SceneDataExt> s_Loaded = new List<SceneDataExt>(4);
        static private readonly Dictionary<Scene, SceneDataExt> s_LoadedMap = new Dictionary<Scene, SceneDataExt>(32, new SceneEqualityComparer());
        static private readonly Dictionary<string, SceneDataExt> s_LoadedMapByPath = new Dictionary<string, SceneDataExt>(32, StringComparer.Ordinal);

        private sealed class SceneEqualityComparer : EqualityComparer<Scene> {
            public override bool Equals(Scene x, Scene y) {
                return x.handle == y.handle;
            }

            public override int GetHashCode(Scene obj) {
                return obj.handle;
            }
        }

        private void OnEnable() {
            s_Loaded.Add(this);

            Scene key = gameObject.scene;
            SceneBinding = key;
            if (!s_LoadedMap.ContainsKey(key)) {
                s_LoadedMap.Add(key, this);
                s_LoadedMapByPath.Add(key.path, this);
            }
        }

        private void OnDisable() {
            s_Loaded.FastRemove(this);

            Scene key = gameObject.scene;
            if (s_LoadedMap.TryGetValue(key, out var current) && current == this) {
                s_LoadedMap.Remove(key);
                s_LoadedMapByPath.Remove(key.path);
            }
        }

        private void OnDestroy() {
            Preload = null;
            SubScenes = null;
            DynamicSubscenes = null;
            LateEnable = null;
            CustomData = null;
            Children.Clear();
            SceneTag = default;
            m_VisitState = VisitFlags.Unloaded;
        }

        /// <summary>
        /// Gets all currently loaded instances.
        /// </summary>
        static internal int GetAll(IList<SceneDataExt> output) {
            foreach (var current in s_Loaded) {
                output.Add(current);
            }
            return s_Loaded.Count;
        }

        /// <summary>
        /// Gets all currently loaded instances for the given scene.
        /// </summary>
        static internal int Get(Scene scene, IList<SceneDataExt> output) {
            int count = 0;
            foreach (var current in s_Loaded) {
                if (current.gameObject.scene == scene) {
                    output.Add(current);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets the first loaded instance for the given scene.
        /// </summary>
        static internal SceneDataExt Get(Scene scene) {
            s_LoadedMap.TryGetValue(scene, out SceneDataExt data);
            return data;
        }

        /// <summary>
        /// Gets the first loaded instance for the given scene.
        /// </summary>
        static internal SceneDataExt GetByPath(string scenePath) {
            s_LoadedMapByPath.TryGetValue(scenePath, out SceneDataExt data);
            return data;
        }

        #endregion // Tracking
    }

    /// <summary>
    /// Types of scene load.
    /// </summary>
    public enum SceneType : byte {
        Main,
        Aux,
        Persistent
    }

    /// <summary>
    /// Custom scene data.
    /// </summary>
    public interface ISceneCustomData {
#if UNITY_EDITOR
        bool Build(Scene scene);
#endif // UNITY_EDITOR
        void OnLateEnable();
        void OnReady();
        void OnUnload();
    }
}