using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldDay.Rendering {
    static public class MeshModUtility {
        static private List<Vector2> s_Vector2Cache = new List<Vector2>(128);
        static private List<Vector3> s_Vector3Cache = new List<Vector3>(128);
        static private readonly Rect s_DefaultUVSpace = new Rect(0, 0, 1, 1);

        /// <summary>
        /// Applies a transform to all vertex positions.
        /// </summary>
        static public void TransformPositions(Mesh mesh, Matrix4x4 matrix) {
            mesh.GetVertices(s_Vector3Cache);
            for (int i = 0; i < s_Vector3Cache.Count; i++) {
                s_Vector3Cache[i] = matrix.MultiplyPoint3x4(s_Vector3Cache[i]);
            }
            mesh.SetVertices(s_Vector3Cache);

            mesh.RecalculateNormals(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.RecalculateTangents(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.RecalculateBounds();

            s_Vector3Cache.Clear();
        }

        /// <summary>
        /// Remaps the UVs for the given mesh channel from 0-1 space to the given space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect uvSpace) {
            mesh.GetUVs(channel, s_Vector2Cache);
            for(int i = 0; i < s_Vector2Cache.Count; i++) {
                s_Vector2Cache[i] = Geom.Remap(s_Vector2Cache[i], s_DefaultUVSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_Vector2Cache);
            s_Vector2Cache.Clear();
        }

        /// <summary>
        /// Remaps the UVs for the given mesh channel from the given original space to the new space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect originalSpace, Rect uvSpace) {
            mesh.GetUVs(channel, s_Vector2Cache);
            for (int i = 0; i < s_Vector2Cache.Count; i++) {
                s_Vector2Cache[i] = Geom.Remap(s_Vector2Cache[i], originalSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_Vector2Cache);
            s_Vector2Cache.Clear();
        }
    }
}