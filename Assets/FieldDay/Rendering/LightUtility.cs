using BeauUtil;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    /// <summary>
    /// Lighting utility methods
    /// </summary>
    static public class LightUtility {
        #region Scene

        public struct SceneSettings {
            public bool fog;
            public float fogStartDistance;
            public float fogEndDistance;
            public FogMode fogMode;
            public Color fogColor;
            public float fogDensity;
            public AmbientMode ambientMode;
            public float ambientIntensity;
            public Color ambientLight;
            public Material skybox;
            public LightmapData[] lightmaps;
            public LightmapsMode lightmapsMode;
            public LightProbes lightProbes;

            public void Read() {
                fog = RenderSettings.fog;
                fogStartDistance = RenderSettings.fogStartDistance;
                fogEndDistance = RenderSettings.fogEndDistance;
                fogMode = RenderSettings.fogMode;
                fogColor = RenderSettings.fogColor;
                fogDensity = RenderSettings.fogDensity;
                ambientMode = RenderSettings.ambientMode;
                ambientIntensity = RenderSettings.ambientIntensity;
                ambientLight = RenderSettings.ambientLight;
                skybox = RenderSettings.skybox;
                lightmaps = ArrayUtils.CreateFrom(LightmapSettings.lightmaps);
                lightmapsMode = LightmapSettings.lightmapsMode;
                lightProbes = LightmapSettings.lightProbes;
            }

            public void Write(LightingImportFlags mask) {
                RenderSettings.fog = fog;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.ambientLight = ambientLight;
                //RenderSettings.skybox = skybox;
                LightmapSettings.lightmaps = lightmaps;
                LightmapSettings.lightmapsMode = lightmapsMode;
                //LightmapSettings.lightProbes = lightProbes;
            }
        }

        static public void CopySettingsToActive(Scene src, LightingImportFlags mask) {
            SceneBinding currentActive = SceneManager.GetActiveScene();

            SceneManager.SetActiveScene(src);
            SceneSettings settings = default;
            settings.Read();
            SceneManager.SetActiveScene(currentActive);
            settings.Write(mask);
        }

        static public void CopySettingsToScene(Scene src, Scene dest, LightingImportFlags mask) {
            Scene currentActive = SceneManager.GetActiveScene();
            
            SceneManager.SetActiveScene(src);
            SceneSettings settings = default;
            settings.Read();
            SceneManager.SetActiveScene(dest);
            settings.Write(mask);

            if (dest != currentActive) {
                SceneManager.SetActiveScene(currentActive);
            }
        }

#if UNITY_EDITOR

        static private SceneSettings? s_CopyBuffer;

        [MenuItem("Field Day/Lighting/Copy Current Settings")]
        static private void CopyCurrentSettings() {
            SceneSettings settings = default(SceneSettings);
            settings.Read();
            s_CopyBuffer = settings;
            Debug.LogFormat("[LightUtility] Copied lighting settings from current scene '{0}'", EditorSceneManager.GetActiveScene().path);
        }

        [MenuItem("Field Day/Lighting/Paste Current Settings", false)]
        static private void PasteCurrentSettings() {
            s_CopyBuffer.Value.Write(LightingImportFlags.All);
            Debug.LogFormat("[LightUtility] Pasted lighting settings into current scene '{0}'", EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Field Day/Lighting/Paste Current Settings", true)]
        static private bool PasteCurrentSettings_Validate() {
            return s_CopyBuffer.HasValue;
        }

#endif // UNITY_EDITOR

        #endregion // Scene
    }

    [Flags]
    public enum LightingImportFlags : uint
    {
        Fog = 0x01,
        Ambient = 0x02,
        LightMaps = 0x04,
        LightProbes = 0x08,
        Skybox = 0x10,

        All = Fog | Ambient | LightMaps | LightProbes | Skybox
    }
}