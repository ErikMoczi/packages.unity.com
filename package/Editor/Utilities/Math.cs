using System;

namespace Unity.MemoryProfiler.Editor
{
    internal static class MathExt
    {
        public static ulong Clamp(this ulong value, ulong min, ulong max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
