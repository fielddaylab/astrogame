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

                Quaternion camRot = CoordinateUtility.LatLongRotation(globe.Latitude, globe.Longitude);
                camState.HorizonPlane.rotation = camRot;
                domeState.HorizonRoot.rotation = camRot;

                SkyDomeUtility.FilterByHorizon(domeState);

                LatLongCoords counterRot = globe;
                counterRot.Latitude = -counterRot.Latitude;
                counterRot.Longitude = -counterRot.Longitude;
                
                Quaternion skyRot = CoordinateUtility.LatLongRotation(counterRot.Latitude, counterRot.Longitude);
                Matrix4x4 skyRotMat = Matrix4x4.Rotate(skyRot);
                Shader.SetGlobalMatrix("_SkyboxRotation", skyRotMat);

                WorldPositionUtility.LookAt(camState, StartingLookCoords);
                TelescopeUtility.SuppressTelescopeRigAudio(false);

                WavelengthToggleState wavelengthState = Find.State<WavelengthToggleState>();
                WavelengthToggleUtility.SetMask(wavelengthState, CelestialObjectVisMask.Visible, true);
            });
            return null;
        }
    }
}