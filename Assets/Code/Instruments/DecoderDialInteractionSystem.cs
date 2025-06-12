using Astro;
using BeauRoutine;
using BeauUtil;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecoderDialInteractionSystem : ComponentSystemBehaviour<SatelliteDecoderDialButton, LabInteractable>
{
    public override void ProcessWork(float deltaTime)
    {
        base.ProcessWork(deltaTime);

        foreach (var component in m_Components)
        {
            if (!component.Secondary.InteractReceived) { continue; }

            DecoderUtility.AdjustDecoderDial(component.Primary.Target, component.Primary.Vector);
        }
    }
}

public static class DecoderUtility
{
    private static Vector3 DialRotationConstant = new Vector3(0f, -90, -90);

    public static void AdjustDecoderDial(SatelliteDecoderDial dial, int vector)
    {
        // TODO: make a routine
        dial.CurrTargetRotation = dial.CurrTargetRotation + dial.FacetAngle * vector;
        if (dial.CurrTargetRotation >= 360 || dial.CurrTargetRotation <= -360) { dial.CurrTargetRotation %= 360; }
        var eulers = new Vector3(dial.CurrTargetRotation, DialRotationConstant.y, DialRotationConstant.z);
        dial.Spinner.localEulerAngles = eulers;
    }
}