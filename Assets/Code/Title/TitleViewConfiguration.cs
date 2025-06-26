using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System.Collections.Generic;

namespace Astro.Title {
    [PreloadOrder(100)]
    public sealed class TitleViewConfiguration : BatchedComponent, IScenePreload {
        public SerializedHash32 NodeId;
        public ActiveGroup Group;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ViewNode node = ViewNavUtility.GetNodeById(NodeId);
            Assert.NotNullOrDestroyed(node);
            node.OnLoad.Register(OnNodeLoad);
            node.OnUnload.Register(OnNodeUnload);

            Group.SetActive(false);
            return null;
        }

        private void OnNodeLoad() {
            Group.SetActive(true);
        }

        private void OnNodeUnload() {
            if (Find.State<TitleState>().LockNodeChanges) {
                return;
            }

            Group.SetActive(false);
        }
    }
}