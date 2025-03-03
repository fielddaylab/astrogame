using UnityEngine;

namespace FieldDay.Layout {
    public sealed class CompressiblePrefab : MonoBehaviour {
        
    }

    public interface ICompressiblePrefabStep {
        void RunPrefabStep(CompressiblePrefab prefab);
    }
}