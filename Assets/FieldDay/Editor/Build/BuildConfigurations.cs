using System;
using System.IO;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    static public class BuildConfigurations {
        [Flags]
        public enum CodeOptimizationFlags : uint {
            OptimizeForRuntimeSpeed = 0x01,
            OptimizeForBuildSize = 0x02,
            UseLTO = 0x04,
            DisableExceptions = 0x08
        }

        public struct ConfigOptions {
            public bool Development;
            public string Defines;
            public ManagedStrippingLevel CodeStripping;
            public CodeOptimizationFlags CodeOptimization;
        }

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
        static public void ApplyBuildConfig(string branchName, string configName, ConfigOptions options, bool forceLogs = false) {
            bool logging = forceLogs;
            bool isBatch = InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs;
            if (!logging) {
                if ((isBatch)) {
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

            if (!options.Development && (isBatch || BuildPipeline.isBuildingPlayer)) {
                options.Defines = options.Defines ?? string.Empty;
                options.Defines += ",IGNORE_UNITY_EDITOR";
            }

            EditorUserBuildSettings.development = options.Development;
            PlayerSettings.SetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup, options.CodeStripping);

            ApplyIl2CppBuildOptions(options, isBatch);
            ApplyWebGLBuildOptions(options, isBatch);
            ApplyAndroidBuildOptions(options, isBatch);
            
            BuildUtils.WriteDefines(options.Defines);

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

        static private void ApplyIl2CppBuildOptions(ConfigOptions options, bool isBatch) {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            Il2CppCompilerConfiguration compilerConfig;
            if (options.Development) {
                compilerConfig = Il2CppCompilerConfiguration.Debug;
            } else if ((options.CodeOptimization & CodeOptimizationFlags.UseLTO) != 0) {
                compilerConfig = Il2CppCompilerConfiguration.Master;
            } else {
                compilerConfig = Il2CppCompilerConfiguration.Release;
            }
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfig);

            Il2CppCodeGeneration codeGen;
            if ((options.Development && !isBatch)) {
                codeGen = Il2CppCodeGeneration.OptimizeSize;
            } else if ((options.CodeOptimization & CodeOptimizationFlags.OptimizeForRuntimeSpeed) != 0) {
                codeGen = Il2CppCodeGeneration.OptimizeSpeed;
            } else {
                codeGen = options.Development ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
            }
            PlayerSettings.SetIl2CppCodeGeneration(namedBuildTarget, codeGen);
        }

        static private void ApplyWebGLBuildOptions(ConfigOptions options, bool isBatch) {
            WebGLExceptionSupport exceptionSupport;
            if (options.Development) {
                exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            } else if ((options.CodeOptimization & CodeOptimizationFlags.DisableExceptions) != 0) {
                exceptionSupport = WebGLExceptionSupport.None;
            } else {
                exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            }
            PlayerSettings.WebGL.exceptionSupport = exceptionSupport;

            UnityEditor.WebGL.WasmCodeOptimization wasmOpt;
            if (options.Development && !isBatch) {
                wasmOpt = UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
            } else {
                bool lto = (options.CodeOptimization & CodeOptimizationFlags.UseLTO) != 0;
                if ((options.CodeOptimization & CodeOptimizationFlags.OptimizeForBuildSize) != 0) {
                    wasmOpt = lto ? UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO : UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
                } else if ((options.CodeOptimization & CodeOptimizationFlags.OptimizeForRuntimeSpeed) != 0) {
                    wasmOpt = lto ? UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeedLTO : UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeed;
                } else {
                    wasmOpt = options.Development ? UnityEditor.WebGL.WasmCodeOptimization.BuildTimes : UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeedLTO;
                }
            }
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = wasmOpt;

            WebGLDebugSymbolMode debugSymbolMode;
            if (options.Development) {
                debugSymbolMode = WebGLDebugSymbolMode.External;
            } else {
                debugSymbolMode = WebGLDebugSymbolMode.Off;
            }
            PlayerSettings.WebGL.debugSymbolMode = debugSymbolMode;
        }

        static private void ApplyAndroidBuildOptions(ConfigOptions options, bool isBatch) {
            EditorUserBuildSettings.androidBuildType = options.Development ? AndroidBuildType.Debug : AndroidBuildType.Release;
            PlayerSettings.Android.minifyDebug = options.Development;
            PlayerSettings.Android.minifyRelease = !options.Development;
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
            ConfigOptions options;
            BuildConfig config = GetDesiredConfig(branchName);
            if (config != null) {
                options.Development = config.DevelopmentBuild;
                options.Defines = config.CustomDefines;
                options.CodeStripping = config.StrippingLevel;
                options.CodeOptimization = config.OptimizationFlags;
                ApplyBuildConfig(branchName, AssetDatabase.GetAssetPath(config), options, forceLogging);
            } else {
                Debug.LogWarningFormat("[BuildConfigurations] Using hard-coded fallback configurations for branch '{0}'", branchName);
                if (branchName == "production") {
                    options.Development = false;
                    options.Defines = "PRODUCTION";
                    options.CodeStripping = ManagedStrippingLevel.Minimal;
                    options.CodeOptimization = CodeOptimizationFlags.OptimizeForRuntimeSpeed | CodeOptimizationFlags.UseLTO;
                    ApplyBuildConfig(branchName, "[fallback-production]", options, forceLogging);
                } else if (branchName == "preview") {
                    options.Development = false;
                    options.Defines = "PREVIEW,ENABLE_LOGGING_ERRORS_BEAUUTIL,ENABLE_LOGGING_WARNINGS_BEAUUTIL,PRESERVE_DEBUG_SYMBOLS";
                    options.CodeStripping = ManagedStrippingLevel.Minimal;
                    options.CodeOptimization = CodeOptimizationFlags.OptimizeForRuntimeSpeed;
                    ApplyBuildConfig(branchName, "[fallback-preview]", options, forceLogging);
                } else {
                    options.Development = true;
                    options.Defines = "DEVELOPMENT,PRESERVE_DEBUG_SYMBOLS";
                    options.CodeStripping = ManagedStrippingLevel.Minimal;
                    options.CodeOptimization = 0;
                    ApplyBuildConfig(branchName, "[fallback-dev]", options, forceLogging);
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