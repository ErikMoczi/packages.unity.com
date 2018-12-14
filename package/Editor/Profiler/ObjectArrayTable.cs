namespace Unity.MemoryProfiler.Editor
{
    internal class ObjectArrayTable : ObjectListTable
    {
        public new const string kTableName = "ManagedObjectArray";
        public new const string kTableDisplayName = "Managed Object Array";
        public ObjectData arrayData;
        public ArrayInfo arrayInfo;
        public ObjectArrayTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectData arrayData, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            this.arrayData = arrayData;

            if (arrayData.isManaged)
            {
                arrayInfo = ArrayTools.GetArrayInfo(snapshot, snapshot.managedHeapSections, arrayData.managedObjectData, arrayData.managedTypeIndex, snapshot.virtualMachineInformation);
            }

            InitObjectList();
        }

        public override string GetName()
        {
            if (arrayInfo == null) return kTableName + "(null)";
            var str = string.Format("0x{0:X16}", arrayData.hostManagedObjectPtr);
            return kTableName + "(" + str + ")";
        }

        public override string GetDisplayName()
        {
            if (arrayInfo == null) return kTableDisplayName + "(null)";
            var str = string.Format("0x{0:X16}", arrayData.hostManagedObjectPtr);
            return kTableDisplayName + "(" + str + ")";
        }

        public override long GetObjectCount()
        {
            if (arrayInfo == null) return 0;
            return arrayInfo.length;
        }

        public override string GetObjectName(long row)
        {
            var str = "[" + arrayInfo.IndexToRankedString((int)row) + "]";
            return str;
        }

        public override ObjectData GetObjectData(long row)
        {
            return arrayData.GetArrayElement(snapshot, arrayInfo, (int)row, true);
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }
    }
}
