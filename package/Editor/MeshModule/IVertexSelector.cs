using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal interface IVertexSelector
    {
        IList<Vertex2D> vertices { get; set; }
        ISelection selection { get; set; }

        void Select();
    }
}
