using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI.MemoryMap
{
    public class NativeMemoryRegion : MemoryRegion
    {
        public CachedSnapshot m_Snapshot;
        public int m_MemoryRegionId;
        public Mesh2D m_CurrentMesh;
        public NativeMemoryRegion(CachedSnapshot snapshot, int memoryRegionId)
        {
            m_Snapshot = snapshot;
            m_MemoryRegionId = memoryRegionId;
        }

        //public override Mesh2D BuildMeshSection(Rect r, int firstPixel, Vector2 pixelSize)
        public override void BuildMeshSection(MeshBuilder mb, Rect r, ulong memBegin, ulong memEnd, Vector2 pixelSize)
        {
            //r.width / pixelSize.x;

            if (GetAddressBegin() != GetAddressEnd())
            {
                var co = ProfilerColors.currentColors[0];
                var colorRectBg = co;
                colorRectBg.a *= 0.1f;
                var colorRectBgHigh = new Color(1, 1, 1, colorRectBg.a);
                var colorRectBgLow = new Color(0, 0, 0, colorRectBg.a);

                var colorRectOutline = co;
                var colorRectOutlineHigh = new Color(1, 1, 1, colorRectOutline.a);
                var colorRectOutlineLow = new Color(0, 0, 0, colorRectOutline.a);

                if (r.width > pixelSize.x)
                {
                    //background
                    mb.Add(0, new MeshBuilder.Rectangle(r
                            , colorRectBg
                            , Color.Lerp(colorRectBg, colorRectBgHigh, 0.33f)
                            , colorRectBg
                            , Color.Lerp(colorRectBg, colorRectBgLow, 0.33f)
                            ), true);


                    //render allocation distribution
                    var coOn = co;
                    var coOff = co;
                    coOff.a = 0;
                    float memSize = memEnd - memBegin;
                    ulong numPixels = (ulong)(r.width / pixelSize.x);

                    int firstAlloc = m_Snapshot.nativeMemoryRegions.firstAllocationIndex[m_MemoryRegionId];
                    int countAlloc = m_Snapshot.nativeMemoryRegions.numAllocations[m_MemoryRegionId];

                    float[] pix = new float[numPixels + 1];
                    for (int i = 0; i != countAlloc; ++i)
                    {
                        int indexAlloc = firstAlloc + i;
                        ulong b = m_Snapshot.nativeAllocations.address[indexAlloc];
                        ulong s = (ulong)m_Snapshot.nativeAllocations.size[indexAlloc];
                        ulong e = b + s;
                        if (b < memBegin || e > memEnd)
                        {
                            continue;
                        }
                        float bRatio = (b - memBegin) / memSize;
                        float eRatio = (e - memBegin) / memSize;
                        float bPixelf = (bRatio * numPixels);
                        float ePixelf = (eRatio * numPixels);
                        int bPixel = Mathf.FloorToInt(bPixelf);
                        int ePixel = Mathf.CeilToInt(ePixelf);
                        float bIntensity = 1 - (bPixelf - bPixel);
                        float eIntensity = ePixel - ePixelf;
                        if (bPixel == ePixel)
                        {
                            pix[bPixel] += bIntensity + eIntensity - 1;
                        }
                        else
                        {
                            pix[bPixel] += bIntensity;
                            pix[ePixel] += eIntensity;
                            for (int k = bPixel + 1; k < ePixel; ++k)
                            {
                                pix[k] += 1;
                            }
                        }
                    }
                    for (int i = 0; i != pix.Length; ++i)
                    {
                        float pRatio = i / (float)pix.Length;
                        float xPos = pRatio * r.width + r.xMin;
                        var color = Color.Lerp(coOff, coOn, pix[i]);
                        if (color.a > 0)
                        {
                            mb.Add(1, new MeshBuilder.Line(new Vector2(xPos, r.yMin), new Vector2(xPos, r.yMax), color));
                        }
                    }


                    //render outline
                    if (r.width > pixelSize.x)
                    {
                        mb.Add(1, new MeshBuilder.Rectangle(r
                                , colorRectOutline
                                , Color.Lerp(colorRectOutline, colorRectOutlineHigh, 0.50f)
                                , colorRectOutline
                                , Color.Lerp(colorRectOutline, colorRectOutlineLow, 0.5f)
                                ), false);
                    }
                }
                else
                {
                    var coLine = co;
                    coLine.a *= Mathf.Clamp01(r.width / pixelSize.x + 0.3f);
                    mb.Add(0, new MeshBuilder.Line(new Vector2(r.x, r.y), new Vector2(r.x, r.yMax), coLine));
                }
            }
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
            return m_Snapshot.nativeMemoryRegions.addressBase[m_MemoryRegionId];
        }

        public override ulong GetAddressEnd()
        {
            return m_Snapshot.nativeMemoryRegions.addressBase[m_MemoryRegionId] +
                m_Snapshot.nativeMemoryRegions.addressSize[m_MemoryRegionId];
        }

        public override string GetDisplayName()
        {
            return m_Snapshot.nativeMemoryRegions.memoryRegionName[m_MemoryRegionId];
        }

        public override string GetDisplayType()
        {
            return "Native";
        }
    }
}
