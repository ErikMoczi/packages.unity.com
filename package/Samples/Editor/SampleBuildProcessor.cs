using System;
using System.IO;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace UnityEditor.XR.Management.Sample
{
    class SampleBuildProcessor : IPreprocessBuildWithReport
    {

        public int callbackOrder
        {
            get { return 0;  }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTargetGroup targetGroup = report.summary.platformGroup;
            SerializeSettingsForBuildTargetGroup(targetGroup);
        }

        public void SerializeSettingsForBuildTargetGroup(BuildTargetGroup targetGroup, bool useTempPath = false)
        {
            var outputPath = SampleEditorUtilities.GetStreamingAssetsBuildPathForBuildTarget(targetGroup, useTempPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            SerializeProviderSettings(targetGroup, outputPath);
        }

        private void SerializeProviderSettings(BuildTargetGroup targetGroup, string outputPath)
        {
            SampleSettings settings = null;

            EditorBuildSettings.TryGetConfigObject(SampleConstants.kSettingsKey, out settings);
            if (settings == null)
                return;

            string filename = SampleUtilities.GetSerializationFilename("SampleData", outputPath);
            SampleUtilities.WriteSettings(settings, filename);
        }
    }
}
