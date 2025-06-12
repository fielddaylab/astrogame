using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SatelliteDecoderDial : BatchedComponent, IRegistrationCallbacks
{
    public float FacetAngle = 45; // How much the spinner turns on each click
    public Transform Spinner;
    public TextMeshPro[] TextDisplays;
    [SerializeField] private float SpacingRadius;
    [SerializeField] private float YOffset;

    [HideInInspector] public int CurrValIndex = 0;
    [HideInInspector] public int CurrDisplayIndex = 0;
    [HideInInspector] public float CurrTargetRotation = 0;

    [HideInInspector] public char[] Values = new char[]
    {
        'A', 'B', 'C', 'D', 'E',
        'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
    };

#if UNITY_EDITOR
    [ContextMenu("Arrange Dial Texts")]
    private void MenuArrangeDialTexts()
    {
        float step = 360f / TextDisplays.Length;
        for (int i = 0; i < TextDisplays.Length; i++)
        {
            // position
            TextDisplays[i].transform.position = Spinner.transform.position;
            TextDisplays[i].transform.localPosition += new Vector3(Mathf.Cos(Mathf.Deg2Rad * i * step) * SpacingRadius, Mathf.Sin(Mathf.Deg2Rad * i * step) * SpacingRadius + YOffset, 0);

            // rotation
            TextDisplays[i].transform.localRotation = Quaternion.Euler(i * step, -90, 0);
        }
    }

    public void OnRegister()
    {
        DecoderUtility.UpdateDecoderDialVals(this);
    }

    public void OnDeregister()
    {

    }
#endif
}
