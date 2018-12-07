using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IOutlineGenerator
    {
        void GenerateOutline(ITextureDataProvider textureDataProvider, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths);
    }
}
