using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay.SharedState;
using System;
using FieldDay.Scenes;
using BeauUtil;
using FieldDay;

namespace Astro
{
    public class WorldPositionState : SharedStateComponent, IScenePreload
    {
        public EqCoords StartingLookCoords;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.QueueOnEnable(() => {
                var camState = Find.State<SpaceCameraState>();
                WorldPositionUtility.TryLook(camState.HorizonPlane, camState.Camera.RootTransform, StartingLookCoords);
            });
            return null;
        }
    }
}