using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// Registers and deregisters all system behaviours in this hierarchy.
    /// </summary>
    [DefaultExecutionOrder(-21000), DisallowMultipleComponent]
    public sealed class SystemBoot : MonoBehaviour {
        private ISystem[] m_Systems;

        private void Awake() {
            m_Systems = gameObject.GetComponentsInChildren<ISystem>(true);

            foreach(var system in m_Systems) {
                Game.Systems.Register(system);
            }
            Game.Systems.ProcessInitQueue();
        }

        private void OnDestroy() {
            if (Game.IsShuttingDown) {
                return;
            }

            foreach (var system in m_Systems) {
                Game.Systems.Deregister(system);
            }
        }
    }
}