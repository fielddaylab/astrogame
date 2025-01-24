using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Astro {
    public sealed class RenderAtlasOutput : BatchedComponent, IRegistrationCallbacks {
        [Header("Texture Destination")]
        [Required] public RenderAtlasUpdateState Group;
        public SerializedHash32 RegionId;

        [Header("Local")]
        [Required] public Camera Contents;
        [Required] public Renderer TargetRenderer;
        [Required] public MeshFilter TargetMeshFilter;

        [NonSerialized] public int RenderHandle = -1;
        [NonSerialized] public RenderAtlas.TextureRegion RenderRegion;
        [NonSerialized] public Mesh OriginalMesh;
        [NonSerialized] public Mesh RemappedMesh;

        public void MarkDirty() {
            RenderAtlasUtility.MarkRegionDirty(Group, RenderHandle);
        }

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            UnityHelper.SafeDestroy(ref RemappedMesh);
            TargetMeshFilter.sharedMesh = OriginalMesh;
        }

        void IRegistrationCallbacks.OnRegister() {
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