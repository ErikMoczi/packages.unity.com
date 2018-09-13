using System;
using System.Collections;
using System.IO;

using UnityEditor;

using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.XR.Management.Sample
{
	public class SampleEditorUtilities
	{

        public static string GetStreamingAssetsBuildPathForBuildTarget(BuildTargetGroup targetGroup, bool useTempPath)
        {
            string stagingArea = useTempPath ? Application.temporaryCachePath : SampleConstants.kStagingArea;
            string outputPath;

            switch (targetGroup)
            {
                case BuildTargetGroup.Android:
                    outputPath = Path.Combine(stagingArea, SampleConstants.kAndroidStreamingAssetsFolder);
                    break;

                case BuildTargetGroup.iOS:
                    outputPath = Path.Combine(stagingArea, SampleConstants.kiOSStreamingAssetsFolder);
                    break;

                default:
                    outputPath = Path.Combine(stagingArea, SampleConstants.kDesktopStreamingAssetsFolder);
                    break;
            }

            return outputPath;
        }
	}
}
