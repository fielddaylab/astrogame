using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using Astro;
using BeauUtil;

[SysUpdate(GameLoopPhase.Update, 50)] // After InteractAdjustDialSystem
public class DialAdjustInstrumentSystem : ComponentSystemBehaviour<DialAdjustableInstrument>
{
    public override void ProcessWorkForComponent(DialAdjustableInstrument primary, float deltaTime)
    {
        base.ProcessWorkForComponent(primary, deltaTime);

        if (primary.Source.ValChanged)
        {
            primary.Readout.SetText(((int)(primary.Source.CurrConstrainedVal * primary.LinearMap + primary.Offset)).ToStringLookup());
        }
    }
}
