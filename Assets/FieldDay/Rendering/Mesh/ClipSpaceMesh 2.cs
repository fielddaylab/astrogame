using System;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldDay.Rendering {
    static public class ClipSpaceMesh {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VertexPC {
            [VertexAttr(VertexAttribute.Position)] public Vector4 Position;
            [VertexAttr(VertexAttribute.Color)] public Color32 Color;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VertexPCU {
            [VertexAttr(VertexAttribute.Position)] public Vector4 Position;
            [VertexAttr(VertexAttribute.Color)] public Color32 Color;
            [VertexAttr(VertexAttribute.TexCoord0)] public Vector2 TexCoord0;
        }

        /// <summary>
        /// Creates data for a colored quad mesh.
        /// </summary>
        static public MeshData16<VertexPC> CreateColorQuad() {
            MeshData16<VertexPC> buff = new MeshData16<VertexPC>(4, 6, MeshTopology.Triangles, false);
            Color32 color = Color.white;
            VertexPC lb = new VertexPC() { Color = color, Position = new Vector4(-1, -1, 0, 0) };
            VertexPC lu = new VertexPC() { Color = color, Position = new Vector4(-1, 1, 0, 0) };
            VertexPC rb = new VertexPC() { Color = color, Position = new Vector4(1, -1, 0, 0) };
            VertexPC ru = new VertexPC() { Color = color, Position = new Vector4(1, 1, 0, 0) };
            buff.AddQuad(lb, lu, rb, ru);
            return buff;
        }

        /// <summary>
        /// Updates the color of the given mesh data.
        /// </summary>
        static public void UpdateColor(MeshData16<VertexPC> data, Color32 color) {
            for(int i = 0, n = data.VertexCount; i < n; i++) {
                data.Vertex(i).Color = color;
            }
        }

        /// <summary>
        /// Updates the color of the given mesh data.
        /// </summary>
        static public void UpdateColor(MeshData16<VertexPCU> data, Color32 color) {
            for (int i = 0, n = data.VertexCount; i < n; i++) {
                data.Vertex(i).Color = color;
            }
        }
    }
}