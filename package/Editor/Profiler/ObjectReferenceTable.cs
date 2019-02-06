namespace Unity.MemoryProfiler.Editor
{
    internal class ObjectReferenceTable : ObjectListTable
    {
        public static string kObjectReferenceTableName = "ManagedObjectReference";
        //not used //static string k_ObjectReferenceTableDisplayName = "Managed Object Reference";
        //not used //public ManagedConnection[] managedReference;

        ObjectData m_Object;
        ObjectData[] m_References;

        public ObjectReferenceTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectData obj, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            m_Object = obj;
            m_References = ObjectConnection.GetAllObjectConnectingTo(snapshot, obj);
#if DEBUG_VALIDATION
            int toUnifiedIndex = m_Object.GetUnifiedObjectIndex(snapshot);
            var toManagedIndex = snapshot.UnifiedObjectIndexToManagedObjectIndex(toUnifiedIndex);

            for (int k = 0; k != m_References.Length; ++k)
            {
                var oFrom = m_References[k];

                switch (oFrom.dataType)
                {
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.Value:
                        if (oFrom.m_Parent != null)
                        {
                            oFrom = oFrom.m_Parent.obj;
                        }
                        break;
                }

                bool connectionFound = false;
                switch (oFrom.dataType)
                {
                    case ObjectDataType.Global:
                    {
                        if (snapshot.HasGlobalConnection(toManagedIndex))
                        {
                            connectionFound = true;
                        }
                    }
                    break;
                    case ObjectDataType.Type:
                    {
                        if (snapshot.HasManagedTypeConnection(oFrom.managedTypeIndex, toManagedIndex))
                        {
                            connectionFound = true;
                        }
                    }
                    break;
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.Object:
                    {
                        if (snapshot.HasObjectConnection(oFrom.GetUnifiedObjectIndex(snapshot), toUnifiedIndex))
                        {
                            connectionFound = true;
                            break;
                        }
                    }
                    break;
                    case ObjectDataType.NativeObject:
                    {
                        if (snapshot.HasObjectConnection(oFrom.GetUnifiedObjectIndex(snapshot), toUnifiedIndex))
                        {
                            connectionFound = true;
                            break;
                        }
                    }
                    break;
                }
                if (!connectionFound)
                {
                    UnityEngine.Debug.LogError("Connection not found, index =" + k);
                }
            }
#endif
            InitObjectList();
        }

        public override string GetName()
        {
            var str = Renderer.Render(m_Object); //string.Format("0x{0:X16}", ptr);
            return kObjectReferenceTableName + "(" + str + ")";
        }

        public override string GetDisplayName() { return GetName(); }


        public override long GetObjectCount()
        {
            return m_References.LongLength;
        }

        public override string GetObjectName(long row)
        {
            if (m_References[row].isManaged)
            {
                if (m_References[row].m_Parent != null)
                {
                    var typeIndex = m_References[row].m_Parent.obj.managedTypeIndex;
                    var typeName = Snapshot.typeDescriptions.typeDescriptionName[typeIndex];
                    var iField = m_References[row].m_Parent.iField;
                    var arrayIndex = m_References[row].m_Parent.arrayIndex;
                    if (iField >= 0)
                    {
                        var fieldName = Snapshot.fieldDescriptions.fieldDescriptionName[iField];
                        return typeName + "." + fieldName;
                    }
                    else if (arrayIndex >= 0)
                    {
                        if (typeName.EndsWith("[]"))
                        {
                            return typeName.Substring(0, typeName.Length - 2) + "[" + arrayIndex + "]";
                        }
                        else
                        {
                            return typeName + "[" + arrayIndex + "]";
                        }
                    }
                }
            }
            return "[" + row + "]";
        }

        public override ObjectData GetObjectData(long row)
        {
            return m_References[row];
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }
    }
}
