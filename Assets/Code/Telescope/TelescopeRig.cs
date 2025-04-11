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
            rig.LastAppliedRotation.x = spaceCam.localEulerAngles.y;
            rig.LastAppliedRotation.y = spaceCam.localEulerAngles.x;

            // up/down (NOTE: space cam up/down is oriented along x, whereas the telescope up/down is oriented along z)
            float xRot = spaceCam.localEulerAngles.x;

            // left/right
            float yRot = spaceCam.localEulerAngles.y;

            var localBase = rig.Base.localEulerAngles;
            localBase.y = yRot + rig.RotationOffset.y;
            rig.Base.localEulerAngles = localBase;

            var localDome = rig.Dome.localEulerAngles;
            localDome.y = yRot + rig.RotationOffset.y; // CalcDomeRotation(rig, yRot) + rig.RotationOffset.y;
            rig.Dome.localEulerAngles = localDome;

            var localShaft = rig.Shaft.localEulerAngles;
            localShaft.y = yRot + rig.RotationOffset.y;
            rig.Shaft.localEulerAngles = localShaft;

            localShaft = rig.Shaft.localEulerAngles;
            localShaft.z = xRot + rig.RotationOffset.z;
            rig.Shaft.localEulerAngles = localShaft;
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
            Vector3 edgePoint = Vector3.zero;

            /*
            // get tan(theta) = slope
            var slope = Mathf.Tan(baseRot * Mathf.Deg2Rad);

            // use slope Ax + By + C = 0 -> y = (-Ax - C) / B
            var a1 = -slope;
            var b1 = 1;
            var c1 = (basePos - domePos).z;

            var EPS = 0.2f;


            double x0 = -a1* c1 / (a1* a1+ b1* b1), y0 = -b1* c1 / (a1* a1+ b1* b1);
            if (c1 * c1 > r * r * (a1* a1+ b1* b1) + EPS)
            {

            }
            else if (Mathf.Abs(c1 * c1 - r * r * (a1* a1+ b1* b1)) < EPS)
            {
                edgePoint = new Vector3((float)x0, 0, (float)y0);
            }
            else
            {
                double d = r * r - c1 * c1 / (a1* a1+ b1* b1);
                double mult = Math.Sqrt(d / (a1* a1+ b1* b1));
                double ax, ay, bx, by;
                ax = x0 + b1 * mult;
                bx = x0 - b1 * mult;
                ay = y0 - a1 * mult;
                by = y0 + a1 * mult;
                edgePoint = new Vector3((float)ax, 0, (float)bx);
            }
            */

            // Vector3 edgePoint = new Vector3((float)x0, 0, (float)y0); // basePos + ; // new Vector3(Mathf.Cos(baseRot * Mathf.Deg2Rad) * r, 0, Mathf.Sin(baseRot * Mathf.Deg2Rad) * r);
            float b = Vector3.Distance(edgePoint, basePos);
            Debug.Log("[Circle math] curr b: " + b);

            // Calculate dome angle using Cosine
            float cosTheta = (Mathf.Pow(r, 2) + Mathf.Pow(a, 2) - Mathf.Pow(b, 2)) / (2 * a * r);
            Debug.Log("[Circle math] curr cos: " + cosTheta);
            float finalYRot = Mathf.Acos(cosTheta) * Mathf.Rad2Deg;
            // if (edgePoint.z < 0) { finalYRot *= -1; }
            Debug.Log("[Circle math] curr arccos: " + Mathf.Acos(cosTheta) * Mathf.Rad2Deg);

            // var finalYRot = Quaternion.LookRotation((edgePoint - domePos).normalized).eulerAngles.y;

            return finalYRot;
        }
    }
}