using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    [InitializeOnLoad]
    internal static class GUIClip
    {
        static MethodInfo m_UnclipMethod;
        static MethodInfo m_GetTopRectMethod;
        static PropertyInfo m_TopMostRectProperty;

        static GUIClip()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.ManifestModule.Name == "UnityEngine.IMGUIModule.dll")
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Namespace == "UnityEngine" && type.Name == "GUIClip")
                        {
                            m_UnclipMethod = type.GetMethod("Unclip", new Type[] { typeof(Vector2) });
                            m_GetTopRectMethod = type.GetMethod("GetTopRect", BindingFlags.Static | BindingFlags.NonPublic);
                            m_TopMostRectProperty = type.GetProperty("topmostRect", typeof(Rect));
                            break;
                        }
                    }
                    break;
                }
            }
            if (m_UnclipMethod == null)
                Debug.LogError("GUIClip.Unclip method not found");
            if (m_GetTopRectMethod == null)
                Debug.LogError("GUIClip.GetTopRect method not found");
            if (m_TopMostRectProperty == null)
                Debug.LogError("GUIClip.topmostRect property not found");
        }

        static public Vector2 Unclip(Vector2 v)
        {
            return (Vector2)m_UnclipMethod.Invoke(null, new System.Object[] { v });
        }

        public static Rect topmostRect
        {
            get { return (Rect)m_TopMostRectProperty.GetValue(null, null); }
        }

        public static Rect GetTopRect()
        {
            return (Rect)m_GetTopRectMethod.Invoke(null, null);
        }
    }
}
