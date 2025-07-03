using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using Astro;
using BeauUtil;
using BeauRoutine;

[SysUpdate(GameLoopPhase.Update, 60)] // After DialAdjustInstrumentSystem
public class DialRotationSystem : ComponentSystemBehaviour<InteractAdjustDial>
{
    public override void ProcessWorkForComponent(InteractAdjustDial primary, float deltaTime)
    {
        base.ProcessWorkForComponent(primary, deltaTime);

        float rotationAmt = 0;
        rotationAmt = primary.RotateSpeed * primary.ConstrainedValDelta;

        if (rotationAmt != 0) {
            Vector3 axis = default;
            switch (primary.PivotAxis) {
                case Axis.X:
                    axis.x = 1;
                    break;
                case Axis.Y:
                    axis.y = 1;
                    break;
                case Axis.Z:
                    axis.z = 1;
                    break;
            }

            primary.DialRoot.Rotate(axis, rotationAmt, Space.Self);
        }
    }
}
