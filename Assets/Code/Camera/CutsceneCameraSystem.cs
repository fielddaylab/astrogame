using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.ApplicationPreRender, 10000)]
    public sealed class CutsceneCameraSystem : ComponentSystemBehaviour<CutsceneCamera> {
        public override void ProcessWork(float deltaTime) {
            ViewState view = Find.State<ViewState>();
            CutsceneUtility.SyncCamera(m_Components[0], view);
        }
    }
}