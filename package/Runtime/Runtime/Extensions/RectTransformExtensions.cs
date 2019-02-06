

using UnityEngine;

namespace Unity.Tiny
{
    internal static class RectTransformExtensions
    {
        public static void SetSize(this RectTransform rt, Vector2 size)
        {
            var delta = size - rt.rect.size;
            rt.offsetMin -= new Vector2(delta.x * rt.pivot.x, delta.y * rt.pivot.y);
            rt.offsetMax += new Vector2(delta.x * (1f - rt.pivot.x), delta.y * (1f - rt.pivot.y));
        }
    }
}

