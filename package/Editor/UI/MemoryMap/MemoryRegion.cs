using System.Collections.Generic;
using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI.MemoryMap
{
    public abstract class MemoryRegion
    {
        public class EndComparer : IComparer<MemoryRegion>
        {
            int IComparer<MemoryRegion>.Compare(MemoryRegion a, MemoryRegion b)
            {
                return a.GetAddressEnd().CompareTo(b.GetAddressEnd());
            }
        }
        public virtual Mesh2D BuildMeshSection(Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            MeshBuilder mb = new MeshBuilder();
            BuildMeshSection(mb, r, memBegin, memEnd, pixelSize);
            return mb.CreateMesh();
        }

        public abstract void BuildMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize);
        public abstract void BuildSelectionMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize);
        public abstract void CleanupMeshes();
        public abstract ulong GetAddressBegin();
        public abstract ulong GetAddressEnd();
        public abstract string GetDisplayName();
        public abstract string GetDisplayType();
    }
}
