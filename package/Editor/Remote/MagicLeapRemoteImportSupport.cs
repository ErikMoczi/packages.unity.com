using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

using SysDebug = System.Diagnostics.Debug;
using UnityDebug = UnityEngine.Debug;

namespace UnityEditor.XR.MagicLeap
{
    [Serializable]
    class SDKManifest
    {
        public string schemaVersion = null;
        public string label = null;
        public string version = null;
        public string mldb = null;
    }

    internal class WorkingDirectoryShift : IDisposable
    {
        private string m_CurrentDirectory;
        public WorkingDirectoryShift(string new_wd)
        {
            if (string.IsNullOrEmpty(new_wd))
                throw new ArgumentNullException("new_wd");
            m_CurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(new_wd);
        }

        void IDisposable.Dispose()
        {
            Directory.SetCurrentDirectory(m_CurrentDirectory);
        }
    }

    public class ImportFailureException : Exception
    {
        const string kHelpMessage = @"Cannot import {0} as it appears to already have been previously imported. Please delete the existing {1}, restart Unity, and try importing again";

        public string HelpMessage
        {
            get
            {
                return string.Format(kHelpMessage, Path, System.IO.Path.GetDirectoryName(Path));
            }
        }

        public string Path { get; }

        public ImportFailureException(string path) : base(string.Format(kHelpMessage, path, System.IO.Path.GetDirectoryName(path)))
        {
            Path = path;
        }
    }

#if UNITY_EDITOR_OSX
    class MacOSDependencyChecker
    {
        const string kRegexPattern = @"\t(.+) \(compatibility version \d{1,4}\.\d{1,4}\.\d{1,4}, current version \d{1,4}\.\d{1,4}\.\d{1,4}\)";

        public class DependencyMap
        {
            public string file = null;
            public List<string> dependencies = new List<string>();
        }

        internal static IEnumerable<string> LaunchOtool(string filepath)
        {
            var psi = new ProcessStartInfo {
                FileName = "/usr/bin/otool",
                Arguments = string.Format("-L {0}", filepath),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using (Process p = Process.Start(psi))
            {
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        internal static DependencyMap GetDependencies(string file)
        {
            var regex = new Regex(kRegexPattern);
            var dm = new DependencyMap { file = file };
            var output = LaunchOtool(file);
            foreach (var line in output)
            {
                var m = regex.Match(line);
                if (m.Success)
                {
                    var dep_path = m.Groups[1].Value;
                    dm.dependencies.Add(dep_path.Replace("@loader_path", Path.GetDirectoryName(file)));
                }
            }
            return dm;
        }

        internal static void Migrate(string src, string dest)
        {
            var dir = Path.GetDirectoryName(dest);
            using (new WorkingDirectoryShift(dir))
            {
                var psi = new ProcessStartInfo {
                    FileName = "lipo",
                    Arguments = string.Format("-create {0} -output {1}", src, Path.GetFileName(dest)),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                //UnityDebug.LogFormat("{0} {1}", psi.FileName, psi.Arguments);
                using (Process p = Process.Start(psi))
                    p.WaitForExit();

                psi = new ProcessStartInfo {
                    FileName = "install_name_tool",
                    Arguments = string.Format("-id {0} {0}", Path.GetFileName(dest)),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                //UnityDebug.LogFormat("{0} {1}", psi.FileName, psi.Arguments);
                using (Process p = Process.Start(psi))
                    p.WaitForExit();

            }

        }
    }
#endif

    public static class MagicLeapRemoteImportSupport
    {
        const string kDestinationProjectFolder = "Assets/Plugins/Lumin/Editor/x64";
        const string kManifestPath = ".metadata/sdk.manifest";
        internal const string kShimDiscoveryPath = ".metadata/sdk_shim_discovery.txt";
        const string kShimPathTemplate = "ZI_SHIM_PATH_{0}";
        const string kFallbackShimData = @"
# some comments
STUB_PATH=$(MLSDK)/lib/$(HOST)

# in older builds, ML Remote lives inside MLSDK
MLREMOTE_BASE_0.16.0=$(MLSDK)/VirtualDevice
MLREMOTE_BASE_0.17.0=$(MLSDK)/VirtualDevice
MLREMOTE_BASE_0.18.0=$(MLSDK)/VirtualDevice

# ML Remote is installed in parallel, using the same version as MLSDK
MLREMOTE_BASE_0.19.0=$(MLSDK)/../../MLRemote/v$(MLSDK_VERSION)

# select the appropriate base for the running version
MLREMOTE_BASE=$(MLREMOTE_BASE_$(MLSDK_VERSION))

ZI_SHIM_PATH_win64=$(MLREMOTE_BASE)/lib;$(MLREMOTE_BASE)/bin;$(STUB_PATH)
ZI_SHIM_PATH_osx=$(MLREMOTE_BASE)/lib;$(STUB_PATH)
ZI_SHIM_PATH_linux64=$(MLREMOTE_BASE)/lib/linux64;$(STUB_PATH)
";
        const string kMacOSDependencyPath = "{0}/VirtualDevice/bin";

        internal static IEnumerable<string> discoveryFileOrFallbackData
        {
            get
            {
                return (hasDiscoveryFile)
                    ? File.ReadAllLines(Path.Combine(sdkPath, kShimDiscoveryPath))
                    : kFallbackShimData.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        public static string host
        {
            get
            {
#if UNITY_EDITOR_WIN
                return "win64";
#elif UNITY_EDITOR_OSX
                return "osx";
#elif UNITY_EDITOR_LINUX
                return "linux64";
#else
                throw new NotSupportedException("Not supported on this platform!");
#endif
            }
        }

        internal static string hostExtensionGlob
        {
            get
            {
#if UNITY_EDITOR_WIN
                return "*.dll";
#elif UNITY_EDITOR_OSX
                return "*.*"; // extension checking is handled later for OSX.
#elif UNITY_EDITOR_LINUX
                return "*.so";
#else
                throw new NotSupportedException("Not supported on this platform!");
#endif
            }
        }

        internal static bool hasDiscoveryFile
        {
            get
            {
                if (!sdkAvailable) return false;
                return File.Exists(Path.Combine(sdkPath, kShimDiscoveryPath));
            }
        }

        internal static string macOSDependencyPath
        {
            get
            {
                return string.Format(kMacOSDependencyPath, sdkPath);
            }
        }

        internal static bool sdkAvailable
        {
            get
            {
                if (string.IsNullOrEmpty(sdkPath)) return false;
                return File.Exists(Path.Combine(sdkPath, kManifestPath));
            }
        }

        public static string sdkPath
        {
            get
            {
                return EditorPrefs.GetString("LuminSDKRoot", null);
            }
        }

        internal static IEnumerable<string> shimSearchPaths
        {
            get
            {
                return ParseDiscoveryData(discoveryFileOrFallbackData);
            }
        }

        public static string version
        {
            get
            {
                var manifest = Path.Combine(sdkPath, kManifestPath);
                return JsonUtility.FromJson<SDKManifest>(File.ReadAllText(manifest)).version;
            }
        }

        public static IEnumerable<string> LocateMLRemoteLibraries()
        {
            var paths = new HashSet<string>();
            foreach (var dir in shimSearchPaths)
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists) continue;
                foreach (var fi in di.GetFiles(hostExtensionGlob))
                {
#if UNITY_EDITOR_OSX
                    var base_name = Path.GetFileNameWithoutExtension(fi.Name);
                    if (!base_name.StartsWith("lib"))
                        base_name = "lib" + base_name;
                    if (paths.Add(base_name))
#else
                    if (paths.Add(fi.Name))
#endif
                        yield return fi.FullName;
                }
            }
        }

        internal static IEnumerable<string> ParseDiscoveryData(IEnumerable<string> lines)
        {
            Dictionary<string, string> vars = new Dictionary<string, string>();
            vars.Add("MLSDK", sdkPath);
            vars.Add("MLSDK_VERSION", version);
            vars.Add("HOST", host);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                // discard comments
                if (line.StartsWith("#"))
                    continue;

                bool expansionPerformed = false;
                // replace all '$(VAR)' expansions with actual values, if possible.
                do
                {
                    expansionPerformed = false;
                    foreach (var kv in vars)
                    {
                        var v = string.Format("$({0})", kv.Key);
                        var temp = line.Replace(v, kv.Value);
                        if (temp != line)
                        {
                            expansionPerformed = true;
                            line = temp;
                        }
                    }

                } while (expansionPerformed);

                // if we see an equals sign, update the variable list with the new value.
                if (line.IndexOf("=") != -1)
                {
                    var parts = line.Split('=');
                    vars[parts[0].Trim()] = parts[1].Trim();
                }
            }

            var key = string.Format(kShimPathTemplate, host);
            if (!vars.ContainsKey(key))
                throw new Exception(string.Format("'{0}' key not found during shim lookup!", key));
            return vars[key].Split(';');
        }

#if UNITY_EDITOR_OSX
        internal static string GetOSXTargetPath(string destFolder, string srcPath)
        {
            // we only want to rename the ML libs we load directly, not their dependencies.
            // let's assume for now that all front-facing libs begin with "libml_"
            if (Path.GetFileName(srcPath).StartsWith("libml_"))
            {
                var new_name = Path.GetFileNameWithoutExtension(srcPath) + ".bundle";
                new_name = new_name.TrimStart(new char[] {'l','i','b'});
                return Path.Combine(destFolder, new_name);
            }
            return null;
        }
#endif

        internal static void ImportSupportLibrares(string destFolder)
        {
            Directory.CreateDirectory(destFolder);
            foreach (var lib in LocateMLRemoteLibraries())
            {
                //UnityDebug.Log(lib);
#if UNITY_EDITOR_OSX
                var target = GetOSXTargetPath(destFolder, lib);
                if (target == null) continue;
                CheckIfFileCanBeCopiedAndThrow(lib, target);
                MacOSDependencyChecker.Migrate(lib, target);
                var dm = MacOSDependencyChecker.GetDependencies(target);
                var missing = new List<string>();
                using (new WorkingDirectoryShift(Path.GetDirectoryName(target)))
                {
                    foreach (var dep in dm.dependencies)
                    {
                        if (File.Exists(dep))
                            continue;
                        else
                            missing.Add(dep);
                    }
                }
                foreach (var item in missing)
                {
                    var dep_path = Path.GetFullPath(item);
                    if (!File.Exists(dep_path))
                    {
                        //UnityDebug.LogFormat("missing dep: {0} for {1}", dep_path, dm.file);
                        Directory.CreateDirectory(Path.GetDirectoryName(dep_path));
                        var src = Path.Combine(macOSDependencyPath, Path.GetFileName(item));
                        //UnityDebug.LogFormat("searching for {0}: {1}", Path.GetFileName(item), src);
                        if (File.Exists(src))
                            File.Copy(src, dep_path);
                    }

                }
#else
                var basename = Path.GetFileName(lib);
                var target = Path.Combine(destFolder, basename);
                if (target == null) continue; // null indicates the file shouldn't be copied.
                CheckIfFileCanBeCopiedAndThrow(lib, target);
                File.Copy(lib, target);
#endif

            }
            AssetDatabase.Refresh();
        }

        private static void CheckIfFileCanBeCopiedAndThrow(string src, string dest)
        {
            if (!File.Exists(src))
                throw new Exception(string.Format("Cannot import {0}: file not found", src));
            if (File.Exists(dest))
                throw new ImportFailureException(dest);
        }

        [MenuItem("Magic Leap/ML Remote/Import Support Libraries")]
        static void DoImport()
        {
            ImportSupportLibrares(kDestinationProjectFolder);
        }

        [MenuItem("Magic Leap/ML Remote/Import Support Libraries", true)]
        static bool CanImport()
        {
            return true;
        }
    }
}