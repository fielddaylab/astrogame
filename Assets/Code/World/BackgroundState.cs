using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

public class BackgroundState : SharedStateComponent, IScenePreload {
    public GameObject DeskPicture;

    IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
        DeskPicture.SetActive(false);
        return null;
    }
}
