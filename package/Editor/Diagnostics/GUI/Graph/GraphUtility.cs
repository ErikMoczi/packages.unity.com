using UnityEngine;

namespace UnityEditor.ResourceManagement.Diagnostics
{
    public static class GraphUtility
    {
        public static float ValueToPixel(float value, float min, float max, float pixelRange)
        {
            return Mathf.Clamp01((value - min) / (max - min)) * pixelRange;
        }

        public static float ValueToPixelUnclamped(float value, float min, float max, float pixelRange)
        {
            return ((value - min) / (max - min)) * pixelRange;
        }

        public static float PixelToValue(float pixel, float min, float max, float valueRange)
        {
            return Mathf.Clamp01((pixel - min) / (max - min)) * valueRange;
        }
    }
}
