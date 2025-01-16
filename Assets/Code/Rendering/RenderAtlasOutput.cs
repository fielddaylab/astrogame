using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
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

        [NonSerialized] public int RenderHandle = -1;
        [NonSerialized] public RenderAtlas.TextureRegion RenderRegion;

        public void MarkDirty() {
            RenderAtlasUtility.MarkRegionDirty(Group, RenderHandle);
        }

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            // TODO: deregister
        }

        void IRegistrationCallbacks.OnRegister() {
            RenderHandle = RenderAtlasUtility.RegisterRegion(Group, Contents, RegionId, out RenderRegion);

            Vector4 st;
            st.z = RenderRegion.UVRect.x;
            st.w = RenderRegion.UVRect.y;
            st.x = RenderRegion.UVRect.width;
            st.y = RenderRegion.UVRect.height;

            MaterialPropertyBlock b = new MaterialPropertyBlock();
            b.SetTexture("_MainTex", RenderRegion.Texture);
            b.SetVector("_MainTex_ST", st);
            TargetRenderer.SetPropertyBlock(b);
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