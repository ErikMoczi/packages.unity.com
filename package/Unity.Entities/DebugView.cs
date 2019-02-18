using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Unity.Entities
{
    sealed class UintListDebugView
    {
        private UintList m_UintList;
        public UintListDebugView(UintList UintList)
        {
            m_UintList = UintList;
        }
        public unsafe uint[] Items
        {
            get
            {
                uint[] result = new uint[m_UintList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = m_UintList.p[i];
                return result;
            }
        }
    }

    sealed class IntListDebugView
    {
        private IntList m_IntList;
        public IntListDebugView(IntList intList)
        {
            m_IntList = intList;
        }
        public unsafe int[] Items
        {
            get
            {
                var result = new int[m_IntList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = m_IntList.p[i];
                return result;
            }
        }
    }

    sealed class ChunkListDebugView
    {
        private ChunkList m_ChunkList;
        public ChunkListDebugView(ChunkList chunkList)
        {
            m_ChunkList = chunkList;
        }
        public unsafe ArchetypeChunk[] Items
        {
            get
            {
                var result = new ArchetypeChunk[m_ChunkList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = *(ArchetypeChunk*)&m_ChunkList.p[i];
                return result;
            }
        }
    }

    sealed class ArchetypeChunkDataDebugView
    {
        private ArchetypeChunkData m_ChunkData;
        public ArchetypeChunkDataDebugView(ArchetypeChunkData chunkData)
        {
            m_ChunkData = chunkData;
        }
        public unsafe ArchetypeChunk[] Items
        {
            get
            {
                var result = new ArchetypeChunk[m_ChunkData.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = *(ArchetypeChunk*)&m_ChunkData.p[i];
                return result;
            }
        }
    }

    sealed class ArchetypeListDebugView
    {
        private ArchetypeList m_ArchetypeList;
        public ArchetypeListDebugView(ArchetypeList ArchetypeList)
        {
            m_ArchetypeList = ArchetypeList;
        }
        public unsafe EntityArchetype[] Items
        {
            get
            {
                var result = new EntityArchetype[m_ArchetypeList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = *(EntityArchetype*)&m_ArchetypeList.p[i];
                return result;
            }
        }
    }

    sealed class MatchingArchetypeListDebugView
    {
        private MatchingArchetypeList m_MatchingArchetypeList;
        public MatchingArchetypeListDebugView(MatchingArchetypeList MatchingArchetypeList)
        {
            m_MatchingArchetypeList = MatchingArchetypeList;
        }
        public unsafe MatchingArchetype*[] Items
        {
            get
            {
                var result = new MatchingArchetype*[m_MatchingArchetypeList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = m_MatchingArchetypeList.p[i];
                return result;
            }
        }
    }

    sealed class EntityGroupDataListDebugView
    {
        private EntityGroupDataList m_EntityGroupDataList;
        public EntityGroupDataListDebugView(EntityGroupDataList EntityGroupDataList)
        {
            m_EntityGroupDataList = EntityGroupDataList;
        }
        public unsafe EntityGroupData*[] Items
        {
            get
            {
                var result = new EntityGroupData*[m_EntityGroupDataList.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = m_EntityGroupDataList.p[i];
                return result;
            }
        }
    }

#if !UNITY_CSHARP_TINY
    sealed class ArchetypeChunkDebugView
    {
        private ArchetypeChunk m_ArchetypeChunk;
        public ArchetypeChunkDebugView(ArchetypeChunk ArchetypeChunk)
        {
            m_ArchetypeChunk = ArchetypeChunk;
        }

        unsafe object GetObject(void* pointer, Type type)
        {
            if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(type);
                var instance = (IList)Activator.CreateInstance(constructedListType);
                var size = Marshal.SizeOf(type);
                BufferHeader* header = (BufferHeader*) pointer;
                var begin = BufferHeader.GetElementPointer(header);
                for (var i = 0; i < header->Length; ++i)
                {
                    var item = begin + (size * i);
                    instance.Add(Marshal.PtrToStructure((IntPtr) item, type));
                }
                return instance;
            }
            if(typeof(IComponentData).IsAssignableFrom(type))
            {
                return Marshal.PtrToStructure((IntPtr) pointer, type);
            }
            return null;
        }

        public unsafe IList[] Components
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == null)
                    return new IList[0];
                var archetype = chunk->Archetype;
                var types = chunk->Archetype->TypesCount;
                var entities = chunk->Count;
                var result = new IList[types];
                for (var i = 0; i < types; ++i)
                {
                    var componentType = chunk->Archetype->Types[i];
                    if (componentType.IsSharedComponent)
                        continue;
                    var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
                    var type = typeInfo.Type;
                    var offset = archetype->Offsets[i];
                    var size = archetype->SizeOfs[i];
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(type);
                    if (typeof(IBufferElementData).IsAssignableFrom(type))
                        constructedListType = listType.MakeGenericType(constructedListType);
                    var instance = (IList)Activator.CreateInstance(constructedListType);
                    for (var j = 0; j < entities; ++j)
                    {
                       var pointer = chunk->Buffer + (offset + size * j);
                       instance.Add(GetObject(pointer, type));
                    }
                    result[i] = instance;
                }

                return result;
            }
        }

        public unsafe IList[] Entities
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == null)
                    return new IList[0];
                var archetype = chunk->Archetype;
                var types = chunk->Archetype->TypesCount;
                var entities = chunk->Count;
                var result = new IList[entities];
                for (var j = 0; j < entities; ++j)
                {
                    var instance = new List<object>();
                    for (var i = 0; i < types; ++i)
                    {
                        var componentType = chunk->Archetype->Types[i];
                        if (componentType.IsSharedComponent)
                            continue;
                        var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
                        var type = typeInfo.Type;
                        var offset = archetype->Offsets[i];
                        var size = archetype->SizeOfs[i];
                        var pointer = chunk->Buffer + (offset + size * j);
                        instance.Add(GetObject(pointer,type));
                    }
                    result[j] = instance;
                }

                return result;
            }
        }

        public unsafe int[] SharedComponentValueArray
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == null)
                    return new int[0];
                var archetype = chunk->Archetype;
                int[] result = new int[archetype->NumSharedComponents];
                for (var i = 0; i < archetype->NumSharedComponents; ++i)
                    result[i] = chunk->SharedComponentValues[i];
                return result;
            }
        }

        public unsafe uint[] ChangeVersion
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == null)
                    return new uint[0];
                var archetype = chunk->Archetype;
                uint[] result = new uint[archetype->TypesCount];
                for (var i = 0; i < archetype->TypesCount; ++i)
                    result[i] = chunk->GetChangeVersion(i);
                return result;
            }
        }

    }
#else
    sealed class ArchetypeChunkDebugView
    {
    }
#endif

#if !UNITY_CSHARP_TINY
    sealed class EntityArchetypeDebugView
    {
        private EntityArchetype m_EntityArchetype;
        public EntityArchetypeDebugView(EntityArchetype entityArchetype)
        {
            m_EntityArchetype = entityArchetype;
        }

        public unsafe Type[] Types
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return new Type[0];
                var types = archetype->TypesCount;
                var result = new Type[types];
                for (var i = 0; i < types; ++i)
                {
                    var type = TypeManager.GetTypeInfo(archetype->Types[i].TypeIndex).Type;
                    result[i] = type;
                }
                return result;
            }
        }

        public unsafe int[] Offsets
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return new int[0];
                int[] result = new int[archetype->TypesCount];
                Marshal.Copy((IntPtr)archetype->Offsets, result, 0, archetype->TypesCount);
                return result;
            }
        }

        public unsafe int[] SizeOfs
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return new int[0];
                int[] result = new int[archetype->TypesCount];
                Marshal.Copy((IntPtr)archetype->SizeOfs, result, 0, archetype->TypesCount);
                return result;
            }
        }

        public unsafe int[] TypeMemoryOrder
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return new int[0];
                int[] result = new int[archetype->TypesCount];
                Marshal.Copy((IntPtr)archetype->TypeMemoryOrder, result, 0, archetype->TypesCount);
                return result;
            }
        }

        public unsafe int[] ManagedArrayOffset
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null || archetype->ManagedArrayOffset == null)
                    return new int[0];
                int[] result = new int[archetype->NumManagedArrays];
                Marshal.Copy((IntPtr)archetype->ManagedArrayOffset, result, 0, archetype->NumManagedArrays);
                return result;
            }
        }
    }
#else
    sealed class EntityArchetypeDebugView
    {
    }
#endif
}
