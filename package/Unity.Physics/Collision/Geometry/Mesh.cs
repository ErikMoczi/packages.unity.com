using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Physics
{
    // A collision mesh, containing triangles and quads.
    // Warning: This is just the header, the mesh's variable sized data follows it in memory.
    // Therefore this struct must always be passed by reference, never by value.
    public struct Mesh
    {
        // A set of vertex indices into the section's vertex buffer
        // TODO: "Primitive" is an overloaded term, rename?
        public struct PrimitiveVertexIndices
        {
            public byte A, B, C, D;
        }

        // Flags describing how the primitive vertices are used
        [Flags]
        public enum PrimitiveFlags : byte
        {
            IsTriangle = 1 << 0,        // the primitive stores a single triangle
            IsTrianglePair = 1 << 1,    // the primitive stores a pair of triangles
            IsQuad = 1 << 2            // the primitive stores a pair of coplanar triangles, which can be represented as a single quad
        }

        // A section of the mesh, containing up to 256 vertices.
        public struct Section
        {
            public const int MaxNumVertices = 1 << 8;

            internal BlobArray PrimitiveFlagsBlob;
            public BlobArray.Accessor<PrimitiveFlags> PrimitiveFlags => new BlobArray.Accessor<PrimitiveFlags>(ref PrimitiveFlagsBlob);

            internal BlobArray PrimitiveVertexIndicesBlob;
            public BlobArray.Accessor<PrimitiveVertexIndices> PrimitiveVertexIndices => new BlobArray.Accessor<PrimitiveVertexIndices>(ref PrimitiveVertexIndicesBlob);

            internal BlobArray VerticesBlob;
            public BlobArray.Accessor<float3> Vertices => new BlobArray.Accessor<float3>(ref VerticesBlob);

            internal BlobArray PrimitiveFilterIndicesBlob;
            public BlobArray.Accessor<short> PrimitiveFilterIndices => new BlobArray.Accessor<short>(ref PrimitiveFilterIndicesBlob);

            internal BlobArray FiltersBlob;
            public BlobArray.Accessor<CollisionFilter> Filters => new BlobArray.Accessor<CollisionFilter>(ref FiltersBlob);

            internal BlobArray PrimitiveMaterialIndicesBlob;
            public BlobArray.Accessor<short> PrimitiveMaterialIndices => new BlobArray.Accessor<short>(ref PrimitiveMaterialIndicesBlob);

            internal BlobArray MaterialsBlob;
            public BlobArray.Accessor<Material> Materials => new BlobArray.Accessor<Material>(ref MaterialsBlob);
        }

        // The bounding volume
        private BlobArray m_BvhNodesBlob;
        public unsafe BoundingVolumeHierarchy BoundingVolumeHierarchy
        {
            get
            {
                fixed (BlobArray* blob = &m_BvhNodesBlob)
                {
                    var firstNode = (BoundingVolumeHierarchy.Node*)((byte*)&(blob->Offset) + blob->Offset);
                    return new BoundingVolumeHierarchy(firstNode, nodeFilters: null);
                }
            }
        }

        // The mesh sections
        private BlobArray m_SectionsBlob;
        public BlobArray.Accessor<Section> Sections => new BlobArray.Accessor<Section>(ref m_SectionsBlob);


        // Get the number of bits required to store a key to any of the leaf colliders
        public uint NumColliderKeyBits => (uint)((32 - math.lzcnt(Sections.Length - 1)) + 8 + 1);

        // Burst friendly HasFlag
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrimitveFlagSet(PrimitiveFlags flags, PrimitiveFlags testFlag) => (flags & testFlag) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNumPolygonsInPrimitive(PrimitiveFlags primitiveFlags) => primitiveFlags == PrimitiveFlags.IsTrianglePair ? 2 : 1;

        // Get the flags of a primitive
        internal PrimitiveFlags GetPrimitiveFlags(int primitiveKey)
        {
            int sectionIndex = primitiveKey >> 8;
            int sectionPrimitiveIndex = primitiveKey & 0xFF;

            return Sections[sectionIndex].PrimitiveFlags[sectionPrimitiveIndex];
        }

        // Get the vertices, flags and collision filter of a primitive
        internal void GetPrimitive(int primitiveKey, out float3x4 vertices, out PrimitiveFlags flags, out CollisionFilter filter)
        {
            int sectionIndex = primitiveKey >> 8;
            int sectionPrimitiveIndex = primitiveKey & 0xFF;

            ref Section section = ref Sections[sectionIndex];

            PrimitiveVertexIndices vertexIndices = section.PrimitiveVertexIndices[sectionPrimitiveIndex];

            vertices = new float3x4(
                section.Vertices[vertexIndices.A],
                section.Vertices[vertexIndices.B],
                section.Vertices[vertexIndices.C],
                section.Vertices[vertexIndices.D]);

            flags = section.PrimitiveFlags[sectionPrimitiveIndex];

            short filterIndex = section.PrimitiveFilterIndices[sectionPrimitiveIndex];
            filter = section.Filters[filterIndex];
        }

        // Get the vertices, flags, collision filter and material of a primitive
        internal void GetPrimitive(int primitiveKey, out float3x4 vertices, out PrimitiveFlags flags, out CollisionFilter filter, out Material material)
        {
            int sectionIndex = primitiveKey >> 8;
            int sectionPrimitiveIndex = primitiveKey & 0xFF;

            ref Section section = ref Sections[sectionIndex];

            PrimitiveVertexIndices vertexIndices = section.PrimitiveVertexIndices[sectionPrimitiveIndex];

            vertices = new float3x4(
                section.Vertices[vertexIndices.A],
                section.Vertices[vertexIndices.B],
                section.Vertices[vertexIndices.C],
                section.Vertices[vertexIndices.D]);

            flags = section.PrimitiveFlags[sectionPrimitiveIndex];

            short filterIndex = section.PrimitiveFilterIndices[sectionPrimitiveIndex];
            filter = section.Filters[filterIndex];

            short materialIndex = section.PrimitiveMaterialIndices[sectionPrimitiveIndex];
            material = section.Materials[materialIndex];
        }

        // Configure a polygon based on the given key. The polygon must be initialized already.
        // Returns false if the polygon is filtered out with respect to the given filter.
        internal bool GetPolygon(uint meshKey, CollisionFilter filter, ref PolygonCollider polygon)
        {
            int primitiveKey = (int)meshKey >> 1;
            int polygonIndex = (int)meshKey & 1;

            int sectionIndex = primitiveKey >> 8;
            int sectionPrimitiveIndex = primitiveKey & 0xFF;

            ref Section section = ref Sections[sectionIndex];

            short filterIndex = section.PrimitiveFilterIndices[sectionPrimitiveIndex];
            if (!CollisionFilter.IsCollisionEnabled(filter, section.Filters[filterIndex]))
            {
                return false;
            }
            // Note: Currently not setting the filter on the output polygon,
            // because the caller doesn't need it

            PrimitiveVertexIndices vertexIndices = section.PrimitiveVertexIndices[sectionPrimitiveIndex];
            var vertices = new float3x4(
                section.Vertices[vertexIndices.A],
                section.Vertices[vertexIndices.B],
                section.Vertices[vertexIndices.C],
                section.Vertices[vertexIndices.D]);

            PrimitiveFlags flags = section.PrimitiveFlags[sectionPrimitiveIndex];
            if (IsPrimitveFlagSet(flags, PrimitiveFlags.IsQuad))
            {
                polygon.SetAsQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
            }
            else
            {
                polygon.SetAsTriangle(vertices[0], vertices[1 + polygonIndex], vertices[2 + polygonIndex]);
            }

            short materialIndex = section.PrimitiveMaterialIndices[sectionPrimitiveIndex];
            polygon.Material = section.Materials[materialIndex];

            return true;
        }

        internal bool GetFirstPolygon(out uint meshKey, ref PolygonCollider polygon)
        {
            if (Sections.Length == 0 || Sections[0].PrimitiveFlags.Length == 0)
            {
                meshKey = 0xffffffff;
                return false;
            }

            meshKey = 0;

            ref Section section = ref Sections[0];
            PrimitiveVertexIndices vertexIndices = section.PrimitiveVertexIndices[0];
            var vertices = new float3x4(
                section.Vertices[vertexIndices.A],
                section.Vertices[vertexIndices.B],
                section.Vertices[vertexIndices.C],
                section.Vertices[vertexIndices.D]);

            PrimitiveFlags flags = section.PrimitiveFlags[0];
            if (IsPrimitveFlagSet(flags, PrimitiveFlags.IsQuad))
            {
                polygon.SetAsQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
            }
            else
            {
                polygon.SetAsTriangle(vertices[0], vertices[1], vertices[2]);
            }

            short filterIndex = section.PrimitiveFilterIndices[0];
            polygon.Filter = section.Filters[filterIndex];

            short materialIndex = section.PrimitiveMaterialIndices[0];
            polygon.Material = section.Materials[materialIndex];

            return true;
        }

        internal bool GetNextPolygon(uint previousMeshKey, out uint meshKey, ref PolygonCollider polygon)
        {
            int primitiveKey = (int)previousMeshKey >> 1;
            int polygonIndex = (int)previousMeshKey & 1;

            int sectionIndex = primitiveKey >> 8;
            int sectionPrimitiveIndex = primitiveKey & 0xFF;

            // Get next primitive
            {
                ref Section section = ref Sections[sectionIndex];

                if (polygonIndex == 0 && IsPrimitveFlagSet(section.PrimitiveFlags[sectionPrimitiveIndex], PrimitiveFlags.IsTrianglePair))
                {
                    // Move to next triangle
                    polygonIndex = 1;
                }
                else
                {
                    // Move to next primitive
                    polygonIndex = 0;

                    if (++sectionPrimitiveIndex == section.PrimitiveFlags.Length)
                    {
                        // Move to next geometry section
                        sectionPrimitiveIndex = 0;
                        sectionIndex++;
                    }
                }

                if (sectionIndex >= Sections.Length)
                {
                    // We ran out of keys.
                    meshKey = 0xffffffff;
                    return false;
                }
            }

            // Return its key and polygon
            {
                meshKey = (uint)(sectionIndex << 9 | sectionPrimitiveIndex << 1 | polygonIndex);

                ref Section section = ref Sections[sectionIndex];
                PrimitiveVertexIndices vertexIndices = section.PrimitiveVertexIndices[sectionPrimitiveIndex];
                var vertices = new float3x4(
                    section.Vertices[vertexIndices.A],
                    section.Vertices[vertexIndices.B],
                    section.Vertices[vertexIndices.C],
                    section.Vertices[vertexIndices.D]);

                PrimitiveFlags flags = section.PrimitiveFlags[sectionPrimitiveIndex];
                if (IsPrimitveFlagSet(flags, PrimitiveFlags.IsQuad))
                {
                    polygon.SetAsQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
                }
                else
                {
                    polygon.SetAsTriangle(vertices[0], vertices[1 + polygonIndex], vertices[2 + polygonIndex]);
                }

                short filterIndex = section.PrimitiveFilterIndices[sectionPrimitiveIndex];
                polygon.Filter = section.Filters[filterIndex];
                
                short materialIndex = section.PrimitiveMaterialIndices[sectionPrimitiveIndex];
                polygon.Material = section.Materials[materialIndex];

                return true;
            }
        }

        #region Construction helpers

        // Calculate the number of bytes needed to store the given mesh data, excluding the header (sizeof(Mesh))
        internal static int CalculateMeshDataSize(int nodeCount, List<MeshBuilder.TempSection> tempSections)
        {
            int totalSize = 0;

            foreach (MeshBuilder.TempSection section in tempSections)
            {
                int numPrimitives = section.Primitives.Count;
                totalSize += Math.NextMultipleOf(numPrimitives * UnsafeUtility.SizeOf<PrimitiveFlags>(), 4);
                totalSize += Math.NextMultipleOf(numPrimitives * UnsafeUtility.SizeOf<PrimitiveVertexIndices>(), 4);
                totalSize += Math.NextMultipleOf(section.Vertices.Count * UnsafeUtility.SizeOf<float3>(), 4);
                totalSize += Math.NextMultipleOf(numPrimitives * sizeof(short), 4);
                totalSize += Math.NextMultipleOf(1 * UnsafeUtility.SizeOf<CollisionFilter>(), 4);
                totalSize += Math.NextMultipleOf(numPrimitives * sizeof(short), 4);
                totalSize += Math.NextMultipleOf(1 * UnsafeUtility.SizeOf<Material>(), 4);
            }

            int sectionBlobArraySize = Math.NextMultipleOf(tempSections.Count * UnsafeUtility.SizeOf<Section>(), 16);

            int treeSize = nodeCount * UnsafeUtility.SizeOf<BoundingVolumeHierarchy.Node>();

            return totalSize + sectionBlobArraySize + treeSize;
        }

        // Initialize the data. Assumes the appropriate memory has already been allocated.
        internal unsafe void Init(BoundingVolumeHierarchy.Node* nodes, int nodeCount, List<MeshBuilder.TempSection> tempSections, CollisionFilter filter, Material material)
        {
            byte* end = (byte*)UnsafeUtility.AddressOf(ref this) + sizeof(Mesh);
            end = (byte*)Math.NextMultipleOf16((ulong)end);   

            m_BvhNodesBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref m_BvhNodesBlob)));
            m_BvhNodesBlob.Length = nodeCount;
            int sizeOfNodes = sizeof(BoundingVolumeHierarchy.Node) * nodeCount;
            UnsafeUtility.MemCpy(end, nodes, sizeOfNodes);
            end += sizeOfNodes;

            Section* section = (Section*)end;

            m_SectionsBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref m_SectionsBlob)));
            m_SectionsBlob.Length = tempSections.Count;

            end += Math.NextMultipleOf(sizeof(Section) * tempSections.Count, 16);

            for (int sectionIndex = 0; sectionIndex < tempSections.Count; sectionIndex++)
            {
                MeshBuilder.TempSection tempSection = tempSections[sectionIndex];

                section->PrimitiveFlagsBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->PrimitiveFlagsBlob)));
                section->PrimitiveFlagsBlob.Length = tempSection.PrimitivesFlags.Count;
                end += Math.NextMultipleOf(section->PrimitiveFlagsBlob.Length * sizeof(PrimitiveFlags), 4);

                section->PrimitiveVertexIndicesBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->PrimitiveVertexIndicesBlob)));
                section->PrimitiveVertexIndicesBlob.Length = tempSection.Primitives.Count;
                end += Math.NextMultipleOf(section->PrimitiveVertexIndicesBlob.Length * sizeof(PrimitiveVertexIndices), 4);

                section->VerticesBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->VerticesBlob)));
                section->VerticesBlob.Length = tempSection.Vertices.Count;
                end += Math.NextMultipleOf(section->VerticesBlob.Length * sizeof(float3), 4);

                for (int i = 0; i < tempSection.PrimitivesFlags.Count; i++)
                {
                    Sections[sectionIndex].PrimitiveFlags[i] = tempSection.PrimitivesFlags[i];
                    Sections[sectionIndex].PrimitiveVertexIndices[i] = tempSection.Primitives[i];
                }

                for (int i = 0; i < tempSection.Vertices.Count; i++)
                {
                    Sections[sectionIndex].Vertices[i] = tempSection.Vertices[i];
                }

                // Filters

                section->PrimitiveFilterIndicesBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->PrimitiveFilterIndicesBlob)));
                section->PrimitiveFilterIndicesBlob.Length = tempSection.Primitives.Count;
                end += Math.NextMultipleOf(section->PrimitiveFilterIndicesBlob.Length * sizeof(short), 4);

                section->FiltersBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->FiltersBlob)));
                section->FiltersBlob.Length = 1;
                end += Math.NextMultipleOf(section->FiltersBlob.Length * sizeof(CollisionFilter), 4);

                Sections[sectionIndex].Filters[0] = filter;
                for (int i = 0; i < tempSection.Primitives.Count; i++)
                {
                    Sections[sectionIndex].PrimitiveFilterIndices[i] = 0;
                }

                // Materials

                section->PrimitiveMaterialIndicesBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->PrimitiveMaterialIndicesBlob)));
                section->PrimitiveMaterialIndicesBlob.Length = tempSection.Primitives.Count;
                end += Math.NextMultipleOf(section->PrimitiveMaterialIndicesBlob.Length * sizeof(short), 4);

                section->MaterialsBlob.Offset = (int)(end - (byte*)(UnsafeUtility.AddressOf(ref section->MaterialsBlob)));
                section->MaterialsBlob.Length = 1;
                end += Math.NextMultipleOf(section->FiltersBlob.Length * sizeof(Material), 4);

                Sections[sectionIndex].Materials[0] = material;
                for (int i = 0; i < tempSection.Primitives.Count; i++)
                {
                    Sections[sectionIndex].PrimitiveMaterialIndices[i] = 0;
                }

                section++;
            }
        }

        #endregion
    }
}
