using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using Astro;
using BeauUtil;
using BeauPools;

[SysUpdate(GameLoopPhase.Update, 50, AstroGame.InstrumentUpdateMask)] // After InteractAdjustDialSystem
public class DialAdjustInstrumentSystem : ComponentSystemBehaviour<DialAdjustableInstrument>
{
    public override void ProcessWorkForComponent(DialAdjustableInstrument primary, float deltaTime)
    {
        base.ProcessWorkForComponent(primary, deltaTime);

        if (primary.Source.ValChanged)
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                int finalVal = (int) Mathf.Round(primary.Source.CurrConstrainedVal * primary.LinearMap + primary.Offset);
                primary.CurrentValue = finalVal;
                psb.Builder.AppendNoAlloc(finalVal);
                psb.Builder.Append(primary.ReadoutSuffix);
                primary.Readout.SetText(psb);
                primary.Updated = true;
            }
        }
        else
        {
            primary.Updated = false;
        }
    }
}
