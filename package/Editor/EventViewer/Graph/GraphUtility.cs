using UnityEngine;

namespace EditorDiagnostics
{
    public static class GraphUtility
    {
        public static float ValueToPixel(float val, float min, float max, float pixels)
        {
            return Mathf.Clamp01((val - min) / (max - min)) * pixels;
        }

        public static float ValueToPixelUnclamped(float val, float min, float max, float pixels)
        {
            return ((val - min) / (max - min)) * pixels;
        }

        public static float PixelToValue(float pixel, float xMin, float xMax, float valueRange)
        {
            return Mathf.Clamp01((pixel - xMin) / (xMax - xMin)) * valueRange;
        }
    }
}
