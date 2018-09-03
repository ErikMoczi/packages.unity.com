using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    // done
    internal class RectVertexSelector : IVertexSelector
    {
        public ISelection selection { get; set; }
        public IList<Vertex2D> vertices { get; set; }
        public Rect rect { get; set; }

        public void Select()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                if (rect.Contains(vertices[i].position, true))
                {
                    selection.Select(i, true);
                }
            }
        }
    }
}
