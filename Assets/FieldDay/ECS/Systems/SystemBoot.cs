using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// Registers and deregisters all system behaviours in this hierarchy.
    /// </summary>
    [DefaultExecutionOrder(-21000), DisallowMultipleComponent]
    public sealed class SystemBoot : MonoBehaviour {
        private void Awake() {
            foreach(var system in gameObject.GetComponentsInChildren<ISystem>(true)) {
                Game.Systems.Register(system);
            }
            Game.Systems.ProcessInitQueue();
        }

        private void OnDestroy() {
            if (Game.IsShuttingDown) {
                return;
            }

            foreach (var system in gameObject.GetComponentsInChildren<ISystem>(true)) {
                Game.Systems.Deregister(system);
            }
        }
    }
}