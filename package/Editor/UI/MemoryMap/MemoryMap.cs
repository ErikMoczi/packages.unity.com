using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.MemoryProfiler.Editor.UI.MemoryMap
{
    public class MemoryMap
    {
        public CachedSnapshot m_Snapshot;
        private ZoomArea m_ZoomArea;

        public IViewEventListener m_EventListener;
        public MemoryMap(IViewEventListener l)
        {
            m_EventListener = l;
        }

        private List<Mesh2D> m_cachedMeshes = new List<Mesh2D>();
        private List<Mesh2D> m_SelectionMeshes = new List<Mesh2D>();

        private Vector2 mousePosition { get { return m_ZoomArea.ViewToWorldTransformPoint(Event.current.mousePosition); } }

        public delegate void OnSelectRegionsDelegate(MemoryRegion[] rngs);
        public OnSelectRegionsDelegate OnSelectRegions;

        public Rect mDrawRect;
        public int m_RowCount = 32;
        public ulong m_RowMemorySize;
        public ulong m_MemoryAddressMin;
        public ulong m_MemoryAddressMax;
        public MemoryRegion[] m_MemoryRegion;
        public MemoryRegion[] m_SelectedMemoryRegions;
        public class RegionMesh
        {
            public MemoryRegion m_Region;
            public class SectionMesh
            {
                public Rect m_Rect;
            }
        }
        public class Row
        {
            public ulong m_Begin;
            public ulong m_End;
            public Row(ulong begin, ulong end)
            {
                m_Begin = begin;
                m_End = end;
            }

            protected MemoryRegion[] m_Region;
            protected ulong[] m_RegionBegin;
            protected ulong[] m_RegionEnd;
            public void SetRegions(List<MemoryRegion> regionList)
            {
                var r = regionList.ToArray();
                System.Array.Sort(r, new MemoryRegion.EndComparer());
                m_Region = r;
                m_RegionBegin = new ulong[r.Length];
                m_RegionEnd = new ulong[r.Length];
                for (int i = 0; i != r.Length; ++i)
                {
                    m_RegionBegin[i] = r[i].GetAddressBegin();
                    m_RegionEnd[i] = r[i].GetAddressEnd();
                }
            }

            public MemoryRegion[] GetRegionIn(ulong minAddress, ulong maxAddress)
            {
                int first = Algorithm.LowerBound(m_RegionEnd, minAddress);
                int last = first;
                while (last < m_RegionBegin.Length && m_RegionBegin[last] < maxAddress)
                {
                    ++last;
                }
                MemoryRegion[] o = new MemoryRegion[last - first];
                Array.Copy(m_Region, first, o, 0, last - first);
                return o;
            }
        }
        public Row[] m_Row;

        Rect m_World_Rect;
        Vector2 m_World_TopLeft;
        Vector2 m_World_BottomRight;
        Vector2 m_World_Diff;
        float m_World_RowHeight;

        public void Setup(CachedSnapshot snapshot)//(MemoryProfilerWindow hostWindow, CrawledMemorySnapshot _unpackedCrawl)
        {
            m_Snapshot = snapshot;
            m_MemoryRegion = new MemoryRegion[m_Snapshot.nativeMemoryRegions.Count + m_Snapshot.managedHeapSections.Count + m_Snapshot.managedStacks.Count];
            for (int i = 0; i != m_Snapshot.nativeMemoryRegions.Count; ++i)
            {
                m_MemoryRegion[i] = new NativeMemoryRegion(m_Snapshot, i);
            }
            for (int i = 0; i != m_Snapshot.managedHeapSections.Count; ++i)
            {
                m_MemoryRegion[m_Snapshot.nativeMemoryRegions.Count + i] = new ManagedMemoryRegion(m_Snapshot, i);
            }
            for (int i = 0; i != m_Snapshot.managedStacks.Count; ++i)
            {
                m_MemoryRegion[m_Snapshot.nativeMemoryRegions.Count + m_Snapshot.managedHeapSections.Count + i] = new ManagedStackMemoryRegion(m_Snapshot, i);
            }
            ComputeFullMemoryRange();
            m_Row = new Row[m_RowCount];
            List<MemoryRegion>[] mrList = new List<MemoryRegion>[m_Row.Length];
            for (int i = 0; i != m_RowCount; ++i)
            {
                m_Row[i] = new Row(m_MemoryAddressMin + (ulong)i * m_RowMemorySize, m_MemoryAddressMin + (ulong)(i + 1) * m_RowMemorySize);
                mrList[i] = new List<MemoryRegion>();
            }

            foreach (var mr in m_MemoryRegion)
            {
                ulong b = mr.GetAddressBegin();
                ulong e = mr.GetAddressEnd();
                if (b != e)
                {
                    ulong br = MapAddressRow(b);
                    ulong er = MapAddressRow(e);
                    mrList[br].Add(mr);
                    if (er != br && er < (ulong)m_RowCount)
                    {
                        mrList[er].Add(mr);
                    }
                }
            }
            for (int i = 0; i != m_RowCount; ++i)
            {
                m_Row[i].SetRegions(mrList[i]);
            }

            m_ZoomArea = new ZoomArea();
            m_ZoomArea.resizeWorld(new Rect(-1, -1, 2, 2));

            ComputeWorldMetrics();
        }

        private void ComputeWorldMetrics()
        {
            m_World_Rect = m_ZoomArea.m_WorldSpace;
            m_World_TopLeft = new Vector2(m_World_Rect.xMin, m_World_Rect.yMax);
            m_World_BottomRight = new Vector2(m_World_Rect.xMax, m_World_Rect.yMin);
            m_World_Diff = m_World_BottomRight - m_World_TopLeft;
            m_World_RowHeight = m_World_Diff.y / m_RowCount;
        }

        public void OnGUI(Rect r)
        {
            mDrawRect = r;

            if (r != m_ZoomArea.m_ViewSpace)
            {
                m_ZoomArea.resizeView(r);
                RefreshMesh(r);
            }

            //_ZoomArea.rect = r;
            m_ZoomArea.BeginViewGUI();

            GUI.BeginGroup(r);
            Handles.matrix = m_ZoomArea.worldToViewMatrix;
            HandleMouseClick(r);
            RenderMap();
            GUI.EndGroup();

            m_ZoomArea.EndViewGUI();
        }

        public void SelectRegions(MemoryRegion[] rngs)
        {
            m_SelectedMemoryRegions = rngs;
            RefreshSelectionhMesh();
            if (OnSelectRegions != null) OnSelectRegions(m_SelectedMemoryRegions);
            if (m_EventListener != null) m_EventListener.OnRepaint();
        }

        private void HandleMouseClick(Rect r)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                var vMouseInWorld = m_ZoomArea.ViewToWorldTransformPoint(Event.current.mousePosition);
                var vMouseInWorldRect = vMouseInWorld - m_World_TopLeft;
                var mouseRow = (int)(vMouseInWorldRect.y / m_World_RowHeight);

                if (mouseRow >= 0 && mouseRow < m_RowCount)
                {
                    var xT = vMouseInWorldRect.x / m_World_Diff.x;
                    var xDelta = m_ZoomArea.worldPixelSize.x / m_World_Diff.x;
                    ulong addressBegin = (ulong)Mathf.Floor(xT * m_RowMemorySize) + m_Row[mouseRow].m_Begin;
                    ulong addressEnd = addressBegin + (ulong)Mathf.Ceil(xDelta * m_RowMemorySize);
                    var rngs = m_Row[mouseRow].GetRegionIn(addressBegin, addressEnd);
                    SelectRegions(rngs);
                }
            }
        }

        public void CleanupMeshes()
        {
            if (m_cachedMeshes == null)
            {
                m_cachedMeshes = new List<Mesh2D>();
            }
            else
            {
                for (int i = 0; i != m_MemoryRegion.Length; ++i)
                {
                    m_MemoryRegion[i].CleanupMeshes();
                }

                m_cachedMeshes.Clear();
            }
        }

        public void ComputeFullMemoryRange()
        {
            ulong memMin = ulong.MaxValue;
            ulong memMax = ulong.MinValue;
            for (int i = 0; i != m_MemoryRegion.Length; ++i)
            {
                ulong b = m_MemoryRegion[i].GetAddressBegin();
                ulong e = m_MemoryRegion[i].GetAddressEnd();
                if (b != e)
                {
                    memMin = Math.Min(memMin, b);
                    memMax = Math.Max(memMax, e);
                }
            }
            m_MemoryAddressMin = memMin;
            m_MemoryAddressMax = memMax;
            m_RowMemorySize = (ulong)Mathf.Ceil((m_MemoryAddressMax - m_MemoryAddressMin) / (float)m_RowCount);
        }

        private Vector2 MapAddressOld(Rect r, ulong addr)
        {
            float rowHeight = r.height / m_RowCount;
            float rowMemory = (m_MemoryAddressMax - m_MemoryAddressMin) / (float)m_RowCount;

            ulong offset = addr - m_MemoryAddressMin;
            float row = Mathf.Floor(offset / rowMemory);
            float rowMin = row * rowMemory;
            float inRow = (offset - rowMin) / rowMemory * r.width;

            return new Vector2(inRow, row * rowHeight);
        }

        private ulong MapAddressRow(ulong addr)
        {
            ulong offset = addr - m_MemoryAddressMin;
            ulong row = (ulong)(offset / m_RowMemorySize);
            return row;
        }

        private int MapYToRow(Rect r, float y)
        {
            ulong rowHeight = (ulong)(r.height / m_RowCount);
            int row = (int)(y / rowHeight);
            return row;
        }

        private ulong MapPixelToAddress(Rect r, Vector2 pos, int row)
        {
            ulong offset = (ulong)((pos.x / r.width + row) * m_RowMemorySize);
            return offset + m_MemoryAddressMin;
        }

        private ulong MapPixelToAddressRange(Rect r)
        {
            ulong addr = (ulong)Mathf.Ceil(m_RowMemorySize / r.width);
            return addr;
        }

        private MemoryRegionSection[] MapRegionToWorldRect(MemoryRegion rgn)
        {
            List<MemoryRegionSection> o = new List<MemoryRegionSection>();


            ulong b = rgn.GetAddressBegin();
            ulong e = rgn.GetAddressEnd();
            ulong first_row = MapAddressRow(b);
            ulong Last_row = MapAddressRow(e);
            ulong minInRowByte;
            ulong maxInRowByte;
            while (b < e)
            {
                minInRowByte = (b - m_MemoryAddressMin) - (first_row * m_RowMemorySize);
                ulong e2;
                if (first_row == Last_row)
                {
                    maxInRowByte = (e - m_MemoryAddressMin) - (first_row * m_RowMemorySize);
                    e2 = e;
                }
                else
                {
                    maxInRowByte = m_RowMemorySize;
                    e2 = m_MemoryAddressMin + (first_row * m_RowMemorySize) + m_RowMemorySize;
                }
                float minRatio = minInRowByte / (float)m_RowMemorySize;
                float maxRatio = maxInRowByte / (float)m_RowMemorySize;
                var vMin = m_World_TopLeft + new Vector2(minRatio * m_World_Diff.x, first_row * m_World_RowHeight);
                var vMax = m_World_TopLeft + new Vector2(maxRatio * m_World_Diff.x, (first_row + 1) * m_World_RowHeight);
                Rect rr = new Rect(vMin.x, vMin.y, vMax.x - vMin.x, vMax.y - vMin.y);
                o.Add(new MemoryRegionSection(rr, b, e2));

                ++first_row;
                b = (first_row * m_RowMemorySize) + m_MemoryAddressMin;
            }
            return o.ToArray();
        }

        private void RefreshMesh(Rect r)
        {
            m_cachedMeshes.Clear();


            MeshBuilder mb = new MeshBuilder();
            for (int i = 1; i != m_RowCount; ++i)
            {
                float rowRatio = i / (float)m_RowCount;
                var vMin = m_World_TopLeft + new Vector2(0, rowRatio * m_World_Diff.y);
                var vMax = m_World_TopLeft + new Vector2(m_World_Diff.x, rowRatio * m_World_Diff.y);
                mb.Add(0, new MeshBuilder.Line(vMin, vMax, new Color(0.33f, 0.33f, 0.33f)));
            }
            m_cachedMeshes.Add(mb.CreateMesh());


            mb = new MeshBuilder();
            for (int i = 0; i != m_MemoryRegion.Length; ++i)
            {
                MemoryRegionSection[] sections = MapRegionToWorldRect(m_MemoryRegion[i]);
                foreach (var sec in sections)
                {
                    m_MemoryRegion[i].BuildMeshSection(mb, sec.rect, sec.beginAddress, sec.endAddress, m_ZoomArea.worldPixelSize);
                }
            }
            m_cachedMeshes.Add(mb.CreateMesh());


            mb = new MeshBuilder();
            Rect rAll = new Rect(m_World_TopLeft.x, m_World_TopLeft.y, m_World_Diff.x - 0.00001f, m_World_Diff.y + 0.00001f);
            mb.Add(0, new MeshBuilder.Rectangle(rAll, Color.white), false);
            m_cachedMeshes.Add(mb.CreateMesh());
        }

        private void RefreshSelectionhMesh()
        {
            m_SelectionMeshes.Clear();


            MeshBuilder mb = new MeshBuilder();
            for (int i = 0; i != m_SelectedMemoryRegions.Length; ++i)
            {
                MemoryRegionSection[] sections = MapRegionToWorldRect(m_SelectedMemoryRegions[i]);
                foreach (var sec in sections)
                {
                    m_MemoryRegion[i].BuildSelectionMeshSection(mb, sec.rect, sec.beginAddress, sec.endAddress, m_ZoomArea.worldPixelSize);
                }
            }
            m_SelectionMeshes.Add(mb.CreateMesh());
        }

        public void RenderMap()
        {
            if (Event.current.type != EventType.Repaint || m_cachedMeshes == null)
                return;

            for (int i = 0; i < m_cachedMeshes.Count; i++)
            {
                m_cachedMeshes[i].Render();
            }
            for (int i = 0; i < m_SelectionMeshes.Count; i++)
            {
                m_SelectionMeshes[i].Render();
            }
        }
    }
}
