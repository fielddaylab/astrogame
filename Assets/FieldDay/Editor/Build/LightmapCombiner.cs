#define _LIGHTMAPCOMBINER_DEBUG

using BeauUtil;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Editor {
    public sealed class LightmapCombiner {
        private struct RendererInfo {
            public Renderer Component;
            public int VirtualLightmapIndex;
            public Vector4 LightmapScaleOffset;
        }

        // TODO: support for realtime lightmaps?
        // TODO: combine smaller maps together into atlas?

        private readonly RingBuffer<RendererInfo> m_Renderers = new RingBuffer<RendererInfo>(512, RingBufferMode.Expand);
        private readonly RingBuffer<LightmapData> m_Lightmaps = new RingBuffer<LightmapData>(32, RingBufferMode.Expand);

        private readonly List<GameObject> m_GatherGameObjectWorkList = new List<GameObject>(64);
        private readonly List<Renderer> m_ChildRendererWorkList = new List<Renderer>(512);

        public void GatherFromScene(Scene scene) {
            Scene currentActive = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);

            var lightmapMode = LightmapSettings.lightmapsMode;
            var lightmaps = LightmapSettings.lightmaps;
            
            SceneManager.SetActiveScene(currentActive);

            m_GatherGameObjectWorkList.Clear();
            m_ChildRendererWorkList.Clear();

            scene.GetRootGameObjects(m_GatherGameObjectWorkList);
            foreach(var go in m_GatherGameObjectWorkList) {
                go.GetComponentsInChildren<Renderer>(true, m_ChildRendererWorkList);
                foreach(var r in m_ChildRendererWorkList) {
                    int lightmapIndex = r.lightmapIndex;
                    if (lightmapIndex < 0 || lightmapIndex >= 0xFFFE) {
                        continue;
                    }

                    Vector4 lightmapScaleOffset = r.lightmapScaleOffset;

                    if (lightmapScaleOffset == Vector4.zero) {
                        continue;
                    }

                    LightmapData sourceData = lightmaps[lightmapIndex];
                    int virtualLightmapIndex = m_Lightmaps.FindIndex((d, t) => d.lightmapColor == t, sourceData.lightmapColor);
                    if (virtualLightmapIndex < 0) {
                        virtualLightmapIndex = m_Lightmaps.Count;
                        m_Lightmaps.PushBack(sourceData);
                    }

                    RendererInfo info;
                    info.Component = r;
                    info.VirtualLightmapIndex = virtualLightmapIndex;
                    info.LightmapScaleOffset = lightmapScaleOffset;

                    m_Renderers.PushBack(info);
                }
                m_ChildRendererWorkList.Clear();
            }

            m_ChildRendererWorkList.Clear();
            m_GatherGameObjectWorkList.Clear();
        }

        public void ApplyChanges() {
            LightmapSettings.lightmaps = m_Lightmaps.ToArray();

            for(int i = 0; i < m_Renderers.Count; i++) {
                RendererInfo render = m_Renderers[i];
                if (!render.Component) {
                    continue;
                }

                render.Component.lightmapIndex = render.VirtualLightmapIndex;
                render.Component.lightmapScaleOffset = render.LightmapScaleOffset;
            }
        }
    }
}