using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class MeshRendererUtility {
        static private readonly List<Material> s_MaterialWorkList = new List<Material>(4);
        
        /// <summary>
        /// Sets the shared material at the given index.
        /// </summary>
        static public void SetSharedMaterialAtIndex(this MeshRenderer meshRenderer, int materialIndex, Material newMaterial) {
            meshRenderer.GetSharedMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
            meshRenderer.SetSharedMaterials(s_MaterialWorkList);
            s_MaterialWorkList.Clear();
        }

        /// <summary>
        /// Sets the material at the given index.
        /// </summary>
        static public void SetMaterialAtIndex(this MeshRenderer meshRenderer, int materialIndex, Material newMaterial) {
            meshRenderer.GetMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
            meshRenderer.SetMaterials(s_MaterialWorkList);
            s_MaterialWorkList.Clear();
        }
    }
}