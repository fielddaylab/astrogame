using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(PointerListener))]
    public sealed class ViewBackButton : MonoBehaviour, IScenePreload {
        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            GetComponent<PointerListener>().onClick.Register(HandleClick);
            return null;
        }

        private void HandleClick() {
            ViewState view = Find.State<ViewState>();
            if (!view.ActiveNode || view.ActiveTransitionRoutine || !view.ActiveNode.BackLink) {
                return;
            }
            ViewNavUtility.MoveByLink(view, view.ActiveNode.BackLink);
        }
    }
}