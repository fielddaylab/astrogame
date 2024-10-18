using BeauUtil;
using System.Collections.Generic;
using System;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using FieldDay.Scenes;

namespace FieldDay.Editor {
    static internal class PlayModeDelayedSceneProcessor {
        internal struct Entry {
            public IProcessSceneWithReport Processor;
            public Action<Scene, BuildReport, object> Callback;
            public Scene Scene;
            public BuildReport Report;
            public object Arg;
        }

        static private bool s_Queued;
        static private RingBuffer<Entry> s_Entries = new RingBuffer<Entry>(8, RingBufferMode.Expand);
        static private HashSet<Scene> s_IgnoreScenes = new HashSet<Scene>();

        static internal bool IsQueued() {
            return s_Queued;
        }

        static private void StartQueue() {
            if (!s_Queued) {
                s_Queued = true;
                Debug.Log("[PlayModeDelayedSceneProcessor] Queue started");
                ScenesUtility.Editor.SetDelayedSceneProcessorsRunning(true);
                EditorApplication.delayCall += TryFlushQueue;
            }
        }

        static internal bool IsRunningDelayed() {
            return !s_Queued && s_Entries.Count > 0;
        }

        static internal void Queue(IProcessSceneWithReport processor, Scene scene, BuildReport report, object arg) {
            if (s_IgnoreScenes.Contains(scene)) {
                return;
            }

            StartQueue();
            Debug.LogFormat("[PlayModeDelayedSceneProcessor] Added '{0}' with scene '{1}'", processor.GetType().FullName, scene.path);
            s_Entries.PushBack(new Entry() {
                Processor = processor,
                Scene = scene,
                Report = report,
                Arg = arg
            });
        }

        static internal void Queue(Action<Scene, BuildReport, object> callback, Scene scene, BuildReport report, object arg) {
            if (s_IgnoreScenes.Contains(scene)) {
                return;
            }

            StartQueue();
            Debug.LogFormat("[PlayModeDelayedSceneProcessor] Added '{0}' with scene '{1}'", callback.Method.Name, scene.path);
            s_Entries.PushBack(new Entry() {
                Callback = callback,
                Scene = scene,
                Report = report,
                Arg = arg
            });
        }

        static internal void QueueFront(Action<Scene, BuildReport, object> callback, Scene scene, BuildReport report, object arg) {
            if (s_IgnoreScenes.Contains(scene)) {
                return;
            }

            StartQueue();
            Debug.LogFormat("[PlayModeDelayedSceneProcessor] Added to front '{0}' with scene '{1}'", callback.Method.Name, scene.path);
            s_Entries.PushFront(new Entry() {
                Callback = callback,
                Scene = scene,
                Report = report,
                Arg = arg
            });
        }


        static internal void HaltProcessingForFrame() {
            s_Queued = true;
        }

        static private void TryFlushQueue() {
            s_Queued = false;
            Debug.Log("[PlayModeDelayedSceneProcessor] Processing queue...");
            while (!s_Queued && s_Entries.Count > 0) {
                Entry e = s_Entries.PeekFront();
                e.Callback?.Invoke(e.Scene, e.Report, e.Arg);
                e.Processor?.OnProcessScene(e.Scene, e.Report);
                s_Entries.PopFront();
            }

            if (s_Entries.Count == 0 && !s_Queued) {
                s_IgnoreScenes.Clear();
                ScenesUtility.Editor.SetDelayedSceneProcessorsRunning(false);
                Debug.Log("[PlayModeDelayedSceneProcessor] ...Queue finished!");
            } else {
                Debug.Log("[PlayModeDelayedSceneProcessor] ...Queue interrupted");
            }
        }

        static internal void IgnoreScene(Scene scene) {
            if (s_IgnoreScenes.Add(scene)) {
                Debug.LogFormat("[PlayModeDelayedSceneProcessor] Ignoring scene '{0}'", scene.path);
                StartQueue();
            }
        }

        static internal bool IsSceneIgnored(Scene scene) {
            return s_IgnoreScenes.Contains(scene);
        }
    }
}