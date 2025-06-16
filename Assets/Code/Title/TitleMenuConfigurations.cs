using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using System.Collections.Generic;

namespace Astro.Title {
    public sealed class TitleMenuConfigurations : SharedStateComponent, IRegistrationCallbacks, IScenePreload {
        public FadeGroup TitleGroup;
        public FadeGroup NewGroup;
        public FadeGroup ContinueGroup;
        public FadeGroup InProgressGroup;
        public FadeGroup BackGroup;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            TitleGroup.SetVisibleNow(false);
            yield return null;
            NewGroup.SetVisibleNow(false);
            yield return null;
            InProgressGroup.SetVisibleNow(false);
            yield return null;
            BackGroup.SetVisibleNow(false);
            yield return null;

            AstroGame.Events.Register<ViewNode>(ViewNavUtility.Events.NodeExited, OnNodeExited)
                .Register<ViewNode>(ViewNavUtility.Events.NodeLoaded, OnNodeLoading)
                .Register<ViewNode>(ViewNavUtility.Events.NodeEntered, OnNodeEntered);
        }

        private void OnNodeExited(ViewNode node) {
            TitleGroup.Hide();
            NewGroup.Hide();
            ContinueGroup.Hide();
            InProgressGroup.Hide();
            BackGroup.Hide();
        }

        private void OnNodeEntered(ViewNode node) {
            StringHash32 nodeId = node.Id;
            
            if (nodeId == "NewCutscene" || nodeId == "ContinueForward") {
                return;
            }

            if (nodeId == "Title") {
                TitleGroup.Show();
            } else if (nodeId == "New") {
                NewGroup.Show();
            } else if (nodeId == "Continue") {
                ContinueGroup.Show();
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