using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Scenes {
    public abstract class SceneCustomData : MonoBehaviour, ISceneCustomData {
#if UNITY_EDITOR
        public abstract bool Build(Scene scene);
#endif // UNITY_EDITOR

        public virtual void OnLateEnable() { }
        public virtual void OnReady() { }
        public virtual void OnUnload() { }
    }
}