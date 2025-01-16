using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Astro {
    [DefaultExecutionOrder(-1000)]
    public sealed class RenderAtlasUpdateState : BatchedComponent, IRegistrationCallbacks {
        public RenderAtlas Atlas;
        
        [NonSerialized] public RenderAtlasRegionSetup[] Regions;
        [NonSerialized] public int RegionCount;
        [NonSerialized] public BitSet64 DirtyRegions;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Assets?.RemoveNamed(Atlas.AssetId, Atlas);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Assets.AddNamed(Atlas.AssetId, Atlas);

            Regions = new RenderAtlasRegionSetup[Atlas.RegionCount];
        }

        #endregion // IRegistrationCallbacks
    }

    public struct RenderAtlasRegionSetup {
        public Camera Camera;
        public RenderAtlas.PackedRegion Region;
    }

    static public class RenderAtlasUtility {
        static public int RegisterRegion(RenderAtlasUpdateState state, Camera camera, StringHash32 regionId, out RenderAtlas.TextureRegion mapping) {
            if (!state.Atlas.TryGetRegion(regionId, out var packedRegion)) {
                Log.Error("[RenderAtlasUtility] No region found with id '{0}'", regionId);
                mapping = default;
                return -1;
            }

            // TODO: search for reuses of the same camera

            Rect viewportRegion = Rect.MinMaxRect(packedRegion.UVs.x, packedRegion.UVs.y, packedRegion.UVs.z, packedRegion.UVs.w);

            camera.enabled = false;
            camera.targetTexture = state.Atlas.Texture;
            camera.rect = viewportRegion;

            int newIdx = state.RegionCount++;
            state.DirtyRegions.Set(newIdx);
            state.Regions[newIdx] = new RenderAtlasRegionSetup() {
                Camera = camera,
                Region = packedRegion
            };

            mapping = new RenderAtlas.TextureRegion() {
                Texture = state.Atlas.Texture,
                UVRect = viewportRegion
            };
            return newIdx;
        }

        static public void MarkRegionDirty(RenderAtlasUpdateState state, int idx) {
            if (idx >= 0) {
                state.DirtyRegions.Set(idx);
            }
        }
    }
}