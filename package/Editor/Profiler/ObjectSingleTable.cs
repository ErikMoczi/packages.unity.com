using Unity.MemoryProfiler.Editor.Database;

namespace Unity.MemoryProfiler.Editor
{
    internal class ObjectSingleTable : ObjectListTable
    {
        public ObjectData obj;
        public int objOffset;

        public ObjectSingleTable(Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectData obj, ObjectMetaType metaType)
            : base(schema, renderer, snapshot, crawledData, metaType)
        {
            this.obj = obj;
            if (!obj.dataIncludeObjectHeader)
            {
                var od = ObjectData.FromManagedPointer(snapshot, obj.hostManagedObjectPtr);
                objOffset = obj.managedObjectData.offset - od.managedObjectData.offset;
            }
            else
            {
                objOffset = 0;
            }
            InitObjectList();
            ExpandCell(0, 0, true);
        }

        public override string GetName()
        {
            var str = string.Format("0x{0:X16}", obj.hostManagedObjectPtr);
            return ObjectTable.TableName + "(" + str + ")";
        }

        public override string GetDisplayName() { return GetName(); }
        public override long GetObjectCount()
        {
            return 1;
        }

        public override string GetObjectName(long row)
        {
            string str;
            if (objOffset == 0)
            {
                str = string.Format("[0x{0:X16}]", obj.hostManagedObjectPtr);
            }
            else
            {
                str = string.Format("[0x{0:x16}+0x{1:x}]", obj.hostManagedObjectPtr, objOffset);
            }
            return str;
        }

        public override ObjectData GetObjectData(long row)
        {
            return obj;
        }

        public override bool GetObjectStatic(long row)
        {
            return false;
        }
    }
}
