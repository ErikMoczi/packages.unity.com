using System.Collections.Generic;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor
{
    public class ObjectListUnifiedIndexColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListUnifiedIndexColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListUnifiedIndexColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override int GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            var i = obj.GetUnifiedObjectIndex(table.snapshot);
            return i;
        }

        public override string GetRowValueString(long row)
        {
            var i = GetRowValue(row);
            if (i < 0) return "";
            return i.ToString();
        }

        public override void SetRowValue(long row, int value)
        {
        }
    }
    public class ObjectListNameColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNameColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListNameColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            return table.GetObjectName(row);
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }
    public class ObjectListValueColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListValueColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListValueColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            var result = table.renderer.Render(obj);
            return result;
        }

        public override void SetRowValue(long row, string value)
        {
        }

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            if (!table.IsGroupLinked(obj)) return null;
            switch (obj.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                {
                    var lr = new Database.View.LinkRequest();
                    lr.metaLink = new Database.View.MetaLink();
                    lr.metaLink.linkViewName = ObjectTable.kTableName;
                    lr.sourceTable = table;
                    lr.sourceColumn = this;
                    lr.row = row;
                    lr.param = new Database.ParameterSet();
                    lr.param.param.Add(ObjectTable.kObjParamName, new Database.Operation.ExpConst<ulong>(obj.hostManagedObjectPtr));
                    lr.param.param.Add(ObjectTable.kTypeParamName, new Database.Operation.ExpConst<int>(obj.managedTypeIndex));
                    return lr;
                }

                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                {
                    ulong result = obj.GetReferencePointer();
                    if (result == 0) return null;
                    var lr = new Database.View.LinkRequest();
                    lr.metaLink = new Database.View.MetaLink();
                    lr.metaLink.linkViewName = ObjectTable.kTableName;
                    lr.sourceTable = table;
                    lr.sourceColumn = this;
                    lr.row = row;
                    lr.param = new Database.ParameterSet();
                    lr.param.param.Add(ObjectTable.kObjParamName, new Database.Operation.ExpConst<ulong>(result));
                    return lr;
                }
                default:
                    return null;
            }
        }
    }
    public class ObjectListTypeColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListTypeColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListTypeColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var d = table.GetObjectData(row).displayObject;
            switch (d.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.Value:
                    if (d.managedTypeIndex < 0) return "<unknown type>";
                    return table.snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];

                case ObjectDataType.ReferenceObject:
                {
                    var ptr = d.GetReferencePointer();
                    if (ptr != 0)
                    {
                        var obj = ObjectData.FromManagedPointer(table.snapshot, ptr);
                        if (obj.IsValid && obj.managedTypeIndex != d.managedTypeIndex)
                        {
                            return "(" + table.snapshot.typeDescriptions.typeDescriptionName[obj.managedTypeIndex] + ") "
                                + table.snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];
                        }
                    }
                    return table.snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];
                }

                case ObjectDataType.Global:
                    return "Global";
                case ObjectDataType.Type:
                    return "Type";
                case ObjectDataType.NativeObject:
                {
                    int iType = table.snapshot.nativeObjects.nativeTypeArrayIndex[d.nativeObjectIndex];
                    return table.snapshot.nativeTypes.typeName[iType];
                }
                case ObjectDataType.Unknown:
                default:
                    return "<unknown>";
            }
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }

    public class ObjectListLengthColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListLengthColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListLengthColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var l = GetRowValue(row);
            if (l < 0) return "";
            return l.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.ReferenceArray:
                {
                    obj = ObjectData.FromManagedPointer(table.snapshot, obj.GetReferencePointer());
                    if (obj.hostManagedObjectPtr != 0)
                    {
                        goto case ObjectDataType.Array;
                    }
                    return -1;
                }
                case ObjectDataType.Array:
                {
                    var arrayInfo = ArrayTools.GetArrayInfo(table.snapshot, table.snapshot.managedHeapSections, obj.managedObjectData, obj.managedTypeIndex, table.snapshot.virtualMachineInformation);
                    return arrayInfo.length;
                }
                default:
                    return -1;
            }
        }

        public override void SetRowValue(long row, int value)
        {
        }
    }
    public class ObjectListStaticColumn : Database.ColumnTyped<bool>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListStaticColumn<bool>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListStaticColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override bool GetRowValue(long row)
        {
            var b = table.GetObjectStatic(row);
            return b;
        }

        public override void SetRowValue(long row, bool value)
        {
        }
    }
    public class ObjectListRefCountColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListRefCountColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListRefCountColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var rc = GetRowValue(row);
            if (rc < 0)
            {
                return "N/A";
            }
            return rc.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                {
                    var ptr = obj.hostManagedObjectPtr;
                    if (ptr > 0)
                    {
                        ManagedObjectInfo moi;
                        if (table.crawledData.managedObjectByAddress.TryGetValue(ptr, out moi))
                        {
                            return moi.refCount;
                        }
                    }
                    break;
                }

                case ObjectDataType.NativeObject:
                    return table.snapshot.nativeObjects.refcount[obj.nativeObjectIndex];
            }
            return -1;
        }

        public override void SetRowValue(long row, int value)
        {
        }

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            int i = obj.GetUnifiedObjectIndex(table.snapshot);
            if (i >= 0)
            {
                var lr = new Database.View.LinkRequest();
                lr.metaLink = new Database.View.MetaLink();
                lr.metaLink.linkViewName = ObjectReferenceTable.kObjectReferenceTableName;
                lr.sourceTable = table;
                lr.sourceColumn = this;
                lr.row = row;
                lr.param = new Database.ParameterSet();
                lr.param.param.Add(ObjectTable.kObjParamName, new Database.Operation.ExpConst<int>(i));
                return lr;
            }
            return null;
        }
    }

    public class ObjectListOwnedSizeColumn : Database.ColumnTyped<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListOwnedSizeColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListOwnedSizeColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override long GetRowValue(long row)
        {
            if (table.GetObjectStatic(row)) return 0;
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Array:
                case ObjectDataType.Value:
                    return table.snapshot.typeDescriptions.size[obj.managedTypeIndex];
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                    return table.snapshot.virtualMachineInformation.pointerSize;
                case ObjectDataType.Type:
                    return table.snapshot.typeDescriptions.size[obj.managedTypeIndex];
                case ObjectDataType.NativeObject:
                    return (long)table.snapshot.nativeObjects.size[obj.nativeObjectIndex];
                case ObjectDataType.NativeObjectReference:
                    return 0;
                default:
                    return 0;
            }
        }

        public override void SetRowValue(long row, long value)
        {
        }
    }

    public class ObjectListTargetSizeColumn : Database.ColumnTyped<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListTargetSizeColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListTargetSizeColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override long GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.Value:
                    if (table.GetObjectStatic(row))
                    {
                        return table.snapshot.typeDescriptions.size[obj.managedTypeIndex];
                    }
                    return 0;

                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = obj.GetReferencePointer();
                    if (ptr == 0) return 0;
                    ManagedObjectInfo moi;
                    if (table.crawledData.managedObjectByAddress.TryGetValue(ptr, out moi))
                    {
                        return moi.size;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Managed object at address '" + obj.hostManagedObjectPtr + "' not found");
                        return 0;
                    }
                }
                case ObjectDataType.NativeObject:
                    return 0;
                case ObjectDataType.NativeObjectReference:
                    return (long)table.snapshot.nativeObjects.size[obj.nativeObjectIndex];
                default:
                    return 0;
            }
        }

        public override void SetRowValue(long row, long value)
        {
        }
    }

    public class ObjectListObjectTypeColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListObjectTypeColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListTable table;
        public ObjectListObjectTypeColumn(ObjectListTable table)
        {
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public static string GetObjecType(ObjectData obj)
        {
            switch (obj.dataType)
            {
                case ObjectDataType.Array:
                    return "Managed Array";
                case ObjectDataType.BoxedValue:
                    return "Managed Boxed Value";
                case ObjectDataType.Global:
                    return "Managed Global";
                case ObjectDataType.NativeObject:
                    return "Native Object";
                case ObjectDataType.NativeObjectReference:
                    return "Native Object Reference";
                case ObjectDataType.Object:
                    return "Managed Object";
                case ObjectDataType.ReferenceArray:
                    if (obj.m_Parent != null)
                    {
                        return GetObjecType(obj.m_Parent.obj) + ".Array Reference";
                    }
                    return "Managed Array Reference";
                case ObjectDataType.ReferenceObject:
                    if (obj.m_Parent != null)
                    {
                        return GetObjecType(obj.m_Parent.obj) + ".Object Reference";
                    }
                    return "Managed Object Reference";
                case ObjectDataType.Type:
                    return "Manage Type";
                case ObjectDataType.Unknown:
                    return "Unknown";
                case ObjectDataType.Value:
                    if (obj.m_Parent != null)
                    {
                        return GetObjecType(obj.m_Parent.obj) + ".Value";
                    }
                    return "Managed Value";
            }
            return "";
        }

        public override string GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            return GetObjecType(obj);
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }
    public abstract class ObjectListNativeLinkColumn<DataT> : Database.ColumnTyped<DataT> where DataT : System.IComparable
    {
        public ObjectListTable table;
        public ObjectListNativeLinkColumn(ObjectListTable table)
        {
            this.table = table;
        }

        private Database.View.LinkRequest MakeLink(string tableName, int instanceId, long rowFrom)
        {
            var lr = new Database.View.LinkRequest();
            lr.metaLink = new Database.View.MetaLink();
            lr.metaLink.linkViewName = tableName;
            var b = new Database.View.Where.Builder("NativeInstanceId", Operator.equal, new Expression.MetaExpression(instanceId.ToString()));
            lr.metaLink.linkWhere = new System.Collections.Generic.List<Database.View.Where.Builder>();
            lr.metaLink.linkWhere.Add(b);
            lr.sourceTable = table;
            lr.sourceColumn = this;
            lr.row = rowFrom;

            return lr;
        }

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            if (obj.isManaged)
            {
                ManagedObjectInfo moi = GetInfo(obj);
                if (moi != null && moi.nativeObjectIndex >= 0)
                {
                    var instanceId = table.snapshot.nativeObjects.instanceId[moi.nativeObjectIndex];
                    if (instanceId == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return null;
                    return MakeLink(ObjectAllNativeTable.kTableName, instanceId, row);
                }
            }
            else if (obj.isNative)
            {
                int index = obj.GetNativeObjectIndex(table.snapshot);
                if (index < 0) return null;
                var instanceId = table.snapshot.nativeObjects.instanceId[index];
                if (instanceId == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return null;
                return MakeLink(ObjectAllManagedTable.kTableName, instanceId, row);
            }
            return null;
        }

        protected ManagedObjectInfo GetInfo(ObjectData obj)
        {
            ManagedObjectInfo moi;
            switch (obj.dataType)
            {
                case ObjectDataType.Object:
                    table.crawledData.managedObjectByAddress.TryGetValue(obj.hostManagedObjectPtr, out moi);
                    return moi;
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = obj.GetReferencePointer();
                    if (ptr == 0) return null;
                    table.crawledData.managedObjectByAddress.TryGetValue(ptr, out moi);
                    return moi;
                }
                default:
                    return null;
            }
        }
    }

    public class ObjectListNativeObjectNameColumn : ObjectListNativeLinkColumn<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeObjectNameColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeObjectNameColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return table.snapshot.nativeObjects.objectName[obj.nativeObjectIndex];
            }

            ManagedObjectInfo moi = GetInfo(obj);
            if (moi != null && moi.nativeObjectIndex >= 0)
            {
                return table.snapshot.nativeObjects.objectName[moi.nativeObjectIndex];
            }
            return "";
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }

    public class ObjectListNativeObjectSizeColumn : ObjectListNativeLinkColumn<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeObjectNameColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeObjectSizeColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            long l = GetRowValue(row);
            if (l < 0) return "";
            return l.ToString();
        }

        public override long GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            ManagedObjectInfo moi = GetInfo(obj);
            if (moi != null && moi.nativeObjectIndex >= 0)
            {
                return (long)table.snapshot.nativeObjects.size[moi.nativeObjectIndex];
            }
            return -1;
        }

        public override void SetRowValue(long row, long value)
        {
        }
    }

    public class ObjectListNativeInstanceIdColumn : ObjectListNativeLinkColumn<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeInstanceIdColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeInstanceIdColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var l = GetRowValue(row);
            if (l == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return "";
            return l.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return table.snapshot.nativeObjects.instanceId[obj.nativeObjectIndex];
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceObject:
                {
                    ManagedObjectInfo moi = GetInfo(obj);
                    if (moi != null && moi.nativeObjectIndex >= 0)
                    {
                        return table.snapshot.nativeObjects.instanceId[moi.nativeObjectIndex];
                    }
                    break;
                }
            }
            return CachedSnapshot.NativeObjectEntriesCache.InstanceID_None;
        }

        public override void SetRowValue(long row, int value)
        {
        }
    }

    public abstract class ObjectTable : Database.ExpandTable
    {
        public const string kTableName = "Object";
        public const string kTableDisplayName = "Object";
        public const string kObjParamName = "obj";
        public const string kTypeParamName = "type";
        private static Database.MetaTable[] ms_meta;
        static ObjectTable()
        {
            ms_meta = new Database.MetaTable[(int)ObjectMetaType.Count];

            var metaManaged = new Database.MetaTable();
            var metaNative = new Database.MetaTable();
            ms_meta[(int)ObjectMetaType.Managed] = metaManaged;
            ms_meta[(int)ObjectMetaType.Native] = metaNative;
            ms_meta[(int)ObjectMetaType.All] = metaManaged;

            metaManaged.name = kTableName;
            metaManaged.displayName = kTableDisplayName;
            metaNative.name = kTableName;
            metaNative.displayName = kTableDisplayName;

            var metaColIndex           = new Database.MetaColumn("Index", "Index", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(int)));
            var metaColName            = new Database.MetaColumn("Name", "Name", typeof(string), true, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)));
            var metaColValue           = new Database.MetaColumn("Value", "Value", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)));
            var metaColType            = new Database.MetaColumn("Type", "Type", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)));
            var metaColDataType        = new Database.MetaColumn("DataType", "Data Type", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)));
            var metaColNOName          = new Database.MetaColumn("NativeObjectName", "Native Object Name", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)));
            var metaColLength          = new Database.MetaColumn("Length", "Length", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(int)));
            var metaColStatic          = new Database.MetaColumn("Static", "Static", typeof(bool), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(bool)));
            var metaColRefCount        = new Database.MetaColumn("RefCount", "RefCount", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(int)));
            var metaColOwnerSize       = new Database.MetaColumn("OwnedSize", "Owned Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)));
            var metaColTargetSize      = new Database.MetaColumn("TargetSize", "Target Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)));
            var metaColNativeSize      = new Database.MetaColumn("NativeSize", "Native Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)));
            var metaColNativeId        = new Database.MetaColumn("NativeInstanceId", "Native Instance ID", typeof(int), false, Grouping.groupByDuplicate, null);

            metaColIndex.displayDefaultWidth = 40;
            metaColName.displayDefaultWidth = 200;
            metaColValue.displayDefaultWidth = 180;
            metaColType.displayDefaultWidth = 250;
            metaColDataType.displayDefaultWidth = 150;
            metaColNOName.displayDefaultWidth = 125;
            metaColLength.displayDefaultWidth = 50;
            metaColStatic.displayDefaultWidth = 50;
            metaColRefCount.displayDefaultWidth = 50;
            metaColOwnerSize.displayDefaultWidth = 50;
            metaColTargetSize.displayDefaultWidth = 50;
            metaColNativeSize.displayDefaultWidth = 75;
            metaColNativeId.displayDefaultWidth = 75;

            var metaManagedCol = new List<Database.MetaColumn>();
            metaManagedCol.Add(metaColIndex);
            metaManagedCol.Add(metaColName);
            metaManagedCol.Add(metaColValue);
            metaManagedCol.Add(metaColType);
            metaManagedCol.Add(metaColDataType);
            metaManagedCol.Add(metaColNOName);
            metaManagedCol.Add(metaColLength);
            metaManagedCol.Add(metaColStatic);
            metaManagedCol.Add(metaColRefCount);
            metaManagedCol.Add(metaColOwnerSize);
            metaManagedCol.Add(metaColTargetSize);
            metaManagedCol.Add(metaColNativeSize);
            metaManagedCol.Add(metaColNativeId);

            var metaNativeCol = new List<Database.MetaColumn>();
            metaNativeCol.Add(new Database.MetaColumn(metaColIndex));
            metaNativeCol.Add(new Database.MetaColumn(metaColName));
            metaNativeCol.Add(new Database.MetaColumn(metaColValue));
            metaNativeCol.Add(new Database.MetaColumn(metaColType));
            metaNativeCol.Add(new Database.MetaColumn(metaColNOName));
            metaNativeCol.Add(new Database.MetaColumn(metaColDataType));
            metaNativeCol.Add(new Database.MetaColumn(metaColRefCount));
            metaNativeCol.Add(new Database.MetaColumn(metaColOwnerSize));
            metaNativeCol.Add(new Database.MetaColumn(metaColTargetSize));
            metaNativeCol.Add(new Database.MetaColumn(metaColNativeId));

            metaManaged.SetColumns(metaManagedCol.ToArray());
            metaNative.SetColumns(metaNativeCol.ToArray());
        }

        public enum ObjectMetaType
        {
            Native,
            Managed,
            All,
            Count,
        }
        public ObjectMetaType m_MetaType;
        public ObjectTable(Database.Scheme scheme, ObjectMetaType metaType)
            : base(scheme)
        {
            m_MetaType = metaType;
            m_Meta = ms_meta[(int)metaType];
        }
    }
    public abstract class ObjectListTable : ObjectTable
    {
        public SnapshotDataRenderer renderer;
        public CachedSnapshot snapshot;
        public ManagedData crawledData;

        public ObjectListTable(Database.Scheme scheme, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(scheme, metaType)
        {
            this.renderer = renderer;
            this.snapshot = snapshot;
            this.crawledData = crawledData;

            var col = new List<Database.Column>();
            switch (metaType)
            {
                case ObjectMetaType.All:
                case ObjectMetaType.Managed:
                    col.Add(new ObjectListUnifiedIndexColumn(this));
                    col.Add(new ObjectListNameColumn(this));
                    col.Add(new ObjectListValueColumn(this));
                    col.Add(new ObjectListTypeColumn(this));
                    col.Add(new ObjectListObjectTypeColumn(this));
                    col.Add(new ObjectListNativeObjectNameColumn(this));
                    col.Add(new ObjectListLengthColumn(this));
                    col.Add(new ObjectListStaticColumn(this));
                    col.Add(new ObjectListRefCountColumn(this));
                    col.Add(new ObjectListOwnedSizeColumn(this));
                    col.Add(new ObjectListTargetSizeColumn(this));
                    col.Add(new ObjectListNativeObjectSizeColumn(this));
                    col.Add(new ObjectListNativeInstanceIdColumn(this));
                    break;
                case ObjectMetaType.Native:
                    col.Add(new ObjectListUnifiedIndexColumn(this));
                    col.Add(new ObjectListNameColumn(this));
                    col.Add(new ObjectListValueColumn(this));
                    col.Add(new ObjectListTypeColumn(this));
                    col.Add(new ObjectListNativeObjectNameColumn(this));
                    col.Add(new ObjectListObjectTypeColumn(this));
                    col.Add(new ObjectListRefCountColumn(this));
                    col.Add(new ObjectListOwnedSizeColumn(this));
                    col.Add(new ObjectListTargetSizeColumn(this));
                    col.Add(new ObjectListNativeInstanceIdColumn(this));
                    break;
            }

            InitExpandColumn(col);
        }

        protected void InitObjectList()
        {
            InitGroup(GetObjectCount());
        }

        public override Database.Table CreateGroupTable(long groupIndex)
        {
            var subObj = GetObjectData(groupIndex).displayObject;
            switch (subObj.dataType)
            {
                case ObjectDataType.Array:
                    return new ObjectArrayTable(scheme, renderer, snapshot, crawledData, subObj, m_MetaType);
                case ObjectDataType.ReferenceArray:
                {
                    var ptr = subObj.GetReferencePointer();
                    subObj = ObjectData.FromManagedPointer(snapshot, ptr);
                    return new ObjectArrayTable(scheme, renderer, snapshot, crawledData, subObj, m_MetaType);
                }
                case ObjectDataType.Value:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.Type:
                    return new ObjectFieldTable(scheme, renderer, snapshot, crawledData, subObj, m_MetaType);
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = subObj.GetReferencePointer();
                    subObj = ObjectData.FromManagedPointer(snapshot, ptr);
                    return new ObjectFieldTable(scheme, renderer, snapshot, crawledData, subObj, m_MetaType);
                }
                case ObjectDataType.NativeObject:
                    return new ObjectReferenceTable(scheme, renderer, snapshot, crawledData, subObj, m_MetaType);
                default:
                    return null;
            }
        }

        public override bool IsColumnExpandable(int col)
        {
            return col == 1;
        }

        public override bool IsGroupExpandable(long groupIndex, int col)
        {
            if (!IsColumnExpandable(col)) return false;
            var obj = GetObjectData(groupIndex);
            var subObj = obj.displayObject;
            return IsGroupExpandable(subObj);
        }

        public bool IsGroupLinked(ObjectData od)
        {
            return IsGroupExpandable(od, renderer.forceLinkAllObject);
        }

        public bool IsGroupExpandable(ObjectData od, bool forceExpandAllObject = false)
        {
            switch (od.dataType)
            {
                case ObjectDataType.Array:
                {
                    var l = ArrayTools.ReadArrayLength(snapshot, snapshot.managedHeapSections, od.hostManagedObjectPtr, od.managedTypeIndex, snapshot.virtualMachineInformation);
                    return l > 0 || forceExpandAllObject;
                }
                case ObjectDataType.ReferenceArray:
                {
                    var ptr = od.GetReferencePointer();
                    if (ptr != 0)
                    {
                        var arr = ObjectData.FromManagedPointer(snapshot, ptr);
                        var l = ArrayTools.ReadArrayLength(snapshot, snapshot.managedHeapSections, arr.hostManagedObjectPtr, arr.managedTypeIndex, snapshot.virtualMachineInformation);
                        return l > 0 || forceExpandAllObject;
                    }
                    return false;
                }
                case ObjectDataType.ReferenceObject:
                {
                    ulong ptr = od.GetReferencePointer();
                    if (ptr == 0) return false;
                    var obj = ObjectData.FromManagedPointer(snapshot, ptr);
                    if (!obj.IsValid) return false;
                    if (forceExpandAllObject) return true;
                    if (!renderer.IsExpandable(obj.managedTypeIndex)) return false;
                    return snapshot.typeDescriptions.HasAnyField(obj.managedTypeIndex);
                }
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.Value:
                    if (forceExpandAllObject) return true;
                    if (!renderer.IsExpandable(od.managedTypeIndex)) return false;
                    return snapshot.typeDescriptions.HasAnyField(od.managedTypeIndex);
                case ObjectDataType.Type:
                    if (!renderer.IsExpandable(od.managedTypeIndex)) return false;
                    if (renderer.flattenFields)
                    {
                        return snapshot.typeDescriptions.HasAnyStaticField(od.managedTypeIndex);
                    }
                    else
                    {
                        return snapshot.typeDescriptions.HasStaticField(od.managedTypeIndex);
                    }
                default:
                    return false;
            }
        }

        public abstract long GetObjectCount();
        public virtual string GetObjectName(long row)
        {
            var obj = GetObjectData(row);
            switch (obj.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.Value:
                    return renderer.RenderPointer(obj.GetObjectPointer(snapshot));
                case ObjectDataType.Global:
                case ObjectDataType.Type:
                case ObjectDataType.Unknown:
                default:
                    return renderer.Render(obj);
            }
        }

        public abstract ObjectData GetObjectData(long row);
        public abstract bool GetObjectStatic(long row);
    }
}
