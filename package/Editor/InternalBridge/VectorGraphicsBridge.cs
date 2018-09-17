using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Interface;
using UnityEditor.Experimental.U2D;
using UnityEngine.U2D.Interface;

namespace Unity.VectorGraphics.Editor
{
    internal static class InternalBridge
    {
        internal static void ShowSpriteEditorWindow()
        {
            SpriteEditorWindow.GetWindow();
        }

        internal static List<Vector2[]> GenerateOutline(UnityEngine.Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection)
        {
            Vector2[][] paths;
            UnityEditor.Sprites.SpriteUtility.GenerateOutline(texture, rect, detail, alphaTolerance, holeDetection, out paths);
            if (paths == null || paths.Length == 0)
                return null;
            return paths.ToList();
        }
    }
}