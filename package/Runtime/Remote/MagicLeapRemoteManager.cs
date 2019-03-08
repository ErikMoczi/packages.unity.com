#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.XR;

using AOT;

namespace UnityEditor.XR.MagicLeap.Remote
{
    [InitializeOnLoad]
    public class MagicLeapRemoteManager
    {
#if UNITY_EDITOR && PLATFORM_LUMIN
        [return : MarshalAs(UnmanagedType.U1)]
        public delegate bool RemoteLoaderCallback([MarshalAs(UnmanagedType.LPStr)] string libName, out IntPtr handle);

        private static Dictionary<string, string> s_PluginLookupCache = new Dictionary<string, string>();

        private static bool s_MagicLeapRemoteInitialized = false;

        static class Native
        {
            [DllImport("UnityMagicLeap", EntryPoint="UnityMagicLeap_RemoteSetLoaderCallback")]
            public static extern void RemoteSetLoaderCallback(RemoteLoaderCallback callback);

            [DllImport("UnityMagicLeap", EntryPoint="UnityMagicLeap_RemoteInitialize")]
            public static extern void RemoteInitialize();

            [DllImport("UnityMagicLeap", EntryPoint="UnityMagicLeap_RemoteShutdown")]
            public static extern void RemoteShutdown();

            public static IntPtr OpenLibrary(string path)
            {
#if UNITY_EDITOR_OSX
                return LoadLibrary(path, RTLD_LAZY | RTLD_LOCAL);
#elif UNITY_EDITOR_WIN
                var cwd = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
                IntPtr handle = LoadLibrary(path);
                if (handle == IntPtr.Zero)
                    Debug.LogFormat("LoadLibrary returned: {0}", Marshal.GetLastWin32Error());
                Directory.SetCurrentDirectory(cwd);
                return handle;
#else
                return LoadLibrary(path);
#endif
            }

            public static void CloseLibrary(IntPtr handle)
            {
                FreeLibrary(handle);
            }

#if UNITY_EDITOR_OSX
            const int RTLD_LAZY = 1;
            const int RTLD_LOCAL = 0;

            [DllImport("libdl", CharSet=CharSet.Ansi, EntryPoint="dlopen")]
            static extern IntPtr LoadLibrary(string path, int flags);

            [DllImport("libdl", EntryPoint="dlclose")]
            static extern int FreeLibrary(IntPtr handle);
#elif UNITY_EDITOR_WIN
            [DllImport("Kernel32", CharSet=CharSet.Auto, SetLastError=true)]
            static extern IntPtr LoadLibrary(string path);

            [DllImport("Kernel32")]
            static extern bool FreeLibrary(IntPtr handle);
#else
            static IntPtr LoadLibrary(string path)
            {
                Debug.LogFormat("Unsupported Platform!")
                return IntPtr.Zero;
            }
            static void FreeLibrary(IntPtr handle) {}
#endif
        }

        public static bool isInitialized
        {
            get { return s_MagicLeapRemoteInitialized; }
        }

        static MagicLeapRemoteManager()
        {
            EditorApplication.playModeStateChanged -= PlaymodeStateChanged;
            EditorApplication.playModeStateChanged += PlaymodeStateChanged;
        }

        static void PlaymodeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    SetLoaderCallback();
                    s_MagicLeapRemoteInitialized = true;
                    break;
                }
                case PlayModeStateChange.ExitingEditMode:
                {
                    InitializeWithLoaderCallback();
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    s_MagicLeapRemoteInitialized = false;
                    Shutdown();
                    break;
                }
                case PlayModeStateChange.EnteredEditMode:
                default:
                    break;
            }
        }

        [MonoPInvokeCallback(typeof(RemoteLoaderCallback))]
        internal static bool DefaultLoader(string name, out IntPtr handle)
        {
            string path;
            handle = IntPtr.Zero;
            if (TryResolveMLPluginPath(name, out path))
            {
                handle = Native.OpenLibrary(path);
                return handle != IntPtr.Zero;
            }
            return false;
        }

        public static void InitializeWithLoaderCallback(RemoteLoaderCallback cb = null)
        {
            SetLoaderCallback(cb);
            Native.RemoteInitialize();
        }

        public static void SetLoaderCallback(RemoteLoaderCallback cb = null)
        {
            Native.RemoteSetLoaderCallback(cb ?? DefaultLoader);
        }

        public static void Shutdown()
        {
            Native.RemoteShutdown();
        }

        public static bool TryResolveMLPluginPath(string name, out string path)
        {
            if (!s_PluginLookupCache.TryGetValue(name, out path))
            {
                //Debug.LogFormat("{0} not cached, querying plugin importers", name);
                foreach (var importer in PluginImporter.GetAllImporters().Where(ap => LooksLikeRemoteLibrary(ap.assetPath)))
                {
                    var filename = Path.GetFileNameWithoutExtension(importer.assetPath);
                    if (filename.EndsWith(name))
                    {
                        path = Path.GetFullPath(importer.assetPath);
                        s_PluginLookupCache[name] = path;
                        //Debug.LogFormat("found {0} -> {1}", name, path);
                        return true;
                    }
                }
                path = null;
                return false;
            }
            //Debug.LogFormat("using cached value for {0} ({1})", name, path);
            return true;
        }

        static bool LooksLikeRemoteLibrary(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.StartsWith("ml_") && fileName.EndsWith(hostExtension);
        }

#if ML_REMOTE_ENABLE_EAGER_INIT
        [DidReloadScripts]
        static void ResetLoaderCallback()
        {
            InstallLoaderCallback(DefaultLoader);
        }

        [InitializeOnLoadMethod]
        static void OnProjectLoadedCallback()
        {
            InstallLoaderCallback(DefaultLoader);
        }
#endif //  ML_REMOTE_ENABLE_EAGER_INIT

        internal static string hostExtension
        {
            get
            {
#if UNITY_EDITOR_WIN
                return ".dll";
#elif UNITY_EDITOR_OSX
                return ".bundle";
#elif UNITY_EDITOR_LINUX
                return "*.so";
#else
                throw new NotSupportedException("Not supported on this platform!");
#endif
            }
        }
#endif // UNITY_EDITOR && PLATFORM_LUMIN
    }
}
#endif // UNITY_EDITOR