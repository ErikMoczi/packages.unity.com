
using System;
using UnityEditor;

namespace Unity.Tiny
{
    internal static class GUIViewBridge
    {
        public static void RepaintCurrentView()
        {
            GUIView.current.Repaint();
        }
    }
}
