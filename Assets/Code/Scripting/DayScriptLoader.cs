
using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using UnityEngine;

namespace Astro {
    public sealed class DayScriptLoader : MonoBehaviour, IScenePreload, ISceneUnloadHandler {
        [NonSerialized] private UniqueId16[] m_LoadHandles;
        
        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadHandles != null) {
                for (int i = 0; i < m_LoadHandles.Length; i++) {
                    ScriptDBUtility.Unload(m_LoadHandles[i]);
                }
            }
        }
        
        public IEnumerator<WorkSlicer.Result?> Preload() {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();

            m_LoadHandles = new UniqueId16[config.Scripts.Length];
            for (int i = 0; i < config.Scripts.Length; i++) {
                m_LoadHandles[i] = ScriptDBUtility.Load(config.Scripts[i]);
            }

            return null;
        }

    }
}