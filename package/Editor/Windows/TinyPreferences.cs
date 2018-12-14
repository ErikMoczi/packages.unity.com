using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class TinyPreferences : SettingsProvider
    {
        public TinyPreferences(string path, SettingsScope scopes = SettingsScope.User) 
            : base(path, scopes)
        {
        }

        [SettingsProvider]
        public static SettingsProvider Provider()
        {
            return new TinyPreferences("Preferences/Tiny Unity"){ label = "Tiny Preferences" };
        }
        
        private static string DefaultMonoPath()
        {
            return Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin"));
        }

        public static string Default7zPath()
        {
            var path = Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "Tools/7z"));
#if UNITY_EDITOR_WIN
            path += ".exe";
#else
            path += "a";
#endif
            return path;
        }

        private class ProgramPath
        {
            private bool m_Initialized;
            private string m_Name, m_Key, m_Value, m_DefaultValue, m_VersionCommand, m_Version, m_DefaultVersion;

            public string Name => m_Name;

            public string Value
            {
                get { Initialize(); return m_Value ?? m_DefaultValue; }
                set
                {
                    Initialize();
                    
                    if (string.Equals(value, m_Value, StringComparison.Ordinal))
                    {
                        return;
                    }
                    
                    if (string.IsNullOrEmpty(value) || 
                        string.Equals(value, m_DefaultValue, StringComparison.Ordinal))
                    {
                        Reset();
                        return;
                    }

                    if (TryGetVersion(value, m_VersionCommand, out m_Version))
                    {
                        m_Value = value;
                        EditorPrefs.SetString(m_Key, m_Value);
                    }
                    else
                    {
                        Reset();
                    }
                }
            }

            public string Version => m_Version ?? m_DefaultVersion;

            public ProgramPath(string name, string key, string versionCommand, string defaultValue)
            {
                m_Name = name;
                m_Key = key;
                m_DefaultValue = defaultValue;
                m_VersionCommand = versionCommand;
            }

            private void Initialize()
            {
                if (m_Initialized)
                    return;
                
                if (!TryGetVersion(m_DefaultValue, m_VersionCommand, out m_DefaultVersion))
                {
                    Debug.LogError($"Tiny: Could not find '{Name}' at default location '{m_DefaultValue}'");
                }
                m_Value = EditorPrefs.GetString(m_Key);
                if (m_Value == string.Empty || m_Value == m_DefaultValue)
                {
                    Reset();
                }

                if (m_Value != null)
                {
                    TryGetVersion(m_Value, m_VersionCommand, out m_Version);
                }
                m_Initialized = true;
            }

            public void Reset()
            {
                if (m_Value != null)
                {
                    m_Value = null;
                    m_Version = null;
                    EditorPrefs.DeleteKey(m_Key);
                }
            }

            public void Draw()
            {
                Value = EditorGUILayout.DelayedTextField($"{Name} Path", Value);
                EditorGUILayout.SelectableLabel(Version);
            }

            private static bool TryGetVersion(string directory, string command, out string version)
            {
                version = null;
                
                if (string.IsNullOrEmpty(command))
                    return false;
                
                if (!Directory.Exists(directory))
                    return false;
                
                var output = TinyShell.RunInShell(command, new ShellProcessArgs()
                {
                    ExtraPaths = directory.AsEnumerable(),
                    ThrowOnError = false
                });

                if (!output.Succeeded)
                    return false;

                version = output.CommandOutput;
                return true;
            }
        }

        private class IDEProgramPath
        {
            private bool m_Initialized;
            private string m_Name, m_Key, m_Value;
            private Dictionary<string, string> m_IDEList;

            public string Name => m_Name;

            public IDEProgramPath(string name, string key)
            {
                m_Name = name;
                m_Key = key;
            }

            public void Draw()
            {
                Selected = EditorGUILayout.Popup($"{Name} Path", Selected, m_IDEList.Values.Append("Browse...").ToArray());
            }

            private Dictionary<string, string> BuildIDEList()
            {
                return CollectInstallPaths();
            }

            private Dictionary<string, string> CollectInstallPaths()
            {
                var installPaths = new Dictionary<string, string>();
                installPaths.Add("", "Open with Unity preferred IDE");

#if UNITY_EDITOR_WIN
                const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                CollectPathsFromRegistryWin32(registryKey, installPaths);
                const string wowRegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                CollectPathsFromRegistryWin32(wowRegistryKey, installPaths);
#endif
                if (!installPaths.Keys.Contains(m_Value))
                {
                    installPaths.Add(m_Value, Path.GetFileNameWithoutExtension(m_Value));
                }

                return installPaths;
            }
#if UNITY_EDITOR_WIN
            private void CollectPathsFromRegistryWin32(string registryKey, Dictionary<string, string> installPaths)
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key == null) return;
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using (var subkey = key.OpenSubKey(subkeyName))
                        {
                            var folderObject = subkey?.GetValue("InstallLocation");
                            if (folderObject == null) continue;
                            var folder = folderObject.ToString();

                            var path = "";
                            var name = "";
                            if (folder.Contains("VS Code"))
                            {
                                path = Path.Combine(folder, "Code.exe");
                                name = "VS Code";
                            }
                            else if (folder.Contains("Sublime Text"))
                            {
                                path = Path.Combine(folder,"subl.exe");
                                name = "Sublime Text";
                            }
                            else if (folder.Contains("WebStorm"))
                            {
                                path = Path.Combine(folder, "bin/webstorm64.exe");
                                name = "WebStorm";
                            }

                            if (path != "" && File.Exists(path))
                                installPaths.Add(path, name);
                        }
                    }
                }
            }
#endif
            private void Initialize()
            {
                if (m_Initialized)
                    return;

                m_Value = EditorPrefs.HasKey(m_Key) ? EditorPrefs.GetString(m_Key) : "";
                m_IDEList = BuildIDEList();

                if(!m_IDEList.Keys.ToList().Contains(m_Value))
                {
                    Reset();
                }

                m_Initialized = true;
            }

            private void Reset() 
            {
                m_Value = "";

                if(EditorPrefs.HasKey(m_Key))
                    EditorPrefs.DeleteKey(m_Key);
            }

            public int Selected
            {
                get { Initialize(); return m_IDEList.Keys.ToList().IndexOf(m_Value); } 
                set
                {
                    Initialize();

                    if (value == m_IDEList.Keys.ToList().IndexOf(m_Value))
                    {
                        return;
                    }

                    if (value == 0)
                    {
                        Reset();
                        return;
                    }

                    if (value < m_IDEList.Keys.Count)
                    {
                        m_Value = m_IDEList.Keys.ElementAt(value);
                        EditorPrefs.SetString(m_Key, m_Value);
                    }
                    else
                    {
#if UNITY_EDITOR_WIN
                        var path = EditorUtility.OpenFilePanel("Browsing for IDE", "", "exe");
#elif UNITY_EDITOR_OSX
                        var path = EditorUtility.OpenFilePanel("Browsing for IDE", "", "app");
#endif
                        if (path != "")
                        {
                            if(!m_IDEList.Keys.Contains(path))
                            {
                                m_IDEList.Add(path, Path.GetFileNameWithoutExtension(path));
                            }

                            m_Value = path;
                            EditorPrefs.SetString(m_Key, m_Value);
                        }
                    }
                }
            }

            public string Value
            {
                get { Initialize(); return m_Value; }
            }
        }
        
        private static readonly ProgramPath s_MonoDir = new ProgramPath(
            "Mono",
            "TINY_MONO_DIR",
            "mono --version", // very verbose
            DefaultMonoPath());

        private static readonly IDEProgramPath s_IDEDir = new IDEProgramPath(
            "IDE",
            "TINY_IDE_DIR");

        public static string MonoDirectory
        {
            get
            {
#if UNITY_EDITOR_OSX
                LogErrorMonoPathWhiteSpaces();
#endif
                return s_MonoDir.Value;
            }
            set
            {
                s_MonoDir.Value = value;


#if UNITY_EDITOR_OSX
                LogErrorMonoPathWhiteSpaces();
#endif
            }
        }
        public static string IDEDirectory => s_IDEDir.Value;

#if UNITY_EDITOR_OSX
        public static void LogErrorMonoPathWhiteSpaces()
        {
            if(s_MonoDir.Value.Contains(" "))
            {
                Debug.LogError($"{TinyConstants.ApplicationName}: Your mono install location ({s_MonoDir.Value}) contains spaces, which will result in export errors. "
                + "\nYou can fix this by removing the spaces in the path, or installing mono to a different location with no spaces and setting the path in the Tiny Preferences.");
            }
        }
#endif

        public override void OnGUI(string searchContext)
        {
            s_MonoDir.Draw();
            s_IDEDir.Draw();
        }
    }
}
