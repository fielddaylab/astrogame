using BeauUtil;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System derived from a MonoBehaviour.
    /// </summary>
    [NonIndexed]
    public abstract class SystemBehaviour : MonoBehaviour, ISystem {

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled;
        }

        public virtual void ProcessWork(float deltaTime) {

        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
        }

        public virtual void Shutdown() {
        }

        #endregion // Lifecycle
    }
}