using System.Collections;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay.Scenes;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Common scripting methods.
    /// </summary>
    static internal class ScriptCommands {
        #region Events

        [LeafMember("DispatchEvent")]
        static internal void LeafDispatchEvent(StringHash32 eventId) {
            Game.Events.Dispatch(eventId);
        }

        [LeafMember("QueueEvent")]
        static internal void LeafQueueEvent(StringHash32 eventId) {
            Game.Events.Queue(eventId);
        }

        #endregion // Events

        #region Signals

        [LeafMember("Signal")]
        static internal void LeafDispatchSignal(StringHash32 eventId, Variant argument = default) {
            ScriptUtility.DispatchSignal(eventId, argument);
        }

        [LeafMember("QueueSignal")]
        static internal void LeafQueueSignal(StringHash32 eventId, Variant argument = default) {
            ScriptUtility.QueueSignal(eventId, argument);
        }

        #endregion // Signals

        #region Input

        [LeafMember("InputPushPause")]
        static internal void LeafPushInputPause() {
            Game.Input.PauseRaycasts();
            Game.Input.PauseDevices();
        }

        [LeafMember("InputPopPause")]
        static internal void LeafPopInputPause() {
            Game.Input.ResumeRaycasts();
            Game.Input.ResumeDevices();
        }

        #endregion // Input

        #region Scene Loading

        [LeafMember("TransitionToScene")]
        static internal void LeafLoadScene([BindThread] ScriptThread thread, string sceneName, StringHash32 transitionType = default) {
            SceneReference sceneRef = SceneUtils.GetSceneByName(sceneName);
            Assert.True(sceneRef.IsValid, "No scene with name '{0}'", sceneName);
            Game.Scenes.LoadMainScene(sceneRef, true, new MainSceneTransitionArgs() {
                TransitionType = transitionType
            });
        }

        #endregion // Scene Loading
    }
}