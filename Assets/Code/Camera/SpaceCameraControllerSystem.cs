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
        private string buttonHeld = "";

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

            ProcessAnalogInput();
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

            m_State.OnLookUpdated.Invoke(m_State);
        }

        private void AdjustHorizLook(float adjustment)
        {
            m_State.HorizLook += adjustment;

            m_State.HorizLook = ClampAngle(m_State.HorizLook, m_State.LookXClamp.x, m_State.LookXClamp.y);

            var angles = m_State.Camera.RootTransform.localEulerAngles;
            angles.y = m_State.HorizLook;
            m_State.Camera.RootTransform.localEulerAngles = angles;

            m_State.OnLookUpdated.Invoke(m_State);
        }

        private void ProcessKeyboardLookDiscrete()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                // look up
                AdjustVertLook(-m_State.LookIncrement);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                // look down
                AdjustVertLook(m_State.LookIncrement);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                // look left
                AdjustHorizLook(-m_State.LookIncrement);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                // look right
                AdjustHorizLook(m_State.LookIncrement);
            }
        }

        private void ProcessKeyboardLookSmooth()
        {
            // Speed at which it turns is related to the zoom (zoom goes from .5 at furthest to 5 at closest)
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                // look up
                AdjustVertLook(-m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                // look down
                AdjustVertLook(m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
            }
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                // look left
                AdjustHorizLook(-m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                // look right
                AdjustHorizLook(m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
            }
        }

        private void ProcessZoom()
        {
            ProcessMouseZoom();

            ProcessKeyboardZoom();
        }

        private void ProcessMouseZoom()
        {
            var yScrollDelta = Input.mouseScrollDelta.y;
            if (yScrollDelta != 0)
            {
                float newZoom = m_State.Camera.FOVPlane.Zoom;

                // inverse relationship: as player scrolls updwards, fov decreases
                newZoom = Mathf.Clamp(newZoom + yScrollDelta * 0.1f * m_State.ZoomSpeed, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Camera.FOVPlane.Zoom = newZoom;
            }
        }

        private void ProcessKeyboardZoom()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                // Zoom in
                float newZoom = m_State.Camera.FOVPlane.Zoom;

                newZoom = Mathf.Clamp(newZoom - m_State.ZoomIncrement, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Camera.FOVPlane.Zoom = newZoom;
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                // Zoom out
                float newZoom = m_State.Camera.FOVPlane.Zoom;

                newZoom = Mathf.Clamp(newZoom + m_State.ZoomIncrement, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                m_State.Camera.FOVPlane.Zoom = newZoom;
            }
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
        public void AnalogButtonHelper(string btnName, bool held)
        {
            if (held)
            {
                buttonHeld = btnName;
            }
            else
            {
                buttonHeld = "";
            }
        }
        private void ProcessAnalogInput()
        {
            float newZoom;
            switch (buttonHeld)
            {
                case "up":
                    AdjustVertLook(-m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
                    break;
                case "down":
                    AdjustVertLook(m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
                    break;
                case "left":
                    AdjustHorizLook(-m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
                    break;
                case "right":
                    AdjustHorizLook(m_State.SmoothLookIncrement / m_State.Camera.FOVPlane.Zoom);
                    break;
                case "zoomIn":
                    newZoom = m_State.Camera.FOVPlane.Zoom;

                    newZoom = Mathf.Clamp(newZoom + 0.025f * m_State.ZoomSpeed, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                    m_State.Camera.FOVPlane.Zoom = newZoom;
                    break;
                case "zoomOut":
                    newZoom = m_State.Camera.FOVPlane.Zoom;

                    newZoom = Mathf.Clamp(newZoom - 0.025f * m_State.ZoomSpeed, m_State.ZoomBounds.x, m_State.ZoomBounds.y);

                    m_State.Camera.FOVPlane.Zoom = newZoom;
                    break;
            }

        }

        /* TODO: revisit once monitors are re-implemented
        #region Handlers

        private void HandleUnfocusDown()
        {
            m_State.MouseDragLookActive = true;
            m_State.PrevMousePos = m_State.Camera.Camera.ScreenToViewportPoint(Input.mousePosition);
        }

        private void HandleUnfocusUp()
        {
            m_State.MouseDragLookActive = false;
            m_State.PrevMousePos = Vector3.zero;
        }

        #endregion // Handlers
        */
    }
}
