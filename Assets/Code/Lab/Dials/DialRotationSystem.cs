using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using Astro;
using BeauUtil;
using BeauRoutine;

[SysUpdate(GameLoopPhase.Update, 50, AstroGame.InstrumentUpdateMask)] // After InteractAdjustDialSystem
public class DialRotationSystem : ComponentSystemBehaviour<InteractAdjustDial>
{
    public override void ProcessWorkForComponent(InteractAdjustDial primary, float deltaTime)
    {
        base.ProcessWorkForComponent(primary, deltaTime);

        float rotationAmt = 0;
        if (primary.PassThrough && primary.RawValDelta != 0)
        {
            rotationAmt = primary.RotateSpeed * primary.RawValDelta;
        }
        else if (!primary.PassThrough && primary.ConstrainedValDelta != 0)
        {
            rotationAmt = primary.RotateSpeed * primary.ConstrainedValDelta;
        }
        Vector3 pivot = Vector3.zero;
        if (primary.PivotAxis == Axis.Y) {
            pivot = primary.DialRoot.up;
        }
        else if (primary.PivotAxis == Axis.X)
        {
            pivot = primary.DialRoot.right;
        }
        primary.DialRoot.RotateAround(primary.DialRoot.position, pivot, rotationAmt);
    }
}
