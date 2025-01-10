using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using Astro;
using BeauUtil;

[SysUpdate(GameLoopPhase.Update, 50)] // After InteractAdjustDialSystem
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
        primary.DialRoot.RotateAround(primary.DialRoot.position, primary.DialRoot.up, rotationAmt);
    }
}
