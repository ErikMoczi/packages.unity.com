using System.Collections.Generic;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor
{
    internal class ObjectListUnifiedIndexColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListUnifiedIndexColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListUnifiedIndexColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override int GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            var i = obj.GetUnifiedObjectIndex(m_Table.Snapshot);
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

    internal class ObjectListNameColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNameColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListNameColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            return m_Table.GetObjectName(row);
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }

    internal class ObjectListValueColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListValueColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListValueColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            var result = m_Table.Renderer.Render(obj);
            return result;
        }

        public override void SetRowValue(long row, string value)
        {
        }

        public override Database.LinkRequest GetRowLink(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            if (!m_Table.IsGroupLinked(obj)) return null;
            switch (obj.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                {
                    var lr = new Database.LinkRequestTable();
                    lr.LinkToOpen = new Database.TableLink();
                    lr.LinkToOpen.TableName = ObjectTable.TableName;
                    lr.SourceTable = m_Table;
                    lr.SourceColumn = this;
                    lr.SourceRow = row;
                    lr.Parameters.Add(ObjectTable.ObjParamName, new Database.Operation.ExpConst<ulong>(obj.hostManagedObjectPtr));
                    lr.Parameters.Add(ObjectTable.TypeParamName, new Database.Operation.ExpConst<int>(obj.managedTypeIndex));
                    return lr;
                }

                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                {
                    ulong result = obj.GetReferencePointer();
                    if (result == 0) return null;
                    var lr = new Database.LinkRequestTable();
                    lr.LinkToOpen = new Database.TableLink();
                    lr.LinkToOpen.TableName = ObjectTable.TableName;
                    lr.SourceTable = m_Table;
                    lr.SourceColumn = this;
                    lr.SourceRow = row;
                    lr.Parameters.Add(ObjectTable.ObjParamName, new Database.Operation.ExpConst<ulong>(result));
                    return lr;
                }
                default:
                    return null;
            }
        }
    }
    
    internal class ObjectListUniqueStringColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListUniqueStringColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListUniqueStringColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            var result = m_Table.Renderer.RenderUniqueString(obj);
            return result;
        }

        public override void SetRowValue(long row, string value)
        {
        }

        public override Database.LinkRequest GetRowLink(long row)
        {
            return null;
        }
    }
	
    internal class ObjectListAddressColumn : Database.ColumnTyped<ulong>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListAddressColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListAddressColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override ulong GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            return obj.GetObjectPointer(m_Table.Snapshot, false);
        }

        public override void SetRowValue(long row, ulong value)
        {
        }

        public override Database.LinkRequest GetRowLink(long row)
        {
            return null;
        }
    }
    
    internal class ObjectListTypeColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListTypeColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListTypeColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var d = m_Table.GetObjectData(row).displayObject;
            switch (d.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.Value:
                    if (d.managedTypeIndex < 0) return "<unknown type>";
                    return m_Table.Snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];

                case ObjectDataType.ReferenceObject:
                {
                    var ptr = d.GetReferencePointer();
                    if (ptr != 0)
                    {
                        var obj = ObjectData.FromManagedPointer(m_Table.Snapshot, ptr);
                        if (obj.IsValid && obj.managedTypeIndex != d.managedTypeIndex)
                        {
                            return "(" + m_Table.Snapshot.typeDescriptions.typeDescriptionName[obj.managedTypeIndex] + ") "
                                + m_Table.Snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];
                        }
                    }
                    return m_Table.Snapshot.typeDescriptions.typeDescriptionName[d.managedTypeIndex];
                }

                case ObjectDataType.Global:
                    return "Global";
                case ObjectDataType.Type:
                    return "Type";
                case ObjectDataType.NativeObject:
                {
                    int iType = m_Table.Snapshot.nativeObjects.nativeTypeArrayIndex[d.nativeObjectIndex];
                    return m_Table.Snapshot.nativeTypes.typeName[iType];
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

    internal class ObjectListLengthColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListLengthColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListLengthColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var l = GetRowValue(row);
            if (l < 0) return "";
            return l.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.ReferenceArray:
                {
                    obj = ObjectData.FromManagedPointer(m_Table.Snapshot, obj.GetReferencePointer());
                    if (obj.hostManagedObjectPtr != 0)
                    {
                        goto case ObjectDataType.Array;
                    }
                    return -1;
                }
                case ObjectDataType.Array:
                {
                    var arrayInfo = ArrayTools.GetArrayInfo(m_Table.Snapshot, obj.managedObjectData, obj.managedTypeIndex);
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
    internal class ObjectListStaticColumn : Database.ColumnTyped<bool>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListStaticColumn<bool>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListStaticColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override bool GetRowValue(long row)
        {
            var b = m_Table.GetObjectStatic(row);
            return b;
        }

        public override void SetRowValue(long row, bool value)
        {
        }
    }
    internal class ObjectListRefCountColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListRefCountColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListRefCountColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
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
            var obj = m_Table.GetObjectData(row).displayObject;
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
                        if (m_Table.CrawledData.ManagedObjectByAddress.TryGetValue(ptr, out moi))
                        {
                            return moi.RefCount;
                        }
                    }
                    break;
                }

                case ObjectDataType.NativeObject:
                    return m_Table.Snapshot.nativeObjects.refcount[obj.nativeObjectIndex];
            }
            return -1;
        }

        public override void SetRowValue(long row, int value)
        {
        }

        public override Database.LinkRequest GetRowLink(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            int i = obj.GetUnifiedObjectIndex(m_Table.Snapshot);
            if (i >= 0)
            {
                var lr = new Database.LinkRequestTable();
                lr.LinkToOpen = new Database.TableLink();
                lr.LinkToOpen.TableName = ObjectReferenceTable.kObjectReferenceTableName;
                lr.SourceTable = m_Table;
                lr.SourceColumn = this;
                lr.SourceRow = row;
                lr.Parameters.AddValue(ObjectTable.ObjParamName, i);
                return lr;
            }
            return null;
        }
    }

    internal class ObjectListOwnedSizeColumn : Database.ColumnTyped<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListOwnedSizeColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListOwnedSizeColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override long GetRowValue(long row)
        {
            if (m_Table.GetObjectStatic(row)) return 0;
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Array:
                case ObjectDataType.Value:
                    return m_Table.Snapshot.typeDescriptions.size[obj.managedTypeIndex];
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                    return m_Table.Snapshot.virtualMachineInformation.pointerSize;
                case ObjectDataType.Type:
                    return m_Table.Snapshot.typeDescriptions.size[obj.managedTypeIndex];
                case ObjectDataType.NativeObject:
                    return (long)m_Table.Snapshot.nativeObjects.size[obj.nativeObjectIndex];
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

    internal class ObjectListTargetSizeColumn : Database.ColumnTyped<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListTargetSizeColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListTargetSizeColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override long GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.Value:
                    if (m_Table.GetObjectStatic(row))
                    {
                        return m_Table.Snapshot.typeDescriptions.size[obj.managedTypeIndex];
                    }
                    return 0;

                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = obj.GetReferencePointer();
                    if (ptr == 0) return 0;
                    ManagedObjectInfo moi;
                    if (m_Table.CrawledData.ManagedObjectByAddress.TryGetValue(ptr, out moi))
                    {
                        return moi.Size;
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
                    return (long)m_Table.Snapshot.nativeObjects.size[obj.nativeObjectIndex];
                default:
                    return 0;
            }
        }

        public override void SetRowValue(long row, long value)
        {
        }
    }

    internal class ObjectListObjectTypeColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListObjectTypeColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListObjectTypeColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
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
            var obj = m_Table.GetObjectData(row).displayObject;
            return GetObjecType(obj);
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }
    internal abstract class ObjectListNativeLinkColumn<DataT> : Database.ColumnTyped<DataT> where DataT : System.IComparable
    {
        protected ObjectListTable m_Table;
        public ObjectListNativeLinkColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        private Database.LinkRequest MakeLink(string tableName, int instanceId, long rowFrom)
        {
            var lr = new Database.LinkRequestTable();
            lr.LinkToOpen = new Database.TableLink();
            lr.LinkToOpen.TableName = tableName;
            var b = new Database.View.Where.Builder("NativeInstanceId", Operator.Equal, new Expression.MetaExpression(instanceId.ToString(), true));
            lr.LinkToOpen.RowWhere = new System.Collections.Generic.List<Database.View.Where.Builder>();
            lr.LinkToOpen.RowWhere.Add(b);
            lr.SourceTable = m_Table;
            lr.SourceColumn = this;
            lr.SourceRow = rowFrom;

            return lr;
        }

        public override Database.LinkRequest GetRowLink(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            if (obj.isManaged)
            {
                ManagedObjectInfo moi = GetInfo(obj);
                if (moi.IsValid() && moi.NativeObjectIndex >= 0)
                {
                    var instanceId = m_Table.Snapshot.nativeObjects.instanceId[moi.NativeObjectIndex];
                    if (instanceId == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return null;
                    return MakeLink(ObjectAllNativeTable.TableName, instanceId, row);
                }
            }
            // we are linking native objects to themselves currently as that allows us 
            // to jump from a native object to the native object's table. (eg: MemoryMap / TreeMap spreadsheets to tables) 
            // TODO: Improve column link API so it supports all 3 cases ( native - native , managed - native,  native - managed)
            else if (obj.isNative)
            {
                int index = obj.GetNativeObjectIndex(m_Table.Snapshot);
                if (index < 0) return null;
                var instanceId = m_Table.Snapshot.nativeObjects.instanceId[index];
                if (instanceId == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return null;
                return MakeLink(ObjectAllNativeTable.TableName, instanceId, row);
            }
            return null;
        }

        protected ManagedObjectInfo GetInfo(ObjectData obj)
        {
            ManagedObjectInfo moi;
            switch (obj.dataType)
            {
                case ObjectDataType.Object:
                    m_Table.CrawledData.ManagedObjectByAddress.TryGetValue(obj.hostManagedObjectPtr, out moi);
                    return moi;
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = obj.GetReferencePointer();
                    if (ptr == 0) return default(ManagedObjectInfo);
                    m_Table.CrawledData.ManagedObjectByAddress.TryGetValue(ptr, out moi);
                    return moi;
                }
                default:
                    return default(ManagedObjectInfo);
            }
        }
    }

    internal class ObjectListNativeObjectNameLinkColumn : ObjectListNativeLinkColumn<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeObjectNameLinkColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeObjectNameLinkColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return m_Table.Snapshot.nativeObjects.objectName[obj.nativeObjectIndex];
            }

            ManagedObjectInfo moi = GetInfo(obj);
            if (moi.IsValid() && moi.NativeObjectIndex >= 0)
            {
                return m_Table.Snapshot.nativeObjects.objectName[moi.NativeObjectIndex];
            }
            return "";
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }

    internal class ObjectListNativeObjectNameColumn : Database.ColumnTyped<string>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeObjectNameColumn<string>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_table;
        public ObjectListNativeObjectNameColumn(ObjectListTable table)
            : base()
        {
            m_table = table;
        }

        public override long GetRowCount()
        {
            return m_table.GetObjectCount();
        }

        public override string GetRowValue(long row)
        {
            var obj = m_table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return m_table.Snapshot.nativeObjects.objectName[obj.nativeObjectIndex];
            }

            throw new System.NotSupportedException("ObjectListNativeObjectNameColumn is only allowed to be used for purely native lists");
        }

        public override void SetRowValue(long row, string value)
        {
        }
    }

    internal class ObjectListNativeObjectSizeColumn : ObjectListNativeLinkColumn<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeObjectSizeColumn<long>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeObjectSizeColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            long l = GetRowValue(row);
            if (l < 0) return "";
            return l.ToString();
        }

        public override long GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            ManagedObjectInfo moi = GetInfo(obj);
            if (moi.IsValid() && moi.NativeObjectIndex >= 0)
            {
                return (long)m_Table.Snapshot.nativeObjects.size[moi.NativeObjectIndex];
            }
            return -1;
        }

        public override void SetRowValue(long row, long value)
        {
        }
    }

    internal class ObjectListNativeInstanceIdLinkColumn : ObjectListNativeLinkColumn<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeInstanceIdLinkColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        public ObjectListNativeInstanceIdLinkColumn(ObjectListTable table)
            : base(table)
        {
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var l = GetRowValue(row);
            if (l == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return "";
            return l.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return m_Table.Snapshot.nativeObjects.instanceId[obj.nativeObjectIndex];
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceObject:
                    ManagedObjectInfo moi = GetInfo(obj);
                    if (moi.IsValid() && moi.NativeObjectIndex >= 0)
                    {
                        return m_Table.Snapshot.nativeObjects.instanceId[moi.NativeObjectIndex];
                    }
                    break;

            }
            return CachedSnapshot.NativeObjectEntriesCache.InstanceID_None;
        }

        public override void SetRowValue(long row, int value)
        {
        }
    }

    internal class ObjectListNativeInstanceIdColumn : Database.ColumnTyped<int>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ObjectListNativeInstanceIdColumn<int>[" + row + "]{" + GetRowValueString(row) + "}";
        }

#endif
        ObjectListTable m_Table;
        public ObjectListNativeInstanceIdColumn(ObjectListTable table)
        {
            m_Table = table;
        }

        public override long GetRowCount()
        {
            return m_Table.GetObjectCount();
        }

        public override string GetRowValueString(long row)
        {
            var l = GetRowValue(row);
            if (l == CachedSnapshot.NativeObjectEntriesCache.InstanceID_None) return "";
            return l.ToString();
        }

        public override int GetRowValue(long row)
        {
            var obj = m_Table.GetObjectData(row).displayObject;
            switch (obj.dataType)
            {
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeObjectReference:
                    return m_Table.Snapshot.nativeObjects.instanceId[obj.nativeObjectIndex];
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceObject:
                    throw new System.NotSupportedException("ObjectListNativeInstanceIdColumn is only to be used in native-objects-only-lists. Use ObjectListNativeInstanceIdLinkColumn instead");
            }
            return CachedSnapshot.NativeObjectEntriesCache.InstanceID_None;
        }

        public override void SetRowValue(long row, int value)
        {
        }
    }

    internal abstract class ObjectTable : Database.ExpandTable
    {
        public const string TableName = "Object";
        public const string TableDisplayName = "Object";
        public const string ObjParamName = "obj";
        public const string TypeParamName = "type";
        static Database.MetaTable[] s_Meta;
        static ObjectTable()
        {
            s_Meta = new Database.MetaTable[(int)ObjectMetaType.Count];

            var metaManaged = new Database.MetaTable();
            var metaNative = new Database.MetaTable();
            s_Meta[(int)ObjectMetaType.Managed] = metaManaged;
            s_Meta[(int)ObjectMetaType.Native] = metaNative;
            s_Meta[(int)ObjectMetaType.All] = metaManaged;

            metaManaged.name = TableName;
            metaManaged.displayName = TableDisplayName;
            metaNative.name = TableName;
            metaNative.displayName = TableDisplayName;

            var metaColIndex           = new Database.MetaColumn("Index", "Index", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(int)), 40);
            var metaColName            = new Database.MetaColumn("Name", "Name", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)), 200);
            var metaColValue           = new Database.MetaColumn("Value", "Value", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)), 180);
            var metaColType            = new Database.MetaColumn("Type", "Type", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)), 250);
            var metaColDataType        = new Database.MetaColumn("DataType", "Data Type", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)), 150);
            var metaColNOName          = new Database.MetaColumn("NativeObjectName", "Native Object Name", typeof(string), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(string)), 125);
            var metaColLength          = new Database.MetaColumn("Length", "Length", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(int)), 50);
            var metaColStatic          = new Database.MetaColumn("Static", "Static", typeof(bool), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.first, typeof(bool)), 50);
            var metaColRefCount        = new Database.MetaColumn("RefCount", "RefCount", typeof(int), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(int)), 50);
            var metaColOwnerSize       = new Database.MetaColumn("OwnedSize", "Owned Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)), 50);
            var metaColTargetSize      = new Database.MetaColumn("TargetSize", "Target Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)), 50);
            var metaColNativeSize      = new Database.MetaColumn("NativeSize", "Native Size", typeof(long), false, Grouping.groupByDuplicate, Grouping.GetMergeAlgo(Grouping.MergeAlgo.sumpositive, typeof(long)), 75);
            var metaColNativeId        = new Database.MetaColumn("NativeInstanceId", "Native Instance ID", typeof(int), false, Grouping.groupByDuplicate, null, 75);
            var metaColAddress         = new Database.MetaColumn("Address", "Address", typeof(ulong), false, Grouping.groupByDuplicate, null, 75);
            var metaColUniqueString    = new Database.MetaColumn("UniqueString", "Unique String", typeof(string), true, Grouping.groupByDuplicate, null, 250);
            
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
            metaManagedCol.Add(metaColAddress);
            metaManagedCol.Add(metaColUniqueString);
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
            metaNativeCol.Add(new Database.MetaColumn(metaColAddress));
            metaNativeCol.Add(new Database.MetaColumn(metaColUniqueString));

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
        protected ObjectMetaType m_MetaType;
        public ObjectTable(Database.Schema schema, ObjectMetaType metaType)
            : base(schema)
        {
            m_MetaType = metaType;
            m_Meta = s_Meta[(int)metaType];
        }
    }
    internal abstract class ObjectListTable : ObjectTable
    {
        public readonly SnapshotDataRenderer Renderer;
        public readonly CachedSnapshot Snapshot;
        public readonly ManagedData CrawledData;

        public ObjectListTable(Database.Schema schema, SnapshotDataRenderer renderer, CachedSnapshot snapshot, ManagedData crawledData, ObjectMetaType metaType)
            : base(schema, metaType)
        {
            Renderer = renderer;
            Snapshot = snapshot;
            CrawledData = crawledData;

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
                    col.Add(new ObjectListNativeObjectNameLinkColumn(this));
                    col.Add(new ObjectListLengthColumn(this));
                    col.Add(new ObjectListStaticColumn(this));
                    col.Add(new ObjectListRefCountColumn(this));
                    col.Add(new ObjectListOwnedSizeColumn(this));
                    col.Add(new ObjectListTargetSizeColumn(this));
                    col.Add(new ObjectListNativeObjectSizeColumn(this));
                    col.Add(new ObjectListNativeInstanceIdLinkColumn(this));
                    col.Add(new ObjectListAddressColumn(this));
                    col.Add(new ObjectListUniqueStringColumn(this));
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
                    col.Add(new ObjectListAddressColumn(this));
                    col.Add(new ObjectListUniqueStringColumn(this));
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
                    return new ObjectArrayTable(Schema, Renderer, Snapshot, CrawledData, subObj, m_MetaType);
                case ObjectDataType.ReferenceArray:
                {
                    var ptr = subObj.GetReferencePointer();
                    subObj = ObjectData.FromManagedPointer(Snapshot, ptr);
                    return new ObjectArrayTable(Schema, Renderer, Snapshot, CrawledData, subObj, m_MetaType);
                }
                case ObjectDataType.Value:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.Type:
                    return new ObjectFieldTable(Schema, Renderer, Snapshot, CrawledData, subObj, m_MetaType);
                case ObjectDataType.ReferenceObject:
                {
                    var ptr = subObj.GetReferencePointer();
                    subObj = ObjectData.FromManagedPointer(Snapshot, ptr);
                    return new ObjectFieldTable(Schema, Renderer, Snapshot, CrawledData, subObj, m_MetaType);
                }
                case ObjectDataType.NativeObject:
                    return new ObjectReferenceTable(Schema, Renderer, Snapshot, CrawledData, subObj, m_MetaType);
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
            return IsGroupExpandable(od, Renderer.forceLinkAllObject);
        }

        public bool IsGroupExpandable(ObjectData od, bool forceExpandAllObject = false)
        {
            switch (od.dataType)
            {
                case ObjectDataType.Array:
                {
                    var l = ArrayTools.ReadArrayLength(Snapshot, od.hostManagedObjectPtr, od.managedTypeIndex);
                    return l > 0 || forceExpandAllObject;
                }
                case ObjectDataType.ReferenceArray:
                {
                    var ptr = od.GetReferencePointer();
                    if (ptr != 0)
                    {
                        var arr = ObjectData.FromManagedPointer(Snapshot, ptr);
                        var l = ArrayTools.ReadArrayLength(Snapshot, arr.hostManagedObjectPtr, arr.managedTypeIndex);
                        return l > 0 || forceExpandAllObject;
                    }
                    return false;
                }
                case ObjectDataType.ReferenceObject:
                {
                    ulong ptr = od.GetReferencePointer();
                    if (ptr == 0) return false;
                    var obj = ObjectData.FromManagedPointer(Snapshot, ptr);
                    if (!obj.IsValid) return false;
                    if (forceExpandAllObject) return true;
                    if (!Renderer.IsExpandable(obj.managedTypeIndex)) return false;
                    return Snapshot.typeDescriptions.HasAnyField(obj.managedTypeIndex);
                }
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.Value:
                    if (forceExpandAllObject) return true;
                    if (!Renderer.IsExpandable(od.managedTypeIndex)) return false;
                    return Snapshot.typeDescriptions.HasAnyField(od.managedTypeIndex);
                case ObjectDataType.Type:
                    if (!Renderer.IsExpandable(od.managedTypeIndex)) return false;
                    if (Renderer.flattenFields)
                    {
                        return Snapshot.typeDescriptions.HasAnyStaticField(od.managedTypeIndex);
                    }
                    else
                    {
                        return Snapshot.typeDescriptions.HasStaticField(od.managedTypeIndex);
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
                    return Renderer.RenderPointer(obj.GetObjectPointer(Snapshot));
                case ObjectDataType.Global:
                case ObjectDataType.Type:
                case ObjectDataType.Unknown:
                default:
                    return Renderer.Render(obj);
            }
        }

        public abstract ObjectData GetObjectData(long row);
        public abstract bool GetObjectStatic(long row);
    }
}
