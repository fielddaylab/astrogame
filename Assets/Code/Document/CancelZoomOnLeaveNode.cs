using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(ViewNode))]
    public sealed class CancelZoomOnLeaveNode : MonoBehaviour, IScenePreload {
        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            GetComponent<ViewNode>().OnExit.Register(() => {
                DocumentUtility.CancelZoom(Find.State<DocumentBoardState>());
            });
            return null;
        }
    }
}