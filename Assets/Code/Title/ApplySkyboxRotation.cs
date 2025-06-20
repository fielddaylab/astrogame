using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astro.Title {
    public sealed class ApplySkyboxRotation : MonoBehaviour, IScenePreload {
        public LatLongCoords GlobePosition;
        public HmsCoords TimeOffset;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.QueueOnEnable(ApplyChanges);
            return null;
        }

        public void ApplyChanges() {
            LatLongCoords globe = GlobePosition;
            globe.Longitude -= CoordinateUtility.HmsToDms(TimeOffset);

            LatLongCoords counterRot = globe;
            counterRot.Latitude = -counterRot.Latitude;
            counterRot.Longitude = -counterRot.Longitude;

            Quaternion skyRot = CoordinateUtility.LatLongRotation(counterRot.Latitude, counterRot.Longitude);
            Matrix4x4 skyRotMat = Matrix4x4.Rotate(skyRot);
            Shader.SetGlobalMatrix("_SkyboxRotation", skyRotMat);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Frame.IsActive(this)) {
                return;
            }

            if (Application.isPlaying && Game.Scenes.IsLoadingAnyScene()) {
                return;
            }

            ApplyChanges();
        }
#endif // UNITY_EDITOR
    }
}