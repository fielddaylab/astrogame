using System;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI {
    /// <summary>
    /// Gui command information.
    /// </summary>
    internal struct GuiCommandData {
        public GuiCommandType Type;
        public GuiCommandArgument Arg;
        public object Target;
        public object ArgObject;
    }

    /// <summary>
    /// Gui command types.
    /// </summary>
    internal enum GuiCommandType : byte {
        SetActive_GO,
        SetActive_Behaviour,
        SetActive_ActiveGroup,
        TryClick_GO,
        ForceClick_GO,
        ExecuteAction_Void,
        ExecuteAction_Bool,
        ExecuteAction_Int,
        ExecuteAction_Float,
        ExecuteAction_StringHash32,
        ExecuteAction_Object,
        RebuildLayout_RectTransform,
        RebuildLayoutManually_LayoutGroup
    }

    /// <summary>
    /// Packed argument for gui commands.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct GuiCommandArgument {
        [FieldOffset(0)] public bool Bool;
        [FieldOffset(0)] public int Int;
        [FieldOffset(0)] public float Float;
        [FieldOffset(0)] public StringHash32 StringHash32;

        public GuiCommandArgument(bool value) : this() {
            Bool = value;
        }

        public GuiCommandArgument(int value) : this() {
            Int = value;
        }

        public GuiCommandArgument(float value) : this() {
            Float = value;
        }

        public GuiCommandArgument(StringHash32 value) : this() {
            StringHash32 = value;
        }
    }

    /// <summary>
    /// Queued gui operations.
    /// </summary>
    static public class GuiCommands {
        static public void SetActive(GameObject go, bool state) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.SetActive_GO,
                Arg = new GuiCommandArgument(state),
                Target = go
            });
        }

        static public void SetActive(Behaviour behaviour, bool state) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.SetActive_Behaviour,
                Arg = new GuiCommandArgument(state),
                Target = behaviour
            });
        }

        static public void SetActive(ActiveGroup group, bool state) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.SetActive_ActiveGroup,
                Arg = new GuiCommandArgument(state),
                Target = group
            });
        }

        static public void TryClick(GameObject go) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.TryClick_GO,
                Target = go
            });
        }

        static public void ForceClick(GameObject go) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ForceClick_GO,
                Target = go
            });
        }

        static public void ExecuteAction(Action action) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_Void,
                Target = action
            });
        }

        static public void ExecuteAction(Action<bool> action, bool arg) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_Bool,
                Arg = new GuiCommandArgument(arg),
                Target = action
            });
        }

        static public void ExecuteAction(Action<int> action, int arg) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_Int,
                Arg = new GuiCommandArgument(arg),
                Target = action
            });
        }

        static public void ExecuteAction(Action<float> action, float arg) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_Float,
                Arg = new GuiCommandArgument(arg),
                Target = action
            });
        }

        static public void ExecuteAction(Action<StringHash32> action, StringHash32 arg) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_StringHash32,
                Arg = new GuiCommandArgument(arg),
                Target = action
            });
        }

        static public void ExecuteAction(Action<object> action, object arg) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.ExecuteAction_Object,
                Target = action,
                ArgObject = arg
            });
        }

        static public void RebuildLayout(RectTransform transform) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.RebuildLayout_RectTransform,
                Target = transform
            });
        }

        static public void RebuildLayoutManually(LayoutGroup group) {
            Game.Gui.QueueCommand(new GuiCommandData() {
                Type = GuiCommandType.RebuildLayoutManually_LayoutGroup,
                Target = group
            });
        }
    }
}