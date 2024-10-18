using UnityEngine;

namespace FieldDay.Scenes {
    /// <summary>
    /// Behaviour marking a GameObject hierarchy as only existing during editing.
    /// </summary>
    [DefaultExecutionOrder(-31500)]
    public sealed class LateEnable : MonoBehaviour {
        public int Order = 0;

#if UNITY_EDITOR
        private void Awake() {
            gameObject.SetActive(false);
        }
#endif // UNITY_EDITOR
    }
}