using System;
using BeauUtil;
using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI {
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MirrorImage : MaskableGraphic {
        [SerializeField] private Sprite m_Sprite;
        [SerializeField] private bool m_MirrorX = true;
        [SerializeField] private bool m_MirrorY = true;

        private MirrorImage() {
            useLegacyMeshGeneration = false;
        }

        public override Texture mainTexture {
            get { return m_Sprite ? m_Sprite.texture : Texture2D.whiteTexture; }
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            if (!m_Sprite) {
                base.OnPopulateMesh(vh);
                return;
            }

            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            Vector4 draw = new Vector4(rect.x, rect.y, rect.xMax, rect.yMax);
            Vector2 drawCenter = rect.center;

            bool x = m_MirrorX;
            bool y = m_MirrorY;

            Color32 color32 = color;
            Vector4 uv = DataUtility.GetOuterUV(m_Sprite);

            if (x) {
                if (y) {
                    vh.AddVert(drawCenter, color32, new Vector2(uv.x, uv.y));
                    vh.AddVert(new Vector3(drawCenter.x, draw.w), color32, new Vector2(uv.x, uv.w));
                    vh.AddVert(new Vector3(draw.z, draw.w), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(draw.z, drawCenter.y), color32, new Vector2(uv.z, uv.y));
                    vh.AddVert(new Vector3(draw.z, draw.y), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(drawCenter.x, draw.y), color32, new Vector2(uv.x, uv.w));
                    vh.AddVert(new Vector3(draw.x, draw.y), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(draw.x, drawCenter.y), color32, new Vector2(uv.z, uv.y));
                    vh.AddVert(new Vector3(draw.x, draw.w), color32, new Vector2(uv.z, uv.w));

                    vh.AddTriangle(1, 2, 3);
                    vh.AddTriangle(1, 3, 0);
                    vh.AddTriangle(0, 3, 5);
                    vh.AddTriangle(3, 4, 5);
                    vh.AddTriangle(0, 5, 7);
                    vh.AddTriangle(5, 6, 7);
                    vh.AddTriangle(0, 7, 1);
                    vh.AddTriangle(7, 8, 1);
                } else {
                    vh.AddVert(new Vector3(drawCenter.x, draw.y), color32, new Vector2(uv.x, uv.y));
                    vh.AddVert(new Vector3(drawCenter.x, draw.w), color32, new Vector2(uv.x, uv.w));
                    vh.AddVert(new Vector3(draw.z, draw.w), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(draw.z, draw.y), color32, new Vector2(uv.z, uv.y));
                    vh.AddVert(new Vector3(draw.x, draw.y), color32, new Vector2(uv.z, uv.y));
                    vh.AddVert(new Vector3(draw.x, draw.w), color32, new Vector2(uv.z, uv.w));

                    vh.AddTriangle(0, 1, 2);
                    vh.AddTriangle(2, 3, 0);
                    vh.AddTriangle(0, 4, 5);
                    vh.AddTriangle(5, 1, 0);
                }
            } else if (y) {
                vh.AddVert(new Vector3(draw.x, drawCenter.y), color32, new Vector2(uv.x, uv.y));
                vh.AddVert(new Vector3(draw.x, draw.w), color32, new Vector2(uv.x, uv.w));
                vh.AddVert(new Vector3(draw.z, draw.w), color32, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(draw.z, drawCenter.y), color32, new Vector2(uv.z, uv.y));
                vh.AddVert(new Vector3(draw.z, draw.y), color32, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(draw.x, draw.y), color32, new Vector2(uv.x, uv.w));

                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
                vh.AddTriangle(3, 4, 5);
                vh.AddTriangle(3, 5, 0);
            } else {
                vh.AddVert(new Vector3(draw.x, draw.y), color32, new Vector2(uv.x, uv.y));
                vh.AddVert(new Vector3(draw.x, draw.w), color32, new Vector2(uv.x, uv.w));
                vh.AddVert(new Vector3(draw.z, draw.w), color32, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(draw.z, draw.y), color32, new Vector2(uv.z, uv.y));
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
            }
        }
    }
}