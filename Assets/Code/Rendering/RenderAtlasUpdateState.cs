using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Astro {
    public sealed class RenderAtlasUpdateState : IComponentData, IRefCounted, IRegistrationCallbacks {
        public RenderAtlas Atlas;
        public RenderAtlasRegionSetup[] Regions;
        public int RegionCount;
        public BitSet64 DirtyRegions;

        public RenderAtlasUpdateState(RenderAtlas atlas) {
            Atlas = atlas;
            Regions = new RenderAtlasRegionSetup[Atlas.RegionCount];
            RegionCount = 0;
            DirtyRegions.Clear();
        }

        private void OnRenderTextureCreated() {
            for(int i = 0; i < RegionCount; i++) {
                Regions[i].Camera.targetTexture = Atlas.Texture;
            }
        }

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Assets?.RemoveNamed(Atlas.AssetId, Atlas);
            s_Cache.Remove(Atlas.AssetId);
            Atlas.OnTextureCreated.Deregister(OnRenderTextureCreated);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Assets.AddNamed(Atlas.AssetId, Atlas);
            s_Cache.Add(Atlas.AssetId, this);
            Atlas.OnTextureCreated.Register(OnRenderTextureCreated);
        }

        #endregion // IRegistrationCallbacks

        #region IRefCounted

        int IRefCounted.ReferenceCount { get; set; }

        void IRefCounted.OnReferenced() {
            Game.Components.Register(this);
        }

        void IRefCounted.OnReleased() {
            Game.Components.Deregister(this);
        }

        #endregion // IRefCounted

        #region Cache

        static private Dictionary<StringHash32, RenderAtlasUpdateState> s_Cache = new Dictionary<StringHash32, RenderAtlasUpdateState>(3);

        static public RenderAtlasUpdateState RetrieveState(RenderAtlas atlas) {
            if (!s_Cache.TryGetValue(atlas.AssetId, out var state)) {
                state = new RenderAtlasUpdateState(atlas);
            }
            state.AcquireRef();
            return state;
        }

        static public void ReleaseState(ref RenderAtlasUpdateState updateState) {
            if (updateState != null) {
                updateState.ReleaseRef();
                updateState = null;
            }
        }

        #endregion // Cache
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