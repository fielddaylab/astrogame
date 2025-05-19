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

                CameraParams parms = CalculateParams(m_StateB);

                UpdateFociiPositions(parms, m_StateA, m_StateB);

                if (m_StateA.CurrentFocus) {
                    FocusVisualsUtility.AlignFocusOutlineToTarget(m_StateB, m_StateA, parms.Billboard);
                }
            }
        }

        private struct CameraParams {
            public Vector3 Forward;
            public float DotProductThreshold;
            public Quaternion Billboard;
        }

        static private CameraParams CalculateParams(SpaceCameraState spaceCam) {
            Camera refCam = spaceCam.Camera.Camera;
            Transform refCamTransform = refCam.transform;

            float refCamFOV = refCam.fieldOfView + 10;
            float dotProductThreshold = Mathf.Cos(refCamFOV * Mathf.Deg2Rad);

            Quaternion refCamRot = refCamTransform.rotation;
            Vector3 forward = Geom.Forward(refCamRot);
            Vector3 up = Geom.Up(refCamRot);

            CameraParams parms;
            parms.Billboard = Quaternion.LookRotation(-forward, up);
            parms.Forward = forward;
            parms.DotProductThreshold = dotProductThreshold;
            return parms;
        }

        static private void UpdateFociiPositions(in CameraParams parms, FocusState focusState, SpaceCameraState spaceCam) {
            ref var packedEnabled = ref focusState.ActiveFociiVisibleBits;
            var packedData = focusState.ActiveFociiPacked;
            int packedIdx = 0;
            foreach (UIFocus focus in focusState.ActiveFocii) {
                float dot = Vector3.Dot(packedData[packedIdx].TargetVector, parms.Forward);

                if (focus.IsVisibleInCurrentFilter && dot >= parms.DotProductThreshold) {
                    focus.Root.localRotation = parms.Billboard;

                    if (!packedEnabled.IsSet(packedIdx)) {
                        focus.Represent2D.enabled = true;
                        focus.Clickable.enabled = true;
                        packedEnabled.Set(packedIdx);
                    }
                } else {
                    if (focus.HasHighlight) {
                        focus.Root.localRotation = parms.Billboard;
                    }

                    if (packedEnabled.IsSet(packedIdx)) {
                        focus.Represent2D.enabled = false;
                        focus.Clickable.enabled = false;
                        packedEnabled.Unset(packedIdx);
                    }
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

        public static void AlignFocusOutlineToTarget(SpaceCameraState spaceCam, FocusState state, Quaternion overrideRotation) {
            Vector3 p = state.CurrentFocus.Root.position;
            state.FocusOutline.transform.SetPositionAndRotation(p, overrideRotation);
        }
    }
}