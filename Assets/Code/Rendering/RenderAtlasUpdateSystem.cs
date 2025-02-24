using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Astro {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 10000)]
    public sealed class RenderAtlasUpdateSystem : ComponentSystemBehaviour<RenderAtlasUpdateState> {
        public override void ProcessWorkForComponent(RenderAtlasUpdateState component, float deltaTime) {
            if (!component.Atlas.Texture.IsCreated()) {
                component.Atlas.Texture.Create();
                component.DirtyRegions = new BitSet64((1UL << component.RegionCount) - 1);
            }

            if (DebugFlags.IsFlagSet(DebugSettings.AlwaysRender)) {
                component.DirtyRegions = new BitSet64((1UL << component.RegionCount) - 1);
            }

            if (component.DirtyRegions.IsEmpty) {
                return;
            }

            foreach(var bit in component.DirtyRegions) {
                if (bit >= component.RegionCount) {
                    continue;
                }

                var region = component.Regions[bit];
                region.Camera.enabled = true;
                region.Camera.Render();
                region.Camera.enabled = false;

                component.Atlas.UpdateRegion(region.Region, region.Region.X, region.Region.Y);
            }

            Log.Msg("[RenderAtlasUpdateSystem] Re-rendered {0} regions for '{1}'", component.DirtyRegions.Count, component.Atlas.name);

            component.DirtyRegions.Clear();
        }

        [Flags]
        private enum DebugSettings {
            AlwaysRender = 0x01
        }

        [DebugMenuFactory]
        static private DMInfo DebugMenu() {
            DMInfo info = new DMInfo("RenderAtlas");
            DebugFlags.Menu.AddFlagToggle(info, "Always Refresh", DebugSettings.AlwaysRender);
            return info;
        }
    }
}