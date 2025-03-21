using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Astro {
    public sealed class RenderAtlasOutput : BatchedComponent, IRegistrationCallbacks, IBaked {
        [Header("Texture Destination")]
        [Required] public RenderAtlas Atlas;
        public SerializedHash32 RegionId;

        [Header("Local")]
        [Required] public Camera Contents;
        [Required] public Renderer TargetRenderer;
        [Required] public MeshFilter TargetMeshFilter;

        [NonSerialized] public RenderAtlasUpdateState Group;
        [NonSerialized] public int RenderHandle = -1;
        [NonSerialized] public RenderAtlas.TextureRegion RenderRegion;
        [NonSerialized] public Mesh OriginalMesh;
        [NonSerialized] public Mesh RemappedMesh;

        public void MarkDirty() {
            RenderAtlasUtility.MarkRegionDirty(Group, RenderHandle);
        }

#if UNITY_EDITOR

        int IBaked.Order { get { return -100; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if((flags & BakeFlags.IsBuild) != 0) {
                Contents.targetTexture = null;
            } 
            return true;
        }

#endif // UNITY_EDITOR

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            UnityHelper.SafeDestroy(ref RemappedMesh);
            if (TargetMeshFilter) {
                TargetMeshFilter.sharedMesh = OriginalMesh;
            }
            RenderAtlasUpdateState.ReleaseState(ref Group);
        }

        void IRegistrationCallbacks.OnRegister() {
            Group = RenderAtlasUpdateState.RetrieveState(Atlas);
            RenderHandle = RenderAtlasUtility.RegisterRegion(Group, Contents, RegionId, out RenderRegion);

            Rect st = RenderRegion.UVRect;
            OriginalMesh = TargetMeshFilter.sharedMesh;
            RemappedMesh = Instantiate(OriginalMesh);
            MeshUVUtility.RemapUVs(RemappedMesh, 0, st);
            RemappedMesh.UploadMeshData(true);
            TargetMeshFilter.sharedMesh = RemappedMesh;

            Material mat = TargetRenderer.sharedMaterial;
            mat.mainTexture = RenderRegion.Texture;
        }

        #endregion // IRegistrationCallbacks

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Frame.IsActive(this) || RenderHandle < 0) {
                return;
            }

            MarkDirty();
        }
#endif // UNITY_EDITOR
    }
}