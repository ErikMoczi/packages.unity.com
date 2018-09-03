// -----------------------------------------------------------------------
// <copyright file="IFileFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace UnityEngine.Experimental.U2D.TriangleNet
.IO
{
    public interface IFileFormat
    {
        bool IsSupported(string file);
    }
}
