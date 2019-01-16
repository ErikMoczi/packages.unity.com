using System;
using System.Diagnostics;
using System.Xml;

namespace Unity.MemoryProfiler.Editor.Debuging
{
    internal static class DebugUtility
    {
        public static bool IsInValidRange<T>(T[] array, long index)
        {
            return index >= 0 && index < array.Length;
        }

        public static bool IsInValidRange<T>(System.Collections.Generic.List<T> list, long index)
        {
            return index >= 0 && index < list.Count;
        }

        public static bool CheckIndexOutOfRange<T>(T[] array, long index)
        {
            if (index >= 0 && index < array.LongLength)
            {
                return false;
            }
            else
            {
#if MEMPROFILER_DEBUG
                UnityEngine.Debug.LogError("Index Out of Range " + index + " [" + array.LongLength + "]");
                return true;
#else
                throw new IndexOutOfRangeException();
#endif
            }
        }

        public static string GetExceptionHelpMessage(System.Exception e)
        {
#if MEMPROFILER_DEBUGCHECK
            throw new System.Exception(e.Message, e);
#else
            return "Unhandled exception. To get additional information add in project settings \"scripting define symbols\" : MEMPROFILER_DEBUGCHECK\n"
                + e.Message + "\n"
                + e.StackTrace;
#endif
        }

        [Conditional("MEMPROFILER_DEBUG")]
        public static void DebugLog(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static IDebugContext GetCurrentDebugContext()
        {
            var contextService = Service<IDebugContextService>.Current;
            if (contextService != null)
            {
                return contextService.GetCurrent();
            }
            return null;
        }

        public static void LogError(string message)
        {
            var debugContext = GetCurrentDebugContext();
            if (debugContext != null)
            {
                UnityEngine.Debug.LogError(message + "\nContext:" + debugContext.GetContextString("\n"));
            }
            else
            {
                UnityEngine.Debug.LogError(message);
            }
        }

        public static void LogWarning(string message)
        {
            var debugContext = GetCurrentDebugContext();
            if (debugContext != null)
            {
                UnityEngine.Debug.LogWarning(message + "\nContext:" + debugContext.GetContextString("\n"));
            }
            else
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }

        public static void LogInvalidXmlChild(System.Xml.XmlElement parent, System.Xml.XmlElement child)
        {
            MemoryProfilerAnalytics.AddMetaDatatoEvent<MemoryProfilerAnalytics.LoadViewXMLEvent>(2);
            var debugContext = GetCurrentDebugContext();
            if (debugContext != null)
            {
                DebugUtility.LogWarning("Unhandled element '" + child.Name + "' under element '" + parent.Name + "'.\nContext:" + debugContext.GetContextString("\n"));
            }
            else
            {
                DebugUtility.LogWarning("Unhandled element '" + child.Name + "' under element '" + parent.Name + "'.");
            }
        }

        public static void LogAnyXmlChildAsInvalid(System.Xml.XmlElement root)
        {
            foreach (XmlNode xNode in root.ChildNodes)
            {
                if (xNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement e = (XmlElement)xNode;
                    DebugUtility.LogInvalidXmlChild(root, e);
                }
            }
        }

        public static bool TryGetMandatoryXmlAttribute(System.Xml.XmlElement element, string attributeName, out string value)
        {
            string valueGot = element.GetAttribute(attributeName);
            if (String.IsNullOrEmpty(valueGot))
            {
                LogWarning("Element '" + element.Name + "' is missing the '" + attributeName + "' attribute.");
                value = null;

                byte errorID = 3;
                switch (attributeName)
                {
                    case "column":
                        errorID = 4;
                        break;
                    case "view":
                        errorID = 5;
                        break;
                    case "order":
                        errorID = 6;
                        break;
                    default:
                        break;
                }
                MemoryProfilerAnalytics.AddMetaDatatoEvent<MemoryProfilerAnalytics.LoadViewXMLEvent>(errorID);
                return false;
            }
            value = valueGot;
            return true;
        }

        private static bool TestFloat(float v, float min = float.MinValue, float max = float.MaxValue)
        {
            return float.IsNaN(v) || float.IsInfinity(v) || v < min || v > max;
        }

        [Conditional("MEMPROFILER_DEBUGCHECK")]
        public static void CheckCondition(bool condition, string message)
        {
            if (!condition)
            {
                LogError(message);
            }
        }

        [Conditional("MEMPROFILER_DEBUGCHECK")]
        public static void CheckFloat(float v, float min = float.MinValue, float max = float.MaxValue)
        {
            if (TestFloat(v, min, max))
            {
                LogError("Bad float value " + v);
            }
        }

        [Conditional("MEMPROFILER_DEBUGCHECK")]
        public static void CheckFloat(UnityEngine.Vector2 v, float min = float.MinValue, float max = float.MaxValue)
        {
            if (TestFloat(v.x, min, max) || TestFloat(v.y, min, max))
            {
                LogError("Bad float value " + v);
            }
        }

        [Conditional("MEMPROFILER_DEBUGCHECK")]
        public static void CheckFloat(UnityEngine.Vector3 v, float min = float.MinValue, float max = float.MaxValue)
        {
            if (TestFloat(v.x, min, max) || TestFloat(v.y, min, max) || TestFloat(v.z, min, max))
            {
                LogError("Bad float value " + v);
            }
        }

        [Conditional("MEMPROFILER_DEBUGCHECK")]
        public static void CheckFloat(UnityEngine.Matrix4x4 v, float min = float.MinValue, float max = float.MaxValue)
        {
            if (TestFloat(v.m00, min, max) || TestFloat(v.m01, min, max) || TestFloat(v.m02, min, max) || TestFloat(v.m03, min, max)
                || TestFloat(v.m10, min, max) || TestFloat(v.m11, min, max) || TestFloat(v.m12, min, max) || TestFloat(v.m13, min, max)
                || TestFloat(v.m20, min, max) || TestFloat(v.m21, min, max) || TestFloat(v.m22, min, max) || TestFloat(v.m23, min, max)
                || TestFloat(v.m30, min, max) || TestFloat(v.m31, min, max) || TestFloat(v.m32, min, max) || TestFloat(v.m33, min, max)
                )
            {
                LogError("Bad float value " + v);
            }
        }
    }
}
