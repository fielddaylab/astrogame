using System.Collections;
using System.Collections.Generic;
using Astro.Save;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.HID;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using FieldDay.UI.Animation;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Astro.Title {
    public sealed class TitleNewGameMenu : MonoBehaviour, IScenePreload {
        public CursorHint ParentCursor;
        [SerializeField] private TMP_InputField m_PlayerCodeInput;
        [SerializeField] private Button m_BeginButton;
        public PointerListener BeginButtonListener;
        public FadeGroup MenuFade;
        public FadeGroup CloseFade;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            PlayerKnowledgeUtility.ResetAll();
            BeginButtonListener.onClick.AddListener(OnClickBegin);
            ParentCursor.onClick.AddListener(OnEnterNewGameMenu);
            return null;
        }

        private void OnEnterNewGameMenu() {
            OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
        }

        #region OGD

        private void HandleNewPlayerId(string id)
        {
            m_PlayerCodeInput.SetTextWithoutNotify(id);
            m_BeginButton.interactable = true;
            HandlePlayerCodeUpdated(id);
        }

        private void HandleNewPlayerIdError(OGD.Core.Error err)
        {
            OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
        }

        private void HandlePlayerCodeUpdated(string text)
        {
            m_BeginButton.interactable = text.Length > 1;
        }

        private void HandleClaimNewIdSuccess()
        {
            Game.SharedState.Get<UserSettingsState>().PlayerCode = m_PlayerCodeInput.text;

            NewGameBegin();
        }

        private void HandleClaimNewIdError(OGD.Core.Error err)
        {
            Debug.LogError(err.ToString());
        }

        #endregion // OGD

        private void OnClickBegin()
        {
            AstroGame.SaveBuffer.Clear();
            OGD.Player.ClaimId(m_PlayerCodeInput.text, null, HandleClaimNewIdSuccess, HandleClaimNewIdError);
        }

        public void NewGameBegin(bool ignoreSave = false)
        {
            Find.State<ViewState>().ActiveNode.BackLink = null;

            MenuFade.Hide();
            CloseFade.Hide();

            foreach (var comp in Find.Components<DisableDuringPrologue>())
            {
                GuiCommands.SetActive(comp.gameObject, false);
            }

            if (!ignoreSave) {
                SaveUtility.Save(SaveSlot.Main);
            }

            Routine.Start(this, NewGameBeginSequence()).ExecuteWhileDisabled();
        }

        private IEnumerator NewGameBeginSequence() {
            Find.GuiModule<LoadingIcon>().Show();
            while(Game.Scenes.IsLoadingAnyScene()) {
                yield return null;
            }
            Find.GuiModule<LoadingIcon>().Hide();

            Game.Events.Dispatch(GameEvents.TitleGameStarting);
            ScriptUtility.Trigger("BeginPrelude");

            Routine.StartDelay(() => {
                Game.Scenes.UnloadScenesByTag("Menu");
            }, 1);
        }
    }
}