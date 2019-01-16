using System;
using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;

namespace Unity.MemoryProfiler.Editor
{
    internal struct ManagedConnection
    {
        public enum ConnectionType
        {
            Global_To_ManagedObject,
            ManagedObject_To_ManagedObject,
            ManagedType_To_ManagedObject,
            UnityEngineObject,
        }
        public ManagedConnection(ConnectionType t, int from, int to, int fieldFrom, int arrayIndexFrom)
        {
            connectionType = t;
            index0 = from;
            index1 = to;
            this.fieldFrom = fieldFrom;
            this.arrayIndexFrom = arrayIndexFrom;
        }

        private int index0;
        private int index1;

        public int fieldFrom;
        public int arrayIndexFrom;

        public ConnectionType connectionType;
        public int GetUnifiedIndexFrom(CachedSnapshot snapshot)
        {
            switch (connectionType)
            {
                case ConnectionType.ManagedObject_To_ManagedObject:
                    return snapshot.ManagedObjectIndexToUnifiedObjectIndex(index0);
                case ConnectionType.ManagedType_To_ManagedObject:
                    return index0;
                case ConnectionType.UnityEngineObject:
                    return snapshot.NativeObjectIndexToUnifiedObjectIndex(index0);
                default:
                    return -1;
            }
        }

        public int GetUnifiedIndexTo(CachedSnapshot snapshot)
        {
            switch (connectionType)
            {
                case ConnectionType.Global_To_ManagedObject:
                case ConnectionType.ManagedObject_To_ManagedObject:
                case ConnectionType.ManagedType_To_ManagedObject:
                case ConnectionType.UnityEngineObject:
                    return snapshot.ManagedObjectIndexToUnifiedObjectIndex(index1);
                default:
                    return -1;
            }
        }

        public int fromManagedObjectIndex
        {
            get
            {
                switch (connectionType)
                {
                    case ConnectionType.Global_To_ManagedObject:
                    case ConnectionType.ManagedObject_To_ManagedObject:
                    case ConnectionType.ManagedType_To_ManagedObject:
                        return index0;
                }
                return -1;
            }
        }
        public int toManagedObjectIndex
        {
            get
            {
                switch (connectionType)
                {
                    case ConnectionType.Global_To_ManagedObject:
                    case ConnectionType.ManagedObject_To_ManagedObject:
                    case ConnectionType.ManagedType_To_ManagedObject:
                        return index1;
                }
                return -1;
            }
        }

        public int fromManagedType
        {
            get
            {
                if (connectionType == ConnectionType.ManagedType_To_ManagedObject)
                {
                    return index0;
                }
                return -1;
            }
        }
        public int UnityEngineNativeObjectIndex
        {
            get
            {
                if (connectionType == ConnectionType.UnityEngineObject)
                {
                    return index0;
                }
                return -1;
            }
        }
        public int UnityEngineManagedObjectIndex
        {
            get
            {
                if (connectionType == ConnectionType.UnityEngineObject)
                {
                    return index1;
                }
                return -1;
            }
        }
        public static ManagedConnection MakeUnityEngineObjectConnection(int NativeIndex, int ManagedIndex)
        {
            return new ManagedConnection(ConnectionType.UnityEngineObject, NativeIndex, ManagedIndex, 0, 0);
        }

        public static ManagedConnection MakeConnection(CachedSnapshot snapshot, int fromIndex, ulong fromPtr, int toIndex, ulong toPtr, int fromTypeIndex, int fromField, int fieldArrayIndexFrom)
        {
            if (fromIndex >= 0)
            {
                //from an object
#if DEBUG_VALIDATION
                if (fromField >= 0)
                {
                    if (snapshot.fieldDescriptions.isStatic[fromField])
                    {
                        Debug.LogError("Cannot make a connection from an object using a static field.");
                    }
                }
#endif
                return new ManagedConnection(ConnectionType.ManagedObject_To_ManagedObject, fromIndex, toIndex, fromField, fieldArrayIndexFrom);
            }
            else if (fromTypeIndex >= 0)
            {
                //from a type static data
#if DEBUG_VALIDATION
                if (fromField >= 0)
                {
                    if (!snapshot.fieldDescriptions.isStatic[fromField])
                    {
                        Debug.LogError("Cannot make a connection from a type using a non-static field.");
                    }
                }
#endif
                return new ManagedConnection(ConnectionType.ManagedType_To_ManagedObject, fromTypeIndex, toIndex, fromField, fieldArrayIndexFrom);
            }
            else
            {
                return new ManagedConnection(ConnectionType.Global_To_ManagedObject, fromIndex, toIndex, fromField, fieldArrayIndexFrom);
            }
        }
    }

    internal class ManagedObjectInfo
    {
        public ulong ptrObject;
        public ulong ptrTypeInfo;
        public int nativeObjectIndex = -1;
        public int managedObjectIndex;
        public int iTypeDescription;
        public int size;
        public int refCount = 0;
        public bool IsKnownType()
        {
            return iTypeDescription >= 0;
        }

        public BytesAndOffset data;
    }

    internal class ManagedData
    {
        public bool valid;
        public CachedSnapshot m_Snapshot;


        public List<ManagedObjectInfo> managedObjects;  //includes gcHandle and all crawled objects
        public SortedDictionary<ulong, ManagedObjectInfo> managedObjectByAddress = new SortedDictionary<ulong, ManagedObjectInfo>();

        public int[] typesWithStaticFields;

        public List<ManagedConnection> connections;
        public ManagedData(CachedSnapshot snapshot)
        {
            this.m_Snapshot = snapshot;
            snapshot.m_CrawledData = this;

            List<int> l = new List<int>();
            for (int i = 0; i != m_Snapshot.typeDescriptions.Count; ++i)
            {
                if (m_Snapshot.typeDescriptions.staticFieldBytes[i] != null
                    && m_Snapshot.typeDescriptions.staticFieldBytes[i].Length > 0)
                {
                    l.Add(m_Snapshot.typeDescriptions.typeIndex[i]);
                }
            }
            typesWithStaticFields = l.ToArray();
            valid = true;
        }
    }

    internal struct BytesAndOffset
    {
        public byte[] bytes;
        public int offset;
        public int pointerSize;
        public bool IsValid { get { return bytes != null; } }
        public BytesAndOffset(byte[] bytes, int pointerSize)
        {
            this.bytes = bytes;
            this.pointerSize = pointerSize;
            offset = 0;
        }

        public UInt64 ReadPointer()
        {
            if (pointerSize == 4)
                return BitConverter.ToUInt32(bytes, offset);
            if (pointerSize == 8)
                return BitConverter.ToUInt64(bytes, offset);
            throw new ArgumentException("Unexpected pointersize: " + pointerSize);
        }

        public byte ReadByte()
        {
            return bytes[offset];
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(bytes, offset);
        }

        public Int32 ReadInt32()
        {
            return BitConverter.ToInt32(bytes, offset);
        }

        public Int64 ReadInt64()
        {
            return BitConverter.ToInt64(bytes, offset);
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(bytes, offset);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(bytes, offset);
        }

        public ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(bytes, offset);
        }

        public bool ReadBoolean()
        {
            return BitConverter.ToBoolean(bytes, offset);
        }

        public char ReadChar()
        {
            return BitConverter.ToChar(bytes, offset);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(bytes, offset);
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(bytes, offset);
        }

        public string ReadString()
        {
            int strLength = ReadInt32();
            return System.Text.Encoding.Default.GetString(bytes, offset + sizeof(int), strLength * 2);
        }

        public BytesAndOffset Add(int add)
        {
            return new BytesAndOffset() { bytes = bytes, offset = offset + add, pointerSize = pointerSize };
        }

        public void WritePointer(UInt64 value)
        {
            for (int i = 0; i < pointerSize; i++)
            {
                bytes[i + offset] = (byte)value;
                value >>= 8;
            }
        }

        public BytesAndOffset NextPointer()
        {
            return Add(pointerSize);
        }
    }

    internal class Crawler
    {
        CachedSnapshot m_Snapshot;

        List<ManagedObjectInfo> m_ObjectList;
        List<ManagedConnection> m_ConnectionList;

        public ManagedData Crawl(CachedSnapshot snapshot)
        {
            m_Snapshot = snapshot;

            m_Snapshot.m_CrawledData = new ManagedData(m_Snapshot);

            m_ObjectList = new List<ManagedObjectInfo>((int)m_Snapshot.gcHandles.Count * 3);

            m_ConnectionList = new List<ManagedConnection>((int)m_Snapshot.gcHandles.Count * 6);

            // Add all gchandle in the beginning of the output list to keep the same object order as the input
            for (int i = 0; i != m_Snapshot.gcHandles.Count; i++)
            {
                var moi = new ManagedObjectInfo();
                if (m_Snapshot.gcHandles.target[i] == 0)
                {
                    Debuging.DebugUtility.LogWarning("null object in gc handles " + i);
                    moi.managedObjectIndex = i;
                    m_ObjectList.Add(moi);
                    continue;
                }
                if (m_Snapshot.m_CrawledData.managedObjectByAddress.ContainsKey(m_Snapshot.gcHandles.target[i]))
                {
                    Debuging.DebugUtility.LogWarning("Duplicated object in gc handles " + i + " addr:" + m_Snapshot.gcHandles.target[i]);
                    moi.managedObjectIndex = i;
                    m_ObjectList.Add(moi);
                    continue;
                }
                moi.managedObjectIndex = i;
                //moi.ptrObject = m_Snapshot.gcHandles.target[i];
                //moi.
                m_ObjectList.Add(moi);
                m_Snapshot.m_CrawledData.managedObjectByAddress.Add(m_Snapshot.gcHandles.target[i], moi);
            }

            // Set to true to ignore all the first objects that are bad.
            // For some reason, the first few gcHandles Mono returns are not valid objects
            bool ignoreBadObject = false;
            for (int i = 0; i != m_Snapshot.gcHandles.Count; i++)
            {
                if (CrawlPointer(m_Snapshot.gcHandles.target[i], 0, -1, -1, -1, -1, ignoreBadObject))
                {
                    ignoreBadObject = false;
                }
            }

            for (int i = 0; i < m_Snapshot.m_CrawledData.typesWithStaticFields.Length; i++)
            {
                var iTypeDescription = m_Snapshot.m_CrawledData.typesWithStaticFields[i];
                CrawlRawObjectData(
                    new BytesAndOffset { bytes = m_Snapshot.typeDescriptions.staticFieldBytes[iTypeDescription], offset = 0, pointerSize = m_Snapshot.virtualMachineInformation.pointerSize }
                    , iTypeDescription, true, 0, -1);
            }

            ConnectNativeToManageObject();

            m_Snapshot.m_CrawledData.managedObjects = m_ObjectList;
            m_Snapshot.m_CrawledData.connections = m_ConnectionList;

            AddupRawRefCount();
            return m_Snapshot.m_CrawledData;
        }

        private void AddupRawRefCount()
        {
            for (int i = 0; i != m_Snapshot.connections.Count; ++i)
            {
                int iManagedTo = m_Snapshot.UnifiedObjectIndexToManagedObjectIndex(m_Snapshot.connections.to[i]);
                if (iManagedTo >= 0)
                {
                    ++m_Snapshot.m_CrawledData.managedObjects[iManagedTo].refCount;
                    continue;
                }

                int iNativeTo = m_Snapshot.UnifiedObjectIndexToNativeObjectIndex(m_Snapshot.connections.to[i]);
                if (iNativeTo >= 0)
                {
                    ++m_Snapshot.nativeObjects.refcount[iNativeTo];
                    continue;
                }
            }
        }

        private void ConnectNativeToManageObject()
        {
            if (m_Snapshot.typeDescriptions.Count == 0)
                return;

            // Get UnityEngine.Object
            int iTypeDescription_UnityEngineObject = m_Snapshot.typeDescriptions.typeDescriptionName.FindIndex(x => x == "UnityEngine.Object");
            if (iTypeDescription_UnityEngineObject < 0)
            {
                //No Unity Object ?
                return;
            }

            //Get UnityEngine.Object.m_InstanceID field
            int iField_UnityEngineObject_m_InstanceID = Array.FindIndex(
                    m_Snapshot.typeDescriptions.fieldIndices[iTypeDescription_UnityEngineObject]
                    , iField => m_Snapshot.fieldDescriptions.fieldDescriptionName[iField] == "m_InstanceID");

            int instanceIDOffset = -1;
            int cachedPtrOffset = -1;
            if (iField_UnityEngineObject_m_InstanceID >= 0)
            {
                var fieldIndex = m_Snapshot.typeDescriptions.fieldIndices[iTypeDescription_UnityEngineObject][iField_UnityEngineObject_m_InstanceID];
                instanceIDOffset = m_Snapshot.fieldDescriptions.offset[fieldIndex];
            }
            if (instanceIDOffset < 0)
            {
                // on UNITY_5_4_OR_NEWER, there is the member m_CachedPtr we can use to identify the connection
                //Since Unity 5.4, UnityEngine.Object no longer stores instance id inside when running in the player. Use cached ptr instead to find the instanceID of native object
                int iField_UnityEngineObject_m_CachedPtr = Array.FindIndex(
                        m_Snapshot.typeDescriptions.fieldIndices[iTypeDescription_UnityEngineObject]
                        , iField => m_Snapshot.fieldDescriptions.fieldDescriptionName[iField] == "m_CachedPtr");
                if (iField_UnityEngineObject_m_CachedPtr >= 0)
                {
                    cachedPtrOffset = m_Snapshot.fieldDescriptions.offset[iField_UnityEngineObject_m_CachedPtr];
                }
            }
            if (instanceIDOffset < 0 && cachedPtrOffset < 0)
            {
                UnityEngine.Debug.LogWarning("Could not find unity object instance id field or m_CachedPtr");
                return;
            }


            for (int i = 0; i != m_ObjectList.Count; i++)
            {
                //Must derive of unity Object
                var o = m_ObjectList[i];
                if (!DerivesFrom(o.iTypeDescription, iTypeDescription_UnityEngineObject))
                    continue;

                //Find object instance id
                int instanceID = CachedSnapshot.NativeObjectEntriesCache.InstanceID_None;
                if (iField_UnityEngineObject_m_InstanceID >= 0)
                {
                    var h = m_Snapshot.managedHeapSections.Find(o.ptrObject + (UInt64)instanceIDOffset, m_Snapshot.virtualMachineInformation);
                    if (h.IsValid)
                    {
                        instanceID = h.ReadInt32();
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Managed object missing head (addr:" + o.ptrObject + ", index:" + o.managedObjectIndex + ")");
                        continue;
                    }
                }
                else if (cachedPtrOffset >= 0)
                {
                    // If you get a compilation error on the following 2 lines, update to Unity 5.4b14.
                    var heapSection = m_Snapshot.managedHeapSections.Find(o.ptrObject + (UInt64)cachedPtrOffset, m_Snapshot.virtualMachineInformation);
                    if (!heapSection.IsValid)
                    {
                        UnityEngine.Debug.LogWarning("Managed object (addr:" + o.ptrObject + ", index:" + o.managedObjectIndex + ") does not have data at cachedPtr offset(" + cachedPtrOffset + ")");
                        continue;
                    }
                    var cachedPtr = heapSection.ReadPointer();
                    var indexOfNativeObject = m_Snapshot.nativeObjects.nativeObjectAddress.FindIndex(no => (ulong)no == cachedPtr);
                    if (indexOfNativeObject >= 0)
                    {
                        instanceID = m_Snapshot.nativeObjects.instanceId[indexOfNativeObject];
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!m_Snapshot.nativeObjects.instanceId2Index.TryGetValue(instanceID, out o.nativeObjectIndex))
                {
                    o.nativeObjectIndex = -1;
                }
                else
                {
                    //The native to manage connections are already in the raw snapshot
                    //m_ConnectionList.Add(ManagedConnection.MakeUnityEngineObjectConnection(o.nativeObjectIndex, i));
                    ////addup refcount
                    //++o.refCount;
                    //++m_Snapshot.nativeObjects.refcount[o.nativeObjectIndex];

                    m_Snapshot.nativeObjects.managedObjectIndex[o.nativeObjectIndex] = i;
                }
                //yield return new NativeToManagedConnection { nativeInstanceId = instanceID, manageIndex = i };
            }
        }

        private bool DerivesFrom(int iTypeDescription, int potentialBase)
        {
            if (iTypeDescription < 0) return false;
            if (iTypeDescription == potentialBase)
                return true;

            var baseIndex = m_Snapshot.typeDescriptions.baseOrElementTypeIndex[iTypeDescription];

            if (baseIndex < 0)
                return false;
            var baseArrayIndex = m_Snapshot.typeDescriptions.TypeIndex2ArrayIndex(baseIndex);
            return DerivesFrom(baseArrayIndex, potentialBase);
        }

        private void CrawlRawObjectData(BytesAndOffset bytesAndOffset, int iTypeDescription, bool useStaticFields, ulong ptrFrom, int indexOfFrom)
        {
            //_instanceFields
            //var fields = useStaticFields ? m_Snapshot.typeDescriptions.fieldIndices_static[iTypeDescription] : m_Snapshot.typeDescriptions.fieldIndices_instance[iTypeDescription];
            var fields = useStaticFields ? m_Snapshot.typeDescriptions.fieldIndicesOwned_static[iTypeDescription] : m_Snapshot.typeDescriptions.fieldIndices_instance[iTypeDescription];

            foreach (var iField in fields)
            {
                int iField_TypeDescription_TypeIndex = m_Snapshot.fieldDescriptions.typeIndex[iField];
                int iField_TypeDescription_ArrayIndex = m_Snapshot.typeDescriptions.TypeIndex2ArrayIndex(iField_TypeDescription_TypeIndex);

                var fieldLocation = bytesAndOffset.Add(m_Snapshot.fieldDescriptions.offset[iField] - (useStaticFields ? 0 : m_Snapshot.virtualMachineInformation.objectHeaderSize));

                if (m_Snapshot.typeDescriptions.HasFlag(iField_TypeDescription_ArrayIndex, TypeFlags.kValueType))
                {
                    CrawlRawObjectData(fieldLocation, iField_TypeDescription_ArrayIndex, useStaticFields, ptrFrom, indexOfFrom);
                    continue;
                }


                //temporary workaround for a bug in 5.3b4 and earlier where we would get literals returned as fields with offset 0. soon we'll be able to remove this code.
                bool gotException = false;
                try
                {
                    fieldLocation.ReadPointer();
                }
                catch (ArgumentException)
                {
#if VERIFY_LITERALS_SENT //literals are currently not being crawled inside the scripting backend
                    UnityEngine.Debug.LogWarningFormat("Skipping field {0} on type {1}", m_Snapshot.fieldDescriptions.fieldDescriptionName[iField], m_Snapshot.typeDescriptions.typeDescriptionName[iTypeDescription]);
                    UnityEngine.Debug.LogWarningFormat("FieldType.name: {0}", m_Snapshot.typeDescriptions.typeDescriptionName[iField_TypeDescription_ArrayIndex]);
#endif
                    gotException = true;
                }

                if (!gotException)
                {
                    CrawlPointer(fieldLocation.ReadPointer(), ptrFrom, iTypeDescription, indexOfFrom, iField, -1, true);
                }
            }
        }

        // return false if bad object
        private bool CrawlPointer(ulong pointer, ulong pointerFrom, int typeFrom, int indexOfFrom, int fieldFrom, int fromArrayIndex, bool ignoreBadHeaderError)
        {
            var bo = m_Snapshot.managedHeapSections.Find(pointer, m_Snapshot.virtualMachineInformation);
            if (!bo.IsValid)
            {
#if DEBUG_VALIDATION
                if (pointer != 0)
                {
                    UnityEngine.Debug.LogError("CrawlPointer ptr not found " + pointer);
                }
#endif
                return false;
            }


            bool wasAlreadyCrawled;
            ManagedObjectInfo obj;

            obj = ParseObjectHeader(m_Snapshot.managedHeapSections, pointer, out wasAlreadyCrawled, ignoreBadHeaderError);
            ++obj.refCount;
            m_ConnectionList.Add(ManagedConnection.MakeConnection(m_Snapshot, indexOfFrom, pointerFrom, obj.managedObjectIndex, pointer, typeFrom, fieldFrom, fromArrayIndex));

            if (!obj.IsKnownType())
                return false;
            if (wasAlreadyCrawled)
                return true;

            if (!m_Snapshot.typeDescriptions.HasFlag(obj.iTypeDescription, TypeFlags.kArray))
            {
                CrawlRawObjectData(bo.Add(m_Snapshot.virtualMachineInformation.objectHeaderSize), obj.iTypeDescription, false, pointer, obj.managedObjectIndex);
                return true;
            }

            var arrayLength = ArrayTools.ReadArrayLength(m_Snapshot, m_Snapshot.managedHeapSections, pointer, obj.iTypeDescription, m_Snapshot.virtualMachineInformation);
            int iElementTypeDescription = m_Snapshot.typeDescriptions.baseOrElementTypeIndex[obj.iTypeDescription];
            if(iElementTypeDescription == -1)
            {
                return false; //do not crawl uninitialized object types, as we currently don't have proper handling for these
            }
            var cursor = bo.Add(m_Snapshot.virtualMachineInformation.arrayHeaderSize);
            for (int i = 0; i != arrayLength; i++)
            {
                if (m_Snapshot.typeDescriptions.HasFlag(iElementTypeDescription, TypeFlags.kValueType))
                {
                    CrawlRawObjectData(cursor, iElementTypeDescription, false, pointer, obj.managedObjectIndex);
                    cursor = cursor.Add(m_Snapshot.typeDescriptions.size[iElementTypeDescription]);
                }
                else
                {
                    CrawlPointer(cursor.ReadPointer(), pointer, obj.iTypeDescription, obj.managedObjectIndex, -1, i, false);
                    cursor = cursor.NextPointer();
                }
            }
            return true;
        }

        int SizeOfObjectInBytes(int iTypeDescription, BytesAndOffset bo, CachedSnapshot.ManagedMemorySectionEntriesCache heap, ulong address)
        {
            return SizeOfObjectInBytes(m_Snapshot, iTypeDescription, bo, heap, address);
        }

        static int SizeOfObjectInBytes(CachedSnapshot snapshot, int iTypeDescription, BytesAndOffset bo, CachedSnapshot.ManagedMemorySectionEntriesCache heap, ulong address)
        {
            if (iTypeDescription < 0) return 0;

            if (snapshot.typeDescriptions.HasFlag(iTypeDescription, TypeFlags.kArray))
                return ArrayTools.ReadArrayObjectSizeInBytes(snapshot, heap, address, iTypeDescription, snapshot.virtualMachineInformation);

            if (snapshot.typeDescriptions.typeDescriptionName[iTypeDescription] == "System.String")
                return StringTools.ReadStringObjectSizeInBytes(bo, snapshot.virtualMachineInformation);

            //array and string are the only types that are special, all other types just have one size, which is stored in the typedescription
            return snapshot.typeDescriptions.size[iTypeDescription];
        }

        int SizeOfObjectInBytes(int iTypeDescription, BytesAndOffset bo, CachedSnapshot.ManagedMemorySectionEntriesCache heap)
        {
            return SizeOfObjectInBytes(m_Snapshot, iTypeDescription, bo, heap);
        }

        static int SizeOfObjectInBytes(CachedSnapshot snapshot, int iTypeDescription, BytesAndOffset bo, CachedSnapshot.ManagedMemorySectionEntriesCache heap)
        {
            if (iTypeDescription < 0) return 0;

            if (snapshot.typeDescriptions.HasFlag(iTypeDescription, TypeFlags.kArray))
                return ArrayTools.ReadArrayObjectSizeInBytes(snapshot, heap, bo, iTypeDescription, snapshot.virtualMachineInformation);

            if (snapshot.typeDescriptions.typeDescriptionName[iTypeDescription] == "System.String")
                return StringTools.ReadStringObjectSizeInBytes(bo, snapshot.virtualMachineInformation);

            //array and string are the only types that are special, all other types just have one size, which is stored in the typedescription
            return snapshot.typeDescriptions.size[iTypeDescription];
        }

        private ManagedObjectInfo ParseObjectHeader(CachedSnapshot.ManagedMemorySectionEntriesCache heap, ulong ptrObjectHeader, out bool wasAlreadyCrawled, bool ignoreBadHeaderError)
        {
            ManagedObjectInfo o;
            if (!m_Snapshot.m_CrawledData.managedObjectByAddress.TryGetValue(ptrObjectHeader, out o))
            {
                o = ParseObjectHeader(m_Snapshot, heap, ptrObjectHeader, ignoreBadHeaderError);
                o.managedObjectIndex = m_ObjectList.Count;
                m_ObjectList.Add(o);
                m_Snapshot.m_CrawledData.managedObjectByAddress.Add(ptrObjectHeader, o);
                wasAlreadyCrawled = false;
                return o;
            }

            // this happens on objects from gcHandles, they are added before any other crawled object but have their ptr set to 0.
            if (o.ptrObject == 0)
            {
                var index = o.managedObjectIndex;
                o = ParseObjectHeader(m_Snapshot, heap, ptrObjectHeader, ignoreBadHeaderError);
                o.managedObjectIndex = index;
                m_ObjectList[index] = o;
                m_Snapshot.m_CrawledData.managedObjectByAddress[ptrObjectHeader] = o;

                wasAlreadyCrawled = false;
                return o;
            }

            wasAlreadyCrawled = true;
            return o;
        }

        public static ManagedObjectInfo ParseObjectHeader(CachedSnapshot snapshot, CachedSnapshot.ManagedMemorySectionEntriesCache heap, ulong ptrObjectHeader, bool ignoreBadHeaderError)
        {
            var boHeader = heap.Find(ptrObjectHeader, snapshot.virtualMachineInformation);
            var o = ParseObjectHeader(snapshot, heap, boHeader, ignoreBadHeaderError);
            o.ptrObject = ptrObjectHeader;
            return o;
        }

        public static ManagedObjectInfo ParseObjectHeader(CachedSnapshot snapshot, CachedSnapshot.ManagedMemorySectionEntriesCache heap, BytesAndOffset obj, bool ignoreBadHeaderError)
        {
            ManagedObjectInfo o;

            o = new ManagedObjectInfo();
            o.ptrObject = 0;
            o.managedObjectIndex = -1;

            var boHeader = obj;
            var ptrIdentity = boHeader.ReadPointer();
            o.ptrTypeInfo = ptrIdentity;
            o.iTypeDescription = snapshot.typeDescriptions.TypeInfo2ArrayIndex(o.ptrTypeInfo);
            bool error = false;
            if (o.iTypeDescription < 0)
            {
                var boIdentity = heap.Find(ptrIdentity, snapshot.virtualMachineInformation);
                if (boIdentity.IsValid)
                {
                    var ptrTypeInfo = boIdentity.ReadPointer();
                    o.ptrTypeInfo = ptrTypeInfo;
                    o.iTypeDescription = snapshot.typeDescriptions.TypeInfo2ArrayIndex(o.ptrTypeInfo);
                    error = o.iTypeDescription < 0;
                }
                else
                {
                    error = true;
                }
            }
            if (!error)
            {
                o.size = SizeOfObjectInBytes(snapshot, o.iTypeDescription, boHeader, heap);
                o.data = boHeader;
            }
            else
            {
                if (!ignoreBadHeaderError)
                {
                    var ptrIdentityTypeIndex = snapshot.typeDescriptions.TypeInfo2ArrayIndex(ptrIdentity);


                    var cursor = boHeader;
                    string str = "";
                    for (int j = 0; j != 4; ++j)
                    {
                        for (int i = 0; i != 8; ++i)
                        {
                            var b = cursor.bytes[cursor.offset + i];
                            str += string.Format(" {0:X2}", b);
                        }

                        var d = cursor.ReadInt64();
                        str += string.Format(" : 0x{0:X}, {1}", d, d);
                        str += "\n";
                        cursor = cursor.Add(8);
                    }
                    UnityEngine.Debug.LogWarning("Unknown object header or type. "
                        + " header: \n" + str
                        + " First pointer as type index = " + ptrIdentityTypeIndex
                        );
                }

                o.ptrTypeInfo = 0;
                o.iTypeDescription = -1;
                o.size = 0;
            }

            return o;
        }
    }

    internal static class StringTools
    {
        public static string ReadString(BytesAndOffset bo, VirtualMachineInformation virtualMachineInformation)
        {
            var lengthPointer = bo.Add(virtualMachineInformation.objectHeaderSize);
            var length = lengthPointer.ReadInt32();
            var firstChar = lengthPointer.Add(4);

            return System.Text.Encoding.Unicode.GetString(firstChar.bytes, firstChar.offset, length * 2);
        }

        public static int ReadStringObjectSizeInBytes(BytesAndOffset bo, VirtualMachineInformation virtualMachineInformation)
        {
            var lengthPointer = bo.Add(virtualMachineInformation.objectHeaderSize);
            var length = lengthPointer.ReadInt32();

            return virtualMachineInformation.objectHeaderSize + /*lengthfield*/ 1 + (length * /*utf16=2bytes per char*/ 2) + /*2 zero terminators*/ 2;
        }
    }
    internal class ArrayInfo
    {
        public ulong baseAddress;
        public int[] rank;
        public int length;
        public int elementSize;
        public int arrayTypeDescription;
        public int elementTypeDescription;
        public BytesAndOffset header;
        public BytesAndOffset data;
        public BytesAndOffset GetArrayElement(int index)
        {
            return data.Add(elementSize * index);
        }

        public ulong GetArrayElementAddress(int index)
        {
            return baseAddress + (ulong)(elementSize * index);
        }

        public string IndexToRankedString(int index)
        {
            return ArrayTools.ArrayRankIndexToString(rank, index);
        }

        public string ArrayRankToString()
        {
            return ArrayTools.ArrayRankToString(rank);
        }
    }
    internal static class ArrayTools
    {
        public static ArrayInfo GetArrayInfo(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, UInt64 address, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            var o = new ArrayInfo();
            o.baseAddress = address;
            o.arrayTypeDescription = iTypeDescriptionArrayType;


            o.header = heap.Find(address, virtualMachineInformation);
            o.data = o.header.Add(data.virtualMachineInformation.arrayHeaderSize);
            var bounds = o.header.Add(virtualMachineInformation.arrayBoundsOffsetInHeader).ReadPointer();

            if (bounds == 0)
            {
                o.length = o.header.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadInt32();
                o.rank = new int[1] { o.length };
            }
            else
            {
                var cursor = heap.Find(bounds, virtualMachineInformation);
                int rank = data.typeDescriptions.GetRank(iTypeDescriptionArrayType);
                o.rank = new int[rank];
                o.length = 1;
                for (int i = 0; i != rank; i++)
                {
                    var l = cursor.ReadInt32();
                    o.length *= l;
                    o.rank[i] = l;
                    cursor = cursor.Add(8);
                }
            }

            o.elementTypeDescription = data.typeDescriptions.baseOrElementTypeIndex[iTypeDescriptionArrayType];
            if (data.typeDescriptions.HasFlag(o.elementTypeDescription, TypeFlags.kValueType))
            {
                o.elementSize = data.typeDescriptions.size[o.elementTypeDescription];
            }
            else
            {
                o.elementSize = virtualMachineInformation.pointerSize;
            }
            return o;
        }

        public static ArrayInfo GetArrayInfo(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, BytesAndOffset arrayData, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            var o = new ArrayInfo();
            o.baseAddress = 0;
            o.arrayTypeDescription = iTypeDescriptionArrayType;


            o.header = arrayData;
            o.data = o.header.Add(data.virtualMachineInformation.arrayHeaderSize);
            var bounds = o.header.Add(virtualMachineInformation.arrayBoundsOffsetInHeader).ReadPointer();

            if (bounds == 0)
            {
                o.length = o.header.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadInt32();
                o.rank = new int[1] { o.length };
            }
            else
            {
                var cursor = heap.Find(bounds, virtualMachineInformation);
                int rank = data.typeDescriptions.GetRank(iTypeDescriptionArrayType);
                o.rank = new int[rank];
                o.length = 1;
                for (int i = 0; i != rank; i++)
                {
                    var l = cursor.ReadInt32();
                    o.length *= l;
                    o.rank[i] = l;
                    cursor = cursor.Add(8);
                }
            }

            o.elementTypeDescription = data.typeDescriptions.baseOrElementTypeIndex[iTypeDescriptionArrayType];
            if (o.elementTypeDescription == -1) //We currently do not handle uninitialized types as such override the type, making it return pointer size
            {
                o.elementTypeDescription = iTypeDescriptionArrayType;
            }
            if (data.typeDescriptions.HasFlag(o.elementTypeDescription, TypeFlags.kValueType))
            {
                o.elementSize = data.typeDescriptions.size[o.elementTypeDescription];
            }
            else
            {
                o.elementSize = virtualMachineInformation.pointerSize;
            }
            return o;
        }

        public static int GetArrayElementSize(CachedSnapshot data, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            int iElementTypeDescription = data.typeDescriptions.baseOrElementTypeIndex[iTypeDescriptionArrayType];
            if (data.typeDescriptions.HasFlag(iElementTypeDescription, TypeFlags.kValueType))
            {
                return data.typeDescriptions.size[iElementTypeDescription];
            }
            return virtualMachineInformation.pointerSize;
        }

        public static string ArrayRankToString(int[] rankLength)
        {
            string o = "";
            for (int i = 0; i < rankLength.Length; ++i)
            {
                if (o.Length > 0)
                {
                    o += ", ";
                }
                o += rankLength[i].ToString();
            }
            return o;
        }

        public static string ArrayRankIndexToString(int[] rankLength, int index)
        {
            string o = "";
            int remainder = index;
            for (int i = 1; i < rankLength.Length; ++i)
            {
                if (o.Length > 0)
                {
                    o += ", ";
                }
                var l = rankLength[i];
                int rankIndex = remainder / l;
                o += rankIndex.ToString();
                remainder = remainder - rankIndex * l;
            }
            if (o.Length > 0)
            {
                o += ", ";
            }
            o += remainder;
            return o;
        }

        public static int[] ReadArrayRankLength(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, UInt64 address, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            if (iTypeDescriptionArrayType < 0) return null;

            var bo = heap.Find(address, virtualMachineInformation);
            var bounds = bo.Add(virtualMachineInformation.arrayBoundsOffsetInHeader).ReadPointer();

            if (bounds == 0)
            {
                return new int[1] { bo.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadInt32() };
            }

            var cursor = heap.Find(bounds, virtualMachineInformation);
            int rank = data.typeDescriptions.GetRank(iTypeDescriptionArrayType);
            int[] l = new int[rank];
            for (int i = 0; i != rank; i++)
            {
                l[i] = cursor.ReadInt32();
                cursor = cursor.Add(8);
            }
            return l;
        }

        public static int ReadArrayLength(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, UInt64 address, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            if (iTypeDescriptionArrayType < 0) return 0;

            var bo = heap.Find(address, virtualMachineInformation);
            return ReadArrayLength(data, heap, bo, iTypeDescriptionArrayType, virtualMachineInformation);
        }

        public static int ReadArrayLength(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, BytesAndOffset arrayData, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            if (iTypeDescriptionArrayType < 0) return 0;

            var bo = arrayData;
            var bounds = bo.Add(virtualMachineInformation.arrayBoundsOffsetInHeader).ReadPointer();

            if (bounds == 0)
                return bo.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadInt32();

            var cursor = heap.Find(bounds, virtualMachineInformation);
            int length = 1;
            int rank = data.typeDescriptions.GetRank(iTypeDescriptionArrayType);
            for (int i = 0; i != rank; i++)
            {
                length *= cursor.ReadInt32();
                cursor = cursor.Add(8);
            }
            return length;
        }

        public static int ReadArrayObjectSizeInBytes(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, UInt64 address, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            var arrayLength = ArrayTools.ReadArrayLength(data, heap, address, iTypeDescriptionArrayType, virtualMachineInformation);


            var ti = data.typeDescriptions.baseOrElementTypeIndex[iTypeDescriptionArrayType];
            var ai = data.typeDescriptions.TypeIndex2ArrayIndex(ti);
            var isValueType = data.typeDescriptions.HasFlag(ai, TypeFlags.kValueType);

            var elementSize = isValueType ? data.typeDescriptions.size[ai] : virtualMachineInformation.pointerSize;
            return virtualMachineInformation.arrayHeaderSize + elementSize * arrayLength;
        }

        public static int ReadArrayObjectSizeInBytes(CachedSnapshot data, CachedSnapshot.ManagedMemorySectionEntriesCache heap, BytesAndOffset arrayData, int iTypeDescriptionArrayType, VirtualMachineInformation virtualMachineInformation)
        {
            var arrayLength = ArrayTools.ReadArrayLength(data, heap, arrayData, iTypeDescriptionArrayType, virtualMachineInformation);


            var ti = data.typeDescriptions.baseOrElementTypeIndex[iTypeDescriptionArrayType];
            if(ti == -1) // check added as element type index can be -1 if we are dealing with a class member (Eg Dictionary.Entry) whose type is uninitialized due to their generic data not getting inflated a.k.a unused types
            {
                ti = iTypeDescriptionArrayType;
            }
            var ai = data.typeDescriptions.TypeIndex2ArrayIndex(ti);
            var isValueType = data.typeDescriptions.HasFlag(ai, TypeFlags.kValueType);

            var elementSize = isValueType ? data.typeDescriptions.size[ai] : virtualMachineInformation.pointerSize;
            return virtualMachineInformation.arrayHeaderSize + elementSize * arrayLength;
        }
    }
}
