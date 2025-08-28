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
using Unity.Collections.LowLevel.Unsafe;
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
        public FadeGroup GlobalFade;

        public TitleNewGameMenu NewGameMenu;
        public TitleStarButton NewGameTitleStarButton;
        public TitleStarButton ContinueGameTitleStarButton;

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
            Debug.LogError("[SaveUtility] load from server failed");
        }

        private void HandlePlayerCodeUpdated(string text)
        {
            ContinueButton.interactable = text.Length > 1;
        }

        private void BeginContinueGame(PointerEventData pointerData)
        {
            if (AstroGame.SaveBuffer.HasSave) {
                AstroGame.SaveBuffer.HandleChunks();
            }

            var progressState = Find.State<PlayerProgressState>();

            Find.State<ViewState>().ActiveNode.BackLink = null;

            MenuFade.Hide();
            CloseFade.Hide();

            if (progressState.CompletedPrelude)
            {
                StringHash32 dayId = "Day" + (progressState.DayIndex + 1);

                Routine.Start(this, ContinueGameSequence(dayId)).ExecuteWhileDisabled();
            }
            else {
                // move to prelude scene
                Routine.Start(this, ContinueToPreludeSequence()).ExecuteWhileDisabled();
            }
        }

        private IEnumerator ContinueToPreludeSequence()
        {
            var viewState = Find.State<ViewState>();

            GlobalFade.Show();

            while (GlobalFade.IsTransitioning()) {
                yield return null;
            }

            ViewNavUtility.MoveByLink(viewState, NewGameTitleStarButton.Link, true);

            while (viewState.ActiveTransitionRoutine.Exists()) {
                yield return null;
            }

            NewGameMenu.NewGameBegin(true);

            GlobalFade.Hide();
        }

        private IEnumerator ContinueGameSequence(StringHash32 dayId) {
            yield return 1;
            ViewNavUtility.MoveToNode(Find.State<ViewState>(), ViewNavUtility.GetNodeById("ContinueForward"), new TweenSettings(5, Curve.Smooth));
            AstroGame.Events.Dispatch(GameEvents.TitleGameStarting, Find.State<UserSettingsState>().PlayerCode);

            AstroGame.Events.Dispatch(GameEvents.GameStart, true);

            // TODO: start playing walking sounds
            yield return 2;
            ScriptTriggers.LoadDay(dayId, default, true);
            yield return null;
            if (Game.Input.AreRaycastsPaused()) {
                Game.Input.ResumeRaycasts();
            }
        }
    }
}