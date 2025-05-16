using System.Collections;
using BeauUtil;

namespace FieldDay.Debugging {
    static public partial class SmokeTestTemplates {
        #region Scene Loads

        static public SmokeTestData CanLoadIntoScenes(float waitDuration, string includePattern = null, string excludePattern = null) {
            SmokeTestData test = default;
            test.Name = "CanLoadIntoScenes";
            test.TimeOut = 120;
            test.ExecuteAsync = (c) => CanLoadIntoScenes_Execute(c, includePattern, excludePattern, waitDuration);
            return test;
        }

        static private IEnumerator CanLoadIntoScenes_Execute(ISmokeTestContext context, string includePattern, string excludePattern, float waitDuration) {
            return null;
        }

        #endregion // Scene Loads
    }
}