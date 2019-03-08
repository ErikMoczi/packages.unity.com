using UnityEngine;

#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

using UBS = UnityEditor.Lumin.UserBuildSettings;
#endif

namespace UnityEngine.XR.MagicLeap
{
    class SDKBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SetupLuminSdkPath()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(UBS.SDKPath))
                throw new Exception("It looks like the path to the Lumin SDK is not set. ML Remote requires the Lumin SDK in order to properly function.");
            SetLibraryPath_Internal(UBS.SDKPath);
#endif
        }

#if UNITY_EDITOR
        [DllImport("UnityMagicLeap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall, EntryPoint = "UnityMagicLeap_SetLibraryPath")]
        private static extern void SetLibraryPath_Internal(string path);
#else
        private static void SetLibraryPath_Internal(string path) { }
#endif
    }
}

