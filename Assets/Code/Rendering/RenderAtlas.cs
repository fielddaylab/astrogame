using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using ScriptableBake;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Render Atlas")]
    public sealed class RenderAtlas : NamedAsset, IBaked, IRegistrationCallbacks {
        #region Types

        [Serializable]
        private struct RegionData {
            public SerializedHash32 Id;
            public ushort Width;
            public ushort Height;
        }

        [Serializable]
        public struct PackedRegion {
            public ushort X;
            public ushort Y;
            public ushort Width;
            public ushort Height;
            public Vector4 UVs;
        }

        public struct TextureRegion {
            public RenderTexture Texture;
            public Rect UVRect;
        }

        #endregion // Types

        #region Inspector

        [EditModeOnly] public uint TextureSize = 4096;
        [SerializeField] private RegionData[] m_TargetRegions;
        [SerializeField] private PackedRegion[] m_PackedRegions;
        [SerializeField] private StringHash32[] m_PackedRegionNames;

        #endregion // Inspector

        [NonSerialized] private RenderTexture m_Texture;

        public int RegionCount {
            get { return m_PackedRegions.Length; }
        }

        public RenderTexture Texture {
            get { return m_Texture; }
        }

        #region Regions

        public bool TryGetRegion(StringHash32 id, out PackedRegion region) {
            int idx = Array.IndexOf(m_PackedRegionNames, id);
            if (idx >= 0) {
                region = m_PackedRegions[idx];
                return true;
            }

            region = default;
            return false;
        }

        public void UpdateRegion(PackedRegion region, int rtOffsetX, int rtOffsetY) {
            //using (Profiling.Time("updating atlas texture", ProfileTimeUnits.Microseconds)) {
            //    //Graphics.CopyTexture(m_TempTexture, 0, 0, rtOffsetX, rtOffsetY, region.Width, region.Height, m_Texture, 0, 0, region.X, region.Y);

            //    RenderTexture prevRT = RenderTexture.active;
            //    RenderTexture.active = m_TempTexture;
            //    m_Texture.ReadPixels(new Rect(rtOffsetX, rtOffsetY, region.Width, region.Height), region.X, region.Y);
            //    m_Texture.Apply();
            //    RenderTexture.active = prevRT;
            //}
        }

        #endregion // Regions

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            m_Texture = new RenderTexture((int) TextureSize, (int) TextureSize, 0, RenderTextureFormat.ARGB32, 0);
            m_Texture.antiAliasing = 2;
            m_Texture.depth = 0;
            m_Texture.filterMode = FilterMode.Bilinear;
            m_Texture.name = name + "_RT";
        }

        void IRegistrationCallbacks.OnDeregister() {
            UnityHelper.SafeDestroy(ref m_Texture);
        }

        #endregion // IRegistrationCallbacks

#if UNITY_EDITOR

        int IBaked.Order { get { return 100000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            PackData();
            return true;
        }

        #region Packing

        private struct SortedPackRecord {
            public int RegionIdx;
            public int Width;
            public int Height;
        }

        public unsafe void PackData() {
            RingBuffer<SortedPackRecord> records = new RingBuffer<SortedPackRecord>(m_TargetRegions.Length);

            for(int i = 0; i < m_TargetRegions.Length; i++) {
                records.PushBack(new SortedPackRecord() {
                    RegionIdx = i,
                    Width = m_TargetRegions[i].Width,
                    Height = m_TargetRegions[i].Height
                });
            }

            m_PackedRegions = new PackedRegion[m_TargetRegions.Length];
            m_PackedRegionNames = new StringHash32[m_TargetRegions.Length]; 

            records.Quicksort((a, b) => {
                if (b.Height == a.Height) {
                    return b.Width - a.Width;
                }
                return b.Height - a.Height;
            });

            int x = 0, y = 0, rowHeight = 0;
            for(int i = 0; i < m_TargetRegions.Length; i++) {
                RegionData region = m_TargetRegions[records[i].RegionIdx];

                if (x + region.Width > TextureSize) {
                    // next row
                    x = 0;
                    y = Unsafe.AlignUp4(y + rowHeight + 2);
                    rowHeight = 0;
                }

                if (y + region.Height > TextureSize) {
                    throw new InvalidOperationException("Regions cannot fit into box");
                }

                PackedRegion packed;
                packed.X = (ushort) x;
                packed.Y = (ushort) y;
                packed.Width = region.Width;
                packed.Height = region.Height;

                packed.UVs.x = ((float) x / TextureSize);
                packed.UVs.y = ((float) y / TextureSize);
                packed.UVs.z = ((float) (x + region.Width) / TextureSize);
                packed.UVs.w = ((float) (y + region.Height) / TextureSize);

                x = Unsafe.AlignUp4(x + region.Width + 2);
                if (region.Height > rowHeight) {
                    rowHeight = region.Height;
                }

                m_PackedRegions[i] = packed;
                m_PackedRegionNames[i] = region.Id;
            }
        }

        #endregion // Packing

#endif // UNITY_EDITOR
    }
}