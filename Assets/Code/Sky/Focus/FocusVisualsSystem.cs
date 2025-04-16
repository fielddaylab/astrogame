//#define FOCUS_ALTERNATE_CODE

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
                using (Profiling.Time("updating star focii", ProfileTimeUnits.Microseconds)) {
#if FOCUS_ALTERNATE_CODE
                    UpdateFociiPositions(m_StateA, m_StateB);
#else
                    UpdateFociiPositions(m_StateA, m_StateB);
#endif // FOCUS_ALTERNATE_CODE
                }

                if (m_StateA.CurrentFocus) {
                    FocusVisualsUtility.AlignFocusOutlineToTarget(m_StateB, m_StateA);
                }
            }
        }

        static private unsafe void UpdateFociiPositions_Alt(FocusState focusState, SpaceCameraState spaceCam) {
            CameraRig cameraRig = spaceCam.Camera;
            Vector3 camPos = cameraRig.RootTransform.position;
            Vector3 camLook = cameraRig.RootTransform.forward;

            float camFOV = spaceCam.Camera.Camera.fieldOfView;
            float camAspect = spaceCam.Camera.Camera.aspect;
            float unitFrustum = CameraHelper.UnitHeightForFOV(camFOV);
            Matrix4x4 toCamLocalSpace = spaceCam.Camera.RootTransform.worldToLocalMatrix;

            float dotProductThreshold = Mathf.Cos(Mathf.Deg2Rad * ((camFOV * camAspect) + 30));

            int fociiCount = focusState.ActiveFocii.Count;
            RingBuffer<UIFocus> focii = focusState.ActiveFocii;
            UIFocusPackedData[] packedData = focusState.ActiveFociiPacked;

            Vector3* localSpacePositions = stackalloc Vector3[fociiCount];

            BitSet256 visibleBits = new BitSet256();
            BitSet256 oldVisible = focusState.ActiveFociiVisibleBits;

            for (int i = 0; i < fociiCount; i++) {
                UIFocus focus = focii[i];
                ref UIFocusPackedData packed = ref packedData[i];
                bool visible = Vector3.Dot(camLook, packed.TargetVector) >= dotProductThreshold;
                if (visible) {
                    visibleBits.Set(i);
                    localSpacePositions[i] = toCamLocalSpace.MultiplyPoint3x4(packed.TargetPos);
                }
            }

            for(int i = 0; i < fociiCount; i++) {
                UIFocus focus = focusState.ActiveFocii[i];
                if (!visibleBits.IsSet(i)) {
                    if (oldVisible.IsSet(i)) {
                        focus.Represent2D.enabled = false;
                        focus.Button.enabled = false;
                        focus.HighlightRect.gameObject.SetActive(false);
                    }
                    continue;
                }

                Vector3 localCamPos = localSpacePositions[i];
                float frustumHeight = localCamPos.z * unitFrustum;
                float frustumWidth = frustumHeight * camAspect;

                Vector2 viewportPos;
                viewportPos.x = 0.5f + localCamPos.x / frustumWidth;
                viewportPos.y = 0.5f + localCamPos.y / frustumHeight;

                focus.Rect.anchorMin = focus.Rect.anchorMax = viewportPos;

                if (!oldVisible.IsSet(i)) {
                    focus.Represent2D.enabled = true;
                    focus.Button.enabled = true;
                    focus.HighlightRect.gameObject.SetActive(true);
                }
            }

            focusState.ActiveFociiVisibleBits = visibleBits;
        }

        static private void UpdateFociiPositions(FocusState focusState, SpaceCameraState spaceCam) {
            foreach (UIFocus focus in focusState.ActiveFocii) {
                if (focus.TargetRenderer.isVisible) {
                    focus.Represent2D.enabled = true;
                    focus.Button.enabled = true;
                    // position 2D representation in screen space
                    Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(focus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                    focus.Rect.anchorMin = focus.Rect.anchorMax = viewPoint;
                    focus.HighlightRect.gameObject.SetActive(true);
                } else {
                    focus.Represent2D.enabled = false;
                    focus.Button.enabled = false;
                    focus.HighlightRect.gameObject.SetActive(false);
                }
            }
        }
    }

    public static class FocusVisualsUtility {
        public static void AlignFocusOutlineToTarget(SpaceCameraState spaceCam, FocusState state) {
            Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(state.CurrentFocus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
            state.FocusOutline.rectTransform.anchorMin = state.FocusOutline.rectTransform.anchorMax = viewPoint;
        }
    }
}