namespace Unity.MemoryProfiler.Editor
{
    public class ObjectAllManagedTable : ObjectListTable
    {
        public new const string kTableName = "AllManagedObject";
        public new const string kTableDisplayName = "All Managed Object";
        private ObjectData[] m_cache;
        public ObjectAllManagedTable(Database.Scheme scheme, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(scheme, renderer, snapshot, crawledData, metaType)
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
            var mo = crawledData.managedObjects[(int)row];
            if (!m_cache[row].IsValid)
            {
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
    public class ObjectAllNativeTable : ObjectListTable
    {
        public new const string kTableName = "AllNativeObject";
        public new const string kTableDisplayName = "All Native Object";
        private ObjectData[] m_cache;
        public ObjectAllNativeTable(Database.Scheme scheme, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(scheme, renderer, snapshot, crawledData, metaType)
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
    public class ObjectAllTable : ObjectListTable
    {
        public new const string kTableName = "AllObject";
        public new const string kTableDisplayName = "All Object";
        private ObjectData[] m_cache;
        public ObjectAllTable(Database.Scheme scheme, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(scheme, renderer, snapshot, crawledData, metaType)
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
