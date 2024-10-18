using System;
using System.Collections.Generic;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using FieldDay.Data;
using ScriptableBake;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    /// <summary>
    /// Adjust splash screen, exception, and logging settings during a build.
    /// </summary>
    public class AdjustSettingsBuildProcessor : IPreprocessBuildWithReport {
        public int callbackOrder { get { return -100; } }

        public void OnPreprocessBuild(BuildReport report) {
            string branch = BuildUtils.GetSourceControlBranchName();
            bool isBatchMode = InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs;
            if (isBatchMode) {
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
                PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }

            BuildConfig config = BuildConfigurations.GetDesiredConfig(branch);
            if (config != null) {
                BuildConfigurations.ApplyBuildConfig(branch, AssetDatabase.GetAssetPath(config), config.DevelopmentBuild, config.CustomDefines, config.StrippingLevel, true);
            }

            if (EditorUserBuildSettings.development) {
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Embedded;
            } else {
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            }

            Debug.LogFormat("[AdjustSettingsBuildProcessor] Building branch '{0}', development mode {1}", branch, EditorUserBuildSettings.development);
        }
    }

    /// <summary>
    /// Bakes all assets with a custom baking procedure.
    /// </summary>
    public class BakeAssetsBuildPreprocessor : IPreprocessBuildWithReport {
        public int callbackOrder { get { return 10; } }

        public void OnPreprocessBuild(BuildReport report) {
            bool batch = InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs;

            try {
                using(Profiling.Time("bake assets")) {
                    using (Log.DisableMsgStackTrace()) {
                        Baking.BakeAssets(batch ? 0 : BakeFlags.Verbose);
                    }
                }
                AssetDatabase.SaveAssets();
            } catch(Exception e) {
                throw new BuildFailedException(e);
            }
        }
    }

    /// <summary>
    /// Strips all assets with editor-only data.
    /// </summary>
    public class StripEditorDataBuildPreprocesor : IPreprocessBuildWithReport {
        public int callbackOrder { get { return 10000; } }

        public void OnPreprocessBuild(BuildReport report) {
            bool batch = InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs;

            List<IEditorOnlyData> toStrip = new List<IEditorOnlyData>(256);
            ScriptableObject[] assets = AssetDBUtils.FindAssets<ScriptableObject>();
            foreach (var asset in assets) {
                IEditorOnlyData data;
                if ((data = asset as IEditorOnlyData) != null) {
                    toStrip.Add(data);
                }
            }

            if (toStrip.Count > 0) {
                Debug.LogFormat("[StripEditorDataBuildPreprocesor] Found {0} assets with editor-only data...", toStrip.Count);
                try {
                    using (Profiling.Time("stripping editor-only data from assets")) {
                        ScriptableObject src;
                        foreach (var obj in toStrip) {
                            src = (ScriptableObject) obj;
                            obj.ClearEditorData(EditorUserBuildSettings.development);
                            EditorUtility.SetDirty(src);
                        }
                    }
                } catch (Exception e) {
                    throw new BuildFailedException(e);
                }
            }
        }
    }
}