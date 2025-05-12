using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class MeshUVUtility {
        static private List<Vector2> s_UVCache = new List<Vector2>(128);
        static private readonly Rect s_DefaultUVSpace = new Rect(0, 0, 1, 1);

        /// <summary>
        /// Remaps the UVs for the given mesh channel from 0-1 space to the given space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect uvSpace) {
            mesh.GetUVs(channel, s_UVCache);
            for(int i = 0; i < s_UVCache.Count; i++) {
                s_UVCache[i] = Geom.Remap(s_UVCache[i], s_DefaultUVSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_UVCache);
            s_UVCache.Clear();
        }

        /// <summary>
        /// Remaps the UVs for the given mesh channel from the given original space to the new space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect originalSpace, Rect uvSpace) {
            mesh.GetUVs(channel, s_UVCache);
            for (int i = 0; i < s_UVCache.Count; i++) {
                s_UVCache[i] = Geom.Remap(s_UVCache[i], originalSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_UVCache);
            s_UVCache.Clear();
        }
    }
}