using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
#if PLATFORM_LUMIN
using UnityEditor.Lumin;
#endif // PLATFORM_LUMIN
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.XR.MagicLeap
{
#if PLATFORM_LUMIN
    public class MLDashboard : EditorWindow
    {
        private IMGUIContainer _remoteChecksUi;
        private VisualElement _mainVisualContainer;

        private string[] _availablePackages = new string[] {};

        private void OnDisable()
        {
        }

        private void OnEnable()
        {
            _remoteChecksUi = new IMGUIContainer(OnRemoteChecksUI);
            _mainVisualContainer = new VisualElement()
            {
                name = "MainVisualContainer"
            };
            _mainVisualContainer.Add(_remoteChecksUi);
            var root = this.rootVisualElement;
            root.Add(_mainVisualContainer);

            _availablePackages = MagicLeapPackageLocator.GetUnityPackages().ToArray();
        }

        private void OnRemoteChecksUI()
        {
            GUILayout.Label("MagicLeap Remote Requirements", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Rendering API:");
                GUILayout.Label(SystemInfo.graphicsDeviceType.ToString(), GUI.skin.textField);
                using (new EditorGUI.DisabledScope(!NeedToSwitchToGLCore))
                {
                    if (GUILayout.Button("Restart w/ OpenGL"))
                    {
                        if (EditorUtility.DisplayDialog("Editor Restart Required",
                            string.Format(
                                "To use Magic Leap zero iteration mode in the editor, the editor must restart using OpenGL."),
                            "Restart", "Do Not Restart"))
                        {
                            Restart("-force-glcore");
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                var present = HasVirtualDevice;
                using (new EditorGUI.DisabledScope(!present))
                {
                    if (GUILayout.Button("Launch ML Remote"))
                    {
                        if (!IsRemoteAlreadyRunning)
                            LaunchRemote();
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import MagicLeap unitypackage"))
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    var versions = new GenericMenu();
                    foreach (var pkg in _availablePackages)
                    {
                        versions.AddItem(new GUIContent(pkg), false, InstallPackage, pkg);
                    }
                    // show options as a drop down.
                    versions.DropDown(rect);
                }

            }
        }

        //[MenuItem("Window/XR/MagicLeap Dashboard", false, 1)]
        private static void Display()
        {
            // Get existing open window or if none, make a new one:
            EditorWindow.GetWindow<MLDashboard>(false, "ML Dashboard").Show();
        }

        private void InstallPackage(object p)
        {
            var path = p as string;
            UnityEngine.Debug.LogFormat("Importing: {0}", path);
            AssetDatabase.ImportPackage(path, true);
        }

        private static void LaunchProcess(string filename, string args = "")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = args,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }

        [MenuItem("Magic Leap/ML Remote/Launch MLRemote")]
        private static void LaunchRemote()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                // MacOSX - Environment Setup
                //LaunchProcess("bash", Path.Combine(SDKPath, "envsetup.sh"));
                //LaunchProcess("bash", Path.Combine(VirtualDevicePath, "mlvdsetup.sh"));
                // MacOSX - MLRemote
                LaunchProcess("bash", Path.Combine(VirtualDevicePath, "MLRemote.sh"));
            }
            else
            {
                // Windows - Environment Setup
                //LaunchProcess(Path.Combine(SDKPath, "envsetup.bat"));
                //LaunchProcess(Path.Combine(VirtualDevicePath, "mlvdsetup.bat"));
                // Windows - MLRemote
                LaunchProcess(Path.Combine(VirtualDevicePath, "MLRemote.bat"));
            }
        }

        [MenuItem("Magic Leap/Lauch MLRemote", true)]
        private static bool CanLaunchRemote()
        {
            return HasVirtualDevice;
        }

        private static string SdkPath
        {
            get { return SDK.Find(true).Path; }
        }

        private static string VirtualDevicePath
        {
            get { return Path.Combine(SdkPath, "VirtualDevice"); }
        }

        private static bool HasVirtualDevice
        {
            get
            {
                try
                {
                    var sdk = SDK.Find(false);
                    return (sdk != null) ? sdk.HasMLRemote : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private static void Restart(params string[] args)
        {
            EditorApplication.OpenProject(ProjectPath, args);
        }

        private static string ProjectPath
        {
            get { return Path.GetDirectoryName(Application.dataPath); }
        }

        private static bool NeedToSwitchToGLCore
        {
            get { return SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore; }
        }

        private static bool IsRemoteAlreadyRunning
        {
            get
            {
                var activeProcesses = Process.GetProcessesByName("MLRemote");
                return activeProcesses.Length > 0;
            }
        }
    }
#endif // PLATFORM_LUMIN

    internal static class MagicLeapPackageLocator
    {
        public static IEnumerable<string> GetUnityPackages()
        {
            var tools = Path.Combine(MagicLeapRoot, "tools");
            return new DirectoryInfo(tools).GetFiles("*.unitypackage", SearchOption.AllDirectories).Select(fi => fi.FullName);
        }

        private static string HomeFolder
        {
            get
            {
                var home = Environment.GetEnvironmentVariable("USERPROFILE");
                return (string.IsNullOrEmpty(home))
                    ? Environment.GetEnvironmentVariable("HOME")
                    : home;
            }
        }

        public static string MagicLeapRoot { get { return Path.Combine(HomeFolder, "MagicLeap"); } }
    }
}