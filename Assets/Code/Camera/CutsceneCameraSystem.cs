using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 10000)]
    public sealed class CutsceneCameraSystem : ComponentSystemBehaviour<CutsceneCamera> {
        public override void ProcessWork(float deltaTime) {
            ViewState view = Find.State<ViewState>();
            m_Components[0].Tracker.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            view.Camera.RootTransform.SetPositionAndRotation(pos, rot);
            view.Camera.EffectsTransform.SetLocalPositionAndRotation(default, default);
        }
    }
}