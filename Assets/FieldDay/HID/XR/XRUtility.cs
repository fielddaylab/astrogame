#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;
using UnityEngine;
using System.Reflection;

#if USING_XR && !UNITY_WEBGL
using UnityEngine.XR;
#endif // USING_XR && !UNITY_WEBGL

#if USING_OCULUSXR && !UNITY_WEBGL
using Unity.XR.Oculus;
#endif // USING_OCULUSXR && !UNITY_WEBGL

namespace FieldDay.HID.XR {
    /// <summary>
    /// Utility methods for handling XR devices and inputs.
    /// </summary>
    static public class XRUtility {
#if USING_XR && !UNITY_WEBGL

        #region Pose

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetPose(this XRNodeState nodeState, out Pose pose) {
            pose = default(Pose);
            return nodeState.tracked && nodeState.TryGetPosition(out pose.position) && nodeState.TryGetRotation(out pose.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetPose(this InputDevice device, out Pose pose) {
            pose = default(Pose);
            return device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out pose.position) && device.TryGetFeatureValue(CommonUsages.deviceRotation, out pose.rotation);
        }

        #endregion // Pose

        #region Features

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetFeature(this InputDevice device, InputFeatureUsage<Hand> usage, out Hand hand) {
            return device.TryGetFeatureValue(usage, out hand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetFeature(this InputDevice device, InputFeatureUsage<Bone> usage, out Bone bone) {
            return device.TryGetFeatureValue(usage, out bone);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool GetFeature(this InputDevice device, InputFeatureUsage<bool> usage) {
            bool val;
            return device.TryGetFeatureValue(usage, out val) && val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public uint GetFeature(this InputDevice device, InputFeatureUsage<uint> usage) {
            uint val;
            device.TryGetFeatureValue(usage, out val);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float GetFeature(this InputDevice device, InputFeatureUsage<float> usage) {
            float val;
            device.TryGetFeatureValue(usage, out val);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2 GetFeature(this InputDevice device, InputFeatureUsage<Vector2> usage) {
            Vector2 val;
            device.TryGetFeatureValue(usage, out val);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2 GetFeature(this InputDevice device, InputFeatureUsage<Vector3> usage) {
            Vector3 val;
            device.TryGetFeatureValue(usage, out val);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Quaternion GetFeature(this InputDevice device, InputFeatureUsage<Quaternion> usage) {
            Quaternion val = default;
            if (!device.TryGetFeatureValue(usage, out val)) {
                val = Quaternion.identity;
            }
            return val;
        }

        #endregion // Features

        #region Hand State

        private const InputDeviceCharacteristics HandCharacteristicsMask = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsHand(InputDeviceCharacteristics characteristics) {
            return (characteristics & HandCharacteristicsMask) == HandCharacteristicsMask;
        }

        /// <summary>
        /// Reads the current button state from the given hand.
        /// </summary>
        static public XRHandButtons ReadButtonStates(InputDevice device, XRHandButtons mask, in XRHandAxisDeadzones deadzones) {
            XRHandButtons buttons = default;
            if (device.isValid) {

#if DEVELOPMENT
                if (!IsHand(device.characteristics)) {
                    Assert.Fail("Provided device '{0}' (serial {1}) is not a hand!", device.name, device.serialNumber);
                }
#endif // DEVELOPMENT

                ReadButtonState(ref buttons, mask, XRHandButtons.Primary, device, CommonUsages.primaryButton);
                ReadButtonState(ref buttons, mask, XRHandButtons.PrimaryTouch, device, CommonUsages.primaryTouch);

                ReadButtonState(ref buttons, mask, XRHandButtons.Secondary, device, CommonUsages.secondaryButton);
                ReadButtonState(ref buttons, mask, XRHandButtons.SecondaryTouch, device, CommonUsages.secondaryTouch);

                ReadButtonState(ref buttons, mask, XRHandButtons.Menu, device, CommonUsages.menuButton);

                ReadButtonState(ref buttons, mask, XRHandButtons.GripButton, device, CommonUsages.gripButton);
                ReadButtonState(ref buttons, mask, XRHandButtons.TriggerButton, device, CommonUsages.triggerButton);

                ReadButtonState(ref buttons, mask, XRHandButtons.PrimaryAxisClick, device, CommonUsages.primary2DAxisClick);
                ReadButtonState(ref buttons, mask, XRHandButtons.PrimaryAxisTouch, device, CommonUsages.primary2DAxisTouch);

                ReadButtonState(ref buttons, mask, XRHandButtons.SecondaryAxisClick, device, CommonUsages.secondary2DAxisClick);
                ReadButtonState(ref buttons, mask, XRHandButtons.SecondaryAxisTouch, device, CommonUsages.secondary2DAxisTouch);

                if ((mask & XRHandButtons.PrimaryDPad) != 0) {
                    device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 val);
                    if ((mask & XRHandButtons.PrimaryAxisUp) != 0 && val.y > deadzones.PrimaryDpad) {
                        buttons |= XRHandButtons.PrimaryAxisUp;
                    }
                    if ((mask & XRHandButtons.PrimaryAxisDown) != 0 && val.y < -deadzones.PrimaryDpad) {
                        buttons |= XRHandButtons.PrimaryAxisDown;
                    }
                    if ((mask & XRHandButtons.PrimaryAxisRight) != 0 && val.x > deadzones.PrimaryDpad) {
                        buttons |= XRHandButtons.PrimaryAxisRight;
                    }
                    if ((mask & XRHandButtons.PrimaryAxisLeft) != 0 && val.x < -deadzones.PrimaryDpad) {
                        buttons |= XRHandButtons.PrimaryAxisLeft;
                    }
                }

                if ((mask & XRHandButtons.SecondaryDPad) != 0) {
                    device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 val);
                    if ((mask & XRHandButtons.SecondaryAxisUp) != 0 && val.y > deadzones.SecondaryDpad) {
                        buttons |= XRHandButtons.SecondaryAxisUp;
                    }
                    if ((mask & XRHandButtons.SecondaryAxisDown) != 0 && val.y < -deadzones.SecondaryDpad) {
                        buttons |= XRHandButtons.SecondaryAxisDown;
                    }
                    if ((mask & XRHandButtons.SecondaryAxisRight) != 0 && val.x > deadzones.SecondaryDpad) {
                        buttons |= XRHandButtons.SecondaryAxisRight;
                    }
                    if ((mask & XRHandButtons.SecondaryAxisLeft) != 0 && val.x < -deadzones.SecondaryDpad) {
                        buttons |= XRHandButtons.SecondaryAxisLeft;
                    }
                }

#if USING_OCULUSXR
                ReadButtonState(ref buttons, mask, XRHandButtons.GripTouch, device, OculusUsages.thumbTouch);
                ReadButtonState(ref buttons, mask, XRHandButtons.TriggerTouch, device, OculusUsages.indexTouch);
#endif // USING_OCULUSXR
            }

            return buttons;
        }

        static private void ReadButtonState(ref XRHandButtons buttonState, XRHandButtons mask, XRHandButtons requested, InputDevice device, InputFeatureUsage<bool> usage) {
            if ((mask & requested) != 0 && device.TryGetFeatureValue(usage, out bool val) && val) {
                buttonState |= requested;
            }
        }

        /// <summary>
        /// Reads the current axis state from the given hand.
        /// </summary>
        static public XRHandAxisFrame ReadAxisStates(InputDevice device, XRHandAxes mask, in XRHandAxisDeadzones deadzones) {
            XRHandAxisFrame state = default;

            if (device.isValid) {
#if DEVELOPMENT
                if (!IsHand(device.characteristics)) {
                    Assert.Fail("Provided device '{0}' (serial {1}) is not a hand!", device.name, device.serialNumber);
                }
#endif // DEVELOPMENT

                ReadAxisState(ref state.PrimaryAxis, mask, XRHandAxes.PrimaryStick, device, CommonUsages.primary2DAxis, deadzones.Primary);
                ReadAxisState(ref state.SecondaryAxis, mask, XRHandAxes.SecondaryStick, device, CommonUsages.secondary2DAxis, deadzones.Secondary);
                ReadAxisState(ref state.Grip, mask, XRHandAxes.Grip, device, CommonUsages.grip, deadzones.Grip);
                ReadAxisState(ref state.Trigger, mask, XRHandAxes.Trigger, device, CommonUsages.trigger, deadzones.Trigger);
            }

            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void ReadAxisState(ref Vector2 axisState, XRHandAxes mask, XRHandAxes requested, InputDevice device, InputFeatureUsage<Vector2> usage, float deadzone) {
            if ((mask & requested) != 0) {
                if (device.TryGetFeatureValue(usage, out axisState) && deadzone > 0) {
                    axisState.x = RemapDeadzone(axisState.x, deadzone);
                    axisState.y = RemapDeadzone(axisState.x, deadzone);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void ReadAxisState(ref float axisState, XRHandAxes mask, XRHandAxes requested, InputDevice device, InputFeatureUsage<float> usage, float deadzone) {
            if ((mask & requested) != 0) {
                if (device.TryGetFeatureValue(usage, out axisState) && deadzone > 0) {
                    axisState = RemapDeadzone(axisState, deadzone);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float RemapDeadzone(float val, float deadzone) {
            if (deadzone <= 0) {
                return val;
            }

            if (val > deadzone) {
                return (val - deadzone) / (1f - deadzone);
            } else if (val < -deadzone) {
                return (val - -deadzone) / (1f - deadzone);
            } else {
                return 0;
            }
        }

        /// <summary>
        /// Reads the current controller state from the given hand.
        /// </summary>
        static public XRHandControllerFrame ReadControllerState(InputDevice device, XRHandButtons buttonMask, XRHandAxes axisMask, in XRHandAxisDeadzones deadzones) {
            XRHandControllerFrame frame;
            frame.Buttons = ReadButtonStates(device, buttonMask, deadzones);
            frame.Axes = ReadAxisStates(device, axisMask, deadzones);
            return frame;
        }

        /// <summary>
        /// Reads the controller and pose state from the given hand.
        /// </summary>
        static public XRHandStateFrame ReadHandState(InputDevice device, XRHandButtons buttonMask, XRHandAxes axisMask, in XRHandAxisDeadzones deadzones) {
            XRHandControllerFrame ctrlFrame;
            ctrlFrame.Buttons = ReadButtonStates(device, buttonMask, deadzones);
            ctrlFrame.Axes = ReadAxisStates(device, axisMask, deadzones);

            XRHandStateFrame frame;
            frame.Controller = ctrlFrame;

            Pose pose;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out pose.position) && device.TryGetFeatureValue(CommonUsages.deviceRotation, out pose.rotation)) {
                frame.Pose = pose;
            } else {
                frame.Pose = null;
            }

            return frame;
        }

        #endregion // Hand State

        #region Head State

        private const InputDeviceCharacteristics HeadCharacteristicsMask = InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsHead(InputDeviceCharacteristics characteristics) {
            return (characteristics & HeadCharacteristicsMask) == HeadCharacteristicsMask;
        }

        /// <summary>
        /// Read the pose from the given head.
        /// Will return null if the pose was not able to be retrieved.
        /// </summary>
        static public Pose? ReadHeadPose(InputDevice device) {
            if (device.isValid) {
#if DEVELOPMENT
                if (!IsHead(device.characteristics)) {
                    Assert.Fail("Provided device '{0}' (serial {1}) is not a head!", device.name, device.serialNumber);
                }
#endif // DEVELOPMENT

                Pose pose;
                bool readDevice = device.TryGetFeatureValue(CommonUsages.devicePosition, out pose.position);
                readDevice &= device.TryGetFeatureValue(CommonUsages.deviceRotation, out pose.rotation);
                if (readDevice) {
                    return pose;
                }
            }

            return null;
        }

        #endregion // Head State

        #region Refresh Rate

        /// <summary>
        /// Attempts to set the refresh rate of the headset.
        /// </summary>
        static public void SetRefreshRate(float desiredRefreshRate) {
#if USING_OCULUSXR
            if (Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(desiredRefreshRate)) {
                Log.Msg("[XRUtility] Set refresh rate to {0}hZ", desiredRefreshRate);
            } else {
                Log.Warn("[XRUtility] Unable to set refresh rate to {0}hZ", desiredRefreshRate);
            }
#endif // USING_OCULUSXR
        }

        #endregion // Refresh Rate

        #region Devices

        /// <summary>
        /// Gets the input device for the given hand.
        /// </summary>
        static public InputDevice GetHand(XRHandIndex hand) {
            return InputDevices.GetDeviceAtXRNode(HandIndexToNode(hand));
        }

        /// <summary>
        /// Maps a hand index to an XRNode
        /// </summary>
        static internal XRNode HandIndexToNode(XRHandIndex index) {
            switch (index) {
                case XRHandIndex.Left:
                    return XRNode.LeftHand;
                case XRHandIndex.Right:
                    return XRNode.RightHand;
                default:
                    return (XRNode) (-1);
            }
        }

        #endregion // Devices

#endif // USING_XR && !UNITY_WEBGL
    }
}