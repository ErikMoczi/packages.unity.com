﻿using System;
using Unity.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal static unsafe class ChunkDataUtility
    {
        public static int GetIndexInTypeArray(Archetype* archetype, int typeIndex)
        {
            var types = archetype->Types;
            var typeCount = archetype->TypesCount;
            for (var i = 0; i != typeCount; i++)
                if (typeIndex == types[i].TypeIndex)
                    return i;

            return -1;
        }

        public static void GetIndexInTypeArray(Archetype* archetype, int typeIndex, ref int typeLookupCache)
        {
            var types = archetype->Types;
            var typeCount = archetype->TypesCount;

            if (typeLookupCache < typeCount && types[typeLookupCache].TypeIndex == typeIndex)
                return;

            for (var i = 0; i != typeCount; i++)
            {
                if (typeIndex != types[i].TypeIndex)
                    continue;

                typeLookupCache = i;
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new InvalidOperationException("Shouldn't happen");
#endif
        }

        public static void GetComponentDataWithTypeAndFixedArrayLength(Chunk* chunk, int index, int typeIndex,
            out byte* outPtr, out int outArrayLength)
        {
            var archetype = chunk->Archetype;
            var indexInTypeArray = GetIndexInTypeArray(archetype, typeIndex);

            var offset = archetype->Offsets[indexInTypeArray];
            var sizeOf = archetype->SizeOfs[indexInTypeArray];

            outPtr = chunk->Buffer + (offset + sizeOf * index);
            outArrayLength = archetype->Types[indexInTypeArray].FixedArrayLength;
        }

        public static byte* GetComponentDataWithTypeRO(Chunk* chunk, int index, int typeIndex, ref int typeLookupCache)
        {
            var archetype = chunk->Archetype;
            GetIndexInTypeArray(archetype, typeIndex, ref typeLookupCache);
            var indexInTypeArray = typeLookupCache;

            var offset = archetype->Offsets[indexInTypeArray];
            var sizeOf = archetype->SizeOfs[indexInTypeArray];

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static byte* GetComponentDataWithTypeRW(Chunk* chunk, int index, int typeIndex, uint globalSystemVersion,
            ref int typeLookupCache)
        {
            var archetype = chunk->Archetype;
            GetIndexInTypeArray(archetype, typeIndex, ref typeLookupCache);
            var indexInTypeArray = typeLookupCache;

            var offset = archetype->Offsets[indexInTypeArray];
            var sizeOf = archetype->SizeOfs[indexInTypeArray];

            chunk->ChangeVersion[indexInTypeArray] = globalSystemVersion;

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static byte* GetComponentDataWithTypeRO(Chunk* chunk, int index, int typeIndex)
        {
            var indexInTypeArray = GetIndexInTypeArray(chunk->Archetype, typeIndex);

            var offset = chunk->Archetype->Offsets[indexInTypeArray];
            var sizeOf = chunk->Archetype->SizeOfs[indexInTypeArray];

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static byte* GetComponentDataWithTypeRW(Chunk* chunk, int index, int typeIndex, uint globalSystemVersion)
        {
            var indexInTypeArray = GetIndexInTypeArray(chunk->Archetype, typeIndex);

            var offset = chunk->Archetype->Offsets[indexInTypeArray];
            var sizeOf = chunk->Archetype->SizeOfs[indexInTypeArray];

            chunk->ChangeVersion[indexInTypeArray] = globalSystemVersion;

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static byte* GetComponentDataRO(Chunk* chunk, int index, int indexInTypeArray)
        {
            var offset = chunk->Archetype->Offsets[indexInTypeArray];
            var sizeOf = chunk->Archetype->SizeOfs[indexInTypeArray];

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static byte* GetComponentDataRW(Chunk* chunk, int index, int indexInTypeArray, uint globalSystemVersion)
        {
            var offset = chunk->Archetype->Offsets[indexInTypeArray];
            var sizeOf = chunk->Archetype->SizeOfs[indexInTypeArray];

            chunk->ChangeVersion[indexInTypeArray] = globalSystemVersion;

            return chunk->Buffer + (offset + sizeOf * index);
        }

        public static void Copy(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstIndex, int count)
        {
            Assert.IsTrue(srcChunk->Archetype == dstChunk->Archetype);

            var arch = srcChunk->Archetype;
            var srcBuffer = srcChunk->Buffer;
            var dstBuffer = dstChunk->Buffer;
            var offsets = arch->Offsets;
            var sizeOfs = arch->SizeOfs;
            var typesCount = arch->TypesCount;

            for (var t = 0; t < typesCount; t++)
            {
                var offset = offsets[t];
                var sizeOf = sizeOfs[t];
                var src = srcBuffer + (offset + sizeOf * srcIndex);
                var dst = dstBuffer + (offset + sizeOf * dstIndex);

                dstChunk->ChangeVersion[t] = srcChunk->ChangeVersion[t];
                UnsafeUtility.MemCpy(dst, src, sizeOf * count);
            }
        }

        public static void ClearComponents(Chunk* dstChunk, int dstIndex, int count)
        {
            var arch = dstChunk->Archetype;

            var offsets = arch->Offsets;
            var sizeOfs = arch->SizeOfs;
            var dstBuffer = dstChunk->Buffer;
            var typesCount = arch->TypesCount;

            for (var t = 1; t != typesCount; t++)
            {
                var offset = offsets[t];
                var sizeOf = sizeOfs[t];
                var dst = dstBuffer + (offset + sizeOf * dstIndex);

                UnsafeUtility.MemClear(dst, sizeOf * count);
            }
        }

        public static void ReplicateComponents(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstBaseIndex,
            int count)
        {
            Assert.IsTrue(srcChunk->Archetype == dstChunk->Archetype);

            var arch = srcChunk->Archetype;
            var srcBuffer = srcChunk->Buffer;
            var dstBuffer = dstChunk->Buffer;
            var offsets = arch->Offsets;
            var sizeOfs = arch->SizeOfs;
            var typesCount = arch->TypesCount;
            // type[0] is always Entity, and will be patched up later, so just skip
            for (var t = 1; t != typesCount; t++)
            {
                var offset = offsets[t];
                var sizeOf = sizeOfs[t];
                var src = srcBuffer + (offset + sizeOf * srcIndex);
                var dst = dstBuffer + (offset + sizeOf * dstBaseIndex);

                UnsafeUtility.MemCpyReplicate(dst, src, sizeOf, count);
            }
        }

        public static void Convert(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstIndex)
        {
            var srcArch = srcChunk->Archetype;
            var dstArch = dstChunk->Archetype;

            var srcI = 0;
            var dstI = 0;
            while (srcI < srcArch->TypesCount && dstI < dstArch->TypesCount)
                if (srcArch->Types[srcI] < dstArch->Types[dstI])
                {
                    ++srcI;
                }
                else if (srcArch->Types[srcI] > dstArch->Types[dstI])
                {
                    // Clear components in the destination that aren't copied
                    var dst = dstChunk->Buffer + dstArch->Offsets[dstI] + dstIndex * dstArch->SizeOfs[dstI];
                    UnsafeUtility.MemClear(dst, dstArch->SizeOfs[dstI]);
                    ++dstI;
                }
                else
                {
                    var src = srcChunk->Buffer + srcArch->Offsets[srcI] + srcIndex * srcArch->SizeOfs[srcI];
                    var dst = dstChunk->Buffer + dstArch->Offsets[dstI] + dstIndex * dstArch->SizeOfs[dstI];
                    UnsafeUtility.MemCpy(dst, src, srcArch->SizeOfs[srcI]);
                    ++srcI;
                    ++dstI;
                }

            // Clear remaining components in the destination that aren't copied
            for (; dstI < dstArch->TypesCount; ++dstI)
            {
                var dst = dstChunk->Buffer + dstArch->Offsets[dstI] + dstIndex * dstArch->SizeOfs[dstI];
                UnsafeUtility.MemClear(dst, dstArch->SizeOfs[dstI]);
            }
        }

        public static void PoisonUnusedChunkData(Chunk* chunk, byte value)
        {
            var arch = chunk->Archetype;
            var bufferSize = Chunk.GetChunkBufferSize(arch->TypesCount, arch->NumSharedComponents);
            var buffer = chunk->Buffer;
            var count = chunk->Count;

            for (int i = 0; i<arch->TypesCount-1; ++i)
            {
                var index = arch->TypeMemoryOrder[i];
                var nextIndex = arch->TypeMemoryOrder[i + 1];
                var startOffset = arch->Offsets[index] + count * arch->SizeOfs[index];
                var endOffset = arch->Offsets[nextIndex];
                UnsafeUtilityEx.MemSet(buffer + startOffset, value, endOffset - startOffset);
            }
            var lastIndex = arch->TypeMemoryOrder[arch->TypesCount - 1];
            var lastStartOffset = arch->Offsets[lastIndex] + count * arch->SizeOfs[lastIndex];
            UnsafeUtilityEx.MemSet(buffer + lastStartOffset, value, bufferSize - lastStartOffset);
        }

        public static void CopyManagedObjects(ArchetypeManager typeMan, Chunk* srcChunk, int srcStartIndex,
            Chunk* dstChunk, int dstStartIndex, int count)
        {
            var srcArch = srcChunk->Archetype;
            var dstArch = dstChunk->Archetype;

            var srcI = 0;
            var dstI = 0;
            while (srcI < srcArch->TypesCount && dstI < dstArch->TypesCount)
                if (srcArch->Types[srcI] < dstArch->Types[dstI])
                {
                    ++srcI;
                }
                else if (srcArch->Types[srcI] > dstArch->Types[dstI])
                {
                    ++dstI;
                }
                else
                {
                    if (srcArch->ManagedArrayOffset[srcI] >= 0)
                        for (var i = 0; i < count; ++i)
                        {
                            var obj = typeMan.GetManagedObject(srcChunk, srcI, srcStartIndex + i);
                            typeMan.SetManagedObject(dstChunk, dstI, dstStartIndex + i, obj);
                        }

                    ++srcI;
                    ++dstI;
                }
        }

        public static void ClearManagedObjects(ArchetypeManager typeMan, Chunk* chunk, int index, int count)
        {
            var arch = chunk->Archetype;

            for (var type = 0; type < arch->TypesCount; ++type)
            {
                if (arch->ManagedArrayOffset[type] < 0)
                    continue;

                for (var i = 0; i < count; ++i)
                    typeMan.SetManagedObject(chunk, type, index + i, null);
            }
        }
    }
}
