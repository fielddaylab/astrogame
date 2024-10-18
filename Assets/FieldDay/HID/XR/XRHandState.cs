using System;
using UnityEngine;

namespace FieldDay.HID.XR {
    /// <summary>
    /// XR hand button mask
    /// </summary>
    [Flags]
    public enum XRHandButtons : uint {
        None = 0,

        Primary = 0x01,
        PrimaryTouch = 0x02,

        Secondary = 0x04,
        SecondaryTouch = 0x08,

        Menu = 0x10,

        GripButton = 0x20,
        TriggerButton = 0x40,

        PrimaryAxisClick = 0x80,
        PrimaryAxisTouch = 0x100,

        SecondaryAxisClick = 0x200,
        SecondaryAxisTouch = 0x400,

        GripTouch = 0x800,
        TriggerTouch = 0x1000,

        PrimaryAxisUp = 0x2000,
        PrimaryAxisDown = 0x4000,
        PrimaryAxisLeft = 0x8000,
        PrimaryAxisRight = 0x10000,

        SecondaryAxisUp = 0x20000,
        SecondaryAxisDown = 0x40000,
        SecondaryAxisLeft = 0x80000,
        SecondaryAxisRight = 0x100000,

        PrimaryDPad = PrimaryAxisUp | PrimaryAxisDown | PrimaryAxisLeft | PrimaryAxisRight,
        SecondaryDPad = SecondaryAxisUp | SecondaryAxisDown | SecondaryAxisLeft | SecondaryAxisRight,

        All = Primary | PrimaryTouch | Secondary | SecondaryTouch
            | Menu | GripButton | TriggerButton | PrimaryAxisClick | PrimaryAxisTouch
            | SecondaryAxisClick | SecondaryAxisTouch | GripTouch | TriggerTouch
            | PrimaryDPad | SecondaryDPad,

        AllIgnoreTouch = Primary | Secondary | Menu | GripButton | TriggerButton
            | PrimaryAxisClick | SecondaryAxisClick
            | PrimaryDPad | SecondaryDPad,
    }

    /// <summary>
    /// Deadzones for XR hand axis inputs.
    /// </summary>
    [Serializable]
    public struct XRHandAxisDeadzones {
        public float Primary;
        public float PrimaryDpad;

        public float Secondary;
        public float SecondaryDpad;

        public float Grip;
        public float Trigger;

        static public readonly XRHandAxisDeadzones Default = new XRHandAxisDeadzones() {
            PrimaryDpad = 0.5f,
            SecondaryDpad = 0.5f
        };
    }

    /// <summary>
    /// XR hand index.
    /// </summary>
    public enum XRHandIndex {
        Left = 0,
        Right = 1,
        Any = 2
    }

    /// <summary>
    /// XR hand axis mask.
    /// </summary>
    [Flags]
    public enum XRHandAxes : uint {
        PrimaryStick = 0x01,
        SecondaryStick = 0x02,
        Grip = 0x04,
        Trigger = 0x08,

        All = PrimaryStick | SecondaryStick | Grip | Trigger,
        SingleStick = PrimaryStick | Grip | Trigger
    }

    /// <summary>
    /// XR hand axis states for a single frame.
    /// </summary>
    public struct XRHandAxisFrame {
        public Vector2 PrimaryAxis;
        public Vector2 SecondaryAxis;
        public float Grip;
        public float Trigger;
    }

    /// <summary>
    /// XR hand button and axis states for a single frame.
    /// </summary>
    public struct XRHandControllerFrame {
        public XRHandButtons Buttons;
        public XRHandAxisFrame Axes;
    }

    /// <summary>
    /// XR hand button, axis, and pose states for a single frame.
    /// </summary>
    public struct XRHandStateFrame {
        public XRHandControllerFrame Controller;
        public Pose? Pose;
    }
}