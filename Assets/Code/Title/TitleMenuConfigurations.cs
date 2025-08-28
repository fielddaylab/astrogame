using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.UI;
using FieldDay.UI.Animation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astro.Title {
    public sealed class TitleMenuConfigurations : SharedStateComponent, IRegistrationCallbacks, IScenePreload {
        public FadeGroup TitleGroup;
        public FadeGroup NewGroup;
        public FadeGroup ContinueGroup;
        public FadeGroup InProgressGroup;
        public FadeGroup BackGroup;
        public FadeGroup OptionsGroup;

        [Header("Loading")]
        public SceneReference UnloadScene;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.UnloadScene(UnloadScene);
            yield return null;

            GameLoop.ResumeUpdates(Bits.All32);
            GameLoop.SuspendUpdates(AstroGame.PauseUpdateMask);

            TitleGroup.SetVisibleNow(false);
            yield return null;
            NewGroup.SetVisibleNow(false);
            yield return null;
            InProgressGroup.SetVisibleNow(false);
            yield return null;
            OptionsGroup.SetVisibleNow(false);
            yield return null;
            BackGroup.SetVisibleNow(false);
            yield return null;

            AstroGame.Events.Register<ViewNode>(ViewNavUtility.Events.NodeExited, OnNodeExited)
                .Register<ViewNode>(ViewNavUtility.Events.NodeLoaded, OnNodeLoading)
                .Register<ViewNode>(ViewNavUtility.Events.NodeEntered, OnNodeEntered);

            ViewState viewState = Find.State<ViewState>();
            ViewNavUtility.SnapToNode(viewState, ViewNavUtility.GetNodeById("Boot"));
            Game.Scenes.QueueOnEnable(HandleInitialMenuState);
        }

        private void HandleInitialMenuState() {
            ViewState viewState = Find.State<ViewState>();
            if (Game.Scenes.GetPreviousMainSceneIndex() > 1) {
                ViewNavUtility.SnapToNode(viewState, ViewNavUtility.GetNodeById("Title"));
            } else {
                Game.Scenes.QueueOnLoad(() => {
                    ViewNavUtility.MoveToNode(viewState, ViewNavUtility.GetNodeById("Title"), new TweenSettings(2, Curve.Smooth));
                });
            }
        }

        private void OnNodeExited(ViewNode node) {
            TitleGroup.Hide();
            NewGroup.Hide();
            ContinueGroup.Hide();
            InProgressGroup.Hide();
            OptionsGroup.Hide();
            BackGroup.Hide();
        }

        private void OnNodeEntered(ViewNode node) {
            StringHash32 nodeId = node.Id;

            if (Find.State<TitleState>().LockNodeChanges) {
                return;
            }

            if (nodeId == "Boot") {
                return;
            }

            if (nodeId == "Title") {
                TitleGroup.Show();
            } else if (nodeId == "New") {
                AstroGame.Events.Dispatch(GameEvents.TitleNewGameClicked);
                NewGroup.Show();
            } else if (nodeId == "Continue") {
                AstroGame.Events.Dispatch(GameEvents.TitleContinueGameClicked);
                ContinueGroup.Show();
            } else if (nodeId == "Options") {
                AstroGame.Events.Dispatch(GameEvents.TitleOptionsClicked);
                OptionsGroup.Show();
            } else {
                InProgressGroup.Show();
            }

            if (node.BackLink) {
                BackGroup.Show();
            }
        }

        private void OnNodeLoading(ViewNode node) {
            // TODO: begin loading some stuff
        }

        void IRegistrationCallbacks.OnRegister() {
        }

        void IRegistrationCallbacks.OnDeregister() {
            Game.Events.DeregisterAllForContext(this);
        }
    }
}