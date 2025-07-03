using System;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.MonitorControlsUpdateMask)]
    public class SpaceCameraControllerSystem : SharedStateSystemBehaviour<SpaceCameraState> {
        public override void ProcessWork(float deltaTime) {
            base.ProcessWork(deltaTime);

            UpdateFlags updated = ProcessInputs();

            if (updated != 0) {
                m_State.LookUpdatedThisFrame = true;
                m_State.OnLookUpdated.Invoke(m_State);

                if (m_State.ShouldDispatchMoveEvents && (updated & UpdateFlags.Rotation) != 0) {
                    ScriptUtility.Trigger(ScriptEvents.OnTelescopeMoved);
                }
            }

            if (Game.IsDevBuild) {
                if (DebugInput.IsPressed(KeyCode.T)) {
                    DebugFlags.ToggleFlag(SpaceCameraState.DebuggingFlags.DisplayLookCoords);
                }

                if (DebugFlags.IsFlagSet(SpaceCameraState.DebuggingFlags.DisplayLookCoords)) {
                    Vector3 spaceCameraEuler = m_State.Camera.RootTransform.eulerAngles;
                    HmsCoords hor = CoordinateUtility.DDToHms(360 - spaceCameraEuler.y);
                    DmsCoords vert = CoordinateUtility.DDToDms(360 - spaceCameraEuler.x);
                    using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                        psb.Builder.Append("Telescope Aimed At: ");
                        hor.ToString(psb);
                        psb.Builder.Append(' ');
                        vert.ToString(psb);

                        DebugDraw.AddLogText(psb, Color.yellow);

                        psb.Builder.Clear();

                        psb.Builder.Append("Local Aim: ")
                            .AppendNoAlloc(m_State.HorizLook, 2)
                            .Append(' ')
                            .AppendNoAlloc(m_State.VertLook, 2);

                        DebugDraw.AddLogText(psb, Color.white);
                    }
                }
            }
        }

        #region Input Processing

        private UpdateFlags ProcessInputs() {
            if (!m_State.InputEnabled) return 0;

            UpdateFlags updated = 0;
            if (!m_State.CameraRotationInputLocked) {
                if (ProcessLook()) {
                    updated |= UpdateFlags.Rotation;
                }
            }
            if (!m_State.ZoomInputLocked) {
                if (ProcessZoom()) {
                    updated |= UpdateFlags.Zoom;
                }
            }

            return updated;
        }

        private bool ProcessLook() {
            bool updated = false;
            if (m_State.EnableMouseAutoControls)
            {
                updated |= ProcessMouseAutoLook();
            }
            if (m_State.EnableMouseControls)
            {
                updated |= ProcessMouseDragLook();
            }

            if (m_State.EnableSmoothKeyboardControls) {
                updated |=ProcessKeyboardLookSmooth();
            } else {
                updated |=ProcessKeyboardLookDiscrete();
            }

            return updated;
        }

        private bool ProcessMouseAutoLook() {
            bool updated = false;
            var cursorPos = m_State.Camera.Camera.ScreenToViewportPoint(Input.mousePosition);

            // Look X
            if (cursorPos.x < m_State.LookThreshold)
            {
                // look left
                var adjustedSpeed = (cursorPos.x < m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                updated |= AdjustHorizLook(-adjustedSpeed * Frame.DeltaTime);
            }
            else if (cursorPos.x > 1 - m_State.LookThreshold)
            {
                // look right
                var adjustedSpeed = (cursorPos.x > 1 - m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                updated |= AdjustHorizLook(adjustedSpeed * Frame.DeltaTime);
            }

            // Look Y
            if (cursorPos.y < m_State.LookThreshold)
            {
                // look down
                var adjustedSpeed = (cursorPos.y < m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                updated |= AdjustVertLook(adjustedSpeed * Frame.DeltaTime);
            }
            else if (cursorPos.y > 1 - m_State.LookThreshold)
            {
                // look up
                var adjustedSpeed = (cursorPos.y > 1 - m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                updated |= AdjustVertLook(-adjustedSpeed * Frame.DeltaTime);
            }

            return updated;
        }

        private bool ProcessMouseDragLook()
        {
            bool updated = false;
            if (m_State.MouseDragLookActive)
            {
                var cursorPos = m_State.Camera.Camera.ScreenToViewportPoint(Input.mousePosition);
                var deltaPos = m_State.PrevMousePos - cursorPos;

                // look horizontal
                updated |= AdjustHorizLook(deltaPos.x * m_State.LookDragMod);

                // look vertical
                updated |= AdjustVertLook(-deltaPos.y * m_State.LookDragMod);

                m_State.PrevMousePos = cursorPos;
            }
            return updated;
        }

        private bool AdjustVertLook(float adjustment)
        {
            float oldVertLook = m_State.VertLook;
            float newVertLook = SpaceCameraUtility.ClampAngle(oldVertLook + adjustment, m_State.LookYClamp.x, m_State.LookYClamp.y);

            if (newVertLook != oldVertLook) {
                m_State.VertLook = newVertLook;

                var angles = m_State.Camera.RootTransform.localEulerAngles;
                angles.x = newVertLook;
                angles.z = 0;
                m_State.Camera.RootTransform.localEulerAngles = angles;
                return true;
            }

            return false;
        }

        private bool AdjustHorizLook(float adjustment)
        {
            if (adjustment == 0) {
                return false;
            }

            m_State.HorizLook = SpaceCameraUtility.ClampAngle(m_State.HorizLook + adjustment, m_State.LookXClamp.x, m_State.LookXClamp.y);

            var angles = m_State.Camera.RootTransform.localEulerAngles;
            angles.y = m_State.HorizLook;
            angles.z = 0;
            m_State.Camera.RootTransform.localEulerAngles = angles;

            return true;
        }

        private bool ProcessKeyboardLookDiscrete()
        {
            bool updated = false;
            if (Game.Input.IsKeyPressed(KeyCode.UpArrow) || Game.Input.IsKeyPressed(KeyCode.W))
            {
                // look up
                updated |= AdjustVertLook(-m_State.LookIncrement * Frame.DeltaTime);
            }
            if (Game.Input.IsKeyPressed(KeyCode.DownArrow) || Game.Input.IsKeyPressed(KeyCode.S))
            {
                // look down
                updated |= AdjustVertLook(m_State.LookIncrement * Frame.DeltaTime);
            }
            if (Game.Input.IsKeyPressed(KeyCode.LeftArrow) || Game.Input.IsKeyPressed(KeyCode.A))
            {
                // look left
                updated |= AdjustHorizLook(-m_State.LookIncrement * Frame.DeltaTime);
            }
            if (Game.Input.IsKeyPressed(KeyCode.RightArrow) || Game.Input.IsKeyPressed(KeyCode.D))
            {
                // look right
                updated |= AdjustHorizLook(m_State.LookIncrement * Frame.DeltaTime);
            }

            return updated;
        }

        private bool ProcessKeyboardLookSmooth()
        {
            bool updated = false;
            if (Game.Input.IsKeyDown(KeyCode.UpArrow) || Game.Input.IsKeyDown(KeyCode.W))
            {
                // look up
                updated |= AdjustVertLook(-m_State.SmoothLookIncrement * Frame.DeltaTime);
            }
            else if (Game.Input.IsKeyDown(KeyCode.DownArrow) || Game.Input.IsKeyDown(KeyCode.S))
            {
                // look down
                updated |= AdjustVertLook(m_State.SmoothLookIncrement * Frame.DeltaTime);
            }
            if (Game.Input.IsKeyDown(KeyCode.LeftArrow) || Game.Input.IsKeyDown(KeyCode.A))
            {
                // look left
                updated |= AdjustHorizLook(-m_State.SmoothLookIncrement * Frame.DeltaTime);
            }
            else if (Game.Input.IsKeyDown(KeyCode.RightArrow) || Game.Input.IsKeyDown(KeyCode.D))
            {
                // look right
                updated |= AdjustHorizLook(m_State.SmoothLookIncrement * Frame.DeltaTime);
            }

            return updated;
        }

        private bool ProcessZoom() {
            bool updated = false;
            if (m_State.EnableMouseControls)
            {
                updated |= ProcessMouseZoom();
            }

            updated |= ProcessKeyboardZoom();
            return updated;
        }

        private bool ProcessMouseZoom()
        {
            var yScrollDelta = Input.mouseScrollDelta.y;
            if (yScrollDelta != 0 && !Game.Input.AreDevicesPaused())
            {
                float oldZoom = m_State.Camera.Camera.fieldOfView;
                float newZoom = Mathf.Clamp(oldZoom - yScrollDelta * m_State.ZoomSpeed, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                // inverse relationship: as player scrolls updwards, fov decreases
                if (newZoom != oldZoom) {
                    m_State.Camera.Camera.fieldOfView = newZoom;
                    m_State.Zoom = m_State.Camera.OriginalFOV / newZoom;
                    return true;
                }
            }

            return false;
        }

        private bool ProcessKeyboardZoom()
        {
            float oldZoom = m_State.Zoom;
            float newZoom = oldZoom;

            if (Game.Input.IsKeyDown(KeyCode.Q)) {
                newZoom = Mathf.Clamp(newZoom - (m_State.ZoomIncrement * Frame.DeltaTime), m_State.ZoomBounds.x, m_State.ZoomBounds.y);
            } else if (Game.Input.IsKeyDown(KeyCode.E)) {
                newZoom = Mathf.Clamp(newZoom + (m_State.ZoomIncrement * Frame.DeltaTime), m_State.ZoomBounds.x, m_State.ZoomBounds.y);
            }

            if (oldZoom != newZoom) {
                m_State.Zoom = newZoom;
                m_State.Camera.Camera.fieldOfView = m_State.Camera.OriginalFOV / newZoom;
                return true;
            }

            return false;
        }

        #endregion // Input Processing

        [Flags]
        public enum UpdateFlags {
            Rotation = 0x01,
            Zoom = 0x02
        }
    }
}
