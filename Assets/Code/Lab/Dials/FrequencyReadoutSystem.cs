using FieldDay;
using FieldDay.Systems;

using BeauUtil;
using BeauPools;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 50)] // After InteractAdjustDialSystem
    public class FrequencyReadoutSystem : ComponentSystemBehaviour<DialAdjustableInstrument> {
        public override void ProcessWorkForComponent(DialAdjustableInstrument primary, float deltaTime) {
            base.ProcessWorkForComponent(primary, deltaTime);

            if (primary.Source.InteractReceived) {
                AstroGame.Events.Dispatch(GameEvents.StartAdjustRadio, primary.CurrentValue);
            }

            if (!primary.Source.ValChanged) {
                primary.Updated = false;
                return;
            }

            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                int finalVal = (int) Mathf.Round(primary.Source.CurrConstrainedVal * primary.LinearMap + primary.Offset);
                primary.CurrentValue = finalVal;

                // Special case for off channel
                if (primary.CurrentValue == 100) {
                    psb.Builder.Append("OFF");
                } else { 
                    psb.Builder.AppendNoAlloc(finalVal);
                    psb.Builder.Append(primary.ReadoutSuffix);
                }

                primary.Readout.SetText(psb);
                primary.Updated = true;

                if (primary.Source.InteractEnded) {
                    AstroGame.Events.Dispatch(GameEvents.EndAdjustRadio, primary.CurrentValue);
                }
            }
        }
    }
}
