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
using UnityEngine.UI;

namespace Astro.Title {
    public sealed class TitleNewGameMenu : MonoBehaviour, IScenePreload {
        public PointerListener BeginButton;
        public FadeGroup MenuFade;
        public FadeGroup CloseFade;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            BeginButton.onClick.AddListener(OnClickBegin);
            return null;
        }

        private void OnClickBegin() {
            Find.State<ViewState>().ActiveNode.BackLink = null;

            MenuFade.Hide();
            CloseFade.Hide();

            foreach(var comp in Find.Components<DisableDuringPrologue>()) {
                GuiCommands.SetActive(comp.gameObject, false);
            }

            Routine.Start(this, NewGameBeginSequence()).ExecuteWhileDisabled();
        }

        private IEnumerator NewGameBeginSequence() {
            Find.GuiModule<LoadingIcon>().Show();
            while(Game.Scenes.IsLoadingAnyScene()) {
                yield return null;
            }
            Find.GuiModule<LoadingIcon>().Hide();

            ScriptUtility.Trigger("BeginPrelude");

            Routine.StartDelay(() => {
                Game.Scenes.UnloadScenesByTag("Menu");
            }, 1);
        }
    }
}