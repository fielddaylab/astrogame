using BeauRoutine;
using FieldDay;
using FieldDay.Systems;

namespace Astro {
    public class SkyWavelengthFilterSystem : SharedStateSystemBehaviour<SkyGenerationState> {
        public override bool HasWork() {
            bool hasWork = base.HasWork();
            if (m_State) {
                hasWork = hasWork && m_State.IsDirty;
            } else { 
                return false; 
            }

            return hasWork;
        }

        public override void ProcessWork(float deltaTime) {
            var focusState = Find.State<FocusState>();
            var spaceCamera = Find.State<SpaceCameraState>();
            var neutrinoState = Find.State<NeutrinoHighlightState>();

            CelestialObjectVisMask visMask = m_State.VisMask;

            foreach (var focus in focusState.ActiveFocii) {
                FocusableUtility.UpdateFocusFilterAppearance(focus, visMask);
            }

            m_State.IsDirty = false;
            spaceCamera.LookUpdatedThisFrame = true;
        }
        
    }
}