using System;
using BeauRoutine;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class TelescopeRig : SharedStateComponent {
        [Header("Components")]
        public Transform Shaft;
        public Transform Base;
        public Transform Dome;

        [Header("Config")]
        public Vector3 RotationOffset;

        [NonSerialized] public Vector2 LastAppliedRotation;

        private void Awake() {
            float ra = 45;
            float dec = 25;
            TelescopeUtility.UpdateTelescopeRigRotation(this, ra, dec);

            Routine.StartLoop(this, () => {
                bool updated = false;
                if (DebugInput.IsDown(DebugInputButtons.DPadLeft)) {
                    ra -= 30 * Frame.DeltaTime;
                    updated = true;
                }
                if (DebugInput.IsDown(DebugInputButtons.DPadRight)) {
                    ra += 30 * Frame.DeltaTime;
                    updated = true;
                }
                if (DebugInput.IsDown(DebugInputButtons.DPadUp)) {
                    dec += 15 * Frame.DeltaTime;
                    updated = true;
                }
                if (DebugInput.IsDown(DebugInputButtons.DPadDown)) {
                    dec -= 15 * Frame.DeltaTime;
                    updated = true;
                }

                if (updated) {
                    TelescopeUtility.UpdateTelescopeRigRotation(this, ra, dec);
                }
            });
        }
    }

    static public partial class TelescopeUtility {
        static public void UpdateTelescopeRigRotation(TelescopeRig rig, float raDeg, float decDeg) {
            rig.LastAppliedRotation.x = raDeg;
            rig.LastAppliedRotation.y = decDeg;

            Vector3 baseRot = rig.RotationOffset;
            baseRot.y += raDeg;

            rig.Base.localEulerAngles = rig.Dome.localEulerAngles = baseRot;

            Vector3 bodyRot = baseRot;
            bodyRot.z += -decDeg;

            rig.Shaft.localEulerAngles = bodyRot;
        }
    }
}