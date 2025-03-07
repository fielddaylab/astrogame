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
        public Transform Shaft;
        public Transform Base;
        public Transform Dome;

        [Header("Config")]
        public Vector3 RotationOffset;

        public bool AutoSync = true;

        [NonSerialized] public Vector2 LastAppliedRotation;
    }

    static public partial class TelescopeUtility {
        static public void UpdateTelescopeRigRotation(TelescopeRig rig, Transform spaceCam)
        {
            rig.LastAppliedRotation.x = spaceCam.localEulerAngles.x;
            rig.LastAppliedRotation.y = spaceCam.localEulerAngles.y;

            // up/down (NOTE: space cam up/down is oriented along x, whereas the telescope up/down is oriented along z)
            float xRot = spaceCam.localEulerAngles.x;

            // left/right
            float yRot = spaceCam.localEulerAngles.y;

            var localBase = rig.Base.localEulerAngles;
            localBase.y = yRot + rig.RotationOffset.y;
            rig.Base.localEulerAngles = localBase;

            var localDome = rig.Dome.localEulerAngles;
            localDome.y = yRot + rig.RotationOffset.y;
            rig.Dome.localEulerAngles = localDome;

            var localShaft = rig.Shaft.localEulerAngles;
            localShaft.y = yRot + rig.RotationOffset.y;
            rig.Shaft.localEulerAngles = localShaft;

            localShaft = rig.Shaft.localEulerAngles;
            localShaft.z = xRot + rig.RotationOffset.z;
            rig.Shaft.localEulerAngles = localShaft;
        }
    }
}