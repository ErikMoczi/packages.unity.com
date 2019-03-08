using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Lumin;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.XR.MagicLeap
{
    public class MLRemoteWindow : EditorWindow
    {
        private static MLRemoteWindow _instance = null;

        private IMGUIContainer _remoteChecksUi;
        private VisualElement _mainVisualContainer;

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
            var root = this.GetRootVisualContainer();
            root.Add(_mainVisualContainer);
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
        }

        [MenuItem("Window/XR/MagicLeap Remote", false, 1)]
        private static void Display()
        {
            // Get existing open window or if none, make a new one:
            EditorWindow.GetWindow<MLRemoteWindow>(false, "ML Remote").Show();
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
            get { return SDK.Find(true).HasMLRemote; }
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
}