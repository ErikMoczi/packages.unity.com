using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class CircleVertexSelector : IVertexSelector
    {
        public ISelection selection { get; set; }
        public IList<Vertex2D> vertices { get; set; }
        public Vector2 position { get; set; }
        public float radius { get; set; }

        public void Select()
        {
            float sqrRadius = radius * radius;

            for (int i = 0; i < vertices.Count; i++)
            {
                if ((vertices[i].position - position).sqrMagnitude <= sqrRadius)
                {
                    selection.Select(i, true);
                }
            }
        }
    }
}
