using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay.Systems;

namespace FieldDay.Scripting {
    [SysUpdate(GameLoopPhase.LateUpdate, 10000, ScriptUtility.RuntimeUpdateMask)]
    internal sealed class ScriptRuntimeTickSystem : ISystem {
        public void Initialize() {
        }

        public void Shutdown() {
        }

        public bool HasWork() {
            return ScriptUtility.Runtime.PauseDepth == 0;
        }

        public void ProcessWork(float deltaTime) {
            if (ScriptUtility.Runtime.ActiveThreads.Count > 0) {
                //using (Profiling.Time("Leaf Update", ProfileTimeUnits.Microseconds)) {
                    Routine.ManualUpdate(deltaTime);
                //}
            }

            // TODO: process queue?
        }
    }
}