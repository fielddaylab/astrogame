#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FieldDay.HID;
using UnityEngine;
using FieldDay.HID.XR;

#if USING_XR && !UNITY_WEBGL
using UnityEngine.XR;
#endif // USING_XR && !UNITY_WEBGL

namespace FieldDay.Debugging {
    static public class DebugInput {
#if DEVELOPMENT
        private struct KeyMapEntry {

        }

        static private DigitalControlStates<DebugInputButtons> s_ButtonStates;
        static private DigitalControlStates<InputModifierKeys> s_ModifierStates;

#if USING_XR && !UNITY_WEBGL
        static private DigitalControlStates<XRHandButtons> s_XRButtonsLeft;
        static private DigitalControlStates<XRHandButtons> s_XRButtonsRight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private DigitalControlStates<XRHandButtons> HandInput(XRHandIndex index) {
            return index == XRHandIndex.Right ? s_XRButtonsRight : s_XRButtonsLeft;
        }
#endif // USING_XR && !UNITY_WEBGL

#endif // DEVELOPMENT

        #region Update

        static internal void Update() {
#if DEVELOPMENT
            DebugInputButtons buttons = default;
            InputModifierKeys modifiers = default;

            KeyboardUpdate(ref buttons, ref modifiers);
#if USING_XR && !UNITY_WEBGL
            XRUpdate(ref buttons, ref modifiers);
#endif // USING_XR && !UNITY_WEBGL

            s_ButtonStates.Update(buttons);
            s_ModifierStates.Update(modifiers);
#endif // DEVELOPMENT
        }

#if DEVELOPMENT
        static private void KeyboardUpdate(ref DebugInputButtons buttons, ref InputModifierKeys modifiers) {
            // modifiers

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                modifiers |= InputModifierKeys.Ctrl;
            }
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                modifiers |= InputModifierKeys.Shift;
            }
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                modifiers |= InputModifierKeys.Alt;
            }
            if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.RightMeta) || Input.GetKey(KeyCode.RightApple)) {
                modifiers |= InputModifierKeys.Platform;
            }

            // directional

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
                buttons |= DebugInputButtons.DPadUp;
            }
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
                buttons |= DebugInputButtons.DPadLeft;
            }
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
                buttons |= DebugInputButtons.DPadRight;
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
                buttons |= DebugInputButtons.DPadDown;
            }

            if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.Backspace)) {
                buttons |= DebugInputButtons.Cancel;
            }

            if (Input.GetKey(KeyCode.Return)) {
                buttons |= DebugInputButtons.Select;
            }
        }

#if USING_XR && !UNITY_WEBGL
        static private void XRUpdate(ref DebugInputButtons buttons, ref InputModifierKeys modifiers) {
            if (!XRSettings.isDeviceActive) {
                s_XRButtonsLeft.Update(XRHandButtons.None);
                s_XRButtonsRight.Update(XRHandButtons.None);
                return;
            }

            var lHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (lHand.isValid) {
                if (lHand.GetFeature(CommonUsages.gripButton)) {
                    modifiers |= InputModifierKeys.L1;
                }
                if (lHand.GetFeature(CommonUsages.triggerButton)) {
                    modifiers |= InputModifierKeys.L2;
                }
                if (lHand.GetFeature(CommonUsages.menuButton)) {
                    modifiers |= InputModifierKeys.Platform;
                }
                if (lHand.GetFeature(CommonUsages.primaryButton)) {
                    buttons |= DebugInputButtons.Select;
                }
                if (lHand.GetFeature(CommonUsages.secondaryButton)) {
                    buttons |= DebugInputButtons.Cancel;
                }

                // dpad
                var lButtons = XRUtility.ReadButtonStates(lHand, XRHandButtons.AllIgnoreTouch, XRHandAxisDeadzones.Default);
                if ((lButtons & XRHandButtons.PrimaryAxisLeft) != 0) {
                    buttons |= DebugInputButtons.DPadLeft;
                }
                if ((lButtons & XRHandButtons.PrimaryAxisRight) != 0) {
                    buttons |= DebugInputButtons.DPadRight;
                }
                if ((lButtons & XRHandButtons.PrimaryAxisUp) != 0) {
                    buttons |= DebugInputButtons.DPadUp;
                }
                if ((lButtons & XRHandButtons.PrimaryAxisDown) != 0) {
                    buttons |= DebugInputButtons.DPadDown;
                }

                s_XRButtonsLeft.Update(lButtons);
            } else {
                s_XRButtonsLeft.Update(XRHandButtons.None);
            }

            if (rHand.isValid) {
                if (rHand.GetFeature(CommonUsages.gripButton)) {
                    modifiers |= InputModifierKeys.R1;
                }
                if (rHand.GetFeature(CommonUsages.triggerButton)) {
                    modifiers |= InputModifierKeys.R2;
                }
                if (rHand.GetFeature(CommonUsages.menuButton)) {
                    modifiers |= InputModifierKeys.Platform;
                }
                if (rHand.GetFeature(CommonUsages.primaryButton)) {
                    buttons |= DebugInputButtons.Select;
                }
                if (rHand.GetFeature(CommonUsages.secondaryButton)) {
                    buttons |= DebugInputButtons.Cancel;
                }

                // dpad
                var rButtons = XRUtility.ReadButtonStates(rHand, XRHandButtons.AllIgnoreTouch, XRHandAxisDeadzones.Default);
                if ((rButtons & XRHandButtons.PrimaryAxisLeft) != 0) {
                    buttons |= DebugInputButtons.DPadLeft;
                }
                if ((rButtons & XRHandButtons.PrimaryAxisRight) != 0) {
                    buttons |= DebugInputButtons.DPadRight;
                }
                if ((rButtons & XRHandButtons.PrimaryAxisUp) != 0) {
                    buttons |= DebugInputButtons.DPadUp;
                }
                if ((rButtons & XRHandButtons.PrimaryAxisDown) != 0) {
                    buttons |= DebugInputButtons.DPadDown;
                }

                s_XRButtonsRight.Update(rButtons);
            } else {
                s_XRButtonsRight.Update(XRHandButtons.None);
            }
        }
#endif // USING_XR && !UNITY_WEBGL

#endif // DEVELOPMENT

        static internal void ConsumeAllForFrame() {
#if DEVELOPMENT
            s_ButtonStates.ClearChanges();
            s_ModifierStates.ClearChanges();
#endif // DEVELOPMENT
        }

        #endregion // Update

        #region Checks

        #region Keyboard

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(KeyCode key) {
#if DEVELOPMENT
            return Input.GetKey(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(InputModifierKeys modifiers, KeyCode key) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && Input.GetKey(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(KeyCode key) {
#if DEVELOPMENT
            return Input.GetKeyDown(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(InputModifierKeys modifiers, KeyCode key) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && Input.GetKeyDown(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(KeyCode key) {
#if DEVELOPMENT
            return Input.GetKeyUp(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(InputModifierKeys modifiers, KeyCode key) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && Input.GetKeyUp(key);
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Keyboard

        #region Mouse

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(MouseButton button) {
#if DEVELOPMENT
            return Input.GetMouseButton((int) button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(MouseButton button) {
#if DEVELOPMENT
            return Input.GetMouseButtonDown((int) button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(MouseButton button) {
#if DEVELOPMENT
            return Input.GetMouseButtonUp((int) button);
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Mouse

        #region Basic Buttons

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(DebugInputButtons button) {
#if DEVELOPMENT
            return s_ButtonStates.IsDown(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(InputModifierKeys modifiers, DebugInputButtons button) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && s_ButtonStates.IsDown(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(InputModifierKeys modifiers) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(DebugInputButtons button) {
#if DEVELOPMENT
            return s_ButtonStates.IsPressed(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(InputModifierKeys modifiers, DebugInputButtons button) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && s_ButtonStates.IsPressed(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(DebugInputButtons button) {
#if DEVELOPMENT
            return s_ButtonStates.IsReleased(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(InputModifierKeys modifiers, DebugInputButtons button) {
#if DEVELOPMENT
            return s_ModifierStates.IsDownAll(modifiers) && s_ButtonStates.IsReleased(button);
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Basic Buttons

        #region XR Buttons

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return HandInput(hand).IsDown(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDown(InputModifierKeys modifiers, XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return s_ModifierStates.IsDownAll(modifiers) && HandInput(hand).IsDown(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return HandInput(hand).IsPressed(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPressed(InputModifierKeys modifiers, XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return s_ModifierStates.IsDownAll(modifiers) && HandInput(hand).IsPressed(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return HandInput(hand).IsReleased(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsReleased(InputModifierKeys modifiers, XRHandIndex hand, XRHandButtons xrButton) {
#if DEVELOPMENT && USING_XR && !UNITY_WEBGL
            return s_ModifierStates.IsDownAll(modifiers) && HandInput(hand).IsReleased(xrButton);
#else
            return false;
#endif // DEVELOPMENT && USING_XR && !UNITY_WEBGL
        }

        #endregion // XR Buttons

        #endregion // Checks
    }

    [Flags]
    public enum DebugInputButtons : uint {
        None = 0,

        DPadUp = 0x001,
        DPadDown = 0x002,
        DPadLeft = 0x004,
        DPadRight = 0x008,

        Select = 0x010,
        Cancel = 0x020
    }
}