using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.U2D.Common;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class OutlineGenerator : IOutlineGenerator
    {
        public void GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            InternalEditorBridge.GenerateOutline(texture, rect, detail, alphaTolerance, holeDetection, out paths);
        }
    }
}
