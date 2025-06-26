using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using FieldDay.UI.Animation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Astro.Title {
    public sealed class TitleContinueGameMenu : MonoBehaviour, IScenePreload {
        public PointerListener[] Buttons;
        public FadeGroup MenuFade;
        public FadeGroup CloseFade;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            for(int i = 0; i < Buttons.Length; i++) {
                Buttons[i].UserData = "Day" + (i + 1).ToStringLookup();
                Buttons[i].onClick.AddListener(OnClickContinue);
            }
            return null;
        }

        private void OnClickContinue(PointerEventData pointerData) {
            Find.State<ViewState>().ActiveNode.BackLink = null;

            MenuFade.Hide();
            CloseFade.Hide();

            Game.Input.PauseRaycasts();

            StringHash32 dayId = "Day1";
            if (PointerListener.TryGetUserData(pointerData, out string day)) {
                dayId = day;
            }

            Routine.Start(this, ContinueGameSequence(dayId)).ExecuteWhileDisabled();
        }

        private IEnumerator ContinueGameSequence(StringHash32 dayId) {
            yield return 1;
            ViewNavUtility.MoveToNode(Find.State<ViewState>(), ViewNavUtility.GetNodeById("ContinueForward"), new TweenSettings(5, Curve.Smooth));
            Game.Events.Dispatch(GameEvents.TitleGameStarting);
            // TODO: start playing walking sounds
            yield return 2;
            ScriptTriggers.LoadDay(dayId);
            yield return null;
            Game.Input.ResumeRaycasts();
        }
    }
}