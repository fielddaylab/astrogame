using System;
using BeauRoutine;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using Leaf.Runtime;
using UnityEngine;

namespace Astro {
    public sealed class TelescopeRig : SharedStateComponent {
        [Header("Components")]
        public Transform Aperture;
        public Transform Shaft;
        public Transform Base;
        public Transform Dome;

        [Header("Config")]
        public Vector3 RotationOffset;
        public Vector3 DomeRotationOffset;

        public bool AutoSync = true;

        [NonSerialized] public Vector2 LastAppliedRotation;
    }

    static public partial class TelescopeUtility {
        static public void UpdateTelescopeRigRotation(TelescopeRig rig, Transform spaceCam)
        {
            var prevX = rig.LastAppliedRotation.x;
            var prevY = rig.LastAppliedRotation.y;

            rig.LastAppliedRotation.x = spaceCam.localEulerAngles.y;
            rig.LastAppliedRotation.y = spaceCam.localEulerAngles.x;

            // up/down (NOTE: space cam up/down is oriented along x, whereas the telescope up/down is oriented along z)
            float xRot = spaceCam.localEulerAngles.x;

            if (prevX < rig.LastAppliedRotation.x) {
                AstroGame.Events.Dispatch(GameEvents.TelescopeTurned, "right");
            }
            else if (prevX > rig.LastAppliedRotation.x)
            {
                AstroGame.Events.Dispatch(GameEvents.TelescopeTurned, "left");
            }

            // left/right
            float yRot = spaceCam.localEulerAngles.y;

            var localBase = rig.Base.localEulerAngles;
            localBase.y = yRot + rig.RotationOffset.y;
            rig.Base.localEulerAngles = localBase;

            var localDome = rig.Dome.localEulerAngles;
            localDome.y = -CalcDomeRotation(rig, yRot);
            rig.Dome.localEulerAngles = localDome;

            var localShaft = rig.Shaft.localEulerAngles;
            localShaft.y = yRot + rig.RotationOffset.y;
            rig.Shaft.localEulerAngles = localShaft;

            localShaft = rig.Shaft.localEulerAngles;
            localShaft.z = xRot + rig.RotationOffset.z;
            rig.Shaft.localEulerAngles = localShaft;

            if (prevY < rig.LastAppliedRotation.y) {
                AstroGame.Events.Dispatch(GameEvents.TelescopeTurned, "down");
            }
            else if (prevY > rig.LastAppliedRotation.y)
            {
                AstroGame.Events.Dispatch(GameEvents.TelescopeTurned, "up");
            }
        }

        static public void SuppressTelescopeRigAudio(bool syncTelescopeRotation) {
            TelescopeRigAudio audio = Find.State<TelescopeRigAudio>();
            TelescopeRig rig = Find.State<TelescopeRig>();

            SpaceCameraState cam = Find.State<SpaceCameraState>();
            var spaceCam = cam.Camera.RootTransform;

            if (syncTelescopeRotation) {
                UpdateTelescopeRigRotation(rig, spaceCam);
            } else {
                rig.LastAppliedRotation.x = spaceCam.localEulerAngles.y;
                rig.LastAppliedRotation.y = spaceCam.localEulerAngles.x;
                cam.LookUpdatedThisFrame = true;
            }

            audio.LastKnownRotation = rig.LastAppliedRotation;
        }

        static private float CalcDomeRotation(TelescopeRig rig, float baseRot)
        {
            // Since the telescope is offset from the center of the circle,
            // the dome rotation must point to where telescope view intersects with the dome edge.

            // Calculate r = dome radius
            float r = 17;

            // Calculate a = distance of telescope from center of dome (on x and z axis only)
            var domePos = rig.Dome.position;
            domePos.y = 0;
            var basePos = rig.Base.position;
            basePos.y = 0;
            float a = Vector3.Distance(domePos, basePos);

            // Calculate b = distance from telescope to edge of dome
            // Use of Law of Sines SSA
            var sinC = Mathf.Sin(baseRot * Mathf.Deg2Rad) / r * a;
            var cTheta = Mathf.Asin(sinC) * Mathf.Rad2Deg;

            var targetTheta = 180 + cTheta - baseRot;
            var finalYRot = targetTheta;

            return finalYRot;
        }
    }
}