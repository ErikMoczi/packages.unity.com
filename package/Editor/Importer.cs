#if UNITY_2018_1_OR_NEWER
using UnityEditor.Analytics;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor
{
    public class StandardEventsImporter : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(Build.Reporting.BuildReport report)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath("dce91326f102345f3ba2f0987c0679c2" /* UnityEngine.StandardEvents.dll */)) as PluginImporter;
            if (importer)
                importer.SetCompatibleWithAnyPlatform(AnalyticsSettings.enabled);
        }
    }
}
#endif // UNITY_2018_1_OR_NEWER