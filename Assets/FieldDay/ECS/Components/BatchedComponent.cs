using BeauUtil;
using UnityEngine;

namespace FieldDay.Components {
    /// <summary>
    /// Base class for a component that automatically registers itself
    /// to all relevant systems in SystemsMgr
    /// </summary>
    [NonIndexed]
    public abstract class BatchedComponent : MonoBehaviour, IComponentData {
        protected virtual void OnEnable() {
            Game.Components.Register(this);
        }

        protected virtual void OnDisable() {
            if (!Game.IsShuttingDown) {
                Game.Components.Deregister(this);
            }
        }
    }
}