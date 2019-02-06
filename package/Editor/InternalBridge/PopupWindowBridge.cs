
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class PopupWindowBridge
    {
        public static void Show(Rect activatorRect, PopupWindowContent content)
        {
            PopupWindow.Show(activatorRect, content, null, ShowMode.PopupMenu);
        }
    }
}
