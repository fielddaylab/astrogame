using System;
using System.Threading;
using BeauUtil.Debugger;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astro.Editor {
    public sealed class MergedSceneLightBaker : ScriptableWizard {
        public struct SceneGroup {
            public Scene Scene;
            public string Path;
            public GameObject[] Roots;
        }

        [MenuItem("Astro/Multi-Scene Lightmap Bake")]
        static public void MergedBake() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            Log.Msg("[MergedSceneLightBaker] Gathering object groups...");

            EditorUtility.DisplayProgressBar("Multi-Scene Lightmap Bake", "Gathering object groups...", 0);
            if (!GatherSceneGroups(out SceneGroup[] groups, out Scene active)) {
                EditorUtility.DisplayDialog("Whoops", "You need an active scene!", "I'll fix it");
                return;
            }

            bool merged = false;
            try {
                EditorUtility.DisplayProgressBar("Multi-Scene Lightmap Bake", "Merging groups into active scene...", 0.1f);
                MergeSceneGroups(groups, active);
                merged = true;
                Thread.Sleep(100);
                Log.Trace("Baking lighting...");
                EditorUtility.DisplayProgressBar("Multi-Scene Lightmap Bake", "Baking lighting...", 0.2f);
                if (!Lightmapping.Bake()) {
                    EditorUtility.DisplayDialog("Baking failed", "Check the console for details", "Will do");
                } else {
                    EditorUtility.DisplayProgressBar("Multi-Scene Lightmap Bake", "Unmerging groups...", 0.9f);
                    UnmergeSceneGroups(groups, true);
                    merged = false;
                    EditorUtility.DisplayProgressBar("Multi-Scene Lightmap Bake", "Copying lightmap data...", 0.9f);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    ExportLightmapData(groups, active);
                }
            } finally {
                EditorUtility.ClearProgressBar();
                if (merged) {
                    UnmergeSceneGroups(groups, false);
                }
            }
        }

        [MenuItem("Astro/Multi-Scene Lightmap Bake", validate = true)]
        static private bool MergedBake_Validate() {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static public bool GatherSceneGroups(out SceneGroup[] groups, out Scene active) {
            active = SceneManager.GetActiveScene();
            if (!active.IsValid()) {
                Log.Error("No active scene!");
                groups = null;
                return false;
            }

            int additionalLoadedCount = SceneManager.loadedSceneCount - 1;
            if (additionalLoadedCount > 0) {
                SceneGroup[] groupArr = new SceneGroup[additionalLoadedCount];
                int written = 0;
                for(int i = 0; i < SceneManager.loadedSceneCount; i++) {
                    Scene nextScene = SceneManager.GetSceneAt(i);
                    if (!nextScene.IsValid() || nextScene == active) {
                        continue;
                    }
                    SceneGroup newGroup;
                    newGroup.Scene = nextScene;
                    newGroup.Path = nextScene.path;
                    newGroup.Roots = nextScene.GetRootGameObjects();
                    groupArr[written++] = newGroup;
                    Log.Trace("Scene '{0}' has {1} roots", newGroup.Path, newGroup.Roots.Length);
                }
                Array.Resize(ref groupArr, written);
                groups = groupArr;
            } else {
                groups = Array.Empty<SceneGroup>();
            }

            return true;
        }
    
        static public void MergeSceneGroups(SceneGroup[] groups, Scene active) {
            Log.Trace("Merging objects to '{0}'", active.path);
            foreach (var group in groups) {
                foreach(var obj in group.Roots) {
                    Log.Trace("Moving '{0}'...", obj.name);
                    SceneManager.MoveGameObjectToScene(obj, active);
                }
            }
        }

        static public void ExportLightmapData(SceneGroup[] groups, Scene active) {
            LightmapData[] lightmaps = (LightmapData[]) LightmapSettings.lightmaps.Clone();
            LightingSettings settings = Lightmapping.lightingSettings;
            LightingDataAsset asset = Lightmapping.lightingDataAsset;

            var setup = EditorSceneManager.GetSceneManagerSetup();
            active = EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);

            EditorSceneManager.SetActiveScene(active);
            
            Log.Trace("Active scene has {0} lightmaps", lightmaps.Length);

            foreach(var group in groups) {
                Scene modifiedScene = EditorSceneManager.OpenScene(group.Path, OpenSceneMode.Single);
                //Scene modifiedScene = group.Scene;

                EditorSceneManager.SetActiveScene(modifiedScene);

                Lightmapping.SetLightingSettingsForScene(modifiedScene, settings);
                Log.Trace("Applying {0} lightmaps to {1}", lightmaps.Length, Lightmapping.lightingDataAsset.name);
                
                Lightmapping.lightingDataAsset = asset;
                LightmapSettings.lightmaps = lightmaps;

                EditorSceneManager.SaveScene(modifiedScene);
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup);
            //EditorSceneManager.SetActiveScene(active);
        }

        static public void UnmergeSceneGroups(SceneGroup[] groups, bool save) {
            Log.Trace("Returning objects to their original scenes");

            foreach(var group in groups) {
                foreach(var obj in group.Roots) {
                    Log.Trace("Moving '{0}' to '{1}'", obj.name, group.Scene.path);
                    SceneManager.MoveGameObjectToScene(obj, group.Scene);
                }
            }

            EditorSceneManager.MarkAllScenesDirty();
            if (save) {
                EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}