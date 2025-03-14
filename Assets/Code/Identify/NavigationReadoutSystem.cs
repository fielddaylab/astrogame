using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Rendering;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {
    public class NavigationReadoutSystem : SharedStateSystemBehaviour<PlayerPointsState, PuzzleNavigationState> {

        public override bool HasWork() {
            return base.HasWork() && m_StateB.NavigationModeActive;
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.ReadoutDirty) {
                ProcessConstellationNav();
            }
        }

        public void ProcessConstellationNav() {
            float newDist = m_StateB.CameraDistanceFromPuzzle; 
            ReviewModule module = m_StateA.ReviewModule;

            if (newDist < 0) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 0);
            } else if (newDist > 0 && newDist < 0.5) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 1);
            } else if (newDist > 0.5 && newDist < 0.8) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 2);
            } else if (newDist > 0.8 && newDist < 0.98) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 3);
            } else if (newDist > 0.9999) {
                ReviewModuleUtility.ShowResultSprite(true, module);
                if (!m_StateB.ConstellationSnapRoutine.Exists()){
                    m_StateB.ConstellationSnapRoutine = Routine.Start( PuzzleNavigationUtility.SnapConstellationAlignment() );
                }
            }
            m_StateB.ReadoutDirty = true;
        }

    }
}
