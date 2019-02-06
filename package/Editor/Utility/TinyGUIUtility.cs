

using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyGUIEnabledScope : GUI.Scope
    {
        private readonly bool m_Enabled;
        
        public TinyGUIEnabledScope(bool enabled)
        {
            m_Enabled = GUI.enabled;
            GUI.enabled = enabled;
        }
        
        protected override void CloseScope()
        {
            GUI.enabled = m_Enabled;
        }
    }
    
    internal class TinyGUIColorScope : GUI.Scope
    {
        private readonly Color m_Color;
        
        public TinyGUIColorScope(Color color)
        {
            m_Color = GUI.color;
            GUI.color = color;
        }
        
        protected override void CloseScope()
        {
            GUI.color = m_Color;
        }
    }

    internal static class TinyGUIUtility
    {
        private static readonly Vector2 LowerLeft = new Vector2(0.0f, 0.0f);
        private static readonly Vector2 LowerCenter = new Vector2(0.5f, 0.0f);
        private static readonly Vector2 LowerRight = new Vector2(1.0f, 0.0f);
        
        private static readonly Vector2 MiddleLeft = new Vector2(0.0f, 0.5f);
        private static readonly Vector2 MiddleCenter = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 MiddleRight = new Vector2(1.0f, 0.5f);
        
        private static readonly Vector2 UpperLeft = new Vector2(0.0f, 1.0f);
        private static readonly Vector2 UpperCenter = new Vector2(0.5f, 1.0f);
        private static readonly Vector2 UpperRight = new Vector2(1.0f, 1.0f);
        
        public static float ComponentHeaderSeperatorHeight { get; } = 2.0f;
        public static float ComponentSeperatorHeight { get; } = 4.0f;

        public static float SingleLineAndSpaceHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public static TextAnchor GetTextAnchorFromPivot(Vector2 pivot)
        {
            if (pivot == LowerLeft)
            {
                return TextAnchor.LowerLeft;
            }
            if (pivot == LowerCenter)
            {
                return TextAnchor.LowerCenter;
            }
            if (pivot == LowerRight)
            {
                return TextAnchor.LowerRight;
            }
            if (pivot == MiddleLeft)
            {
                return TextAnchor.MiddleLeft;
            }
            if (pivot == MiddleCenter)
            {
                return TextAnchor.MiddleCenter;
            }
            if (pivot == MiddleRight)
            {
                return TextAnchor.MiddleRight;
            }
            if (pivot == UpperLeft)
            {
                return TextAnchor.UpperLeft;
            }
            if (pivot == UpperCenter)
            {
                return TextAnchor.UpperCenter;
            }
            if (pivot == UpperRight)
            {
                return TextAnchor.UpperRight;
            }
            throw new ArgumentOutOfRangeException(nameof(pivot), pivot, null);
        }
        
        public static TextAlignmentOptions GetTextAlignmentFromPivot(Vector2 pivot)
        {
            if (pivot == LowerLeft)
            {
                return TextAlignmentOptions.BottomLeft;
            }
            if (pivot == LowerCenter)
            {
                return TextAlignmentOptions.Bottom;
            }
            if (pivot == LowerRight)
            {
                return TextAlignmentOptions.BottomRight;
            }
            if (pivot == MiddleLeft)
            {
                return TextAlignmentOptions.Left;
            }
            if (pivot == MiddleCenter)
            {
                return TextAlignmentOptions.Center;
            }
            if (pivot == MiddleRight)
            {
                return TextAlignmentOptions.Right;
            }
            if (pivot == UpperLeft)
            {
                return TextAlignmentOptions.TopLeft;
            }
            if (pivot == UpperCenter)
            {
                return TextAlignmentOptions.Top;
            }
            if (pivot == UpperRight)
            {
                return TextAlignmentOptions.TopRight;
            }
            throw new ArgumentOutOfRangeException(nameof(pivot), pivot, null);
        }
        
        public static Vector2 GetPivotFromTextAnchor(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return UpperLeft;
                case TextAnchor.UpperCenter: return UpperCenter;
                case TextAnchor.UpperRight: return UpperRight;
                case TextAnchor.MiddleLeft: return MiddleLeft;
                case TextAnchor.MiddleCenter: return MiddleCenter;
                case TextAnchor.MiddleRight: return MiddleRight;
                case TextAnchor.LowerLeft: return LowerLeft;
                case TextAnchor.LowerCenter: return LowerCenter;
                case TextAnchor.LowerRight: return LowerRight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
            }
        }
    }
}

