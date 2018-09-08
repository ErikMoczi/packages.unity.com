using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Profiling.Memory.Experimental;
using Unity.MemoryProfiler.Editor.Database.Soa;

namespace Unity.MemoryProfiler.Editor
{
    internal static class TypeTools
    {
        public enum FieldFindOptions
        {
            OnlyInstance,
            OnlyStatic
        }


        static public IEnumerable<int> AllFieldArrayIndexOf(int iTypeArrayIndex, CachedSnapshot data, FieldFindOptions findOptions, bool includeBase)
        {
            int baseTypeIndex = data.typeDescriptions.baseOrElementTypeIndex[iTypeArrayIndex];

            bool isValueType = data.typeDescriptions.HasFlag(iTypeArrayIndex, TypeFlags.kValueType);
            //yield all fields from base type
            if (includeBase
                && baseTypeIndex != -1
                && !isValueType)
            {
                int baseArrayIndex = data.typeDescriptions.TypeIndex2ArrayIndex(baseTypeIndex);
                foreach (var iField in AllFieldArrayIndexOf(baseArrayIndex, data, findOptions, includeBase))
                    yield return iField;
            }
            int iTypeIndex = data.typeDescriptions.typeIndex[iTypeArrayIndex];
            foreach (var iField in data.typeDescriptions.fieldIndices[iTypeArrayIndex])
            {
                if (!FieldMatchesOptions(iField, data, findOptions))
                    continue;

                if (data.fieldDescriptions.typeIndex[iField] == iTypeIndex && isValueType)
                {
                    // this happens in primitive types like System.Single, which is a weird type that has a field of its own type.
                    continue;
                }

                if (data.fieldDescriptions.offset[iField] == -1)
                {
                    // this is how we encode TLS fields. We don't support TLS fields yet.
                    continue;
                }

                yield return iField;
            }
        }

        static bool FieldMatchesOptions(int fieldIndex, CachedSnapshot data, FieldFindOptions options)
        {
            if (options == FieldFindOptions.OnlyStatic)
            {
                return data.fieldDescriptions.isStatic[fieldIndex];
            }
            if (options == FieldFindOptions.OnlyInstance)
            {
                return !data.fieldDescriptions.isStatic[fieldIndex];
            }
            return false;
        }
    }

    public class CachedSnapshot
    {
        public static int kCacheEntrySize = 4 * 1024;
        private VirtualMachineInformation m_VirtualMachineInfo;
        public ManagedData m_CrawledData;
        public PackedMemorySnapshot packedMemorySnapshot;
        public class NativeAllocationSiteEntriesCache
        {
            public uint Count;
            public DataArray.Cache<long> id;
            public DataArray.Cache<int> memoryLabelIndex;
            public DataArray.Cache<ulong[]> callstackSymbols;
            public SoaDataSet dataSet;
            public NativeAllocationSiteEntriesCache(NativeAllocationSiteEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                id = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.id));
                memoryLabelIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.memoryLabelIndex));
                callstackSymbols = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.callstackSymbols));
            }
        }

        public class TypeDescriptionEntriesCache
        {
            public uint Count;
            public DataArray.Cache<TypeFlags> flags;
            public DataArray.Cache<string> typeDescriptionName;
            public DataArray.Cache<string> assembly;
            public DataArray.Cache<int[]> fieldIndices;
            public DataArray.Cache<byte[]> staticFieldBytes;
            public DataArray.Cache<int> baseOrElementTypeIndex;
            public DataArray.Cache<int> size;
            public DataArray.Cache<ulong> typeInfoAddress;
            public DataArray.Cache<int> typeIndex;
            public SoaDataSet dataSet;
            public TypeDescriptionEntriesCache(TypeDescriptionEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                flags                   = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.flags));
                typeDescriptionName     = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.typeDescriptionName));
                assembly                = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.assembly));
                fieldIndices            = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.fieldIndices));
                staticFieldBytes        = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.staticFieldBytes));
                baseOrElementTypeIndex  = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.baseOrElementTypeIndex));
                size                    = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.size));
                typeInfoAddress         = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.typeInfoAddress));
                typeIndex               = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.typeIndex));
            }

            public int[][] fieldIndices_instance;//includes all bases' instance fields
            public int[][] fieldIndices_static;  //includes all bases' static fields
            public int[][] fieldIndicesOwned_static;  //includes only type's static fields
            public bool[] m_HasStaticFields;
            public int iType_ValueType;
            public int iType_Object;
            public int iType_Enum;

            public Dictionary<UInt64, int> typeInfoToArrayIndex;
            public Dictionary<int, int> typeIndexToArrayIndex;

            // Check all bases' fields
            public bool HasAnyField(int iType)
            {
                return fieldIndices_instance[iType].Length > 0 || fieldIndices_static[iType].Length > 0;
            }

            // Check all bases' fields
            public bool HasAnyStaticField(int iType)
            {
                return fieldIndices_static[iType].Length > 0;
            }

            // Check only the type's fields
            public bool HasStaticField(int iType)
            {
                return m_HasStaticFields[iType];
            }

            public bool HasFlag(int arrayIndex, TypeFlags flag)
            {
                return (flags[arrayIndex] & flag) == flag;
            }

            public int GetRank(int arrayIndex)
            {
                int r = (int)(flags[arrayIndex] & TypeFlags.kArrayRankMask) >> 16;
                return r;
            }

            public int TypeIndex2ArrayIndex(int typeIndex)
            {
                int i;
                if (!typeIndexToArrayIndex.TryGetValue(typeIndex, out i))
                {
                    throw new Exception("typeIndex not found");
                }
                return i;
            }

            public int TypeInfo2ArrayIndex(UInt64 aTypeInfoAddress)
            {
                int i;

                if (!typeInfoToArrayIndex.TryGetValue(aTypeInfoAddress, out i))
                {
                    return -1;
                }
                return i;
            }

            public void InitSecondaryItems(CachedSnapshot cs)
            {
                typeInfoToArrayIndex = Enumerable.Range(0, (int)typeInfoAddress.Length).ToDictionary(x => typeInfoAddress[x], x => x);
                typeIndexToArrayIndex = Enumerable.Range(0, (int)typeIndex.Length).ToDictionary(x => typeIndex[x], x => x);

                m_HasStaticFields = new bool[Count];
                fieldIndices_instance = new int[Count][];
                fieldIndices_static = new int[Count][];
                fieldIndicesOwned_static = new int[Count][];
                for (int i = 0; i < Count; ++i)
                {
                    m_HasStaticFields[i] = false;
                    foreach (var iField in fieldIndices[i])
                    {
                        if (cs.fieldDescriptions.isStatic[iField])
                        {
                            m_HasStaticFields[i] = true;
                            break;
                        }
                    }
                    fieldIndices_instance[i] = TypeTools.AllFieldArrayIndexOf(i, cs, TypeTools.FieldFindOptions.OnlyInstance, true).ToArray();
                    fieldIndices_static[i] = TypeTools.AllFieldArrayIndexOf(i, cs, TypeTools.FieldFindOptions.OnlyStatic, true).ToArray();
                    fieldIndicesOwned_static[i] = TypeTools.AllFieldArrayIndexOf(i, cs, TypeTools.FieldFindOptions.OnlyStatic, false).ToArray();
                }


                iType_ValueType = typeDescriptionName.FindIndex(x => x == "System.ValueType");
                iType_Object = typeDescriptionName.FindIndex(x => x == "System.Object");
                iType_Enum = typeDescriptionName.FindIndex(x => x == "System.Enum");
            }
        }

        public class NativeTypeEntriesCache
        {
            public uint Count;
            public DataArray.Cache<string> typeName;
            public DataArray.Cache<int> nativeBaseTypeArrayIndex;
            public SoaDataSet dataSet;
            public NativeTypeEntriesCache(NativeTypeEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                typeName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.typeName));
                nativeBaseTypeArrayIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.nativeBaseTypeArrayIndex));
            }
        }

        public class NativeRootReferenceEntriesCache
        {
            public uint Count;
            public DataArray.Cache<long> id;
            public DataArray.Cache<string> areaName;
            public DataArray.Cache<string> objectName;
            public DataArray.Cache<ulong> accumulatedSize;
            public SoaDataSet dataSet;
            public NativeRootReferenceEntriesCache(NativeRootReferenceEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                id = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.id));
                areaName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.areaName));
                objectName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.objectName));
                accumulatedSize = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.accumulatedSize));
            }
        }

        public class NativeObjectEntriesCache
        {
            public uint Count;
            public DataArray.Cache<string> objectName;
            public DataArray.Cache<int> instanceId;
            public DataArray.Cache<ulong> size;
            public DataArray.Cache<int> nativeTypeArrayIndex;
            public DataArray.Cache<HideFlags> hideFlags;
            public DataArray.Cache<ObjectFlags> flags;
            public DataArray.Cache<ulong> nativeObjectAddress;
            public DataArray.Cache<long> rootReferenceId;
            public int[] refcount;
            public int[] managedObjectIndex;
            public SoaDataSet dataSet;
            public NativeObjectEntriesCache(NativeObjectEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                objectName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.objectName));
                instanceId = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.instanceId));
                size = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.size));
                nativeTypeArrayIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.nativeTypeArrayIndex));
                hideFlags = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.hideFlags));
                flags = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.flags));
                nativeObjectAddress = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.nativeObjectAddress));
                rootReferenceId = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.rootReferenceId));
            }

            public static int InstanceID_None = 0;
            public SortedDictionary<int, int> instanceId2Index;
            public void InitSecondaryItems()
            {
                instanceId2Index = new SortedDictionary<int, int>();
                for (int i = 0; i != Count; ++i)
                {
                    var id = instanceId[i];
                    instanceId2Index[id] = i;
                }
            }

            public void InitSecondaryItems(CachedSnapshot snapshot)
            {
                refcount = new int[Count];
                managedObjectIndex = new int[Count];

                for (int i = 0; i != Count; ++i)
                {
                    managedObjectIndex[i] = -1;
                }
            }
        }

        public class NativeMemoryRegionEntriesCache
        {
            public uint Count;
            public DataArray.Cache<string> memoryRegionName;
            public DataArray.Cache<int> parentIndex;
            public DataArray.Cache<ulong> addressBase;
            public DataArray.Cache<ulong> addressSize;
            public DataArray.Cache<int> firstAllocationIndex;
            public DataArray.Cache<int> numAllocations;
            public SoaDataSet dataSet;
            public NativeMemoryRegionEntriesCache(NativeMemoryRegionEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                memoryRegionName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.memoryRegionName));
                parentIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.parentIndex));
                addressBase = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.addressBase));
                addressSize = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.addressSize));
                firstAllocationIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.firstAllocationIndex));
                numAllocations = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.numAllocations));
            }
        }
        public class NativeMemoryLabelEntriesCache
        {
            public uint Count;
            public DataArray.Cache<string> memoryLabelName;
            public SoaDataSet dataSet;
            public NativeMemoryLabelEntriesCache(NativeMemoryLabelEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                memoryLabelName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.memoryLabelName));
            }
        }


        public class NativeCallstackSymbolEntriesCache
        {
            public uint Count;
            public DataArray.Cache<ulong> symbol;
            public DataArray.Cache<string> readableStackTrace;
            public SoaDataSet dataSet;
            public NativeCallstackSymbolEntriesCache(NativeCallstackSymbolEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                symbol = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.symbol));
                readableStackTrace = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.readableStackTrace));
            }
        }

        public class NativeAllocationEntriesCache
        {
            public uint Count;
            public DataArray.Cache<int> memoryRegionIndex;
            public DataArray.Cache<long> rootReferenceId;
            public DataArray.Cache<long> allocationSiteId;
            public DataArray.Cache<ulong> address;
            public DataArray.Cache<ulong> size;
            public DataArray.Cache<int> overheadSize;
            public DataArray.Cache<int> paddingSize;
            public SoaDataSet dataSet;
            public NativeAllocationEntriesCache(NativeAllocationEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                memoryRegionIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.memoryRegionIndex));
                rootReferenceId = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.rootReferenceId));
                allocationSiteId = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.allocationSiteId));
                address = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.address));
                size = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.size));
                overheadSize = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.overheadSize));
                paddingSize = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.paddingSize));
            }
        }


        public class ManagedMemorySectionEntriesCache
        {
            public uint Count;
            public byte[][] bytes;
            public ulong[] startAddress;
            public SoaDataSet dataSet;
            public BytesAndOffset Find(UInt64 address, VirtualMachineInformation virtualMachineInformation)
            {
                for (int i = 0; i != Count; ++i)
                {
                    if (address >= startAddress[i] && address < (startAddress[i] + (ulong)bytes[i].Length))
                        return new BytesAndOffset() { bytes = bytes[i], offset = (int)(address - startAddress[i]), pointerSize = virtualMachineInformation.pointerSize };
                }
                return new BytesAndOffset();
            }

            public ManagedMemorySectionEntriesCache(ManagedMemorySectionEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                if (Count > 0)
                {
                    bytes = new byte[Count][];
                    ss.bytes.GetEntries(0, Count, ref bytes);
                    startAddress = new ulong[Count];
                    ss.startAddress.GetEntries(0, Count, ref startAddress);
                }
            }
        }


        public class GCHandleEntriesCache
        {
            public uint Count;
            public DataArray.Cache<ulong> target;
            public SoaDataSet dataSet;
            public GCHandleEntriesCache(GCHandleEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                target = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.target));
            }
        }

        public class FieldDescriptionEntriesCache
        {
            public uint Count;
            public DataArray.Cache<string> fieldDescriptionName;
            public DataArray.Cache<int> offset;
            public DataArray.Cache<int> typeIndex;
            public DataArray.Cache<bool> isStatic;
            public SoaDataSet dataSet;
            public FieldDescriptionEntriesCache(FieldDescriptionEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                fieldDescriptionName = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.fieldDescriptionName));
                offset = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.offset));
                typeIndex = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.typeIndex));
                isStatic = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.isStatic));
            }
        }

        public class ConnectionEntriesCache
        {
            public uint Count;
            public DataArray.Cache<int> from;
            public DataArray.Cache<int> to;
            public SoaDataSet dataSet;
            public ConnectionEntriesCache(ConnectionEntries ss)
            {
                Count = ss.GetNumEntries();
                dataSet = new SoaDataSet(Count, kCacheEntrySize);
                from = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.from));
                to = DataArray.MakeCache(dataSet, DataSourceFromAPI.ApiToDatabase(ss.to));
            }
        }

        public VirtualMachineInformation virtualMachineInformation
        {
            get
            {
                return m_VirtualMachineInfo;
            }
        }


        public NativeAllocationSiteEntriesCache nativeAllocationSites;
        public TypeDescriptionEntriesCache typeDescriptions;
        public NativeTypeEntriesCache nativeTypes;
        public NativeRootReferenceEntriesCache nativeRootReferences;
        public NativeObjectEntriesCache nativeObjects;
        public NativeMemoryRegionEntriesCache nativeMemoryRegions;
        public NativeMemoryLabelEntriesCache nativeMemoryLabels;
        public NativeCallstackSymbolEntriesCache nativeCallstackSymbols;
        public NativeAllocationEntriesCache nativeAllocations;
        public ManagedMemorySectionEntriesCache managedStacks;
        public ManagedMemorySectionEntriesCache managedHeapSections;
        public GCHandleEntriesCache gcHandles;
        public FieldDescriptionEntriesCache fieldDescriptions;
        public ConnectionEntriesCache connections;

        public CachedSnapshot(PackedMemorySnapshot s)
        {
            packedMemorySnapshot = s;
            m_VirtualMachineInfo = s.virtualMachineInformation;
            nativeAllocationSites   = new NativeAllocationSiteEntriesCache(s.nativeAllocationSites);
            typeDescriptions        = new TypeDescriptionEntriesCache(s.typeDescriptions);
            nativeTypes             = new NativeTypeEntriesCache(s.nativeTypes);
            nativeRootReferences    = new NativeRootReferenceEntriesCache(s.nativeRootReferences);
            nativeObjects           = new NativeObjectEntriesCache(s.nativeObjects);
            nativeMemoryRegions     = new NativeMemoryRegionEntriesCache(s.nativeMemoryRegions);
            nativeMemoryLabels      = new NativeMemoryLabelEntriesCache(s.nativeMemoryLabels);
            nativeCallstackSymbols  = new NativeCallstackSymbolEntriesCache(s.nativeCallstackSymbols);
            nativeAllocations       = new NativeAllocationEntriesCache(s.nativeAllocations);
            managedStacks           = new ManagedMemorySectionEntriesCache(s.managedStacks);
            managedHeapSections     = new ManagedMemorySectionEntriesCache(s.managedHeapSections);
            gcHandles               = new GCHandleEntriesCache(s.gcHandles);
            fieldDescriptions       = new FieldDescriptionEntriesCache(s.fieldDescriptions);
            connections             = new ConnectionEntriesCache(s.connections);

            typeDescriptions.InitSecondaryItems(this);
            nativeObjects.InitSecondaryItems();
            nativeObjects.InitSecondaryItems(this);
        }

        //Unified Object index are in that order: gcHandle, native object, crawled objects
        public int ManagedObjectIndexToUnifiedObjectIndex(int i)
        {
            if (i < 0) return -1;
            if (i < gcHandles.Count) return i;
            if (i < m_CrawledData.managedObjects.Count) return i + (int)nativeObjects.Count;
            return -1;
        }

        public int NativeObjectIndexToUnifiedObjectIndex(int i)
        {
            if (i < 0) return -1;
            if (i < nativeObjects.Count) return i + (int)gcHandles.Count;
            return -1;
        }

        public int UnifiedObjectIndexToManagedObjectIndex(int i)
        {
            if (i < 0) return -1;
            if (i < gcHandles.Count) return i;
            int firstCrawled = (int)(gcHandles.Count + nativeObjects.Count);
            int lastCrawled = (int)nativeObjects.Count + m_CrawledData.managedObjects.Count;
            if (i >= firstCrawled && i < lastCrawled) return i - (int)nativeObjects.Count;
            return -1;
        }

        public int UnifiedObjectIndexToNativeObjectIndex(int i)
        {
            if (i < gcHandles.Count) return -1;
            int firstCrawled = (int)(gcHandles.Count + nativeObjects.Count);
            if (i < firstCrawled) return i - (int)gcHandles.Count;
            return -1;
        }

        public int unifiedObjectCount
        {
            get
            {
                return (int)nativeObjects.Count + m_CrawledData.managedObjects.Count;
            }
        }

        public bool HasObjectConnection(int fromUnifiedObject, int toUnifiedObject)
        {
            for (int i = 0; i != connections.Count; ++i)
            {
                if (connections.from[i] == fromUnifiedObject && connections.to[i] == toUnifiedObject)
                {
                    return true;
                }
            }
            int fromManaged = UnifiedObjectIndexToManagedObjectIndex(fromUnifiedObject);
            int toManaged = UnifiedObjectIndexToManagedObjectIndex(toUnifiedObject);
            int fromNative = UnifiedObjectIndexToNativeObjectIndex(fromUnifiedObject);
            int toNative = UnifiedObjectIndexToNativeObjectIndex(toUnifiedObject);
            for (int i = 0; i != m_CrawledData.connections.Count; ++i)
            {
                switch (m_CrawledData.connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.ManagedObject_To_ManagedObject:
                        if (m_CrawledData.connections[i].fromManagedObjectIndex == fromManaged && m_CrawledData.connections[i].toManagedObjectIndex == toManaged)
                        {
                            return true;
                        }
                        break;
                    case ManagedConnection.ConnectionType.UnityEngineObject:
                    {
                        var cManaged = m_CrawledData.connections[i].UnityEngineManagedObjectIndex;
                        var cNative = m_CrawledData.connections[i].UnityEngineNativeObjectIndex;
                        if (cManaged == fromManaged && cNative == toNative)
                        {
                            return true;
                        }
                        if (cManaged == toManaged && cNative == fromNative)
                        {
                            return true;
                        }
                    }
                    break;
                }
            }
            return false;
        }

        public bool HasGlobalConnection(int toManagedObjectIndex)
        {
            for (int i = 0; i != m_CrawledData.connections.Count; ++i)
            {
                switch (m_CrawledData.connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.Global_To_ManagedObject:
                        if (m_CrawledData.connections[i].toManagedObjectIndex == toManagedObjectIndex)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        public bool HasManagedTypeConnection(int fromType, int toManagedObjectIndex)
        {
            for (int i = 0; i != m_CrawledData.connections.Count; ++i)
            {
                switch (m_CrawledData.connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.ManagedType_To_ManagedObject:
                        if (m_CrawledData.connections[i].fromManagedType == fromType && m_CrawledData.connections[i].toManagedObjectIndex == toManagedObjectIndex)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        //public bool HasNativeConnection(int fromNativeObject, int toNativeObject)
        //{

        //}
        //public bool HasManagedConnection(int fromManagedIndex, int toManagedObjectIndex)
        //{

        //}
        //public bool HasUnityEngineObjectConnection(int nativeObjectIndex, int managedObjectIndex)
        //{

        //}
    }
}
