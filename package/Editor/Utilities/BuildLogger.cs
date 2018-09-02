﻿using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Build.Utilities
{
    public static class BuildLogger
    {
        [Conditional("BUILD_CACHE_DEBUG")]
        public static void LogCache(string msg, params object[] attrs)
        {
            LogWarning(msg, attrs);
        }

        [Conditional("DEBUG")]
        public static void Log(string msg)
        {
            Debug.Log(msg);
        }

        [Conditional("DEBUG")]
        public static void Log(object msg)
        {
            Debug.Log(msg);
        }

        [Conditional("DEBUG")]
        public static void Log(string msg, params object[] attrs)
        {
            Debug.Log(string.Format(msg, attrs));
        }

        [Conditional("DEBUG")]
        public static void LogWarning(string msg)
        {
            Debug.LogWarning(msg);
        }

        [Conditional("DEBUG")]
        public static void LogWarning(object msg)
        {
            Debug.LogWarning(msg);
        }

        [Conditional("DEBUG")]
        public static void LogWarning(string msg, params object[] attrs)
        {
            Debug.LogWarning(string.Format(msg, attrs));
        }

        [Conditional("DEBUG")]
        public static void LogError(string msg)
        {
            Debug.LogError(msg);
        }

        [Conditional("DEBUG")]
        public static void LogError(object msg)
        {
            Debug.LogError(msg);
        }

        [Conditional("DEBUG")]
        public static void LogError(string msg, params object[] attrs)
        {
            Debug.LogError(string.Format(msg, attrs));
        }

        [Conditional("DEBUG")]
        public static void LogException(System.Exception e)
        {
            Debug.LogException(e);
        }
    }
}
