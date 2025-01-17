using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using UnityEngine; // Temporary
using Leaf.Runtime;

namespace Astro {
    static public class ProgressTriggers {
        static public readonly StringHash32 PointsUpdated = new StringHash32("PointsUpdated");

        [InvokeOnBoot]
        static public void Init() {
            PointsUtility.OnPointsUpdated.Register(OnScore);
        }

        static private void OnScore() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("SciencePoints", PointsUtility.GetPoints());
                ScriptUtility.Trigger(PointsUpdated, table);
            }
        }
    }
}