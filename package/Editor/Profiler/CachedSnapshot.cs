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

    internal class CachedSnapshot
    {
        public static int kCacheEntrySize = 4 * 1024;

        public ManagedData CrawledData { internal set; get; }
        public PackedMemorySnapshot packedMemorySnapshot { private set; get; }

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

            public string GetReadableCallstackForId( NativeCallstackSymbolEntriesCache symbols, long id)
            {
                int entryIdx = this.id.FindIndex(x=> x==id);

                return entryIdx < 0 ? "" : GetReadableCallstack(symbols, entryIdx);
            }
            public string GetReadableCallstack( NativeCallstackSymbolEntriesCache symbols, long idx)
            {
                string readableStackTrace = "";

                ulong[] callstackSymbols = this.callstackSymbols[idx];

                for (int i=0; i<callstackSymbols.Length; ++i)
                {
                    int symbolIdx = symbols.symbol.FindIndex(x=> x==callstackSymbols[i]);

                    if (symbolIdx < 0)
                        readableStackTrace += "<unknown>\n";
                    else
                        readableStackTrace += symbols.readableStackTrace[symbolIdx];
                }
                
                return readableStackTrace;
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
                    var cacheBytes = new byte[1][];
                    for (uint i = 0; i < Count; ++i)
                    {
                        ss.bytes.GetEntries(i, 1, ref cacheBytes);
                        bytes[i] = cacheBytes[0];

                    }
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

        public VirtualMachineInformation virtualMachineInformation { get; private set; }


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

        public SortedNativeMemoryRegionEntriesCache SortedNativeRegionsEntries;
        public SortedManagedMemorySectionEntriesCache SortedManagedStacksEntries;
        public SortedManagedMemorySectionEntriesCache SortedManagedHeapEntries;
        public SortedManagedObjectsCache SortedManagedObjects;
        public SortedNativeAllocationsCache SortedNativeAllocations;
        public SortedNativeObjectsCache SortedNativeObjects;

        public CachedSnapshot(PackedMemorySnapshot s)
        {
            packedMemorySnapshot = s;
            virtualMachineInformation = s.virtualMachineInformation;
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

            SortedNativeRegionsEntries = new SortedNativeMemoryRegionEntriesCache(this);
            SortedManagedStacksEntries = new SortedManagedMemorySectionEntriesCache(managedStacks);
            SortedManagedHeapEntries   = new SortedManagedMemorySectionEntriesCache(managedHeapSections);

            SortedManagedObjects    = new SortedManagedObjectsCache(this);
            SortedNativeAllocations = new SortedNativeAllocationsCache(this);
            SortedNativeObjects     = new SortedNativeObjectsCache(this);

            CrawledData = new ManagedData();

            typeDescriptions.InitSecondaryItems(this);
            nativeObjects.InitSecondaryItems();
            nativeObjects.InitSecondaryItems(this);
        }

        //Unified Object index are in that order: gcHandle, native object, crawled objects
        public int ManagedObjectIndexToUnifiedObjectIndex(int i)
        {
            if (i < 0) return -1;
            if (i < gcHandles.Count) return i;
            if (i < CrawledData.ManagedObjects.Count) return i + (int)nativeObjects.Count;
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
            int lastCrawled = (int)nativeObjects.Count + CrawledData.ManagedObjects.Count;
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
                return (int)nativeObjects.Count + CrawledData.ManagedObjects.Count;
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
            for (int i = 0; i != CrawledData.Connections.Count; ++i)
            {
                switch (CrawledData.Connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.ManagedObject_To_ManagedObject:
                        if (CrawledData.Connections[i].fromManagedObjectIndex == fromManaged && CrawledData.Connections[i].toManagedObjectIndex == toManaged)
                        {
                            return true;
                        }
                        break;
                    case ManagedConnection.ConnectionType.UnityEngineObject:
                    {
                        var cManaged = CrawledData.Connections[i].UnityEngineManagedObjectIndex;
                        var cNative = CrawledData.Connections[i].UnityEngineNativeObjectIndex;
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
            for (int i = 0; i != CrawledData.Connections.Count; ++i)
            {
                switch (CrawledData.Connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.Global_To_ManagedObject:
                        if (CrawledData.Connections[i].toManagedObjectIndex == toManagedObjectIndex)
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
            for (int i = 0; i != CrawledData.Connections.Count; ++i)
            {
                switch (CrawledData.Connections[i].connectionType)
                {
                    case ManagedConnection.ConnectionType.ManagedType_To_ManagedObject:
                        if (CrawledData.Connections[i].fromManagedType == fromType && CrawledData.Connections[i].toManagedObjectIndex == toManagedObjectIndex)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        public interface ISortedEntriesCache
        {
            void Preload();
            int Count { get; } 
            ulong Address(int index);
            ulong Size(int index);
        }

        public class SortedNativeMemoryRegionEntriesCache : ISortedEntriesCache
        {
            CachedSnapshot m_Snapshot;
            int[] m_Sorting;

            public SortedNativeMemoryRegionEntriesCache(CachedSnapshot snapshot)
            {
                m_Snapshot = snapshot;
            }
            
            public void Preload()
            {
                if (m_Sorting == null)
                {
                    m_Sorting = new int[m_Snapshot.nativeMemoryRegions.Count];

                    for (int i=0; i<m_Sorting.Length; ++i)
                        m_Sorting[i] = i;

                    Array.Sort(m_Sorting, (x,y)=>m_Snapshot.nativeMemoryRegions.addressBase[x].CompareTo(m_Snapshot.nativeMemoryRegions.addressBase[y]));
                }                    
            }

            int this[ int index ]
            {
                get {
                    Preload();
                    return m_Sorting[index];
                }
            }

            public int  Count { get { return (int) m_Snapshot.nativeMemoryRegions.Count; } } 
            public ulong  Address(int index) { return m_Snapshot.nativeMemoryRegions.addressBase[this[index]]; }
            public ulong  Size(int index) { return m_Snapshot.nativeMemoryRegions.addressSize[this[index]]; }

            public string Name(int index) { return m_Snapshot.nativeMemoryRegions.memoryRegionName[this[index]]; }
            public int    UnsortedParentRegionIndex(int index) { return m_Snapshot.nativeMemoryRegions.parentIndex[this[index]]; }
            public int    UnsortedFirstAllocationIndex(int index) { return m_Snapshot.nativeMemoryRegions.firstAllocationIndex[this[index]]; }
            public int    UnsortedNumAllocations(int index) { return m_Snapshot.nativeMemoryRegions.numAllocations[this[index]]; }
        }

        public class SortedManagedMemorySectionEntriesCache : ISortedEntriesCache
        {
            ManagedMemorySectionEntriesCache m_Entries;
            int[] m_Sorting;

            public SortedManagedMemorySectionEntriesCache(ManagedMemorySectionEntriesCache entries)
            {
                m_Entries = entries;
            }

            public void Preload()
            {
                if (m_Sorting == null)
                {
                    m_Sorting = new int[m_Entries.Count];

                    for (int i=0; i<m_Sorting.Length; ++i)
                        m_Sorting[i] = i;

                    Array.Sort(m_Sorting, (x,y)=>m_Entries.startAddress[x].CompareTo(m_Entries.startAddress[y]));
                }
            }

            int this[ int index ]
            {
                get {
                    Preload();                    
                    return m_Sorting[index];
                }
            }

            public int  Count { get { return (int) m_Entries.Count; } } 
            public ulong Address(int index) { return m_Entries.startAddress[this[index]]; }
            public ulong Size(int index) { return (ulong)m_Entries.bytes[this[index]].Length; }
            public byte[] Bytes(int index) { return m_Entries.bytes[this[index]]; }
        }

        public class SortedManagedObjectsCache : ISortedEntriesCache
        {
            CachedSnapshot m_Snapshot;
            int[] m_Sorting;

            public SortedManagedObjectsCache(CachedSnapshot snapshot)
            {
                m_Snapshot = snapshot;
            }

            public void Preload()
            {
                if (m_Sorting == null)
                {
                    m_Sorting = new int[m_Snapshot.CrawledData.ManagedObjects.Count];

                    for (int i=0; i<m_Sorting.Length; ++i)
                        m_Sorting[i] = i;

                    Array.Sort(m_Sorting, (x,y)=>m_Snapshot.CrawledData.ManagedObjects[x].PtrObject.CompareTo(m_Snapshot.CrawledData.ManagedObjects[y].PtrObject));
                }
            }

            ManagedObjectInfo this[ int index ]
            {
                get {
                    Preload();                    
                    return m_Snapshot.CrawledData.ManagedObjects[m_Sorting[index]];
                }
            }

            public int  Count { get { return m_Snapshot.CrawledData.ManagedObjects.Count; } } 
            public ulong Address(int index) { return this[index].PtrObject; }
            public ulong Size(int index) { return (ulong)this[index].Size; }
        }

        public class SortedNativeAllocationsCache : ISortedEntriesCache
        {
            CachedSnapshot m_Snapshot;
            int[] m_Sorting;

            public SortedNativeAllocationsCache(CachedSnapshot snapshot)
            {
                m_Snapshot = snapshot;
            }
            
            public void Preload()
            {
                if (m_Sorting == null)
                {
                    m_Sorting = new int[m_Snapshot.nativeAllocations.address.Length];

                    for (int i=0; i<m_Sorting.Length; ++i)
                        m_Sorting[i] = i;

                    Array.Sort(m_Sorting, (x,y)=>m_Snapshot.nativeAllocations.address[x].CompareTo(m_Snapshot.nativeAllocations.address[y]));
                }
            }

            int this[ int index ]
            {
                get 
                {
                    Preload();                
                    return m_Sorting[index];
                }
            }

            public int  Count { get { return (int)m_Snapshot.nativeAllocations.Count; } } 
            public ulong Address(int index) { return m_Snapshot.nativeAllocations.address[this[index]]; }
            public ulong Size(int index) { return m_Snapshot.nativeAllocations.size[this[index]]; }
            public int MemoryRegionIndex(int index) { return m_Snapshot.nativeAllocations.memoryRegionIndex[this[index]]; }
            public long RootReferenceId(int index) { return m_Snapshot.nativeAllocations.rootReferenceId[this[index]]; }
            public long AllocationSiteId(int index) { return m_Snapshot.nativeAllocations.allocationSiteId[this[index]]; }
            public int OverheadSize(int index) { return m_Snapshot.nativeAllocations.overheadSize[this[index]]; }
            public int PaddingSize(int index) { return m_Snapshot.nativeAllocations.paddingSize[this[index]]; }
        }
            
        public class SortedNativeObjectsCache : ISortedEntriesCache
        {
            CachedSnapshot m_Snapshot;
            int[] m_Sorting;

            public SortedNativeObjectsCache(CachedSnapshot snapshot)
            {
                m_Snapshot = snapshot;
            }

            public void Preload()
            {
                if (m_Sorting == null)
                {
                    m_Sorting = new int[m_Snapshot.nativeObjects.nativeObjectAddress.Length];

                    for (int i=0; i<m_Sorting.Length; ++i)
                        m_Sorting[i] = i;

                    Array.Sort(m_Sorting, (x,y)=>m_Snapshot.nativeObjects.nativeObjectAddress[x].CompareTo(m_Snapshot.nativeObjects.nativeObjectAddress[y]));
                }
            }

            int this[ int index ]
            {
                get 
                {
                    Preload();
                    return m_Sorting[index];
                }
            }

            public int  Count { get { return (int)m_Snapshot.nativeObjects.Count; } } 
            public ulong Address(int index) { return m_Snapshot.nativeObjects.nativeObjectAddress[this[index]]; }
            public ulong Size(int index) { return m_Snapshot.nativeObjects.size[this[index]]; }
            public string Name(int index) { return m_Snapshot.nativeObjects.objectName[this[index]]; }
            public int InstanceId(int index) { return m_Snapshot.nativeObjects.instanceId[this[index]]; }
            public int NativeTypeArrayIndex(int index) { return m_Snapshot.nativeObjects.nativeTypeArrayIndex[this[index]]; }
            public HideFlags HideFlags(int index) { return m_Snapshot.nativeObjects.hideFlags[this[index]]; }
            public ObjectFlags Flags(int index) { return m_Snapshot.nativeObjects.flags[this[index]]; }
            public long RootReferenceId(int index) { return m_Snapshot.nativeObjects.rootReferenceId[this[index]]; }
            public int Refcount(int index) { return m_Snapshot.nativeObjects.refcount[this[index]]; }
            public int ManagedObjectIndex(int index) { return m_Snapshot.nativeObjects.managedObjectIndex[this[index]]; }
        } 
    }
}
