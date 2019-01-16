using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.Profiling.Memory.Experimental;

namespace test
{
    public class TestConnection
    {
        PackedMemorySnapshot m_Snapshot;

        public int m_UnityEngineObject_TypeIndex;

        public int m_UnityEngineObject_instanceIDOffset;
        public int m_UnityEngineObject_cachedPtrOffset;

        public SnapshotData<string> m_ManagedTypeNames;
        public SnapshotData<string> m_ManagedFieldNames;
        public SnapshotData<int> m_ManagedFieldOffsets;
        public SnapshotData<int> m_ManagedbaseOrElementTypeIndex;
        public SnapshotData<int> m_ManagedTypeIndex;
        public SnapshotData<int> m_NativeObject_InstanceID;
        public SnapshotData<ulong> m_NativeObject_Address;
        public SnapshotData<string> m_NativeObject_Name;
        public SnapshotData<Byte[]> m_ManagedHeapSectionsByte;
        public SnapshotData<ulong> m_ManagedHeapSectionsStartAddress;
        public SnapshotData<ulong> m_gcHandlesTarget;
        public SnapshotData<ulong> m_managedTypeInfo;
        public SnapshotData<int[]> m_managedTypeFieldIndex;

        public SnapshotData<int> m_ConnectionFrom;
        public SnapshotData<int> m_ConnectionTo;

        public SnapshotData<string> m_NativeType_Name;
        public SnapshotData<int> m_NativeObject_TypeIndex;

        public SortedDictionary<int, int> m_NativeObjectInstanceID_ToIndex;
        public SortedDictionary<int, int> m_ManagedTypeIndex_ToTypeDescription;
        public SortedDictionary<ulong, int> m_ManagedTypeInfo_ToTypeDescription;

        public ManagedObjectInfo[] m_ManagedObjects;

        public TestConnection(UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot snapshot)
        {
            m_Snapshot = snapshot;
            InitData();
        }

        public class SnapshotData<DataT>
        {
            public DataT[] m_Data;
            public SnapshotData(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<DataT> d)
            {
                uint count = d.GetNumEntries();
                m_Data = new DataT[count];
                d.GetEntries(0, count, ref m_Data);
            }

            public int Length
            {
                get
                {
                    return m_Data.Length;
                }
            }

            public DataT this[int i]
            {
                get
                {
                    return m_Data[i];
                }
            }
            public int FindIndex(Predicate<DataT> match)
            {
                return Array.FindIndex(m_Data, match);
            }
        }
        public SnapshotData<DataT> MakeSnapshotData<DataT>(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<DataT> d)
        {
            return new SnapshotData<DataT>(d);
        }

        public void InitData()
        {
            //Get all managed type names
            m_ManagedTypeNames = MakeSnapshotData(m_Snapshot.typeDescriptions.typeDescriptionName);
            m_ManagedFieldNames = MakeSnapshotData(m_Snapshot.fieldDescriptions.fieldDescriptionName);
            m_ManagedFieldOffsets = MakeSnapshotData(m_Snapshot.fieldDescriptions.offset);
            m_ManagedbaseOrElementTypeIndex = MakeSnapshotData(m_Snapshot.typeDescriptions.baseOrElementTypeIndex);
            m_ManagedTypeIndex = MakeSnapshotData(m_Snapshot.typeDescriptions.typeIndex);
            m_NativeObject_InstanceID = MakeSnapshotData(m_Snapshot.nativeObjects.instanceId);
            m_NativeObject_Address = MakeSnapshotData(m_Snapshot.nativeObjects.nativeObjectAddress);
            m_NativeObject_Name = MakeSnapshotData(m_Snapshot.nativeObjects.objectName);
            m_ManagedHeapSectionsByte = MakeSnapshotData(m_Snapshot.managedHeapSections.bytes);
            m_ManagedHeapSectionsStartAddress = MakeSnapshotData(m_Snapshot.managedHeapSections.startAddress);
            m_gcHandlesTarget = MakeSnapshotData(m_Snapshot.gcHandles.target);
            m_managedTypeInfo = MakeSnapshotData(m_Snapshot.typeDescriptions.typeInfoAddress);

            m_managedTypeFieldIndex = MakeSnapshotData(m_Snapshot.typeDescriptions.fieldIndices);

            m_ConnectionFrom = MakeSnapshotData(m_Snapshot.connections.from);
            m_ConnectionTo = MakeSnapshotData(m_Snapshot.connections.to);

            m_NativeType_Name = MakeSnapshotData(m_Snapshot.nativeTypes.typeName);
            m_NativeObject_TypeIndex = MakeSnapshotData(m_Snapshot.nativeObjects.nativeTypeArrayIndex);

            // Find Unity object
            m_UnityEngineObject_TypeIndex = m_ManagedTypeNames.FindIndex(x => x == "UnityEngine.Object");
            if (m_UnityEngineObject_TypeIndex < 0)
            {
                throw new Exception("No Unity Object!");
            }

            //Get Unity Object field index
            int[] UnityObjectFields = m_managedTypeFieldIndex[m_UnityEngineObject_TypeIndex];

            //Find UnityEngine.Object.m_InstanceID field
            int iField_UnityEngineObject_m_InstanceID = System.Array.FindIndex(UnityObjectFields, iField => m_ManagedFieldNames[iField] == "m_InstanceID");

            m_UnityEngineObject_instanceIDOffset = -1;
            m_UnityEngineObject_cachedPtrOffset = -1;
            if (iField_UnityEngineObject_m_InstanceID >= 0)
            {
                //Get m_InstanceID offset if valid
                var fieldIndex = UnityObjectFields[iField_UnityEngineObject_m_InstanceID];
                m_UnityEngineObject_instanceIDOffset = m_ManagedFieldOffsets[fieldIndex];
            }

            if (m_UnityEngineObject_instanceIDOffset < 0)
            {
                // on UNITY_5_4_OR_NEWER, there is the member m_CachedPtr we can use to identify the connection
                //Since Unity 5.4, UnityEngine.Object no longer stores instance id inside when running in the player. Use cached ptr instead to find the instanceID of native object
                int iField_UnityEngineObject_m_CachedPtr = System.Array.FindIndex(UnityObjectFields, iField => m_ManagedFieldNames[iField] == "m_CachedPtr");
                if (iField_UnityEngineObject_m_CachedPtr >= 0)
                {
                    m_UnityEngineObject_cachedPtrOffset = m_ManagedFieldOffsets[iField_UnityEngineObject_m_CachedPtr];
                }
            }
            if (m_UnityEngineObject_instanceIDOffset < 0 && m_UnityEngineObject_cachedPtrOffset < 0)
            {
                throw new Exception("Could not find unity object instance id field or m_CachedPtr");
            }

            // init m_NativeInstanceID to native object index
            m_NativeObjectInstanceID_ToIndex = new SortedDictionary<int, int>();
            for (int i = 0; i != m_NativeObject_InstanceID.Length; ++i)
            {
                var id = m_NativeObject_InstanceID[i];
                m_NativeObjectInstanceID_ToIndex[id] = i;
            }

            m_ManagedTypeIndex_ToTypeDescription = new SortedDictionary<int, int>();
            for (int i = 0; i != m_ManagedTypeIndex.Length; ++i)
            {
                var index = m_ManagedTypeIndex[i];
                if (index != i)
                {
                    Debug.Log("TypeIndex is not index in array");
                }
                m_ManagedTypeIndex_ToTypeDescription[index] = i;
            }

            m_ManagedTypeInfo_ToTypeDescription = new SortedDictionary<ulong, int>();
            for (int i = 0; i != m_managedTypeInfo.Length; ++i)
            {
                ulong tinfo = m_managedTypeInfo[i];
                m_ManagedTypeInfo_ToTypeDescription[tinfo] = i;
            }
        }

        private bool DerivesFrom(int iTypeDescription, int potentialBase)
        {
            if (iTypeDescription < 0) return false;
            if (iTypeDescription == potentialBase)
                return true;

            var baseIndex = m_ManagedbaseOrElementTypeIndex[iTypeDescription];

            if (baseIndex < 0)
                return false;
            var baseArrayIndex = m_ManagedTypeIndex_ToTypeDescription[baseIndex];
            return DerivesFrom(baseArrayIndex, potentialBase);
        }

        public BytesAndOffset FindManageBytes(UInt64 address)
        {
            for (int i = 0; i != m_ManagedHeapSectionsStartAddress.Length; ++i)
            {
                if (address >= m_ManagedHeapSectionsStartAddress[i] && address < (m_ManagedHeapSectionsStartAddress[i] + (ulong)m_ManagedHeapSectionsByte[i].Length))
                    return new BytesAndOffset() { bytes = m_ManagedHeapSectionsByte[i], offset = (int)(address - m_ManagedHeapSectionsStartAddress[i]), pointerSize = m_Snapshot.virtualMachineInformation.pointerSize };
            }
            return new BytesAndOffset();
        }

        public int ManagedTypeInfoToTypeDescriptionIndex(ulong typeInfoPtr)
        {
            int v;
            if (m_ManagedTypeInfo_ToTypeDescription.TryGetValue(typeInfoPtr, out v))
            {
                return v;
            }
            return -1;
        }

        public struct BytesAndOffset
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
        public class ManagedObjectInfo
        {
            public ulong ptrObject;
            public ulong ptrTypeInfo;
            public int iTypeDescription;
            public int managedObjectIndex;
            public bool usedHeaderIdentityPointer;
            public BytesAndOffset data;
            public int nativeObjectIndex = -1;
            public bool IsValidObject()
            {
                return iTypeDescription >= 0;
            }
        }
        public ManagedObjectInfo ParseObjectHeader(ulong ptr, int index)
        {
            ManagedObjectInfo o;

            o = new ManagedObjectInfo();
            o.ptrObject = ptr;
            o.managedObjectIndex = index;

            var boHeader = FindManageBytes(ptr);
            var ptrIdentity = boHeader.ReadPointer();
            o.ptrTypeInfo = ptrIdentity;
            o.iTypeDescription = ManagedTypeInfoToTypeDescriptionIndex(o.ptrTypeInfo);
            bool error = false;
            if (o.iTypeDescription < 0)
            {
                o.usedHeaderIdentityPointer = true;
                var boIdentity = FindManageBytes(ptrIdentity);
                if (boIdentity.IsValid)
                {
                    var ptrTypeInfo = boIdentity.ReadPointer();
                    o.ptrTypeInfo = ptrTypeInfo;
                    o.iTypeDescription = ManagedTypeInfoToTypeDescriptionIndex(o.ptrTypeInfo);
                    error = o.iTypeDescription < 0;
                }
                else
                {
                    error = true;
                }
            }
            else
            {
                o.usedHeaderIdentityPointer = false;
            }
            if (!error)
            {
                o.data = boHeader;

                //If is a unity object, find its native object index
                if (DerivesFrom(o.iTypeDescription, m_UnityEngineObject_TypeIndex))
                {
                    int instanceID = 0;
                    if (m_UnityEngineObject_instanceIDOffset >= 0)
                    {
                        instanceID = FindManageBytes(o.ptrObject + (UInt64)m_UnityEngineObject_instanceIDOffset).ReadInt32();
                    }
                    else if (m_UnityEngineObject_cachedPtrOffset >= 0)
                    {
                        // If you get a compilation error on the following 2 lines, update to Unity 5.4b14.
                        var cachedPtr = FindManageBytes(o.ptrObject + (UInt64)m_UnityEngineObject_cachedPtrOffset).ReadPointer();
                        var indexOfNativeObject = m_NativeObject_Address.FindIndex(no => no == cachedPtr);
                        if (indexOfNativeObject >= 0)
                        {
                            instanceID = m_NativeObject_InstanceID[indexOfNativeObject];
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Managed object (addr:" + o.ptrObject + ", index:" + o.managedObjectIndex + ") pointing to unknown native object. (cachedPtr:" + cachedPtr + ")");
                        }
                    }

                    if (!m_NativeObjectInstanceID_ToIndex.TryGetValue(instanceID, out o.nativeObjectIndex))
                    {
                        o.nativeObjectIndex = -1;
                    }
                }
            }
            else
            {
                var ptrIdentityTypeIndex = ManagedTypeInfoToTypeDescriptionIndex(ptrIdentity);


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
                UnityEngine.Debug.LogWarning("Unknown object header or type. Index = " + index
                    + " header: \n" + str
                    + " First pointer as type index = " + ptrIdentityTypeIndex
                    );

                o.ptrTypeInfo = 0;
                o.iTypeDescription = -1;
            }

            return o;
        }

        public void CrawlManagedObjects()
        {
            m_ManagedObjects = new ManagedObjectInfo[m_gcHandlesTarget.Length];
            int headerIdentityPtrCount = 0;
            int headerPtrCount = 0;
            int invalidObjectCount = 0;
            for (int i = 0; i != m_gcHandlesTarget.Length; ++i)
            {
                m_ManagedObjects[i] = ParseObjectHeader(m_gcHandlesTarget[i], i);
                if (m_ManagedObjects[i].IsValidObject())
                {
                    if (m_ManagedObjects[i].usedHeaderIdentityPointer)
                    {
                        ++headerIdentityPtrCount;
                    }
                    else
                    {
                        ++headerPtrCount;
                    }
                }
                else
                {
                    ++invalidObjectCount;
                }
            }
            Debug.Log("HeaderPtr=" + headerPtrCount + " HeaderIdentity=" + headerIdentityPtrCount + " Invalid Header=" + invalidObjectCount);
        }

        public ManagedObjectInfo FindManagedObject(int iTypeIndex, string nativeObjectName)
        {
            for (int i = 0; i != m_ManagedObjects.Length; ++i)
            {
                var o = m_ManagedObjects[i];
                if (o.iTypeDescription == iTypeIndex)
                {
                    if (o.nativeObjectIndex >= 0)
                    {
                        if (m_NativeObject_Name[o.nativeObjectIndex] == nativeObjectName)
                        {
                            return o;
                        }
                    }
                }
            }
            return null;
        }

        public int FindType(string name)
        {
            return m_ManagedTypeNames.FindIndex(x => x == name);
        }

        public string GetObjectNameFromConnectionObjectIndex(int objIndex)
        {
            int nativeObjectIndex = objIndex;
            if (objIndex < m_gcHandlesTarget.Length)
            {
                //is a managed object
                int managedObjectIndex = objIndex;// - m_NativeObject_Address.Length;
                nativeObjectIndex = m_ManagedObjects[managedObjectIndex].nativeObjectIndex;// + m_gcHandlesTarget.Length;
            }
            else
            {
                nativeObjectIndex = objIndex - m_gcHandlesTarget.Length;
            }
            if (nativeObjectIndex >= 0)
            {
                return m_NativeObject_Name[nativeObjectIndex];
            }
            return "<unnamed>";
        }

        public string GetObjectTypeNameFromConnectionObjectIndex(int objIndex)
        {
            if (objIndex < m_gcHandlesTarget.Length)
            {
                //is a managed object
                int managedObjectIndex = objIndex;// - m_NativeObject_Address.Length;
                int iType = m_ManagedObjects[managedObjectIndex].iTypeDescription;
                return "(Managed) " + m_ManagedTypeNames[iType];
            }

            if (objIndex >= 0)
            {
                int nativeObjectIndex = objIndex - m_gcHandlesTarget.Length;
                int iType = m_NativeObject_TypeIndex[nativeObjectIndex];
                return "(Native) " + m_NativeType_Name[iType];
            }
            return null;
        }

        public void DoTest()
        {
            CrawlManagedObjects();

            int iType_Transform = FindType("UnityEngine.Transform");
            if (iType_Transform < 0)
            {
                throw new Exception("No Unity Transform!");
            }
            var objCubeTransform = FindManagedObject(iType_Transform, "Cube");
            if (objCubeTransform == null)
            {
                throw new Exception("Could not find Cube Transform object");
            }

            //Managed objects are indexed after the native object in the connection data.
            int objectIndexInConnection = objCubeTransform.managedObjectIndex;// + m_NativeObject_Address.Length;

            string str = "Cube Transform, managed object index = " + objCubeTransform.managedObjectIndex
                + ", native object index = " + objCubeTransform.nativeObjectIndex
                + ", connection object index = " + objectIndexInConnection + "\n";


            List<int> objIndexTo = new List<int>();
            List<int> objIndexFrom = new List<int>();
            for (int i = 0; i != m_ConnectionFrom.Length; ++i)
            {
                if (m_ConnectionFrom[i] == objectIndexInConnection)
                {
                    objIndexTo.Add(m_ConnectionTo[i]);
                }
                if (m_ConnectionTo[i] == objectIndexInConnection)
                {
                    objIndexFrom.Add(m_ConnectionFrom[i]);
                }
            }

            str += "Connection To " + objIndexTo.Count + " object(s):\n";
            foreach (var i in objIndexTo)
            {
                str += "\tIndex=" + i;
                str += "    Name=\"" + GetObjectNameFromConnectionObjectIndex(i) + "\"";
                str += "    Type=\"" + GetObjectTypeNameFromConnectionObjectIndex(i);
                str += "\n";
            }

            str += "Connection From " + objIndexFrom.Count + " object(s):\n";
            foreach (var i in objIndexFrom)
            {
                str += "\tIndex=" + i;
                str += "    Name=\"" + GetObjectNameFromConnectionObjectIndex(i) + "\"";
                str += "    Type=\"" + GetObjectTypeNameFromConnectionObjectIndex(i);
                str += "\n";
            }
            Debug.Log(str);
        }
    }
}
