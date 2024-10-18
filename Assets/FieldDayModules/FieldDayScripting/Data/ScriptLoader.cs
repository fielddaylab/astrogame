using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Scenes;
using Leaf;
using UnityEngine;

namespace FieldDay.Scripting {
    [PreloadOrder(1000)]
    public class ScriptLoader : MonoBehaviour, IScenePreload, ISceneUnloadHandler {
        public LeafAsset[] Scripts;

        [NonSerialized] private UniqueId16[] m_LoadHandles;

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadHandles != null) {
                for(int i = 0; i < m_LoadHandles.Length; i++) {
                    ScriptDBUtility.Unload(m_LoadHandles[i]);
                }
            }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            m_LoadHandles = new UniqueId16[Scripts.Length];
            for(int i = 0; i < Scripts.Length; i++) {
                m_LoadHandles[i] = ScriptDBUtility.Load(Scripts[i]);
            }
            return null;
        }
    }
}