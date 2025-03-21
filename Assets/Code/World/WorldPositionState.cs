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
    [PreloadOrder(-100)]
    public class WorldPositionState : SharedStateComponent, IScenePreload
    {
        public LatLongCoords GlobePosition;

        public EqCoords StartingLookCoords;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.QueueOnEnable(() => {
                var dayConfig = DayConfigUtil.GetConfigForState();
                var camState = Find.State<SpaceCameraState>();
                var domeState = Find.State<SkyDome>();

                LatLongCoords globe = GlobePosition;
                globe.Longitude -= CoordinateUtility.HmsToDms(dayConfig.SkyRotationOffset);
                
                globe.Latitude = -globe.Latitude;
                globe.Longitude = -globe.Longitude;

                domeState.Rotation = new EqCoords() {
                    RightAscension = CoordinateUtility.DmsToHms(globe.Longitude),
                    Declination = globe.Longitude
                };

                Quaternion skyRot = CoordinateUtility.LatLongRotation(globe.Latitude, globe.Longitude);
                Matrix4x4 skyRotMat = Matrix4x4.Rotate(skyRot);

                Shader.SetGlobalMatrix("_SkyboxRotation", skyRotMat);
                domeState.StarRoot.localRotation = skyRot;

                WorldPositionUtility.TryLook(camState, StartingLookCoords);
            });
            return null;
        }
    }
}