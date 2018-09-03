﻿using System;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Entities.Tests
{
    [TestFixture]
    public class TransformUpdateTests : ECSTestsFixture
    {
        struct Parent : IComponentData
        {
            public Entity Value;
        }

        struct Position : IComponentData
        {
            public float3 Value;
        }

        struct Rotation : IComponentData
        {
            public quaternion Value;
        }

        struct Scale : IComponentData
        {
            public float3 Value;
        }

        public struct CustomLocalToWorld : IComponentData
        {
            public float4x4 Value;
        }

        struct Static : IComponentData
        {
        }

        //
        // Managed by system:
        //

        public struct PreviousParent : ISystemStateComponentData
        {
            public Entity Value;
        }

        public struct ChangedVersion : ISystemStateComponentData
        {
            public uint Value;
        }
        
        struct Frozen : ISystemStateComponentData
        {
        }

        public struct Depth : ISystemStateSharedComponentData
        {
            public int Value;
        }

        public struct LocalToWorld : ISystemStateComponentData
        {
            public float4x4 Value;
        }

        public struct LocalToParent : ISystemStateComponentData
        {
            public float4x4 Value;
        }

        // [ComponentSystemPatch]
        [DisableAutoCreation]
        public class TransformPatch : JobComponentSystem
        {
            private uint LastSystemVersion = 0;
            private int LastParentVersion = 0;

            private int LastPositionVersion = 0;
            private int LastRotationVersion = 0;
            private int LastHeadingVersion = 0;
            private int LastScaleVersion = 0;
            private int LastUniformScaleVersion = 0;

            private NativeMultiHashMap<Entity, Entity> ParentToChildTree;

            public TransformPatch()
            {
                ParentToChildTree = new NativeMultiHashMap<Entity, Entity>(1024, Allocator.Persistent);
            }

            public bool IsChildTree(Entity entity)
            {
                NativeMultiHashMapIterator<Entity> it;
                Entity foundChild;
                return ParentToChildTree.TryGetFirstValue(entity, out foundChild, out it);
            }

            public void AddChildTree(Entity parentEntity, Entity childEntity)
            {
                Debug.Log(
                    $"AddChildTree {parentEntity.Index}.{parentEntity.Version} -> {childEntity.Index}.{childEntity.Version}");
                ParentToChildTree.Add(parentEntity, childEntity);
            }

            private void RemoveChildTree(Entity parentEntity, Entity childEntity)
            {
                Debug.Log(
                    $"RemoveChildTree {parentEntity.Index}.{parentEntity.Version} -> {childEntity.Index}.{childEntity.Version}");
                NativeMultiHashMapIterator<Entity> it;
                Entity foundChild;
                if (!ParentToChildTree.TryGetFirstValue(parentEntity, out foundChild, out it))
                {
                    return;
                }

                do
                {
                    if (foundChild == childEntity)
                    {
                        ParentToChildTree.Remove(it);
                        return;
                    }
                } while (ParentToChildTree.TryGetNextValue(out foundChild, ref it));

                throw new System.InvalidOperationException(string.Format("Parent not found in Hierarchy hashmap"));
            }
            
            private int ChildTreeCount(Entity parentEntity)
            {
                NativeMultiHashMapIterator<Entity> it;
                Entity foundChild;
                if (!ParentToChildTree.TryGetFirstValue(parentEntity, out foundChild, out it))
                {
                    return 0;
                }

                int count = 0;

                do
                {
                    count++;
                } while (ParentToChildTree.TryGetNextValue(out foundChild, ref it));

                return count;
            }

            public void UpdateNewRootTransforms(EntityCommandBuffer entityCommandBuffer)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen), typeof(Parent), typeof(LocalToWorld), typeof(ChangedVersion), typeof(Depth)}, // none
                    Array.Empty<ComponentType>(),
                    Allocator.Temp);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkEntities = chunk.GetNativeArray(entityType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];

                        entityCommandBuffer.AddComponent(entity, new LocalToWorld {Value = float4x4.identity});
                    }
                }

                chunks.Dispose();
            }

            public void UpdateNewChildTransforms(EntityCommandBuffer entityCommandBuffer)
            {
                var parentVersion = EntityManager.GetComponentOrderVersion<Parent>();
                Debug.Log(string.Format("parentVersion = {0}", parentVersion));
                Debug.Log(string.Format("LastParentVersion = {0}", LastParentVersion));
                if (parentVersion == LastParentVersion)
                    return;

                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen), typeof(LocalToParent), typeof(LocalToWorld), typeof(PreviousParent)}, // none
                    new ComponentType[] {typeof(Parent)}, // all
                    Allocator.Temp);

                Debug.Log(string.Format("New Child Chunks = {0}", chunks.Length));

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                Debug.Log(string.Format("New Child Transforms = {0}",
                    ArchetypeChunkArray.CalculateEntityCount(chunks)));

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);
                var parentType = EntityManager.GetArchetypeChunkComponentType<Parent>(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var parents = chunk.GetNativeArray(parentType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];
                        var parentEntity = parents[i].Value;

                        entityCommandBuffer.AddComponent(entity, new LocalToWorld {Value = float4x4.identity});
                        entityCommandBuffer.AddComponent(entity, new PreviousParent {Value = parentEntity});
                        entityCommandBuffer.AddComponent(entity, new LocalToParent {Value = float4x4.identity});

                        if (!IsChildTree(parentEntity))
                        {
                            entityCommandBuffer.AddComponent(parentEntity,
                                new ChangedVersion {Value = GlobalSystemVersion});
                            entityCommandBuffer.AddSharedComponent(parentEntity, new Depth {Value = 0});
                            Debug.Log(string.Format("Add Depth to {0}.{1}", parentEntity.Index, parentEntity.Version));
                        }
                        else
                        {
                            entityCommandBuffer.SetComponent(parentEntity,
                                new ChangedVersion {Value = GlobalSystemVersion});
                        }

                        AddChildTree(parentEntity, entity);
                    }
                }

                chunks.Dispose();
            }

            public void UpdateChangedParents(EntityCommandBuffer entityCommandBuffer)
            {
                var parentVersion = EntityManager.GetComponentOrderVersion<Parent>();
                if (parentVersion == LastParentVersion)
                    return;

                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen)}, // none
                    new ComponentType[] {typeof(Parent), typeof(PreviousParent)}, // all
                    Allocator.Temp);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);
                var parentType = EntityManager.GetArchetypeChunkComponentType<Parent>(true);
                var previousParentType = EntityManager.GetArchetypeChunkComponentType<PreviousParent>(false);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(parentType), LastSystemVersion);

                    // AnyChanged
                    // if (!parentChanged)
                    //  continue;

                    var parentCount = chunk.Count;
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var parents = chunk.GetNativeArray(parentType);
                    var previousParents = chunk.GetNativeArray(previousParentType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];
                        var parentEntity = parents[i].Value;
                        var previousParentEntity = previousParents[i].Value;

                        if (parentEntity == previousParentEntity)
                            continue;

                        if (IsChildTree(previousParentEntity))
                        {
                            RemoveChildTree(previousParentEntity, entity);
                            if (!IsChildTree(previousParentEntity))
                            {
                                entityCommandBuffer.RemoveComponent<ChangedVersion>(previousParentEntity);
                                entityCommandBuffer.RemoveComponent<Depth>(previousParentEntity);
                                Debug.Log(string.Format("1. Remove Depth to {0}.{1}", previousParentEntity.Index,
                                    previousParentEntity.Version));
                            }
                        }
                        else
                        {
                            Debug.Log(string.Format("Previous parent not in tree {0}.{1}", previousParentEntity.Index,
                                previousParentEntity.Version));
                        }

                        if (!IsChildTree(parentEntity))
                        {
                            entityCommandBuffer.AddComponent(parentEntity,
                                new ChangedVersion {Value = GlobalSystemVersion});
                            entityCommandBuffer.AddSharedComponent(parentEntity, new Depth {Value = 0});
                            Debug.Log(string.Format("Add Depth to {0}.{1}", parentEntity.Index,
                                previousParentEntity.Version));
                        }
                        else
                        {
                            entityCommandBuffer.SetComponent(parentEntity,
                                new ChangedVersion {Value = GlobalSystemVersion});
                        }

                        AddChildTree(parentEntity, entity);
                        previousParents[i] = new PreviousParent {Value = parentEntity};
                    }
                }

                chunks.Dispose();
            }

            public void UpdateRemovedParents(EntityCommandBuffer entityCommandBuffer)
            {
                var parentVersion = EntityManager.GetComponentOrderVersion<Parent>();
                if (parentVersion == LastParentVersion)
                    return;

                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen), typeof(Parent)}, // none
                    new ComponentType[] {typeof(PreviousParent), typeof(LocalToParent)}, // all
                    Allocator.Temp);

                Debug.Log($"RemoveParent count {ArchetypeChunkArray.CalculateEntityCount(chunks)}");

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);
                var previousParentType = EntityManager.GetArchetypeChunkComponentType<PreviousParent>(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];

                    var parentCount = chunk.Count;
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var previousParents = chunk.GetNativeArray(previousParentType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];
                        var previousParentEntity = previousParents[i].Value;

                        if (IsChildTree(previousParentEntity))
                        {
                            RemoveChildTree(previousParentEntity, entity);

                            if (!IsChildTree(previousParentEntity))
                            {
                                entityCommandBuffer.RemoveComponent<ChangedVersion>(previousParentEntity);
                                entityCommandBuffer.RemoveComponent<Depth>(previousParentEntity);
                                Debug.Log(string.Format("2. Remove Depth to {0}.{1}", previousParentEntity.Index,
                                    previousParentEntity.Version));
                            }
                        }

                        entityCommandBuffer.RemoveComponent<LocalToParent>(entity);
                        entityCommandBuffer.RemoveComponent<PreviousParent>(entity);
                        Debug.Log(string.Format("2. Remove PreviousParent {0}.{1} to {2}.{3} remain={4}", previousParentEntity.Index, previousParentEntity.Version, entity.Index, entity.Version,ChildTreeCount(previousParentEntity)));
                    }
                }

                chunks.Dispose();
            }

            public void CleanupChangedChildTree(Entity parentEntity, Entity entity, int depth,
                EntityCommandBuffer entityCommandBuffer)
            {
                if (!IsChildTree(entity))
                {
                    return;
                }

                entityCommandBuffer.SetSharedComponent(entity, new Depth {Value = depth});
                Debug.Log(string.Format("1. Set Depth to {0}.{1} = {2}", entity.Index, entity.Version, depth));
                entityCommandBuffer.SetComponent(entity, new ChangedVersion {Value = GlobalSystemVersion});

                // Update child versions (dirty)
                NativeMultiHashMapIterator<Entity> it;
                Entity child;

                if (!ParentToChildTree.TryGetFirstValue(entity, out child, out it))
                {
                    throw new System.InvalidOperationException(string.Format("Internal Error: Invalid Hierarchy tree"));
                }

                do
                {
                    CleanupChangedChildTree(entity, child, depth + 1, entityCommandBuffer);
                } while (ParentToChildTree.TryGetNextValue(out child, ref it));
            }

            public void CleanupUnchangedChildTree(Entity parentEntity, Entity entity, int depth,
                EntityCommandBuffer entityCommandBuffer, ComponentDataFromEntity<ChangedVersion> changedVersions)
            {
                if (!IsChildTree(entity))
                {
                    return;
                }

                var changedVersion = changedVersions[entity].Value;
                var parentChanged = ChangeVersionUtility.DidChange(changedVersion, LastSystemVersion);

                if (!parentChanged)
                {
                    // Update child versions (dirty)
                    NativeMultiHashMapIterator<Entity> it;
                    Entity child;

                    if (!ParentToChildTree.TryGetFirstValue(entity, out child, out it))
                    {
                        throw new System.InvalidOperationException(
                            string.Format("Internal Error: Invalid Hierarchy tree"));
                    }

                    do
                    {
                        CleanupUnchangedChildTree(entity, child, 1, entityCommandBuffer, changedVersions);
                    } while (ParentToChildTree.TryGetNextValue(out child, ref it));
                }
                else
                {
                    entityCommandBuffer.SetSharedComponent(entity, new Depth {Value = 0});
                    Debug.Log(string.Format("2. Set Depth to {0}.{1} = 0", entity.Index, entity.Version));

                    // Update child versions (dirty)
                    NativeMultiHashMapIterator<Entity> it;
                    Entity child;

                    if (!ParentToChildTree.TryGetFirstValue(entity, out child, out it))
                    {
                        throw new System.InvalidOperationException(
                            string.Format("Internal Error: Invalid Hierarchy tree"));
                    }

                    do
                    {
                        CleanupChangedChildTree(entity, child, 1, entityCommandBuffer);
                    } while (ParentToChildTree.TryGetNextValue(out child, ref it));
                }
            }

            public void CleanupParentToChildTree(ComponentDataFromEntity<ChangedVersion> changedVersions)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen), typeof(Parent)}, // none
                    new ComponentType[] {typeof(ChangedVersion), typeof(Depth)}, // all
                    Allocator.Temp);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
                var entityType = EntityManager.GetArchetypeChunkEntityType(true);
                var changedVersionType = EntityManager.GetArchetypeChunkComponentType<ChangedVersion>(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var chunkChangedVersions = chunk.GetNativeArray(changedVersionType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];

                        if (!IsChildTree(entity))
                        {
                            continue;
                        }

                        var changedVersion = chunkChangedVersions[i].Value;
                        var parentChanged = ChangeVersionUtility.DidChange(changedVersion, LastSystemVersion);

                        if (!parentChanged)
                        {
                            // Update child versions (dirty)
                            NativeMultiHashMapIterator<Entity> it;
                            Entity child;

                            if (!ParentToChildTree.TryGetFirstValue(entity, out child, out it))
                            {
                                throw new System.InvalidOperationException(
                                    string.Format("Internal Error: Invalid Hierarchy tree"));
                            }

                            do
                            {
                                CleanupUnchangedChildTree(entity, child, 1, entityCommandBuffer, changedVersions);
                            } while (ParentToChildTree.TryGetNextValue(out child, ref it));
                        }
                        else
                        {
                            entityCommandBuffer.SetSharedComponent(entity, new Depth {Value = 0});
                            Debug.Log(string.Format("2. Set Depth to {0}.{1} = 0", entity.Index, entity.Version));

                            // Update child versions (dirty)
                            NativeMultiHashMapIterator<Entity> it;
                            Entity child;

                            if (!ParentToChildTree.TryGetFirstValue(entity, out child, out it))
                            {
                                throw new System.InvalidOperationException(
                                    string.Format("Internal Error: Invalid Hierarchy tree"));
                            }

                            do
                            {
                                CleanupChangedChildTree(entity, child, 1, entityCommandBuffer);
                            } while (ParentToChildTree.TryGetNextValue(out child, ref it));
                        }
                    }
                }

                chunks.Dispose();
                entityCommandBuffer.Playback(EntityManager);
                entityCommandBuffer.Dispose();
            }

            public void UpdateRemovedTransforms(EntityCommandBuffer entityCommandBuffer)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen), typeof(Rotation), typeof(Position), typeof(Scale)}, // none
                    new ComponentType[] {typeof(LocalToWorld)}, // all
                    Allocator.Temp);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkEntities = chunk.GetNativeArray(entityType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];

                        entityCommandBuffer.RemoveComponent<LocalToWorld>(entity);
                    }
                }

                chunks.Dispose();
            }

            public void UpdateFrozen()
            {
                EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
                
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen)}, // none
                    new ComponentType[] {typeof(LocalToWorld), typeof(Static)}, // all
                    Allocator.Temp);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return;
                }

                var entityType = EntityManager.GetArchetypeChunkEntityType(true);

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkEntities = chunk.GetNativeArray(entityType);

                    for (int i = 0; i < parentCount; i++)
                    {
                        var entity = chunkEntities[i];

                        entityCommandBuffer.AddComponent<Frozen>(entity, new Frozen());
                    }
                }

                chunks.Dispose();
                
                entityCommandBuffer.Playback(EntityManager);
                entityCommandBuffer.Dispose();
            }

            public void UpdateDAG()
            {
                Debug.Log("TransformPatch UpdateDAG");

                EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

                UpdateNewRootTransforms(entityCommandBuffer);
                UpdateNewChildTransforms(entityCommandBuffer);
                UpdateChangedParents(entityCommandBuffer);
                UpdateRemovedParents(entityCommandBuffer);
                UpdateRemovedTransforms(entityCommandBuffer);

                entityCommandBuffer.Playback(EntityManager);
                entityCommandBuffer.Dispose();
            }

            public void UpdateChanged()
            {
                var changedVersions = m_EntityManager.GetComponentDataFromEntity<ChangedVersion>();
                CleanupParentToChildTree(changedVersions);
            }

            // [BurstCompile(CompileSynchronously = true)]
            struct RootsLocalToWorld : IJobParallelFor
            {
                [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
                [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
                [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
                [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
                public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
                public uint lastSystemVersion;

                public void Execute(int chunkIndex)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkRotations = chunk.GetNativeArray(rotationType);
                    var chunkPositions = chunk.GetNativeArray(positionType);
                    var chunkScales = chunk.GetNativeArray(scaleType);
                    var chunkLocalToWorlds = chunk.GetNativeArray(localToWorldType);

                    var chunkRotationsChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                    var chunkPositionsChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                    var chunkScalesChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                    var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                    // if (!chunkAnyChanged)
                    //  continue;

                    var chunkRotationsExist = chunkRotations.Length > 0;
                    var chunkPositionsExist = chunkPositions.Length > 0;
                    var chunkScalesExist = chunkScales.Length > 0;

                    // 001
                    if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 010
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.float4x4(chunkRotations[i].Value, new float3())
                            };
                        }
                    }
                    // 011
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, new float3()),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 100
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = float4x4.translate(chunkPositions[i].Value)
                            };
                        }
                    }
                    // 101
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 110
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                            };
                        }
                    }
                    // 111
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToWorlds[i] = new LocalToWorld
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    } 
                }
            }

            public JobHandle UpdateRootsLocalToWorld(JobHandle inputDeps)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen), typeof(Parent)}, // none
                    new ComponentType[] {typeof(LocalToWorld)}, // all
                    Allocator.TempJob);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return inputDeps;
                }

                var rootsLocalToWorldJob = new RootsLocalToWorld
                {
                    chunks = chunks,
                    rotationType = EntityManager.GetArchetypeChunkComponentType<Rotation>(true),
                    positionType = EntityManager.GetArchetypeChunkComponentType<Position>(true),
                    scaleType = EntityManager.GetArchetypeChunkComponentType<Scale>(true),
                    localToWorldType = EntityManager.GetArchetypeChunkComponentType<LocalToWorld>(false),
                    lastSystemVersion = LastSystemVersion,
                };
                var rootsLocalToWorldJobHandle = rootsLocalToWorldJob.Schedule(chunks.Length,64,inputDeps);
                return rootsLocalToWorldJobHandle;
            }

            // [BurstCompile(CompileSynchronously = true)]
            struct InnerTreeLocalToParent : IJobParallelFor
            {
                [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
                [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
                [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
                [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
                public ArchetypeChunkComponentType<LocalToParent> localToParentType;
                public ArchetypeChunkComponentType<ChangedVersion> changedVersionType;
                public uint lastSystemVersion;

                public void Execute(int chunkIndex)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkRotations = chunk.GetNativeArray(rotationType);
                    var chunkPositions = chunk.GetNativeArray(positionType);
                    var chunkScales = chunk.GetNativeArray(scaleType);
                    var chunkLocalToParents = chunk.GetNativeArray(localToParentType);
                    var chunkChangedVersions = chunk.GetNativeArray(changedVersionType);

                    var chunkRotationsChanged = ChangeVersionUtility.DidChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                    var chunkPositionsChanged = ChangeVersionUtility.DidChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                    var chunkScalesChanged = ChangeVersionUtility.DidChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                    var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                    // if (!chunkAnyChanged)
                    //  continue;

                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkChangedVersions[i] = new ChangedVersion
                        {
                            Value = lastSystemVersion
                        };
                    }

                    var chunkRotationsExist = chunkRotations.Length > 0;
                    var chunkPositionsExist = chunkPositions.Length > 0;
                    var chunkScalesExist = chunkScales.Length > 0;

                    // 001
                    if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 010
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.float4x4(chunkRotations[i].Value, new float3())
                            };
                        }
                    }
                    // 011
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, new float3()),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 100
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = float4x4.translate(chunkPositions[i].Value)
                            };
                        }
                    }
                    // 101
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 110
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                            };
                        }
                    }
                    // 111
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                }
            }

            public JobHandle UpdateInnerTreeLocalToParent(JobHandle inputDeps)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen)}, // none
                    new ComponentType[] {typeof(LocalToParent), typeof(Parent), typeof(ChangedVersion)}, // all
                    Allocator.TempJob);
                
                Debug.Log($"UpdateInnerTreeLocalToParent {ArchetypeChunkArray.CalculateEntityCount(chunks)}");

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return inputDeps;
                }

                var innerTreeLocalToParentJob = new InnerTreeLocalToParent
                {
                    chunks = chunks,
                    rotationType = EntityManager.GetArchetypeChunkComponentType<Rotation>(true),
                    positionType = EntityManager.GetArchetypeChunkComponentType<Position>(true),
                    scaleType = EntityManager.GetArchetypeChunkComponentType<Scale>(true),
                    localToParentType = EntityManager.GetArchetypeChunkComponentType<LocalToParent>(false),
                    changedVersionType = EntityManager.GetArchetypeChunkComponentType<ChangedVersion>(false),
                    lastSystemVersion = LastSystemVersion
                };
                var innerTreeLocalToParentJobHandle = innerTreeLocalToParentJob.Schedule(chunks.Length, 64, inputDeps);
                return innerTreeLocalToParentJobHandle;
            }
            
            // [BurstCompile(CompileSynchronously = true)]
            struct LeafLocalToParent : IJobParallelFor
            {
                [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
                [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
                [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
                [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
                public ArchetypeChunkComponentType<LocalToParent> localToParentType;
                public uint lastSystemVersion;

                public void Execute(int chunkIndex)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkRotations = chunk.GetNativeArray(rotationType);
                    var chunkPositions = chunk.GetNativeArray(positionType);
                    var chunkScales = chunk.GetNativeArray(scaleType);
                    var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                    var chunkRotationsChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                    var chunkPositionsChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                    var chunkScalesChanged =
                        ChangeVersionUtility.DidChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                    var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                    // if (!chunkAnyChanged)
                    //  continue;

                    var chunkRotationsExist = chunkRotations.Length > 0;
                    var chunkPositionsExist = chunkPositions.Length > 0;
                    var chunkScalesExist = chunkScales.Length > 0;

                    // 001
                    if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 010
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.float4x4(chunkRotations[i].Value, new float3())
                            };
                        }
                    }
                    // 011
                    else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, new float3()),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 100
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = float4x4.translate(chunkPositions[i].Value)
                            };
                        }
                    }
                    // 101
                    else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                    // 110
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                            };
                        }
                    }
                    // 111
                    else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                    {
                        for (int i = 0; i < parentCount; i++)
                        {
                            chunkLocalToParents[i] = new LocalToParent
                            {
                                Value = math.mul(math.float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                    float4x4.scale(chunkScales[i].Value))
                            };
                        }
                    }
                }
            }

            public JobHandle UpdateLeafLocalToParent(JobHandle inputDeps)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen), typeof(ChangedVersion)}, // none
                    new ComponentType[] {typeof(LocalToParent), typeof(Parent)}, // all
                    Allocator.TempJob);

                Debug.Log($"UpdateLeafLocalToParent {ArchetypeChunkArray.CalculateEntityCount(chunks)}");
                
                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return inputDeps;
                }

                var leafToLocalParentJob = new LeafLocalToParent
                {
                    chunks = chunks,
                    rotationType = EntityManager.GetArchetypeChunkComponentType<Rotation>(true),
                    positionType = EntityManager.GetArchetypeChunkComponentType<Position>(true),
                    scaleType = EntityManager.GetArchetypeChunkComponentType<Scale>(true),
                    localToParentType = EntityManager.GetArchetypeChunkComponentType<LocalToParent>(false),
                    lastSystemVersion = LastSystemVersion
                };
                var leafToLocalParentJobHandle = leafToLocalParentJob.Schedule(chunks.Length,64,inputDeps);
                return leafToLocalParentJobHandle;
            }
            
            // [BurstCompile(CompileSynchronously = true)]
            struct InnerTreeLocalToWorld : IJobParallelFor
            {
                [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> chunkIndices;
                [NativeDisableParallelForRestriction] [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
                [ReadOnly] public ArchetypeChunkComponentType<Parent> parentType;
                [ReadOnly] public ArchetypeChunkEntityType entityType;
                [ReadOnly] public ArchetypeChunkComponentType<LocalToParent> localToParentType;
                [ReadOnly] public ComponentDataFromEntity<ChangedVersion> changedVersionFromEntity;
                [NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
                public uint lastSystemVersion;

                public void Execute(int i)
                {
                    var chunkIndex = chunkIndices[i];
                    var chunk = chunks[chunkIndex];
                    var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                    var chunkParents = chunk.GetNativeArray(parentType);
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var previousParentEntity = Entity.Null;
                    var parentChanged = false;
                    var parentLocalToWorldMatrix = new float4x4();

                    for (int j = 0; j < chunk.Count; j++)
                    {
                        var parentEntity = chunkParents[j].Value;
                        var changed = false;
                        if (parentEntity != previousParentEntity)
                        {
                            if (changedVersionFromEntity.Exists(parentEntity))
                            {
                                parentChanged =
                                    ChangeVersionUtility.DidChange(changedVersionFromEntity[parentEntity].Value,
                                        lastSystemVersion);
                            }
                            else
                            {
                                parentChanged = true;
                            }

                            parentLocalToWorldMatrix = localToWorldFromEntity[parentEntity].Value;
                            previousParentEntity = parentEntity;
                        }

                        if (!parentChanged)
                        {
                            var localToParentChanged =
                                ChangeVersionUtility.DidChange(chunk.GetComponentVersion(localToParentType),
                                    lastSystemVersion);

                            // AnyChanged
                            // if (!localToParentChanged)
                            // continue;
                        }

                        var entity = chunkEntities[j];
                        localToWorldFromEntity[entity] = new LocalToWorld
                        {
                            Value = math.mul(parentLocalToWorldMatrix, chunkLocalToParents[j].Value)
                        };
                    }
                }
            }

            public JobHandle UpdateInnerTreeLocalToWorld(JobHandle inputDeps)
            {
                var sharedDepths = new List<Depth>();
                var sharedDepthIndices = new List<int>();

                var localToWorldFromEntity = EntityManager.GetComponentDataFromEntity<LocalToWorld>(false);
                var changedVersionFromEntity = EntityManager.GetComponentDataFromEntity<ChangedVersion>(true);
                var sharedComponentCount = EntityManager.GetSharedComponentCount();

                EntityManager.GetAllUniqueSharedComponentDatas(sharedDepths, sharedDepthIndices);

                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    new ComponentType[] {typeof(Frozen)}, // none
                    new ComponentType[] {typeof(Depth), typeof(LocalToParent), typeof(Parent), typeof(LocalToWorld)}, // all
                    Allocator.TempJob);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return inputDeps;
                }

                var depthCount = sharedDepths.Count;
                var depths = new NativeArray<int>(sharedComponentCount, Allocator.TempJob);
                var maxDepth = 0;

                for (int i = 0; i < depthCount; i++)
                {
                    var index = sharedDepthIndices[i];
                    var depth = sharedDepths[i].Value;
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                    }

                    depths[index] = depth;
                }

                var chunkIndices = new NativeArray<int>(chunks.Length, Allocator.TempJob);
                var depthType = new ArchetypeChunkSharedComponentType<Depth>();

                // Slow and dirty sort inner tree by depth
                {
                    var chunkIndex = 0;
                    // -1 = Depth has been removed, but still matching archetype for some reason. #todo
                    for (int depth = -1; depth < maxDepth; depth++)
                    {
                        for (int i = 0; i < chunks.Length; i++)
                        {
                            var chunk = chunks[i];
                            var chunkDepthSharedIndex = chunk.GetSharedComponentIndex(depthType);
                            var chunkDepth = -1;
                            if (chunkDepthSharedIndex != -1)
                            {
                                chunkDepth = depths[chunkDepthSharedIndex];
                            }

                            if (chunkDepth == depth)
                            {
                                chunkIndices[chunkIndex] = i;
                                chunkIndex++;
                            }
                        }
                    }
                }
                depths.Dispose();

                var innerTreeLocalToWorldJob = new InnerTreeLocalToWorld
                {
                    chunkIndices = chunkIndices,
                    chunks = chunks,
                    parentType = EntityManager.GetArchetypeChunkComponentType<Parent>(true),
                    entityType = EntityManager.GetArchetypeChunkEntityType(true),
                    localToParentType = EntityManager.GetArchetypeChunkComponentType<LocalToParent>(true),
                    changedVersionFromEntity = changedVersionFromEntity,
                    localToWorldFromEntity = localToWorldFromEntity,
                    lastSystemVersion = LastSystemVersion
                };
                var innerTreeLocalToWorldJobHandle = innerTreeLocalToWorldJob.Schedule(chunks.Length, 64, inputDeps);
                return innerTreeLocalToWorldJobHandle;
            }
            
            // [BurstCompile(CompileSynchronously = true)]
            struct LeafLocalToWorld : IJobParallelFor
            {
                [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
                [ReadOnly] public ArchetypeChunkEntityType entityType;
                [ReadOnly] public ArchetypeChunkComponentType<Parent> parentType;
                [ReadOnly] public ArchetypeChunkComponentType<LocalToParent> localToParentType;
                [ReadOnly] public ComponentDataFromEntity<ChangedVersion> changedVersionFromEntity;
                [NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
                public uint lastSystemVersion;

                public void Execute(int i)
                {
                    var chunk = chunks[i];
                    var chunkLocalToParents = chunk.GetNativeArray(localToParentType);
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var chunkParents = chunk.GetNativeArray(parentType);
                    var previousParentEntity = Entity.Null;
                    var parentChanged = false;
                    var parentLocalToWorldMatrix = new float4x4();

                    for (int j = 0; j < chunk.Count; j++)
                    {
                        var parentEntity = chunkParents[j].Value;
                        var changed = false;
                        if (parentEntity != previousParentEntity)
                        {
                            if (changedVersionFromEntity.Exists(parentEntity))
                            {
                                parentChanged =
                                    ChangeVersionUtility.DidChange(changedVersionFromEntity[parentEntity].Value,
                                        lastSystemVersion);
                            }
                            else
                            {
                                parentChanged = true;
                            }

                            parentLocalToWorldMatrix = localToWorldFromEntity[parentEntity].Value;
                            previousParentEntity = parentEntity;
                        }

                        if (!parentChanged)
                        {
                            var localToParentChanged =
                                ChangeVersionUtility.DidChange(chunk.GetComponentVersion(localToParentType),
                                    lastSystemVersion);

                            // AnyChanged
                            // if (!localToParentChanged)
                            //  continue;
                        }

                        var entity = chunkEntities[j];
                        localToWorldFromEntity[entity] = new LocalToWorld
                        {
                            Value = math.mul(parentLocalToWorldMatrix, chunkLocalToParents[j].Value)
                        };
                    }
                }
            }

            public JobHandle UpdateLeafLocalToWorld(JobHandle inputDeps)
            {
                var chunks = m_EntityManager.CreateArchetypeChunkArray(
                    new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, // any
                    new ComponentType[] {typeof(Frozen), typeof(ChangedVersion), typeof(Depth)}, // none
                    new ComponentType[] {typeof(LocalToParent), typeof(Parent)}, // all
                    Allocator.TempJob);

                if (chunks.Length == 0)
                {
                    chunks.Dispose();
                    return inputDeps;
                }

                var updateLeafLocalToWorldJob = new LeafLocalToWorld
                {
                    chunks = chunks,
                    entityType = EntityManager.GetArchetypeChunkEntityType(true),
                    parentType = EntityManager.GetArchetypeChunkComponentType<Parent>(true),
                    localToParentType = EntityManager.GetArchetypeChunkComponentType<LocalToParent>(true),
                    changedVersionFromEntity = EntityManager.GetComponentDataFromEntity<ChangedVersion>(true),
                    localToWorldFromEntity = EntityManager.GetComponentDataFromEntity<LocalToWorld>(false),
                    lastSystemVersion = LastSystemVersion
                };
                var updateLeafToWorldJobHandle = updateLeafLocalToWorldJob.Schedule(chunks.Length, 64, inputDeps);
                return updateLeafToWorldJobHandle;
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                // #todo When add new Parent, recalc local space
                // #todo Inner tree accurate change tracking if needed

                Debug.Log("TransformPatch Update");

                var positionVersion = EntityManager.GetComponentOrderVersion<Position>();
                var rotationVersion = EntityManager.GetComponentOrderVersion<Rotation>();
                var scaleVersion = EntityManager.GetComponentOrderVersion<Scale>();
                var parentTransformVersion = EntityManager.GetComponentOrderVersion<Parent>();

                var positionChange = positionVersion != LastPositionVersion;
                var rotationChange = rotationVersion != LastRotationVersion;
                var scaleChange = scaleVersion != LastScaleVersion;
                var parentTransformChange = parentTransformVersion != LastParentVersion;
                var possibleChange = positionChange || rotationChange || scaleChange || parentTransformChange;

                if (!possibleChange)
                    return inputDeps;

                UpdateFrozen();
                UpdateDAG();
                
                var updateRootsLocalToWorldJobHandle = UpdateRootsLocalToWorld(inputDeps);
                var updateInnerTreeLocalToParentJobHandle = UpdateInnerTreeLocalToParent(updateRootsLocalToWorldJobHandle);
                var updateLeafLocaltoParentJobHandle = UpdateLeafLocalToParent(updateInnerTreeLocalToParentJobHandle);
                updateLeafLocaltoParentJobHandle.Complete();
                
                UpdateChanged();
                
                var updateInnerTreeLocalToWorldJobHandle = UpdateInnerTreeLocalToWorld(inputDeps);
                var updateLeafLocalToWorldJobHandle = UpdateLeafLocalToWorld(updateInnerTreeLocalToWorldJobHandle);

                LastSystemVersion = GlobalSystemVersion;
                LastPositionVersion = positionVersion;
                LastRotationVersion = rotationVersion;
                LastScaleVersion = scaleVersion;
                LastParentVersion = parentTransformVersion;

                updateLeafLocalToWorldJobHandle.Complete();
                return updateLeafLocalToWorldJobHandle;
            }

            protected override void OnDestroyManager()
            {
                ParentToChildTree.Dispose();
            }
        }

        [DisableAutoCreation]
        public class TestTransformSetup : ComponentSystem
        {
            public NativeArray<Entity> AllEntities;
            public int UpdateStep;

            protected override void OnDestroyManager()
            {
                AllEntities.Dispose();
            }

            protected override void OnUpdate()
            {
                switch (UpdateStep)
                {
                    case 0:
                        Update0();
                        break;
                    case 1:
                        Update1();
                        break;
                    case 2:
                        Update2();
                        break;
                    case 3:
                        Update3();
                        break;
                }
            }

            public void UpdateCase(int step)
            {
                UpdateStep = step;
                Update();
            }

            void Update0()
            {
                int count = 32;

                AllEntities = new NativeArray<Entity>(count, Allocator.Persistent);

                AllEntities[0] = EntityManager.CreateEntity(
                    typeof(Position),
                    typeof(Rotation));

                var parentEntity = AllEntities[0];

                EntityManager.SetComponentData(AllEntities[0], new Position {Value = new float3()});
                EntityManager.SetComponentData(AllEntities[0], new Rotation {Value = quaternion.identity});

                for (int i = 1; i < count; i++)
                {
                    AllEntities[i] = EntityManager.CreateEntity(
                        typeof(Parent),
                        typeof(Position),
                        typeof(Rotation));
                    EntityManager.SetComponentData(AllEntities[i], new Parent {Value = parentEntity});
                    EntityManager.SetComponentData(AllEntities[i], new Rotation {Value = quaternion.identity});
                    EntityManager.SetComponentData(AllEntities[i], new Position {Value = new float3(0.0f, 0.0f, 1.0f)});
                    parentEntity = AllEntities[i];
                }
            }

            void Update1()
            {
                var count = AllEntities.Length;

                for (int i = 1; i < count; i++)
                {
                    EntityManager.SetComponentData(AllEntities[i], new Parent {Value = AllEntities[0]});
                }
            }

            void Update2()
            {
                var count = AllEntities.Length;

                for (int i = 0; i < count; i++)
                {
                    EntityManager.DestroyEntity(AllEntities[i]);
                }
            }

            void Update3()
            {
                EntityManager.SetComponentData(AllEntities[0],
                    new Rotation {Value = quaternion.axisAngle(new float3(0.0f, 1.0f, 0.0f), 3.14f)});
            }
        }

        private static void Dump(object outer, string info)
        {
            foreach (var finfo in outer.GetType().GetFields())
            {
                Console.WriteLine("{0}/{1}={2}", info, finfo.Name, finfo.GetValue(outer));

                if (finfo.FieldType.IsNested)
                {
                    Dump(finfo.GetValue(outer), finfo.Name);
                }
            }
        }

        public void DebugPrintComponents<T>()
            where T : struct, IComponentData
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(T)}, // all
                Allocator.Temp);

            var entityType = m_Manager.GetArchetypeChunkEntityType(true);
            var valueType = m_Manager.GetArchetypeChunkComponentType<T>(true);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkValues = chunk.GetNativeArray(valueType);
                for (int i = 0; i < chunkEntities.Length; i++)
                {
                    var entity = chunkEntities[i];

                    Debug.Log($"{i}: {typeof(T).FullName} = {entity.Index}.{entity.Version}");
                    Dump(chunkValues[i], String.Empty);
                }
            }

            chunks.Dispose();
        }

        public void DebugPrintSharedComponents<T>()
            where T : struct, ISharedComponentData
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(T)}, // all
                Allocator.Temp);

            var entityType = m_Manager.GetArchetypeChunkEntityType(true);
            var sharedType = m_Manager.GetArchetypeChunkSharedComponentType<T>(true);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkSharedIndex = chunk.GetSharedComponentIndex(sharedType);

                for (int i = 0; i < chunkEntities.Length; i++)
                {
                    var entity = chunkEntities[i];
                    Debug.Log($"{i}: {typeof(T).FullName} = {entity.Index}.{entity.Version} = {chunkSharedIndex}");
                }
            }

            chunks.Dispose();
        }


        // Capture reparenting changes to DAG
        
        [Test]
        public void TRA_CatchChangesToParent()
        {
            var testTransformSetup = World.CreateManager<TestTransformSetup>();
            var transformPatch = World.CreateManager<TransformPatch>();

            testTransformSetup.UpdateCase(0);
            transformPatch.Update();

            var rootEntity = testTransformSetup.AllEntities[0];
            var entityCount = testTransformSetup.AllEntities.Length;

            {
                var chunks = m_Manager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    Array.Empty<ComponentType>(), // none
                    new ComponentType[] {typeof(Depth)}, // all
                    Allocator.Temp);

                var entityType = m_Manager.GetArchetypeChunkEntityType(true);

                Assert.AreEqual(entityCount - 1, ArchetypeChunkArray.CalculateEntityCount(chunks));
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    for (int i = 0; i < chunkEntities.Length; i++)
                    {
                        var entity = chunkEntities[i];
                        Debug.Log(string.Format("{0}: Depth Entity = {1}.{2}", i, entity.Index, entity.Version));
                    }
                }

                chunks.Dispose();
            }

            testTransformSetup.UpdateCase(1);

            {
                var chunks = m_Manager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    Array.Empty<ComponentType>(), // none
                    new ComponentType[] {typeof(Depth)}, // all
                    Allocator.Temp);
                Assert.AreEqual(entityCount - 1, ArchetypeChunkArray.CalculateEntityCount(chunks));
                chunks.Dispose();
            }

            transformPatch.Update();

            {
                var chunks = m_Manager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    Array.Empty<ComponentType>(), // none
                    new ComponentType[] {typeof(Depth)}, // all
                    Allocator.Temp);
                Assert.AreEqual(1, ArchetypeChunkArray.CalculateEntityCount(chunks));
                chunks.Dispose();
            }

            {
                var chunks = m_Manager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    Array.Empty<ComponentType>(), // none
                    new ComponentType[] {typeof(Parent)}, // all
                    Allocator.Temp);

                var transformParentType = m_Manager.GetArchetypeChunkComponentType<Parent>(true);
                var entityType = m_Manager.GetArchetypeChunkEntityType(true);

                Debug.Log(string.Format("Parent Chunk Count = {0}", chunks.Length));
                Debug.Log(string.Format("Root = {0}.{1}", rootEntity.Index, rootEntity.Version));

                var rootChildCount = 0;
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var parentCount = chunk.Count;

                    var chunkParents = chunk.GetNativeArray(transformParentType);
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    for (int i = 0; i < parentCount; i++)
                    {
                        Debug.Log(string.Format("{0}: {1}.{2} Parent = {3}.{4}", i,
                            chunkEntities[i].Index,
                            chunkEntities[i].Version,
                            chunkParents[i].Value.Index,
                            chunkParents[i].Value.Version));
                        if (chunkParents[i].Value == rootEntity)
                        {
                            rootChildCount++;
                        }
                    }
                }

                Debug.Log(string.Format("Root Child Count = {0}", rootChildCount));

                chunks.Dispose();
                Assert.AreEqual(entityCount - 1, rootChildCount);
            }

            {
                var chunks = m_Manager.CreateArchetypeChunkArray(
                    Array.Empty<ComponentType>(), // any
                    Array.Empty<ComponentType>(), // none
                    new ComponentType[] {typeof(Parent), typeof(PreviousParent)}, // all
                    Allocator.Temp);

                Assert.AreEqual(entityCount - 1, ArchetypeChunkArray.CalculateEntityCount(chunks));

                chunks.Dispose();
            }

            testTransformSetup.UpdateCase(2);

            {
                for (int i = 0; i < entityCount; i++)
                {
                    Assert.True(m_Manager.Exists(testTransformSetup.AllEntities[i]));
                }
            }

            transformPatch.Update();

            // Print if anything is found (should be nothing.)
            DebugPrintComponents<LocalToWorld>();
            DebugPrintComponents<LocalToParent>();
            DebugPrintComponents<PreviousParent>();
            DebugPrintComponents<ChangedVersion>();
            DebugPrintComponents<Parent>();
            DebugPrintSharedComponents<Depth>();

            {
                for (int i = 0; i < entityCount; i++)
                {
                    Assert.False(m_Manager.Exists(testTransformSetup.AllEntities[i]));
                }
            }
        }

        public void DebugPrintLocalToWorld()
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(LocalToWorld)}, // all
                Allocator.Temp);

            var entityType = m_Manager.GetArchetypeChunkEntityType(true);
            var localToWorldType = m_Manager.GetArchetypeChunkComponentType<LocalToWorld>(true);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkLocalToWorlds = chunk.GetNativeArray(localToWorldType);

                for (int i = 0; i < parentCount; i++)
                {
                    Debug.Log(string.Format("{0}: LocalToWorld {1}.{2} =", i, chunkEntities[i].Index,
                        chunkEntities[i].Version));
                    Debug.Log(
                        $"    {chunkLocalToWorlds[i].Value.c0.x} {chunkLocalToWorlds[i].Value.c0.y} {chunkLocalToWorlds[i].Value.c0.z} {chunkLocalToWorlds[i].Value.c0.w}");
                    Debug.Log(
                        $"    {chunkLocalToWorlds[i].Value.c1.x} {chunkLocalToWorlds[i].Value.c1.y} {chunkLocalToWorlds[i].Value.c1.z} {chunkLocalToWorlds[i].Value.c1.w}");
                    Debug.Log(
                        $"    {chunkLocalToWorlds[i].Value.c2.x} {chunkLocalToWorlds[i].Value.c2.y} {chunkLocalToWorlds[i].Value.c2.z} {chunkLocalToWorlds[i].Value.c2.w}");
                    Debug.Log(
                        $"    {chunkLocalToWorlds[i].Value.c3.x} {chunkLocalToWorlds[i].Value.c3.y} {chunkLocalToWorlds[i].Value.c3.z} {chunkLocalToWorlds[i].Value.c3.w}");
                }
            }

            chunks.Dispose();
        }

        public void DebugPrintLocalToParent()
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(LocalToParent)}, // all
                Allocator.Temp);

            var entityType = m_Manager.GetArchetypeChunkEntityType(true);
            var localToParentType = m_Manager.GetArchetypeChunkComponentType<LocalToParent>(true);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                for (int i = 0; i < parentCount; i++)
                {
                    Debug.Log(string.Format("{0}: LocalToParent {1}.{2} =", i, chunkEntities[i].Index,
                        chunkEntities[i].Version));
                    Debug.Log(
                        $"    {chunkLocalToParents[i].Value.c0.x} {chunkLocalToParents[i].Value.c0.y} {chunkLocalToParents[i].Value.c0.z} {chunkLocalToParents[i].Value.c0.w}");
                    Debug.Log(
                        $"    {chunkLocalToParents[i].Value.c1.x} {chunkLocalToParents[i].Value.c1.y} {chunkLocalToParents[i].Value.c1.z} {chunkLocalToParents[i].Value.c1.w}");
                    Debug.Log(
                        $"    {chunkLocalToParents[i].Value.c2.x} {chunkLocalToParents[i].Value.c2.y} {chunkLocalToParents[i].Value.c2.z} {chunkLocalToParents[i].Value.c2.w}");
                    Debug.Log(
                        $"    {chunkLocalToParents[i].Value.c3.x} {chunkLocalToParents[i].Value.c3.y} {chunkLocalToParents[i].Value.c3.z} {chunkLocalToParents[i].Value.c3.w}");
                }
            }

            chunks.Dispose();
        }
        
        public void DebugPrintPosition()
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(Position)}, // all
                Allocator.Temp);

            var entityType = m_Manager.GetArchetypeChunkEntityType(true);
            var positionType = m_Manager.GetArchetypeChunkComponentType<Position>(true);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkPositions = chunk.GetNativeArray(positionType);

                for (int i = 0; i < parentCount; i++)
                {
                    Debug.Log(string.Format("{0}: Position {1}.{2} =", i, chunkEntities[i].Index,
                        chunkEntities[i].Version));
                    Debug.Log(
                        $"    {chunkPositions[i].Value.x} {chunkPositions[i].Value.y} {chunkPositions[i].Value.z}");
                }
            }

            chunks.Dispose();
        }

        [Test]
        public void TRA_RotateParent()
        {
            var testTransformSetup = World.CreateManager<TestTransformSetup>();
            var transformPatch = World.CreateManager<TransformPatch>();

            Debug.Log("UPDATE 0");
            testTransformSetup.UpdateCase(0);
            transformPatch.Update();

            // DebugPrintPosition();
            // DebugPrintLocalToParent();
            // DebugPrintLocalToWorld();

            Debug.Log("UPDATE 3");
            testTransformSetup.UpdateCase(3);
            transformPatch.Update();
            
            DebugPrintComponents<Parent>();
            DebugPrintLocalToWorld();
            DebugPrintSharedComponents<Depth>();
            
        }

        [Test]
        public void TRA_TestChunkDispose()
        {
            var testTransformSetup = World.CreateManager<TestTransformSetup>();
            var transformPatch = World.CreateManager<TransformPatch>();

            testTransformSetup.UpdateCase(0);
            transformPatch.Update();
            
            var chunks = m_Manager.CreateArchetypeChunkArray(
                Array.Empty<ComponentType>(), // any
                Array.Empty<ComponentType>(), // none
                new ComponentType[] {typeof(Position)}, // all
                Allocator.Temp);

            chunks.Dispose();
        }
    }
}
