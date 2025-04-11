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

        public const float OnePipThreshold = 0; // 180 degrees
        public const float TwoPipThreshold = 0.708f; // ~90 degrees
        public const float ThreePipThreshold = 0.923f; // ~45 degrees
        public const float SuccessThreshold = 0.995f; // ~11 degrees

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

            if (newDist < OnePipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 0);
            } else if (newDist < TwoPipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 1);
            } else if (newDist < ThreePipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 2);
            } else if (newDist < SuccessThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 3);
            } else {
                ReviewModuleUtility.ShowResultSprite(true, module);
                if (!m_StateB.ConstellationSnapRoutine.Exists()) {
                    PuzzleState puzzleState = Find.State<PuzzleState>();
                    EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;

                    m_StateB.ConstellationSnapRoutine = Routine.Start(PuzzleNavigationUtility.SnapConstellationAlignment(target))
                        .OnComplete(() => { Game.Events.Dispatch(GameEvents.PuzzleNavigationComplete); });
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

            if (newDist < OnePipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 0);
            } else if (newDist < TwoPipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 1);
            } else if (newDist < ThreePipThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 2);
            } else if (newDist < SuccessThreshold) {
                module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                ReviewModuleUtility.SetPipReadout(module, 3);
            } else {
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
