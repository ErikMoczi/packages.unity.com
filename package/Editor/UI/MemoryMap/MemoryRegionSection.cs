using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI.MemoryMap
{
    public struct MemoryRegionSection
    {
        public MemoryRegionSection(Rect rect, ulong beginAddress, ulong endAddress)
        {
            this.rect = rect;
            this.beginAddress = beginAddress;
            this.endAddress = endAddress;
        }

        public Rect rect;
        public ulong beginAddress;
        public ulong endAddress;
    }
}
