using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using ScriptableBake;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

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

        [Header("Texture Settings")]
        [EditModeOnly] public uint TextureSize = 4096;
        [SerializeField] private int m_Antialiasing = 0;
        [SerializeField] private FilterMode m_FilterMode = FilterMode.Bilinear;

        [Header("Regions")]
        [SerializeField] private RegionData[] m_TargetRegions;
        [SerializeField, HideInInspector] private PackedRegion[] m_PackedRegions;
        [SerializeField, HideInInspector] private StringHash32[] m_PackedRegionNames;

        #endregion // Inspector

        [NonSerialized] private RenderTexture m_Texture;
        public CastableEvent<RenderAtlas> OnTextureCreated = new CastableEvent<RenderAtlas>();

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
#if UNITY_EDITOR
            PackData();
#endif // UNITY_EDITOR

            m_Texture = new RenderTexture((int) TextureSize, (int) TextureSize, 0, RenderTextureFormat.ARGB32, 0);
            if (m_Antialiasing > 0) {
                m_Texture.antiAliasing = m_Antialiasing;
            }
            m_Texture.depth = 0;
            m_Texture.filterMode = m_FilterMode;
            m_Texture.name = name + "_RT";

            OnTextureCreated.Invoke(this);
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
            SortedPackRecord* records = stackalloc SortedPackRecord[m_TargetRegions.Length];
            
            for(int i = 0; i < m_TargetRegions.Length; i++) {
                records[i] = new SortedPackRecord() {
                    RegionIdx = i,
                    Width = m_TargetRegions[i].Width,
                    Height = m_TargetRegions[i].Height
                };
            }

            m_PackedRegions = new PackedRegion[m_TargetRegions.Length];
            m_PackedRegionNames = new StringHash32[m_TargetRegions.Length];

            Unsafe.Quicksort(records, m_TargetRegions.Length, (a, b) => {
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

            Log.Msg("[RenderAtlas] Packed {0} regions into {1}x{1} texture for '{2}'", m_TargetRegions.Length, TextureSize, name);
        }

        [CustomEditor(typeof(RenderAtlas))]
        private sealed class Inspector : Editor {
            public override bool HasPreviewGUI() {
                return true;
            }

            static private GUIStyle BoxStyle;
            static private GUIStyle OutlinedBoxStyle;

            static private readonly Color[] BoxColors = new Color[] {
                ColorBank.Red,
                ColorBank.PowderBlue,
                ColorBank.Yellow,
                ColorBank.Green,
                ColorBank.Azure,
                ColorBank.Gold
            };

            public override void OnPreviewGUI(Rect r, GUIStyle background) {
                if (BoxStyle == null) {
                    BoxStyle = new GUIStyle(GUI.skin.box);
                    BoxStyle.normal.background = Texture2D.whiteTexture;
                    BoxStyle.padding = new RectOffset();
                    BoxStyle.border = new RectOffset();
                }

                if (OutlinedBoxStyle == null) {
                    OutlinedBoxStyle = new GUIStyle(BoxStyle);
                    OutlinedBoxStyle.normal.background = null;
                    OutlinedBoxStyle.border = new RectOffset(1, 1, 1, 1);
                }
                
                RenderAtlas atlas = (RenderAtlas) target;

                Rect withBorders = r;
                withBorders.x += 4;
                withBorders.width -= 8;
                withBorders.y += 4;
                withBorders.height -= 8;

                Vector2 center = withBorders.center;
                float squareSize = Math.Min(withBorders.width, withBorders.height);

                Rect centeredRect = new Rect(center.x - squareSize / 2, center.y - squareSize / 2, squareSize, squareSize);

                GUIStyle boxStyle;

                if (!atlas.m_Texture) {
                    GUI.Box(centeredRect, string.Empty);
                    boxStyle = BoxStyle;
                } else {
                    boxStyle = OutlinedBoxStyle;
                    GUI.DrawTexture(centeredRect, atlas.m_Texture);
                }

                int boxIdx = 0;
                GUI.color = Color.white;
                Color prevBGColor = GUI.backgroundColor;
                foreach(var packed in atlas.m_PackedRegions) {
                    GUI.backgroundColor = BoxColors[boxIdx % BoxColors.Length];
                    Rect newRect = Rect.MinMaxRect(
                        centeredRect.x + centeredRect.width * packed.UVs.x,
                        centeredRect.y + centeredRect.height * (1 - packed.UVs.y),
                        centeredRect.x + centeredRect.width * packed.UVs.z,
                        centeredRect.y + centeredRect.height * (1 - packed.UVs.w)
                        );
                    GUI.Box(newRect, atlas.m_PackedRegionNames[boxIdx++].ToDebugString(), boxStyle);
                }
                GUI.backgroundColor = prevBGColor;
                //GUI.DrawTexture(centeredRect, Texture2D.blackTexture);
            }
        }

        #endregion // Packing

#endif // UNITY_EDITOR
    }
}