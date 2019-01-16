using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
#endif

namespace Unity.MemoryProfiler.Editor
{

    internal static class UIElementsStyleCompatibilityExtensions
    {

        internal static float GetMarginBottom(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.marginBottom.value.value;
#else 
            return style.marginBottom;
#endif
        }

        internal static float GetMarginTop(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.marginTop.value.value;
#else 
            return style.marginTop;
#endif
        }

        internal static float GetMarginRight(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.marginRight.value.value;
#else 
            return style.marginRight;
#endif
        }

        internal static float GetMarginLeft(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.marginLeft.value.value;
#else 
            return style.marginLeft;
#endif
        }

        internal static float GetPaddingBottom(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.paddingBottom.value.value;
#else 
            return style.paddingBottom;
#endif
        }

        internal static float GetPaddingTop(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.paddingTop.value.value;
#else 
            return style.paddingTop;
#endif
        }

        internal static float GetPaddingRight(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.paddingRight.value.value;
#else 
            return style.paddingRight;
#endif
        }

        internal static float GetPaddingLeft(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.paddingLeft.value.value;
#else 
            return style.paddingLeft;
#endif
        }

        internal static float GetWidth(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.width.value.value;
#else 
            return style.width;
#endif
        }

        internal static float GetMinWidth(this IStyle style)
        {
#if UNITY_2019_1_OR_NEWER
            return style.minWidth.value.value;
#else 
            return style.minWidth;
#endif
        }
    }
}
