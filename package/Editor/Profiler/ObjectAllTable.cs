namespace Unity.MemoryProfiler.Editor
{
    internal class ObjectAllManagedTable : ObjectListTable
    {
        public new const string kTableName = "AllManagedObjects";
        public new const string kTableDisplayName = "All Managed Objects";
        private ObjectData[] m_cache;
        public ObjectAllManagedTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            InitObjectList();
        }

        public override string GetName()
        {
            return kTableName;
        }

        public override string GetDisplayName()
        {
            return kTableDisplayName;
        }

        public override long GetObjectCount()
        {
            return crawledData.managedObjects.Count;
        }

        public override ObjectData GetObjectData(long row)
        {
            if (m_cache == null)
            {
                m_cache = new ObjectData[crawledData.managedObjects.Count];
            }

            if (row < 0 || row >= crawledData.managedObjects.Count)
            {
                UnityEngine.Debug.Log("GetObjectData out of range");
            }
            if (!m_cache[row].IsValid)
            {
                var mo = crawledData.managedObjects[(int)row];
                m_cache[row] = ObjectData.FromManagedPointer(snapshot, mo.ptrObject);
            }
            return m_cache[row];
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }

        public override void EndUpdate(IUpdater updater)
        {
            base.EndUpdate(updater);
            m_cache = null;
        }
    }
    internal class ObjectAllNativeTable : ObjectListTable
    {
        public new const string kTableName = "AllNativeObjects";
        public new const string kTableDisplayName = "All Native Objects";
        private ObjectData[] m_cache;
        public ObjectAllNativeTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            InitObjectList();
        }

        public override string GetName()
        {
            return kTableName;
        }

        public override string GetDisplayName()
        {
            return kTableDisplayName;
        }

        public override long GetObjectCount()
        {
            return snapshot.nativeObjects.Count;
        }

        public override ObjectData GetObjectData(long row)
        {
            if (m_cache == null)
            {
                m_cache = new ObjectData[snapshot.nativeObjects.Count];
            }
            if (!m_cache[row].IsValid)
            {
                m_cache[row] = ObjectData.FromNativeObjectIndex(snapshot, (int)row);
            }
            return m_cache[row];
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }

        public override void EndUpdate(IUpdater updater)
        {
            base.EndUpdate(updater);
            m_cache = null;
        }
    }
    internal class ObjectAllTable : ObjectListTable
    {
        public new const string kTableName = "AllObjects";
        public new const string kTableDisplayName = "All Objects";
        private ObjectData[] m_cache;
        public ObjectAllTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            InitObjectList();
        }

        public override string GetName()
        {
            return kTableName;
        }

        public override string GetDisplayName()
        {
            return kTableDisplayName;
        }

        public override long GetObjectCount()
        {
            return snapshot.nativeObjects.Count + crawledData.managedObjects.Count;
        }

        public override ObjectData GetObjectData(long row)
        {
            if (m_cache == null)
            {
                m_cache = new ObjectData[snapshot.nativeObjects.Count + crawledData.managedObjects.Count];
            }
            if (!m_cache[row].IsValid)
            {
                var iNative = snapshot.UnifiedObjectIndexToNativeObjectIndex((int)row);
                if (iNative >= 0)
                {
                    m_cache[row] = ObjectData.FromNativeObjectIndex(snapshot, iNative);
                }
                var iManaged = snapshot.UnifiedObjectIndexToManagedObjectIndex((int)row);
                if (iManaged >= 0)
                {
                    m_cache[row] = ObjectData.FromManagedObjectIndex(snapshot, iManaged);
                }
            }
            return m_cache[row];
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }

        public override void EndUpdate(IUpdater updater)
        {
            base.EndUpdate(updater);
            m_cache = null;
        }

        //public override int GetObjectType(long row)
        //{
        //    var mo = crawledData.managedObjects[row];
        //    return mo.iTypeDescription;
        //}
    }
}
