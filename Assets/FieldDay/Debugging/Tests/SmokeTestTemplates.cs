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
            WildcardMatch includeMatch = WildcardMatch.Compile(includePattern);
            WildcardMatch excludeMatch = WildcardMatch.Compile(excludePattern);
            foreach (var scene in SceneHelper.FindScenes(SceneCategories.Build)) {
                if (!includeMatch.Match(scene.Name)) {
                    continue;
                }

                if (!excludeMatch.IsEmpty && excludeMatch.Match(scene.Name)) {
                    continue;
                }

                yield return context.LoadMainScene(scene);
                yield return waitDuration;
            }
        }

        #endregion // Scene Loads
    }
}