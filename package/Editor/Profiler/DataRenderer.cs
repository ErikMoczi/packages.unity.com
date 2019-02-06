using System.Collections.Generic;
using System;

namespace Unity.MemoryProfiler.Editor
{
    internal class DataRenderer
    {
        public Dictionary<string, ITypeRenderer> m_TypeRenderer = new Dictionary<string, ITypeRenderer>();
        public bool forceLinkAllObject = false;
        public bool flattenFields = true;
        public bool flattenStaticFields = true;
        public bool ShowPrettyNames
        {
            get
            {
                return m_ShowPrettyNames;
            }
            set
            {
                if(value != m_ShowPrettyNames)
                {
                    m_ShowPrettyNames = value;
                    PrettyNamesOptionChanged();
                }
            }
        }
        bool m_ShowPrettyNames = true;

        public event Action PrettyNamesOptionChanged = delegate { };


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

        public void Clear()
        {
            PrettyNamesOptionChanged = delegate { };
        }
    }


    internal class SnapshotDataRenderer
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

        public void Clear()
        {
            m_Snapshot = null;
            m_BaseRenderer.Clear();
        }

        // Renders using available ITypeRenderer or return false if none available
        public bool TryCustomRender(ObjectData od, out string result)
        {
            if (od.isManaged)
            {
                ITypeRenderer td;
                if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
                {
                    result = td.Render(m_Snapshot, od);
                    return true;
                }
            }
            result = null;
            return false;
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

        public string FormatPointer(ulong ptr)
        {
            return string.Format("0x{0:x16}", ptr);
        }

        // renders "[ptr]" or "null" if ptr == 0
        public string RenderPointer(ulong ptr)
        {
            if (ptr == 0) return "null";
            return string.Format("[{0}]", FormatPointer(ptr));
        }

        // renders "[ptr+offset]"
        public string RenderPointerAndOffset(ulong ptr, int offset)
        {
            if(offset >= 0)
            {
                return string.Format("[{0}+{1:x}]", FormatPointer(ptr), offset);
            }
            else
            {
                return string.Format("[{0}-{1:x}]", FormatPointer(ptr), -offset);
            }
        }

        // renders "[ptr][index]"
        public string RenderPointerAndIndex(ulong ptr, int index)
        {
            return string.Format("[{0}][{1}]", FormatPointer(ptr), index);
        }

        // Renders "{field=value, ...}"
        public string RenderObjectBrief(ObjectData od, bool objectBrief)
        {
            if (objectBrief)
            {
                string result = "{";
                var iid = od.GetInstanceID(m_Snapshot);
                if(iid != ObjectData.InvalidInstanceID)
                {
                    result += "InstanceID=" + iid;
                }
                int fieldCount = od.GetInstanceFieldCount(m_Snapshot);
                if (fieldCount > 0)
                {
                    if (iid != ObjectData.InvalidInstanceID)
                    {
                        result += ", ";
                    }
                    var field = od.GetInstanceFieldByIndex(m_Snapshot, 0);
                    string k = field.GetFieldName(m_Snapshot);
                    string v = Render(field, false);
                    if (fieldCount > 1)
                    {
                        return result + k + "=" + v + ", ...}";
                    }
                    else
                    {
                        return result + k + "=" + v + "}";
                    }
                }
                else
                {
                    return result + "}";
                }
            }
            ulong ptr;
            if(od.TryGetObjectPointer(out ptr))
            {
                return RenderPointer(ptr);
            }
            return "{...}";
        }
        public string RenderValueType(ObjectData od, bool objectBrief)
        {
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
            {
                return td.Render(m_Snapshot, od);
            }
            return RenderObjectBrief(od, objectBrief);
        }

        public string RenderObject(ObjectData od, bool objectBrief)
        {
            ITypeRenderer td;
            if (m_TypeRenderer.TryGetValue(od.managedTypeIndex, out td))
            {
                return td.Render(m_Snapshot, od);
            }
            return RenderObjectBrief(od, objectBrief);
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
                var arrayInfo = ArrayTools.GetArrayInfo(m_Snapshot, od.managedObjectData, od.managedTypeIndex);
                str += "[" + arrayInfo.ArrayRankToString() + "]";
            }
            return str;
        }

        public string Render(ObjectData od, bool objectBrief = true)
        {
            switch (od.dataType)
            {
                case ObjectDataType.BoxedValue:
                    return RenderValueType(od.GetBoxedValue(m_Snapshot, true), objectBrief);
                case ObjectDataType.Value:
                    return RenderValueType(od, objectBrief);
                case ObjectDataType.Object:
                    return RenderObject(od, objectBrief);
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
                        return RenderObject(o, objectBrief);
                    }
                }
                case ObjectDataType.ReferenceArray:
                {
                    ulong ptr = od.GetReferencePointer();
                    if (ptr == 0)
                    {
                        return RenderPointer(ptr);
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

        public string RenderInstanceId(CodeType codeType, int iid)
        {
            switch (codeType)
            {
                case CodeType.Managed:
                    return "iid=" + iid + ",M";
                case CodeType.Native:
                    return "iid=" + iid + ",N";
                default:
                    return "iid=" + iid;
            }
        }

        // Renders a string that *should* uniquely identify an object through multiple snapshots and multiple sessions
        public string RenderUniqueString(ObjectData od)
        {
            switch (od.dataType)
            {
                case ObjectDataType.Type:
                    return m_Snapshot.typeDescriptions.typeDescriptionName[od.managedTypeIndex];
                case ObjectDataType.Global:
                    return "<global>";
                case ObjectDataType.NativeObject:
                    return RenderInstanceId(od.codeType, m_Snapshot.nativeObjects.instanceId[od.nativeObjectIndex]);
                case ObjectDataType.Object:
                    {
                        int index = od.GetManagedObjectIndex(m_Snapshot);
                        if (index >= 0)
                        {
                            int nativeIndex = m_Snapshot.CrawledData.ManagedObjects[index].NativeObjectIndex;
                            if (nativeIndex >= 0)
                            {
                                return RenderInstanceId(CodeType.Managed, m_Snapshot.nativeObjects.instanceId[nativeIndex]);
                            }
                            ulong ptr;
                            if (od.TryGetObjectPointer(out ptr))
                            {
                                return RenderPointer(ptr);
                            }
                        }
                        goto default;
                    }
                case ObjectDataType.Unknown:
                    return "<unknown>";
                default:
                    {
                        if (od.IsField())
                        {
                            int offset = m_Snapshot.fieldDescriptions.offset[od.fieldIndex];
                            ulong objPtr = od.GetObjectPointer(m_Snapshot);
                            return RenderPointerAndOffset(objPtr, offset);
                        }
                        else if (od.IsArrayItem())
                        {
                            ulong objPtr = od.GetObjectPointer(m_Snapshot);
                            return RenderPointerAndIndex(objPtr, od.arrayIndex);
                        }
                        else
                        {
                            ulong ptr;
                            if (od.TryGetObjectPointer(out ptr))
                            {
                                return RenderPointer(ptr);
                            }
                            return od.GetUnifiedObjectIndex(m_Snapshot).ToString();
                        }
                    }
            }
        }
    }
}
