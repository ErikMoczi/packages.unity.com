using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI.MemoryMap
{
    public class ManagedMemoryRegion : MemoryRegion
    {
        public CachedSnapshot m_Snapshot;
        public int m_MemoryRegionId;
        public Mesh2D m_CurrentMesh;
        public ManagedMemoryRegion(CachedSnapshot snapshot, int memoryRegionId)
        {
            m_Snapshot = snapshot;
            m_MemoryRegionId = memoryRegionId;
        }

        public override void BuildMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            var co = ProfilerColors.currentColors[1];
            var coH = new Color(1, 1, 1, co.a);
            var coL = new Color(0, 0, 0, co.a);

            if (r.width > pixelSize.x)
            {
                mb.Add(0, new MeshBuilder.Rectangle(r, co, Color.Lerp(co, coH, 0.33f), co, Color.Lerp(co, coL, 0.33f)), true);
            }
            var coLine = co;
            coLine.a = Mathf.Clamp01(r.width / pixelSize.x + 0.3f);
            mb.Add(0, new MeshBuilder.Line(new Vector2(r.x, r.y), new Vector2(r.x, r.yMax), coLine));
        }

        public override void BuildSelectionMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            var co = Color.white;
            if (r.width > pixelSize.x)
            {
                mb.Add(0, new MeshBuilder.Rectangle(r, co, co, co, co), false);
            }
            else
            {
                var coLine = co;
                coLine.a = Mathf.Clamp01(r.width / pixelSize.x + 0.3f);
                mb.Add(0, new MeshBuilder.Line(new Vector2(r.x, r.y), new Vector2(r.x, r.yMax), coLine));
            }
        }

        public override void CleanupMeshes()
        {
            if (m_CurrentMesh != null) m_CurrentMesh.CleanupMeshes();
        }

        public override ulong GetAddressBegin()
        {
            return m_Snapshot.managedHeapSections.startAddress[m_MemoryRegionId];
        }

        public override ulong GetAddressEnd()
        {
            return m_Snapshot.managedHeapSections.startAddress[m_MemoryRegionId] +
                (ulong)m_Snapshot.managedHeapSections.bytes[m_MemoryRegionId].Length;
        }

        public override string GetDisplayName()
        {
            return string.Format("Heap Sections {0}", m_MemoryRegionId);
        }

        public override string GetDisplayType()
        {
            return "Managed";
        }
    }

    public class ManagedStackMemoryRegion : MemoryRegion
    {
        public CachedSnapshot m_Snapshot;
        public int m_MemoryRegionId;
        public Mesh2D m_CurrentMesh;
        public ManagedStackMemoryRegion(CachedSnapshot snapshot, int memoryRegionId)
        {
            m_Snapshot = snapshot;
            m_MemoryRegionId = memoryRegionId;
        }

        public override void BuildMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            var co = ProfilerColors.currentColors[2];
            co.a = 0.1f;
            var coH = new Color(1, 1, 1, co.a);
            var coL = new Color(0, 0, 0, co.a);

            if (r.width > pixelSize.x)
            {
                mb.Add(0, new MeshBuilder.Rectangle(r, co, Color.Lerp(co, coH, 0.33f), co, Color.Lerp(co, coL, 0.33f)), true);
            }
            var coLine = co;
            coLine.a = Mathf.Clamp01(r.width / pixelSize.x + 0.3f);
            mb.Add(0, new MeshBuilder.Line(new Vector2(r.x, r.y), new Vector2(r.x, r.yMax), coLine));
        }

        public override void BuildSelectionMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            var co = Color.white;
            if (r.width > pixelSize.x)
            {
                mb.Add(0, new MeshBuilder.Rectangle(r, co, co, co, co), false);
            }
            else
            {
                var coLine = co;
                coLine.a = Mathf.Clamp01(r.width / pixelSize.x + 0.3f);
                mb.Add(0, new MeshBuilder.Line(new Vector2(r.x, r.y), new Vector2(r.x, r.yMax), coLine));
            }
        }

        public override void CleanupMeshes()
        {
            m_CurrentMesh.CleanupMeshes();
        }

        public override ulong GetAddressBegin()
        {
            return m_Snapshot.managedStacks.startAddress[m_MemoryRegionId];
        }

        public override ulong GetAddressEnd()
        {
            return m_Snapshot.managedStacks.startAddress[m_MemoryRegionId] +
                (ulong)m_Snapshot.managedStacks.bytes[m_MemoryRegionId].Length;
        }

        public override string GetDisplayName()
        {
            return string.Format("Stack Sections {0}", m_MemoryRegionId);
        }

        public override string GetDisplayType()
        {
            return "Managed";
        }
    }
}
