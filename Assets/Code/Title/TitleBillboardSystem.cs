using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.Systems;
using UnityEngine;
using BeauUtil;

namespace Astro.Title {
    public sealed class TitleBillboardSystem : ComponentSystemBehaviour<TitleBillboard> {
        public override void ProcessWork(float deltaTime) {
            Camera camera = Game.Rendering.PrimaryCamera;
            Quaternion refCamRot = camera.transform.rotation;
            Vector3 forward = Geom.Forward(refCamRot);
            Vector3 up = Geom.Up(refCamRot);
            Quaternion billboardRot = Quaternion.LookRotation(forward, up);

            foreach(var billboard in m_Components) {
                billboard.CachedTransform.localRotation = billboardRot;
            }
        }
    }
}