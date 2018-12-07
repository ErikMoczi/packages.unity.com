

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyColors
    {
        private static bool ProSkin => EditorGUIUtility.isProSkin;

        internal static class Hierarchy 
        {
            public static Color SceneItem { get; } = ProSkin ? new Color32(0x3D, 0x3D, 0x3D, 0xFF) : new Color32(0xDA, 0xDA, 0xDA, 0xFF);
            public static Color SceneSeparator { get; } = ProSkin ? new Color32(0x21, 0x21, 0x21, 0xFF) : new Color32(0x96, 0x96, 0x96, 0xFF);
            public static Color Disabled { get; } =  new Color32(0xFF, 0xFF, 0xFF, 0x93);
            public static Color Prefab { get; } = new Color32(0x6C, 0xB6, 0xFF, 0xFF);
        }

        internal static class Inspector
        {
            public static Color Background { get; } = ProSkin ? new Color32(0x3D, 0x3D, 0x3D, 0xFF) : new Color32(0xC2, 0xC2, 0xC2, 0xFF);
            public static Color HeaderBackground { get; } = ProSkin ? new Color32(0x41, 0x41, 0x41, 0xFF) : new Color32(0xE1, 0xE1, 0xE1, 0xFF);
            public static Color Separator { get; } = ProSkin ? new Color32(0x2B, 0x2C, 0x2D, 0xFF) : new Color32(0x74, 0x74, 0x74, 0xFF);
            public static Color BoxBackground { get; } = ProSkin ? new Color32(0x44, 0x44, 0x44, 0xFF) : new Color32(0xAA, 0xAA, 0xAA, 0xFF);
        }

        internal static class Editor
        {
            public static Color Link { get; } = ProSkin ? new Color32(0x5D, 0xA5, 0xFF, 0xFF) : new Color32(0x07, 0x4A, 0x8D, 0xFF);
        }

        internal static class AnimatedTree
        {
            public static Color Warning { get; } = ProSkin ? new Color32(0x3F, 0x3F, 0x3F, 0xFF) : new Color32(0xC2, 0xC2, 0xC2, 0xFF);
            public static Color Family { get; } = ProSkin ? new Color32(0x3A, 0x3A, 0x3A, 0xFF) : new Color32(0xC2, 0xC2, 0xC2, 0xFF);
            public static Color Background { get; } = ProSkin ? new Color32(0x32, 0x32, 0x32, 0xFF) : new Color32(0xC2, 0xC2, 0xC2, 0xFF);
            public static Color IncludedItem { get; } = ProSkin ? new Color32(0xCC, 0xCC, 0xCC, 0xFF) : new Color32(0, 0, 0, 0xFF);
            public static Color NonIncludedItem { get; } = ProSkin ? new Color32(0x7F, 0x7F, 0x7F, 0xFF) : new Color32(0x5F, 0x5F, 0x5F, 0xFF);
        }
    }
}

