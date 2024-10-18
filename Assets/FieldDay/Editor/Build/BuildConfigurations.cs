using System;
using System.IO;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    static public class BuildConfigurations {
        /// <summary>
        /// Returns the build config that matches the given branch name.
        /// </summary>
        static public BuildConfig GetDesiredConfig(string branchName) {
            if (string.IsNullOrEmpty(branchName)) {
                Debug.LogWarningFormat("[BuildConfigurations] No branch name?");
                return null;
            }

            BuildConfig[] configs = AssetDBUtils.FindAssets<BuildConfig>();
            if (configs.Length == 0) {
                Debug.LogWarningFormat("[BuildConfigurations] No configs located. Retrying...");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                configs = AssetDBUtils.FindAssets<BuildConfig>();
            }

            if (configs.Length == 0) {
                Debug.LogWarningFormat("[BuildConfigurations] No configs found!");
                return null;
            }

            Array.Sort(configs, (a, b) => a.Order - b.Order);
            //Debug.LogFormat("Found {0} build configurations when lookup under branch '{1}'", configs.Length, branchName);

            for (int buildIdx = 0; buildIdx < configs.Length; buildIdx++) {
                BuildConfig config = configs[buildIdx];
                if (WildcardMatch.Match(branchName, config.BranchNamePatterns, '*', true)) {
                    return config;
                }
            }

            Debug.LogWarningFormat("[BuildConfigurations] No configs found matching branch '{0}' out of {1} configs!", branchName, configs.Length);
            return null;
        }

        static private readonly string LibraryBuildConfigFile = "Library/LastAppliedBuildConfig.txt";

        /// <summary>
        /// Applies the given build configuration settings.
        /// </summary>
        static public void ApplyBuildConfig(string branchName, string configName, bool development, string defines, ManagedStrippingLevel codeStripping, bool forceLogs = false) {
            bool logging = forceLogs;
            if (!logging) {
                if ((InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)) {
                    logging = true;
                } else if (File.Exists(LibraryBuildConfigFile)) {
                    string lastApplied = File.ReadAllText(LibraryBuildConfigFile);
                    //Debug.LogFormat("last config is '{0}' vs now '{1}'", lastApplied, configName);
                    logging = lastApplied != configName; 
                }
            }
             
            if (logging) {
                Debug.LogFormat("[BuildConfigurations] Source control branch is '{0}', applying build configuration '{1}'", branchName, configName);
            }

            PlayerSettings.SetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup, codeStripping);
            EditorUserBuildSettings.development = development;
            EditorUserBuildSettings.androidBuildType = development ? AndroidBuildType.Debug : AndroidBuildType.Release;
            PlayerSettings.Android.minifyDebug = development;
            PlayerSettings.Android.minifyRelease = !development;
            BuildUtils.WriteDefines(defines);

            try {
                File.WriteAllText(LibraryBuildConfigFile, configName);
                //Debug.LogFormat("Wrote config '{0}' to file {1} (attributes {2})", configName, Path.GetFullPath(LibraryBuildConfigFile), File.GetAttributes(LibraryBuildConfigFile));
            } catch(Exception e) {
                Debug.LogException(e);
            }

            if (logging && !InternalEditorUtility.inBatchMode) {
                EditorApplication.delayCall += () => BuildUtils.ForceRecompile();
            }
        }

        /// <summary>
        /// Enables BuildInfoGenerator.
        /// </summary>
        [InitializeOnLoadMethod]
        static private void EnableBuildInfo() {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;
        }

        /// <summary>
        /// Retrieves the best configuration for the current branch and applies it.
        /// </summary>
        static public void RetrieveAndApplyConfig(bool forceLogging = false) {
            string branchName = BuildUtils.GetSourceControlBranchName();
            BuildConfig config = GetDesiredConfig(branchName);
            if (config != null) {
                ApplyBuildConfig(branchName, AssetDatabase.GetAssetPath(config), config.DevelopmentBuild, config.CustomDefines, config.StrippingLevel, forceLogging);
            } else {
                Debug.LogWarningFormat("[BuildConfigurations] Using hard-coded fallback configurations for branch '{0}'", branchName);
                if (branchName == "production") {
                    ApplyBuildConfig(branchName, "[fallback-production]", false, "PRODUCTION", ManagedStrippingLevel.Minimal, forceLogging);
                } else if (branchName == "preview") {
                    ApplyBuildConfig(branchName, "[fallback-preview]", false, "PREVIEW,ENABLE_LOGGING_ERRORS_BEAUUTIL,ENABLE_LOGGING_WARNINGS_BEAUUTIL,PRESERVE_DEBUG_SYMBOLS", ManagedStrippingLevel.Minimal, forceLogging);
                } else {
                    ApplyBuildConfig(branchName, "[fallback-dev]", true, "DEVELOPMENT,PRESERVE_DEBUG_SYMBOLS", ManagedStrippingLevel.Minimal, forceLogging);
                }
            }
        }

        [InitializeOnLoadMethod]
        static private void Init_RefreshConfig() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) {
                return;
            }

            RetrieveAndApplyConfig(false); 
        }

        [MenuItem("Assets/Refresh Build Configuration", priority = 2000)]
        static private void Menu_RefreshConfig() {
            RetrieveAndApplyConfig(true);
        }
    }
}