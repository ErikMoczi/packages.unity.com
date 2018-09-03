using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner
{
    [Serializable]
    internal class ApplePlatformSetup : IPlatformSetup
    {
        private readonly BuildTarget m_BuildTarget;

        [SerializeField]
        private bool m_Stripping;

        [SerializeField]
        private string m_DeviceId;

        public ApplePlatformSetup(BuildTarget buildTarget)
        {
            m_BuildTarget = buildTarget;
        }

        public void Setup()
        {
            AvailableDevice device = GetDevice();

            if (!string.IsNullOrEmpty(device.deviceId))
            {
                m_DeviceId = device.deviceId;
                EditorUserBuildSettings.appleDeviceId = m_DeviceId;
            }
            else
            {
                throw new TestLaunchFailedException("Could not find any connected " + m_BuildTarget + " devices");
            }

            var pathToIOSDeploy = Environment.GetEnvironmentVariable("UNITY_PATHTOIOSDEPLOY");
            if (!device.isSimulator || pathToIOSDeploy != null)
            {
                EditorUserBuildSettings.appleBuildAndRunType = AppleBuildAndRunType.iOSDeploy;
            }
            else
                EditorUserBuildSettings.appleBuildAndRunType = AppleBuildAndRunType.Xcode;

            // Camera and fonts are stripped out and app crashes on iOS when test runner is trying to add a scene with... camera and text
            m_Stripping = PlayerSettings.stripEngineCode;
            PlayerSettings.stripEngineCode = false;

            // Hacks to make tests work with UTR/Katana
            var playmodeWithUTR = Environment.GetEnvironmentVariable("UNITY_PLAYMODEWITHUTR") == "1";
            if (playmodeWithUTR)
            {
                var appleDeveloperTeamId = Environment.GetEnvironmentVariable("UNITY_APPLEDEVELOPERTEAMID") ?? "";
                var provisioningProfile = "";
                if (m_BuildTarget == BuildTarget.iOS)
                    provisioningProfile = Environment.GetEnvironmentVariable("UNITY_IOSPROVISIONINGUUID") ?? "";
                else if (m_BuildTarget == BuildTarget.tvOS)
                    provisioningProfile = Environment.GetEnvironmentVariable("UNITY_TVOSPROVISIONINGUUID") ?? "";

                if (provisioningProfile != "")
                {
                    PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                    PlayerSettings.iOS.iOSManualProvisioningProfileID = provisioningProfile;
                }
                else if (appleDeveloperTeamId != "")
                {
                    PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                    PlayerSettings.iOS.appleDeveloperTeamID = appleDeveloperTeamId;
                }

                if (m_BuildTarget == BuildTarget.iOS)
                {
                    PlayerSettings.iOS.targetOSVersionString = "8.0";
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.UnityTesting.Playmode");
                }
                else if (m_BuildTarget == BuildTarget.tvOS)
                {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.tvOS, "com.UnityTesting.Playmode");
                }
            }
        }

        public void PostBuildAction()
        {
            // Restoring player setting as early as possible
            PlayerSettings.stripEngineCode = m_Stripping;
        }

        public void PostSuccessfulBuildAction()
        {
            if (!string.IsNullOrEmpty(m_DeviceId))
            {
                var address = IDeviceUtils.StartPlayerConnectionSupport(m_DeviceId);

                var connectionResult = -1;
                var maxTryCount = 100;
                var tryCount = maxTryCount;
                while (tryCount-- > 0 && connectionResult == -1)
                {
                    Thread.Sleep(1000);
                    connectionResult = EditorConnectionInternal.ConnectPlayerProxy(address.ip, address.port);
                    if (EditorUtility.DisplayCancelableProgressBar("Editor Connection", "Connecting to the player",
                            1 - ((float)tryCount / maxTryCount)))
                    {
                        EditorUtility.ClearProgressBar();
                        throw new TestLaunchFailedException();
                    }
                }
                EditorUtility.ClearProgressBar();
                if (connectionResult == -1)
                    throw new TestLaunchFailedException(
                        "Timed out trying to connect to the player. Player failed to launch or crashed soon after launching");
            }
            else
            {
                throw new TestLaunchFailedException("Could not find any connected " + m_BuildTarget + " devices");
            }
        }

        public void CleanUp()
        {
            if (!string.IsNullOrEmpty(m_DeviceId))
                IDeviceUtils.StopPlayerConnectionSupport(m_DeviceId);

            if (EditorUserBuildSettings.appleBuildAndRunType == AppleBuildAndRunType.iOSDeploy)
                RunAppReturnOutput("/usr/bin/killall", "-9 ios-deploy");

            EditorUserBuildSettings.appleBuildAndRunType = AppleBuildAndRunType.Xcode;
        }

        private class AvailableDevice
        {
            public string deviceId;
            public BuildTarget target;
            public bool isSimulator;
        }

        private AvailableDevice GetDevice()
        {
            var deviceId = Environment.GetEnvironmentVariable(m_BuildTarget == BuildTarget.iOS ? "IOS_DEVICE_ID" : "TVOS_DEVICE_ID") ?? "";

            AvailableDevice[] devices = GetAvailableDevices();
            foreach (var device in devices)
            {
                if (deviceId == device.deviceId)
                    return device;
            }
            foreach (var device in devices)
            {
                if (!device.isSimulator && device.target == m_BuildTarget)
                    return device;
            }
            return null;
        }

        private static AvailableDevice[] GetAvailableDevices()
        {
            Regex systemProfilerRegex = new Regex(@"Serial Number: (?<deviceId>.+)\n");
            Regex instrumentsRegex = new Regex(@"(?<deviceName>.+) \((?<osVersion>\d{1,2}\.\d{1,2}|\d{1,2}\.\d{1,2}\.\d{1,2})\) \[(?<deviceId>.+)\](?<simulatorTail>$| \(Simulator\))");

            string instrumentsOutput = RunAppReturnOutput("/usr/bin/instruments", " -s devices");
            string systemProfilerOutput = RunAppReturnOutput("/usr/sbin/system_profiler", "SPUSBDataType");

            string[] instrumentsSplit = instrumentsOutput.Split('\n');
            string[] systemProfilerSplit = systemProfilerOutput.Split(new string[] {"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

            List<AvailableDevice> devices = new List<AvailableDevice>();

            foreach (var line in instrumentsSplit)
            {
                Match match = instrumentsRegex.Match(line);
                if (match.Success)
                {
                    AvailableDevice device = new AvailableDevice();
                    device.deviceId = match.Groups["deviceId"].Value;
                    device.isSimulator = match.Groups["simulatorTail"].Value != "";
                    if (!device.isSimulator)
                        devices.Add(device);
                }
            }

            // system_profiler is used because instruments doesn't show whether connected device is tvOS or iOS
            for (int i = 0; i < systemProfilerSplit.Length; i++)
            {
                BuildTarget target;
                if (systemProfilerSplit[i].Contains("iPhone") || systemProfilerSplit[i].Contains("iPad") || systemProfilerSplit[i].Contains("iPod"))
                    target = BuildTarget.iOS;
                else if (systemProfilerSplit[i].Contains("TV"))
                    target = BuildTarget.tvOS;
                else
                    continue;

                Match match = systemProfilerRegex.Match(systemProfilerSplit[i + 1]);
                if (match.Success)
                    foreach (var device in devices)
                        if (device.deviceId == match.Groups["deviceId"].Value)
                            device.target = target;
            }

            return devices.ToArray();
        }

        private static string RunAppReturnOutput(string app, string appArguments)
        {
            var start = new ProcessStartInfo(app, appArguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                UseShellExecute = false,
            };

            using (var process = Process.Start(start))
            {
                return process.StandardOutput.ReadToEnd();
            }
        }
    }
}
