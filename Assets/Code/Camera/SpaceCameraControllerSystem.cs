using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SpaceCameraControllerSystem : SharedStateSystemBehaviour<SpaceCameraState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            ProcessInputs();
        }

        #region Input Processing

        private void ProcessInputs()
        {
            if (!m_State.InputEnabled) return;

            ProcessLook();

            ProcessZoom();
        }

        private void ProcessLook()
        {
            if (m_State.EnableMouseAutoControls)
            {
                ProcessMouseAutoLook();
            }
            if (m_State.EnableMouseControls)
            {
                ProcessMouseDragLook();
            }

            if (m_State.EnableSmoothKeyboardControls)
            {
                ProcessKeyboardLookSmooth();
            }
            else
            {
                ProcessKeyboardLookDiscrete();
            }
        }

        private void ProcessMouseAutoLook()
        {
            var cursorPos = m_State.Camera.Camera.ScreenToViewportPoint(Input.mousePosition);

            // Look X
            if (cursorPos.x < m_State.LookThreshold)
            {
                // look left
                var adjustedSpeed = (cursorPos.x < m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                AdjustHorizLook(-adjustedSpeed * Time.deltaTime);
            }
            else if (cursorPos.x > 1 - m_State.LookThreshold)
            {
                // look right
                var adjustedSpeed = (cursorPos.x > 1 - m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                AdjustHorizLook(adjustedSpeed * Time.deltaTime);
            }

            // Look Y
            if (cursorPos.y < m_State.LookThreshold)
            {
                // look down
                var adjustedSpeed = (cursorPos.y < m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                AdjustVertLook(adjustedSpeed * Time.deltaTime);
            }
            else if (cursorPos.y > 1 - m_State.LookThreshold)
            {
                // look up
                var adjustedSpeed = (cursorPos.y > 1 - m_State.LookRapidThreshold) ? m_State.LookRapidSpeed : m_State.LookSpeed;
                AdjustVertLook(-adjustedSpeed * Time.deltaTime);
            }
        }

        private void ProcessMouseDragLook()
        {
            if (m_State.MouseDragLookActive)
            {
                var cursorPos = m_State.Camera.Camera.ScreenToViewportPoint(Input.mousePosition);
                var deltaPos = m_State.PrevMousePos - cursorPos;

                // look horizontal
                AdjustHorizLook(deltaPos.x * m_State.LookDragMod);

                // look vertical
                AdjustVertLook(-deltaPos.y * m_State.LookDragMod);

                m_State.PrevMousePos = cursorPos;
            }
        }

        private void AdjustVertLook(float adjustment)
        {
            m_State.VertLook += adjustment;

            m_State.VertLook = ClampAngle(m_State.VertLook, m_State.LookYClamp.x, m_State.LookYClamp.y);

            var angles = m_State.Camera.RootTransform.localEulerAngles;
            angles.x = m_State.VertLook;
            m_State.Camera.RootTransform.localEulerAngles = angles;

            RecordLookUpdated();
        }

        private void AdjustHorizLook(float adjustment)
        {
            m_State.HorizLook += adjustment;

            m_State.HorizLook = ClampAngle(m_State.HorizLook, m_State.LookXClamp.x, m_State.LookXClamp.y);

            var angles = m_State.Camera.RootTransform.localEulerAngles;
            angles.y = m_State.HorizLook;
            m_State.Camera.RootTransform.localEulerAngles = angles;

            RecordLookUpdated();
        }

        private void ProcessKeyboardLookDiscrete()
        {
            if (Game.Input.IsKeyPressed(KeyCode.UpArrow) || Game.Input.IsKeyPressed(KeyCode.W))
            {
                // look up
                AdjustVertLook(-m_State.LookIncrement);
            }
            if (Game.Input.IsKeyPressed(KeyCode.DownArrow) || Game.Input.IsKeyPressed(KeyCode.S))
            {
                // look down
                AdjustVertLook(m_State.LookIncrement);
            }
            if (Game.Input.IsKeyPressed(KeyCode.LeftArrow) || Game.Input.IsKeyPressed(KeyCode.A))
            {
                // look left
                AdjustHorizLook(-m_State.LookIncrement);
            }
            if (Game.Input.IsKeyPressed(KeyCode.RightArrow) || Game.Input.IsKeyPressed(KeyCode.D))
            {
                // look right
                AdjustHorizLook(m_State.LookIncrement);
            }
        }

        private void ProcessKeyboardLookSmooth()
        {
            if (Game.Input.IsKeyDown(KeyCode.UpArrow) || Game.Input.IsKeyDown(KeyCode.W))
            {
                // look up
                AdjustVertLook(-m_State.SmoothLookIncrement);
            }
            else if (Game.Input.IsKeyDown(KeyCode.DownArrow) || Game.Input.IsKeyDown(KeyCode.S))
            {
                // look down
                AdjustVertLook(m_State.SmoothLookIncrement);
            }
            if (Game.Input.IsKeyDown(KeyCode.LeftArrow) || Game.Input.IsKeyDown(KeyCode.A))
            {
                // look left
                AdjustHorizLook(-m_State.SmoothLookIncrement);
            }
            else if (Game.Input.IsKeyDown(KeyCode.RightArrow) || Game.Input.IsKeyDown(KeyCode.D))
            {
                // look right
                AdjustHorizLook(m_State.SmoothLookIncrement);
            }
        }

        private void ProcessZoom()
        {
            if (m_State.EnableMouseControls)
            {
                ProcessMouseZoom();
            }

            ProcessKeyboardZoom();
        }

        private void ProcessMouseZoom()
        {
            var yScrollDelta = Input.mouseScrollDelta.y;
            if (yScrollDelta != 0 && !Game.Input.AreDevicesPaused())
            {
                float newZoom = m_State.Camera.Camera.fieldOfView;

                // inverse relationship: as player scrolls updwards, fov decreases
                newZoom = Mathf.Clamp(newZoom - yScrollDelta * m_State.ZoomSpeed, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Camera.Camera.fieldOfView = newZoom;
                RecordLookUpdated();
            }
        }

        private void ProcessKeyboardZoom()
        {
            if (Game.Input.IsKeyPressed(KeyCode.I)) {
                // Zoom out
                float newZoom = m_State.Zoom;

                newZoom = Mathf.Clamp(newZoom - m_State.ZoomIncrement, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Zoom = newZoom;
                m_State.Camera.Camera.fieldOfView = m_State.Camera.OriginalFOV / newZoom;
                RecordLookUpdated();
            }
            if (Game.Input.IsKeyPressed(KeyCode.K)) {
                // Zoom in
                float newZoom = m_State.Zoom;

                newZoom = Mathf.Clamp(newZoom + m_State.ZoomIncrement, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Zoom = newZoom;
                m_State.Camera.Camera.fieldOfView = m_State.Camera.OriginalFOV / newZoom;
                RecordLookUpdated();
            }
        }

        private void RecordLookUpdated()
        {
            m_State.OnLookUpdated.Invoke(m_State);
            m_State.LookUpdatedThisFrame = true;
        }

        #endregion // Input Processing

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f)
            {
                lfAngle += 360f;
            }
            if (lfAngle > 360f)
            {
                lfAngle -= 360f;
            }

            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}
