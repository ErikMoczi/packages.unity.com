using UnityEngine;

namespace Unity.Tiny
{
    #if UNITY_EDITOR
    internal static class GUIUtilityBridge
    {
        public static double pixelsPerPoint => GUIUtility.pixelsPerPoint;
    }

#endif
}