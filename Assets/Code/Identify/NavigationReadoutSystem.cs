using FieldDay;
using BeauRoutine;
using FieldDay.Rendering;
using FieldDay.Scripting;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.MonitorControlsUpdateMask)]
    public class NavigationReadoutSystem : SharedStateSystemBehaviour<ReviewState, NavigationState> {

        public const float OnePipThreshold = 0; // 180 degrees
        public const float TwoPipThreshold = 0.708f; // ~90 degrees
        public const float ThreePipThreshold = 0.923f; // ~45 degrees
        public const float SuccessThreshold = 0.992f; // ~11 degrees

        public override bool HasWork() {
            return base.HasWork() && (m_StateB.CurrentNavigationMode != NavigationMode.Inactive);
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.ReviewModule.ResultShown || !m_StateB.ReadoutDirty) {
                return;
            }

            if (m_StateB.CurrentNavigationMode == NavigationMode.Constellation) {
                ProcessConstellationNav();
            }
            if (m_StateB.CurrentNavigationMode == NavigationMode.Neutrino) {
                ProcessNeutrinoNav();
            }
        }

        public void ProcessConstellationNav() {
            float newDist = m_StateB.CameraDistanceFromTarget; 
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
                if (!m_StateB.ConstellationSnapRoutine.Exists()) {
                    ReviewModuleUtility.ShowResultSprite(true, module);
                    PuzzleState puzzleState = Find.State<PuzzleState>();
                    EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;

                    m_StateB.ConstellationSnapRoutine = Routine.Start(NavigationUtility.SnapAlignment(target))
                        .OnComplete(() => {
                            NavigationCanvasUtil.AddPuzzleReticles();
                            Game.Events.Dispatch(GameEvents.PuzzleNavigationComplete); 
                            m_StateB.ConstellationSnapRoutine = Routine.Null;
                            });
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
            float newDist = m_StateB.CameraDistanceFromTarget;
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
            } else if (!m_StateB.ResultShown) {
                m_StateB.ResultShown = true;
                ReviewModuleUtility.ShowResultSprite(true, module);
                Game.Events.Dispatch(GameEvents.NeutrinoNavigationComplete);
                ReviewModuleUtility.ResetReview(module);
            }
            m_StateB.ReadoutDirty = true;

            if (m_StateB.ResultShown) {
                return;
            }

            // Scripting Events
            int newPips = module.PipsRevealed;
            if (prevPips > newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnNeutrinoNavColder, table);
                    AstroGame.Events.Dispatch(GameEvents.LocatorCloser, newPips);
                }
            } else if (prevPips < newPips) {
                using(var table = TempVarTable.Alloc()) {
                    table.Set("newPips", newPips);
                    ScriptUtility.Trigger(ScriptEvents.OnNeutrinoNavWarmer, table);
                    AstroGame.Events.Dispatch(GameEvents.LocatorFurther, newPips);
                }
            }
        }
    }
}
