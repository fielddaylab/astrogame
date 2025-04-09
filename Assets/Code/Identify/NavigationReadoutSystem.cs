using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Rendering;
using FieldDay.Scripting;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.MonitorControlsUpdateMask)]
    public class NavigationReadoutSystem : SharedStateSystemBehaviour<PlayerPointsState, PuzzleNavigationState, NeutrinoNavigationState> {

        public override bool HasWork() {
            return base.HasWork() && (m_StateB.NavigationModeActive || m_StateC.NavigationModeActive);
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.NavigationModeActive && m_StateB.ReadoutDirty) {
                ProcessConstellationNav();
            }
            if (m_StateC.NavigationModeActive && m_StateC.ReadoutDirty) {
                ProcessNeutrinoNav();
            }
        }

        public void ProcessConstellationNav() {
            float newDist = m_StateB.CameraDistanceFromPuzzle; 
            ReviewModule module = m_StateA.ReviewModule;

            int prevPips = module.PipsRevealed;

            if (newDist < 0) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 1);
            } else if (newDist > 0 && newDist < 0.5) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 2);
            } else if (newDist > 0.5 && newDist < 0.998) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 3);
            } else if (newDist > 0.999) {
                ReviewModuleUtility.ShowResultSprite(true, module);
                if (!m_StateB.ConstellationSnapRoutine.Exists()){
                    m_StateB.ConstellationSnapRoutine = Routine.Start( PuzzleNavigationUtility.SnapConstellationAlignment() );
                }
            }

            m_StateB.ReadoutDirty = true;

            // Scripting Events
            int newPips = module.PipsRevealed;
            if (prevPips > newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnConstellationNavColder, table);
                }
            } else if (prevPips < newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnConstellationNavWarmer, table);
                }
            }
        }

        public void ProcessNeutrinoNav() {
            float newDist = m_StateC.CameraDistanceFromOrigin; 
            ReviewModule module = m_StateA.ReviewModule;

            int prevPips = module.PipsRevealed;

            if (newDist < 0) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 1);
            } else if (newDist > 0 && newDist < 0.5) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 2);
            } else if (newDist > 0.5 && newDist < 0.98) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 3);
            } else if (newDist > 0.98) {
                ReviewModuleUtility.ShowResultSprite(true, module);
                Game.Events.Dispatch(GameEvents.NeutrinoNavigationComplete);
                ReviewModuleUtility.ResetReview(module);
            }
            m_StateC.ReadoutDirty = true;

            // Scripting Events
            int newPips = module.PipsRevealed;
            if (prevPips > newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnNeutrinoNavColder, table);
                }
            } else if (prevPips < newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnNeutrinoNavWarmer, table);
                }
            }
        }
    }
}
