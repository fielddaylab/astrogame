using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace Astro {
    public sealed class ViewDynamicBack : BatchedComponent, IScenePreload {
        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            GetComponent<ViewNode>().OnTransitionQueued.Register(OnTransitionQueued);
            return null;
        }

        static private void OnTransitionQueued(ViewTransitionArgs args) {
            args.Target.BackLink.TargetNode = args.Start;
            args.Target.BackLink.TargetNodeId = args.Start.Id;
        }
    }
}