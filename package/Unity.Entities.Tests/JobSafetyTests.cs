using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Entities.Tests
{
    [StandaloneFixme] // Tiny ignores because of the assert in the constructor.
    class JobSafetyTests : ECSTestsFixture
	{
        public JobSafetyTests()
        {
            Assert.IsTrue(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled, "JobDebugger must be enabled for these tests");
        }

        struct TestIncrementJob : IJob
        {
            public NativeArray<Entity> entities;
            public ComponentDataFromEntity<EcsTestData> data;
            public void Execute()
            {
                for (int i = 0; i != entities.Length; i++)
                {
                    var entity = entities[i];
                    
                    var d = data[entity];
                    d.value++;
                    data[entity] = d;
                }
            }
        }

        [Test]
        public void ComponentAccessAfterScheduledJobThrows()
        {
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
           
            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            
            Assert.AreEqual(42, job.data[job.entities[0]].value);

            var fence = job.Schedule();

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                var f = job.data[job.entities[0]].value;
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                f.GetHashCode();
            });

            fence.Complete();
            Assert.AreEqual(43, job.data[job.entities[0]].value);
            
            job.entities.Dispose();
        }

        [Test]
        public void GetComponentCompletesJob()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            group.AddDependency(job.Schedule());

            // Implicit Wait for job, returns value after job has completed.
            Assert.AreEqual(1, m_Manager.GetComponentData<EcsTestData>(entity).value);
            
            job.entities.Dispose();
        }

        [Test]
        public void DestroyEntityCompletesScheduledJobs()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            /*var entity2 =*/ m_Manager.CreateEntity(typeof(EcsTestData));
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            group.AddDependency(job.Schedule());

            m_Manager.DestroyEntity(entity);

            var componentData = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            
            // @TODO: This is maybe a little bit dodgy way of determining if the job has been completed...
            //        Probably should expose api to inspector job debugger state...
            Assert.AreEqual(1, componentData.Length);
            Assert.AreEqual(1, componentData[0].value);
            
            job.entities.Dispose();
            componentData.Dispose();
        }

        [Test]
        public void EntityManagerDestructionDetectsUnregisteredJob()
        {
            #if !UNITY_ZEROPLAYER
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("job is still running"));
            #endif

            /*var entity =*/ m_Manager.CreateEntity(typeof(EcsTestData));
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            job.Schedule();

            TearDown();
            
            job.entities.Dispose();
        }

        [Test]
        public void DestroyEntityDetectsUnregisteredJob()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            var fence = job.Schedule();

            Assert.Throws<System.InvalidOperationException>(() => { m_Manager.DestroyEntity(entity); });

            fence.Complete();
            job.entities.Dispose();
        }

        [Test]
        public void GetComponentDetectsUnregisteredJob()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            var job = new TestIncrementJob();
            job.entities = group.ToEntityArray(Allocator.TempJob);
            job.data = m_Manager.GetComponentDataFromEntity<EcsTestData>();
            var jobHandle = job.Schedule();

            Assert.Throws<System.InvalidOperationException>(() => { m_Manager.GetComponentData<EcsTestData>(entity); });

            jobHandle.Complete();
            
            job.entities.Dispose();
        }

	    [Test]
	    [Ignore("Should work, need to write test")]
	    public void TwoJobsAccessingEntityArrayCanRunInParallel()
	    {
	    }

        struct EntityOnlyDependencyJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType entityType;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
            }
        }

        struct NoDependenciesJob : IJobChunk
        {
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
            }
        }

        [DisableAutoCreation]
        class EntityOnlyDependencySystem : JobComponentSystem
        {
            public JobHandle JobHandle;
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                EntityManager.CreateEntity(typeof(EcsTestData));
                var group = GetComponentGroup(new ComponentType[]{});
                var job = new EntityOnlyDependencyJob
                {
                    entityType = m_EntityManager.GetArchetypeChunkEntityType()
                };
                JobHandle = job.Schedule(group, inputDeps);
                return JobHandle;
            }
        }

        [DisableAutoCreation]
        class NoComponentDependenciesSystem : JobComponentSystem
        {
            public JobHandle JobHandle;
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                EntityManager.CreateEntity(typeof(EcsTestData));
                var group = GetComponentGroup(new ComponentType[]{});
                var job = new NoDependenciesJob{};

                JobHandle = job.Schedule(group, inputDeps);
                return JobHandle;
            }
        }

        [DisableAutoCreation]
        class DestroyAllEntitiesSystem : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                var allEntities = EntityManager.GetAllEntities();
                EntityManager.DestroyEntity(allEntities);
                allEntities.Dispose();
                return inputDeps;
            }
        }

        [Test]
        public void StructuralChangeCompletesEntityOnlyDependencyJob()
        {
            var system = World.GetOrCreateManager<EntityOnlyDependencySystem>();
            system.Update();
            World.GetOrCreateManager<DestroyAllEntitiesSystem>().Update();
            Assert.IsTrue(JobHandle.CheckFenceIsDependencyOrDidSyncFence(system.JobHandle, new JobHandle()));
        }

        [Test]
        public void StructuralChangeCompletesNoComponentDependenciesJob()
        {
            var system = World.GetOrCreateManager<NoComponentDependenciesSystem>();
            system.Update();
            World.GetOrCreateManager<DestroyAllEntitiesSystem>().Update();
            Assert.IsTrue(JobHandle.CheckFenceIsDependencyOrDidSyncFence(system.JobHandle, new JobHandle()));
        }

        [Test]
        public void StructuralChangeAfterSchedulingNoDependenciesJobThrows()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);
            var group = EmptySystem.GetComponentGroup(typeof(EcsTestData));
            var handle = new NoDependenciesJob().Schedule(group);
            Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(entity));
            handle.Complete();
        }

        [Test]
        public void StructuralChangeAfterSchedulingEntityOnlyDependencyJobThrows()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);
            var group = EmptySystem.GetComponentGroup(typeof(EcsTestData));
            var handle = new EntityOnlyDependencyJob{entityType = m_Manager.GetArchetypeChunkEntityType()}.Schedule(group);
            Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(entity));
            handle.Complete();
        }

        [DisableAutoCreation]
        class SharedComponentSystem : JobComponentSystem
        {
            ComponentGroup group;
            protected override void OnCreateManager() {
                group = GetComponentGroup(ComponentType.ReadOnly<EcsTestSharedComp>());
            }
            struct SharedComponentJobChunk : IJobChunk
            {
                [ReadOnly] public ArchetypeChunkSharedComponentType<EcsTestSharedComp> ecsTestSharedCompType;
                public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
                {
                }
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                return new SharedComponentJobChunk
                {
                    ecsTestSharedCompType = GetArchetypeChunkSharedComponentType<EcsTestSharedComp>()
                }.Schedule(group, inputDeps);
            }
        }

        [Test]
        public void JobsUsingArchetypeChunkSharedComponentTypeSyncOnStructuralChange()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestSharedComp));
            var entity = m_Manager.CreateEntity(archetype);

            var sharedComponentSystem = World.CreateManager<SharedComponentSystem>();

            sharedComponentSystem.Update();
            // DestroyEntity should sync the job and not cause any safety error
            m_Manager.DestroyEntity(entity);
        }
    }
}
