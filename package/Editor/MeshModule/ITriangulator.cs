using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface ITriangulator
    {
        void Triangulate(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices);
        void Tessellate(float minAngle, float maxAngle, float meshAreaFactor, float largestTriangleAreaFactor, int smoothIterations, IList<Vector2> vertices, IList<Edge> edges, IList<int> indices);
    }
}
