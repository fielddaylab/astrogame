using System.Collections;
using System.Collections.Generic;
using Astro.Save;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.HID;
using FieldDay.Scenes;
using FieldDay.UI.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Astro.Title {
    public sealed class TitleContinueGameMenu : MonoBehaviour, IScenePreload {
        public CursorHint ParentCursor;
        [SerializeField] private TMP_InputField m_PlayerCodeInput;
        public Button ContinueButton;
        public PointerListener ContinueButtonListener;
        public FadeGroup MenuFade;
        public FadeGroup CloseFade;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            ContinueButtonListener.onClick.AddListener(OnClickContinue);
            ParentCursor.onClick.AddListener(OnEnterContinueGameMenu);
            m_PlayerCodeInput.onValueChanged.AddListener(HandlePlayerCodeUpdated);
            return null;
        }

        private void OnEnterContinueGameMenu()
        {
            m_PlayerCodeInput.SetTextWithoutNotify(Game.SharedState.Get<UserSettingsState>().PlayerCode);
        }

        private void OnClickContinue(PointerEventData pointerData) {
            Future f = SaveUtility.LoadFromServer(m_PlayerCodeInput.text);
            f.OnComplete(() => { BeginContinueGame(pointerData); });
            f.OnFail(HandleLoadError);
        }

        private void HandleLoadError()
        {
            Debug.LogError("load from server failed");
        }

        private void HandlePlayerCodeUpdated(string text)
        {
            ContinueButton.interactable = text.Length > 1;
        }

        private void BeginContinueGame(PointerEventData pointerData)
        {
            Find.State<ViewState>().ActiveNode.BackLink = null;

            MenuFade.Hide();
            CloseFade.Hide();

            Game.Input.PauseRaycasts();

            StringHash32 dayId = "Day1";
            if (PointerListener.TryGetUserData(pointerData, out string day))
            {
                dayId = day;
            }

            Routine.Start(this, ContinueGameSequence(dayId)).ExecuteWhileDisabled();
        }

        private IEnumerator ContinueGameSequence(StringHash32 dayId) {
            yield return 1;
            ViewNavUtility.MoveToNode(Find.State<ViewState>(), ViewNavUtility.GetNodeById("ContinueForward"), new TweenSettings(5, Curve.Smooth));
            // TODO: start playing walking sounds
            yield return 2;
            ScriptTriggers.LoadDay(dayId);
            yield return null;
            Game.Input.ResumeRaycasts();
        }
    }
}