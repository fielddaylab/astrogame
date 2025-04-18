using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;
using System;
using BeauUtil.Debugger;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 0)]
    public class FocusVisualsSystem : SharedStateSystemBehaviour<FocusState, SpaceCameraState> {
        public override void ProcessWork(float deltaTime) {
            if (m_StateB.LookUpdatedThisFrame) {
                //using (Profiling.Time("updating star focii", ProfileTimeUnits.Microseconds)) {
                    UpdateFociiPositions(m_StateA, m_StateB);
                //}

                if (m_StateA.CurrentFocus) {
                    FocusVisualsUtility.AlignFocusOutlineToTarget(m_StateB, m_StateA);
                }
            }
        }

        static private void UpdateFociiPositions(FocusState focusState, SpaceCameraState spaceCam) {
            Camera refCam = spaceCam.Camera.Camera;
            Transform refCamTransform = refCam.transform;

            float refCamFOV = refCam.fieldOfView + 10;
            float dotProductThreshold = Mathf.Cos(refCamFOV * Mathf.Deg2Rad);

            Quaternion refCamRot = refCamTransform.rotation;
            Vector3 forward = Geom.Forward(refCamRot);
            Vector3 up = Geom.Up(refCamRot);

            Quaternion billboardRot = Quaternion.LookRotation(-forward, up);

            ref var packedEnabled = ref focusState.ActiveFociiVisibleBits;
            var packedData = focusState.ActiveFociiPacked;
            int packedIdx = 0;
            foreach (UIFocus focus in focusState.ActiveFocii) {
                float dot = Vector3.Dot(packedData[packedIdx].TargetVector, forward);

                if (dot >= dotProductThreshold) {
                    focus.Root.localRotation = billboardRot;

                    if (!packedEnabled.IsSet(packedIdx)) {
                        focus.Represent2D.enabled = true;
                        focus.Clickable.enabled = true;
                        packedEnabled.Set(packedIdx);
                    }
                } else if (packedEnabled.IsSet(packedIdx)) {
                    focus.Represent2D.enabled = false;
                    focus.Clickable.enabled = false;
                    packedEnabled.Unset(packedIdx);
                }

                packedIdx++;
            }
        }
    }

    public static class FocusVisualsUtility {
        public static void AlignFocusOutlineToTarget(SpaceCameraState spaceCam, FocusState state) {
            state.CurrentFocus.Root.GetPositionAndRotation(out var p, out var r);
            state.FocusOutline.transform.SetPositionAndRotation(p, r);
        }
    }
}