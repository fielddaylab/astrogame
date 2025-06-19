using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class MeshRendererUtility {
        static private readonly List<Material> s_MaterialWorkList = new List<Material>(4);

        /// <summary>
        /// Sets the shared material at the given index.
        /// </summary>
        static public void SetSharedMaterialAtIndex(this Renderer renderer, int materialIndex, Material newMaterial) {
            renderer.GetSharedMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
#if UNITY_2022_2_OR_NEWER
            renderer.SetSharedMaterials(s_MaterialWorkList);
#else
            if (materialIndex == 0 && s_MaterialWorkList.Count == 1) {
                renderer.sharedMaterial = newMaterial;
            } else {
                renderer.sharedMaterials = s_MaterialWorkList.ToArray();
            }
#endif // UNITY_2022_2_OR_NEWER
            s_MaterialWorkList.Clear();
        }

        /// <summary>
        /// Sets the material at the given index.
        /// </summary>
        static public void SetMaterialAtIndex(this Renderer renderer, int materialIndex, Material newMaterial) {
            renderer.GetMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
#if UNITY_2022_2_OR_NEWER
            renderer.SetMaterials(s_MaterialWorkList);
#else
            if (materialIndex == 0 && s_MaterialWorkList.Count == 1) {
                renderer.material = newMaterial;
            } else {
                renderer.materials = s_MaterialWorkList.ToArray();
            }
#endif // UNITY_2022_2_OR_NEWER
            s_MaterialWorkList.Clear();
        }
    }
}