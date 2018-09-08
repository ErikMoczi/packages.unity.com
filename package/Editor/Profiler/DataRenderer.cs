using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor
{
    public class DataRenderer
    {
        public Dictionary<string, ITypeRenderer> m_TypeRenderer = new Dictionary<string, ITypeRenderer>();
        public bool forceLinkAllObject = false;
        public bool flattenFields = false;
        public bool flattenStaticFields = false;
        public bool showPrettyNames = true;

#if MEMPROFILER_DEBUG_INFO
        public bool showDebugValue = false;
#endif
        protected void AddTypeRenderer(ITypeRenderer renderer)
        {
            var name = renderer.GetTypeName();
            m_TypeRenderer.Add(name, renderer);
        }

        public DataRenderer()
        {
            AddTypeRenderer(new Int16TypeDisplay());
            AddTypeRenderer(new Int32TypeDisplay());
            AddTypeRenderer(new Int64TypeDisplay());
            AddTypeRenderer(new UInt16TypeDisplay());
            AddTypeRenderer(new UInt32TypeDisplay());
            AddTypeRenderer(new UInt64TypeDisplay());
            AddTypeRenderer(new BooleanTypeDisplay());
            AddTypeRenderer(new CharTypeDisplay());
            AddTypeRenderer(new DoubleTypeDisplay());
            AddTypeRenderer(new SingleTypeDisplay());
            AddTypeRenderer(new StringTypeDisplay());
            AddTypeRenderer(new IntPtrTypeDisplay());
            AddTypeRenderer(new ByteTypeDisplay());
        }
    }


    public class SnapshotDataRenderer
    {
        public DataRenderer m_BaseRenderer;

        public Dictionary<int, ITypeRenderer> m_TypeRenderer = new Dictionary<int, ITypeRenderer>();
        public CachedSnapshot m_Snapshot;
        public bool flattenFields { get { return m_BaseRenderer.flattenFields; } }
        public bool flattenStaticFields { get { return m_BaseRenderer.flattenStaticFields; } }
        public bool forceLinkAllObject { get { return m_BaseRenderer.forceLinkAllObject; } }

        public SnapshotDataRenderer(DataRenderer dataRenderer, CachedSnapshot d)
        {
            m_BaseRenderer = dataRenderer;
            m_Snapshot = d;
            foreach (var tr in dataRenderer.m_TypeRenderer)
            {
                int i = m_Snapshot.typeDescriptions.typeDescriptionName.FindIndex(x => x == tr.Key);
                if (i >= 0)
                {
                    m_TypeRenderer[i] = tr.Value;
                }
            }
        }

        public bool IsExpandable(int iTypeIndex)
        {
            if (iTypeIndex < 0) return false;
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(iTypeIndex, out td))
            {
                return td.Expandable();
            }
            return true;
        }

        public string RenderPointer(ulong ptr)
        {
            if (ptr == 0) return "null";
            return string.Format("[0x{0:x16}]", ptr);
        }

        public string RenderValueType(ObjectData od)
        {
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
            {
                return td.Render(m_Snapshot, od);
            }
            return "{...}";
        }

        public string RenderObject(ObjectData od)
        {
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
            {
                return td.Render(m_Snapshot, od);
            }
            return RenderPointer(od.hostManagedObjectPtr);
        }

        public string RenderArray(ObjectData od)
        {
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
            {
                return td.Render(m_Snapshot, od);
            }
            var str = RenderPointer(od.hostManagedObjectPtr);
            if (od.hostManagedObjectPtr != 0)
            {
                var arrayInfo = ArrayTools.GetArrayInfo(m_Snapshot, m_Snapshot.managedHeapSections, od.managedObjectData, od.managedTypeIndex, m_Snapshot.virtualMachineInformation);
                str += "[" + arrayInfo.ArrayRankToString() + "]";
            }
            return str;
        }

        public string Render(ObjectData od)
        {
            switch (od.dataType)
            {
                case ObjectDataType.BoxedValue:
                    return RenderValueType(od.GetBoxedValue(m_Snapshot, true));
                case ObjectDataType.Value:
                    return RenderValueType(od);
                case ObjectDataType.Object:
                    return RenderObject(od);
                case ObjectDataType.Array:
                    return RenderArray(od);
                case ObjectDataType.ReferenceObject:
                {
                    ulong ptr = od.GetReferencePointer();
                    if (ptr == 0)
                    {
                        return RenderPointer(ptr);
                    }
                    else
                    {
                        var o = ObjectData.FromManagedPointer(m_Snapshot, ptr);
                        return RenderObject(o);
                    }
                }
                case ObjectDataType.ReferenceArray:
                {
                    ulong ptr = od.GetReferencePointer();
                    if (ptr == 0)
                    {
                        RenderPointer(ptr);
                    }
                    var arr = ObjectData.FromManagedPointer(m_Snapshot, ptr);
                    return RenderArray(arr);
                }
                case ObjectDataType.Type:
                    return m_Snapshot.typeDescriptions.typeDescriptionName[od.managedTypeIndex];
                case ObjectDataType.Global:
                    return "<global>";
                case ObjectDataType.NativeObject:
                    return RenderPointer(m_Snapshot.nativeObjects.nativeObjectAddress[od.nativeObjectIndex]);
                default:
                    return "<unknown type>";
            }
        }
    }
}
