using BeauRoutine;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace Astro
{
    public class SatelliteDecoderDial : BatchedComponent, IRegistrationCallbacks
    {
        public float FacetAngle = 45; // How much the spinner turns on each click
        public Transform Spinner;
        public TextMeshPro[] TextDisplays;
        [SerializeField] private float SpacingRadius;
        [SerializeField] private float YOffset;

        [HideInInspector] public int CurrValIndex = 0;
        [HideInInspector] public int CurrDisplayIndex = 0;
        [HideInInspector] public Quaternion CurrTargetRotation;

        [HideInInspector]
        public char[] Values = new char[]
        {
        'A', 'B', 'C', 'D', 'E',
        'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
        };

        public Routine RotateRoutine;
        public Routine CountTimeRoutine;
        public AudioHandle RotateAudioHandle;
        public float RotationTime = 0; // keeps track of how long dial's been spinning for audio purposes
        public bool InLongSpin = false;

        [HideInInspector] public float RotateDuration;

        [HideInInspector] public float HoldTriggerTime;
        [HideInInspector] public float HoldTriggerTimer = 0;

        [HideInInspector] public float HoldCooldownTime;
        [HideInInspector] public float HoldCooldownTimer = 0;

        public void OnRegister()
        {
            RotateDuration = 0.15f;
            HoldCooldownTime = 0.12f; // must be less than rotate duration!
            HoldTriggerTime = 0.2f;
            CurrTargetRotation = Spinner.localRotation;
            DecoderUtility.UpdateDecoderDialVals(this, true);
        }

        public void OnDeregister()
        {

        }

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
#endif
    }
}