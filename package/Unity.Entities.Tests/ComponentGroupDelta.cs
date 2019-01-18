﻿using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    [TestFixture]
    class ComponentGroupDelta : ECSTestsFixture
    {
        private static Entity[] nothing = {};
        // * TODO: using out of date version cached ComponentDataArray should give exception... (We store the System order version in it...)
        // * TODO: Using monobehaviour as delta inputs?
        // * TODO: Self-delta-mutation doesn't trigger update (ComponentDataFromEntity.cs)
        // /////@TODO: GlobalSystemVersion can't be pulled from m_Entities... indeterministic
        // * TODO: Chained delta works
        // How can this work? Need to use specialized job type because the number of entities to be
        // processed isn't known until running the job... Need some kind of late binding of parallel for length etc...
        // How do we prevent incorrect usage / default...

        [DisableAutoCreation]
        public class DeltaCheckSystem : ComponentSystem
        {
            public Entity[] Expected;

            protected override void OnUpdate()
            {
                var group = GetComponentGroup(typeof(EcsTestData));
                group.SetFilterChanged(typeof(EcsTestData));

                var actualEntityArray = group.GetEntityArray().ToArray();
                var systemVersion = GlobalSystemVersion;
                var lastSystemVersion = LastSystemVersion;

                CollectionAssert.AreEqual(Expected, actualEntityArray);
            }

            public void UpdateExpectedResults(Entity[] expected)
            {
                Expected = expected;
                Update();
            }
        }


        [Test]
        public void CreateEntityTriggersChange()
        {
            Entity[] entity = new Entity[] { m_Manager.CreateEntity(typeof(EcsTestData)) };
            var deltaCheckSystem = World.CreateManager<DeltaCheckSystem>();
            deltaCheckSystem.UpdateExpectedResults(entity);
        }

        public enum ChangeMode
        {
            SetComponentData,
            SetComponentDataFromEntity,
            ComponentDataArray,
            ComponentGroupArray
        }

#pragma warning disable 649
        unsafe struct GroupRW
        {
            public EcsTestData* Data;
        }

        unsafe struct GroupRO
        {
            [ReadOnly]
            public EcsTestData* Data;
        }
#pragma warning restore 649

        // Running SetValue should change the chunk version for the data it's writing to.
        unsafe void SetValue(int index, int value, ChangeMode mode)
        {
            EmptySystem.Update();
            var entityArray = EmptySystem.GetComponentGroup(typeof(EcsTestData)).GetEntityArray();
            var entity = entityArray[index];

            if (mode == ChangeMode.ComponentDataArray)
            {
                var array = EmptySystem.GetComponentGroup(typeof(EcsTestData)).GetComponentDataArray<EcsTestData>();
                array[index] = new EcsTestData(value);
            }
            else if (mode == ChangeMode.SetComponentData)
            {
                m_Manager.SetComponentData(entity, new EcsTestData(value));
            }
            else if (mode == ChangeMode.SetComponentDataFromEntity)
            {
                //@TODO: Chaining correctness... Definitely not implemented right now...
                var array = EmptySystem.GetComponentDataFromEntity<EcsTestData>(false);
                array[entity] = new EcsTestData(value);
            }
            else if (mode == ChangeMode.ComponentGroupArray)
            {
                *(EmptySystem.GetEntities<GroupRW>()[index].Data) = new EcsTestData(value);
            }
        }

        // Running GetValue should not trigger any changes to chunk version.
        void GetValue(ChangeMode mode)
        {
            EmptySystem.Update();
            var entityArray = EmptySystem.GetComponentGroup(typeof(EcsTestData)).GetEntityArray();

            if (mode == ChangeMode.ComponentDataArray)
            {
                var array = EmptySystem.GetComponentGroup(typeof(EcsTestData)).GetComponentDataArray<EcsTestData>();
                for (int i = 0; i != array.Length; i++)
                {
                    var val = array[i];
                }
            }
            else if (mode == ChangeMode.SetComponentData)
            {
                for(int i = 0;i != entityArray.Length;i++)
                    m_Manager.GetComponentData<EcsTestData>(entityArray[i]);
            }
            else if (mode == ChangeMode.SetComponentDataFromEntity)
            {
                for(int i = 0;i != entityArray.Length;i++)
                    m_Manager.GetComponentData<EcsTestData>(entityArray[i]);
            }
            else if (mode == ChangeMode.ComponentGroupArray)
            {
                foreach (var e in EmptySystem.GetEntities<GroupRO>())
                    ;
            }
        }

        [Test]
        public void ChangeEntity([Values]ChangeMode mode)
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var deltaCheckSystem0 = World.CreateManager<DeltaCheckSystem>();
            var deltaCheckSystem1 = World.CreateManager<DeltaCheckSystem>();

            // Chunk versions are considered changed upon creation and until after they're first updated.
            deltaCheckSystem0.UpdateExpectedResults(new Entity[] { entity0, entity1 });

            // First update of chunks. 
            SetValue(0, 2, mode);
            SetValue(1, 2, mode);
            deltaCheckSystem0.UpdateExpectedResults(new Entity[] { entity0, entity1 });

            // Now that everything has been updated, the change filter won't trigger until we explicitly change something.
            deltaCheckSystem0.UpdateExpectedResults(nothing);

            // Change entity0's chunk.
            SetValue(0, 3, mode);
            deltaCheckSystem0.UpdateExpectedResults(new Entity[] { entity0 });

            // Change entity1's chunk.
            SetValue(1, 3, mode);
            deltaCheckSystem0.UpdateExpectedResults(new Entity[] { entity1 });

            // Already did the initial changes to these chunks in another system, so a change in this context is based on the system's change version.
            deltaCheckSystem1.UpdateExpectedResults(new Entity[] { entity0, entity1 });

            deltaCheckSystem0.UpdateExpectedResults(nothing);
            deltaCheckSystem1.UpdateExpectedResults(nothing);
        }

        [Test]
        public void GetEntityDataDoesNotChange([Values]ChangeMode mode)
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            var deltaCheckSystem = World.CreateManager<DeltaCheckSystem>();

            // First update of chunks after creation. 
            SetValue(0, 2, mode);
            SetValue(1, 2, mode);
            deltaCheckSystem.UpdateExpectedResults(new Entity[] { entity0, entity1 });
            deltaCheckSystem.UpdateExpectedResults(nothing);

            // Now ensure that GetValue does not trigger a change on the ComponentGroup.
            GetValue(mode);
            deltaCheckSystem.UpdateExpectedResults(nothing);
        }

        [Test]
        public void ChangeEntityWrap()
        {
           m_Manager.Debug.SetGlobalSystemVersion(uint.MaxValue-3);

            var entity = m_Manager.CreateEntity(typeof(EcsTestData));

            var deltaCheckSystem = World.CreateManager<DeltaCheckSystem>();

            for (int i = 0; i != 7; i++)
            {
                SetValue(0, 1, ChangeMode.SetComponentData);
                deltaCheckSystem.UpdateExpectedResults(new Entity[] { entity });
            }

            deltaCheckSystem.UpdateExpectedResults(nothing);
        }

        [Test]
        public void NoChangeEntityWrap()
        {
            m_Manager.Debug.SetGlobalSystemVersion(uint.MaxValue - 3);

            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            SetValue(0, 2, ChangeMode.SetComponentData);

            var deltaCheckSystem = World.CreateManager<DeltaCheckSystem>();
            deltaCheckSystem.UpdateExpectedResults(new Entity[] { entity });

            for (int i = 0; i != 7; i++)
                deltaCheckSystem.UpdateExpectedResults(nothing);
        }

        [DisableAutoCreation]
        public class DeltaProcessComponentSystem : JobComponentSystem
        {
            struct DeltaJob : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute([ChangedFilter][ReadOnly]ref EcsTestData input, ref EcsTestData2 output)
                {
                    output.value0 += input.value + 100;
                }
            }

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                return new DeltaJob().Schedule(this, deps);
            }
        }


        [Test]
        public void IJobProcessComponentDeltaWorks()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var deltaSystem = World.CreateManager<DeltaProcessComponentSystem>();

            // First update of chunks after creation. 
            SetValue(0, -100, ChangeMode.SetComponentData);
            SetValue(1, -100, ChangeMode.SetComponentData);
            deltaSystem.Update();
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity0).value0);
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity1).value0);

            // Change entity0's chunk.
            SetValue(0, 2, ChangeMode.SetComponentData);

            // Test [ChangedFilter] for real now.
            deltaSystem.Update();

            // Only entity0 should have changed.
            Assert.AreEqual(100 + 2, m_Manager.GetComponentData<EcsTestData2>(entity0).value0);

            // entity1.value0 should be unchanged from 0.
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity1).value0);
        }


        [DisableAutoCreation]
        public class DeltaProcessComponentSystemUsingRun : ComponentSystem
        {
            struct DeltaJob : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute([ChangedFilter][ReadOnly]ref EcsTestData input, ref EcsTestData2 output)
                {
                    output.value0 += input.value + 100;
                }
            }

            protected override void OnUpdate()
            {
               new DeltaJob().Run(this);
            }
        }

        [Test]
        public void IJobProcessComponentDeltaWorksWhenUsingRun()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var deltaSystem = World.CreateManager<DeltaProcessComponentSystemUsingRun>();

            // First update of chunks after creation.
            SetValue(0, -100, ChangeMode.SetComponentData);
            SetValue(1, -100, ChangeMode.SetComponentData);

            deltaSystem.Update();

            // Test [ChangedFilter] for real now.
            SetValue(0, 2, ChangeMode.SetComponentData);

            deltaSystem.Update();

            // Only entity0 should have changed.
            Assert.AreEqual(100 + 2, m_Manager.GetComponentData<EcsTestData2>(entity0).value0);

            // entity1.value0 should be unchanged from 0.
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity1).value0);
        }


#if false
        [Test]
        public void IJobProcessComponentDeltaWorksWhenSetSharedComponent()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestSharedComp));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var deltaSystem = World.CreateManager<DeltaProcessComponentSystem>();

            SetValue(0, 2, ChangeMode.SetComponentData);
            m_Manager.SetSharedComponentData(entity0,new EcsTestSharedComp(50));

            deltaSystem.Update();

            Assert.AreEqual(100 + 2, m_Manager.GetComponentData<EcsTestData2>(entity0).value0);
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity1).value0);
        }
#endif

        [DisableAutoCreation]
        public class ModifyComponentSystem1Comp : JobComponentSystem
        {
            public ComponentGroup m_Group;
            public EcsTestSharedComp m_sharedComp;

            struct DeltaJob : IJobParallelFor
            {
                public ComponentDataArray<EcsTestData> data;

                public void Execute(int index)
                {
                    data[index] = new EcsTestData(100);
                }
            }

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                m_Group = GetComponentGroup(
                    typeof(EcsTestData),
                    ComponentType.ReadOnly(typeof(EcsTestSharedComp)));

                m_Group.SetFilter(m_sharedComp);

                DeltaJob job = new DeltaJob();
                job.data = m_Group.GetComponentDataArray<EcsTestData>();
                return job.Schedule(job.data.Length, 1, deps);
            }
        }

        [DisableAutoCreation]
        public class DeltaModifyComponentSystem1Comp : JobComponentSystem
        {
            struct DeltaJobFirstRunAfterCreation : IJobProcessComponentData<EcsTestData>
            {
                public void Execute([ChangedFilter]ref EcsTestData output)
                {
                    output.value = 0;
                }
            }
            struct DeltaJob : IJobProcessComponentData<EcsTestData>
            {
                public void Execute([ChangedFilter]ref EcsTestData output)
                {
                    output.value += 150;
                }
            }

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                if (LastSystemVersion == 0)
                {
                    return new DeltaJobFirstRunAfterCreation().Schedule(this, deps);
                }
                return new DeltaJob().Schedule(this, deps);
            }
        }

        [Test]
        public void ChangedFilterJobAfterAnotherJob1Comp()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestSharedComp));
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);

            var modifSystem = World.CreateManager<ModifyComponentSystem1Comp>();
            var deltaSystem = World.CreateManager<DeltaModifyComponentSystem1Comp>();

            // First update of chunks after creation.
            modifSystem.Update();
            deltaSystem.Update();

            modifSystem.m_sharedComp = new EcsTestSharedComp(456);
            for (int i = 123; i < entities.Length; i += 345)
            {
                m_Manager.SetSharedComponentData(entities[i], modifSystem.m_sharedComp);
            }

            modifSystem.Update();
            deltaSystem.Update();

            foreach (var entity in entities)
            {
                if (m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value == 456)
                {
                    Assert.AreEqual(250, m_Manager.GetComponentData<EcsTestData>(entity).value);
                }
                else
                {
                    Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);
                }
            }

            entities.Dispose();
        }

        [DisableAutoCreation]
        public class ModifyComponentSystem2Comp : JobComponentSystem
        {
            public ComponentGroup m_Group;
            public EcsTestSharedComp m_sharedComp;

            struct DeltaJob : IJobParallelFor
            {
                public ComponentDataArray<EcsTestData> data;
                public ComponentDataArray<EcsTestData2> data2;

                public void Execute(int index)
                {
                    data[index] = new EcsTestData(100);
                    data2[index] = new EcsTestData2(102);
                }
            }

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                m_Group = GetComponentGroup(
                    typeof(EcsTestData),
                    typeof(EcsTestData2),
                    ComponentType.ReadOnly(typeof(EcsTestSharedComp)));

                m_Group.SetFilter(m_sharedComp);

                DeltaJob job = new DeltaJob();
                job.data = m_Group.GetComponentDataArray<EcsTestData>();
                job.data2 = m_Group.GetComponentDataArray<EcsTestData2>();
                return job.Schedule(job.data.Length, 1, deps);
            }
        }

        [DisableAutoCreation]
        public class DeltaModifyComponentSystem2Comp : JobComponentSystem
        {
            struct DeltaJobFirstRunAfterCreation : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute(ref EcsTestData output, ref EcsTestData2 output2)
                {
                    output.value = 0;
                    output2.value0 = 0;
                }
            }

            struct DeltaJobChanged0 : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute([ChangedFilter]ref EcsTestData output, ref EcsTestData2 output2)
                {
                    output.value += 150;
                    output2.value0 += 152;
                }
            }

            struct DeltaJobChanged1 : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute(ref EcsTestData output, [ChangedFilter]ref EcsTestData2 output2)
                {
                    output.value += 150;
                    output2.value0 += 152;
                }
            }

            public enum Variant
            {
                FirstComponentChanged,
                SecondComponentChanged,
            }

            public Variant variant;

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                if(LastSystemVersion == 0)
                {
                    return new DeltaJobFirstRunAfterCreation().Schedule(this, deps);
                }
                switch (variant)
                {
                    case Variant.FirstComponentChanged:
                        return new DeltaJobChanged0().Schedule(this, deps);
                    case Variant.SecondComponentChanged:
                        return new DeltaJobChanged1().Schedule(this, deps);
                }

                throw new NotImplementedException();
            }
        }

        [Test]
        public void ChangedFilterJobAfterAnotherJob2Comp([Values]DeltaModifyComponentSystem2Comp.Variant variant)
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestSharedComp));
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);

            // All entities have just been created, so they're all technically "changed".
            var modifSystem = World.CreateManager<ModifyComponentSystem2Comp>();
            var deltaSystem = World.CreateManager<DeltaModifyComponentSystem2Comp>();

            // First update of chunks after creation.
            modifSystem.Update();
            deltaSystem.Update();

            deltaSystem.variant = variant;

            modifSystem.m_sharedComp = new EcsTestSharedComp(456);
            for (int i = 123; i < entities.Length; i += 345)
            {
                m_Manager.SetSharedComponentData(entities[i], modifSystem.m_sharedComp);
            }

            modifSystem.Update();
            deltaSystem.Update();

            foreach (var entity in entities)
            {
                if (m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value == 456)
                {
                    Assert.AreEqual(250, m_Manager.GetComponentData<EcsTestData>(entity).value);
                    Assert.AreEqual(254, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
                }
                else
                {
                    Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);
                }
            }

            entities.Dispose();
        }

        [DisableAutoCreation]
        public class ModifyComponentSystem3Comp : JobComponentSystem
        {
            public ComponentGroup m_Group;
            public EcsTestSharedComp m_sharedComp;

            struct DeltaJob : IJobParallelFor
            {
                public ComponentDataArray<EcsTestData> data;
                public ComponentDataArray<EcsTestData2> data2;
                public ComponentDataArray<EcsTestData3> data3;

                public void Execute(int index)
                {
                    data[index] = new EcsTestData(100);
                    data2[index] = new EcsTestData2(102);
                    data3[index] = new EcsTestData3(103);
                }
            }

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                m_Group = GetComponentGroup(
                    typeof(EcsTestData),
                    typeof(EcsTestData2),
                    typeof(EcsTestData3),
                    ComponentType.ReadOnly(typeof(EcsTestSharedComp)));

                m_Group.SetFilter(m_sharedComp);

                DeltaJob job = new DeltaJob();
                job.data = m_Group.GetComponentDataArray<EcsTestData>();
                job.data2 = m_Group.GetComponentDataArray<EcsTestData2>();
                job.data3 = m_Group.GetComponentDataArray<EcsTestData3>();
                return job.Schedule(job.data.Length, 1, deps);
            }
        }

        [DisableAutoCreation]
        public class DeltaModifyComponentSystem3Comp : JobComponentSystem
        {
            struct DeltaJobChanged0 : IJobProcessComponentData<EcsTestData, EcsTestData2, EcsTestData3>
            {
                public void Execute([ChangedFilter]ref EcsTestData output, ref EcsTestData2 output2, ref EcsTestData3 output3)
                {
                    output.value += 150;
                    output2.value0 += 152;
                    output3.value0 += 153;
                }
            }

            struct DeltaJobChanged1 : IJobProcessComponentData<EcsTestData, EcsTestData2, EcsTestData3>
            {
                public void Execute(ref EcsTestData output, [ChangedFilter]ref EcsTestData2 output2, ref EcsTestData3 output3)
                {
                    output.value += 150;
                    output2.value0 += 152;
                    output3.value0 += 153;
                }
            }

            struct DeltaJobChanged2 : IJobProcessComponentData<EcsTestData, EcsTestData2, EcsTestData3>
            {
                public void Execute(ref EcsTestData output, ref EcsTestData2 output2, [ChangedFilter]ref EcsTestData3 output3)
                {
                    output.value += 150;
                    output2.value0 += 152;
                    output3.value0 += 153;
                }
            }

            public enum Variant
            {
                FirstComponentChanged,
                SecondComponentChanged,
                ThirdComponentChanged,
            }

            public Variant variant;

            protected override JobHandle OnUpdate(JobHandle deps)
            {
                switch (variant)
                {
                    case Variant.FirstComponentChanged:
                        return new DeltaJobChanged0().Schedule(this, deps);
                    case Variant.SecondComponentChanged:
                        return new DeltaJobChanged1().Schedule(this, deps);
                    case Variant.ThirdComponentChanged:
                        return new DeltaJobChanged2().Schedule(this, deps);
                }

                throw new NotImplementedException();
            }
        }

        public void ChangedFilterJobAfterAnotherJob3Comp([Values]DeltaModifyComponentSystem3Comp.Variant variant)
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestSharedComp));
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);

            var modifSystem = World.CreateManager<ModifyComponentSystem3Comp>();
            var deltaSystem = World.CreateManager<DeltaModifyComponentSystem3Comp>();

            deltaSystem.variant = variant;

            modifSystem.m_sharedComp = new EcsTestSharedComp(456);
            for (int i = 123; i < entities.Length; i += 345)
            {
                m_Manager.SetSharedComponentData(entities[i], modifSystem.m_sharedComp);
            }

            modifSystem.Update();
            deltaSystem.Update();

            foreach (var entity in entities)
            {
                if (m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value == 456)
                {
                    Assert.AreEqual(250, m_Manager.GetComponentData<EcsTestData>(entity).value);
                    Assert.AreEqual(254, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
                    Assert.AreEqual(256, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
                }
                else
                {
                    Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);
                }
            }

            entities.Dispose();
        }
    }
}
