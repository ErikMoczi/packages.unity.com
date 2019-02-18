﻿using System;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Properties.Serialization;
using UnityEngine;
using Unity.PerformanceTesting;
using Unity.Entities;

namespace Unity.Entities.PerformanceTests
{
    [TestFixture]
    [Category("Performance")]
    public sealed class EntityCommandBufferPerformanceTests
    {
        private World m_PreviousWorld;
        private World m_World;
        private EntityManager m_Manager;

        [SetUp]
        public void Setup()
        {
            m_PreviousWorld = World.Active;
            m_World = World.Active = new World("Test World");
            m_Manager = m_World.GetOrCreateManager<EntityManager>();
        }

        public struct EcsTestData : IComponentData
        {
            public int value;
        }

        private void FillWithEcsTestData(EntityCommandBuffer cmds, int repeat)
        {
            for (int i = repeat; i != 0; --i)
            {
                var e = cmds.CreateEntity();
                cmds.AddComponent(e, new EcsTestData {value = i});
            }
        }

        [PerformanceTest]
        public void EntityCommandBuffer_512SimpleEntities()
        {
            const int kCreateLoopCount = 512;
            const int kPlaybackLoopCount = 1000;


            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            Measure.Method(
                () =>
                {
                    for (int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                    {
                        cmds.Dispose();
                        cmds = new EntityCommandBuffer(Allocator.TempJob);
                        FillWithEcsTestData(cmds, kCreateLoopCount);
                    }
                })
                .Definition("CreateEntities")
                .Run();

            Measure.Method(
                    () =>
                    {
                        for(int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                            cmds.Playback(m_Manager);
                    })
                .Definition("Playback")
                .CleanUp(() =>
                {
                })
                .Run();

            cmds.Dispose();

        }

        struct EcsTestDataWithEntity : IComponentData
        {
            public int value;
            public Entity entity;
        }

        private void FillWithEcsTestDataWithEntity(EntityCommandBuffer cmds, int repeat)
        {
            for (int i = repeat; i != 0; --i)
            {
                var e = cmds.CreateEntity();
                cmds.AddComponent(e, new EcsTestDataWithEntity {value = i});
            }
        }

        [PerformanceTest]
        public void EntityCommandBuffer_512EntitiesWithEmbeddedEntity()
        {
            const int kCreateLoopCount = 512;
            const int kPlaybackLoopCount = 1000;


            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            Measure.Method(
                    () =>
                    {
                        for (int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                        {
                            cmds.Dispose();
                            cmds = new EntityCommandBuffer(Allocator.TempJob);
                            FillWithEcsTestDataWithEntity(cmds, kCreateLoopCount);
                        }
                    })
                .Definition("CreateEntities")
                .Run();

            Measure.Method(
                () =>
                {
                    for (int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                        cmds.Playback(m_Manager);
                })
                .Definition("Playback")
                .Run();
            cmds.Dispose();
        }

        [PerformanceTest]
        public void EntityCommandBuffer_OneEntityWithEmbeddedEntityAnd512SimpleEntities()
        {
            // This test should not be any slower than EntityCommandBuffer_SimpleEntities_512x1000
            // It shows that adding one component that needs fix up will not make the fast
            // path any slower

            const int kCreateLoopCount = 512;
            const int kPlaybackLoopCount = 1000;


            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            Measure.Method(
                    () =>
                    {
                        for (int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                        {
                            cmds.Dispose();
                            cmds = new EntityCommandBuffer(Allocator.TempJob);
                            Entity e0 = cmds.CreateEntity();
                            cmds.AddComponent(e0, new EcsTestDataWithEntity {value = -1, entity = e0 });
                            FillWithEcsTestData(cmds, kCreateLoopCount);
                        }
                    })
                .Definition("CreateEntities")
                .Run();
            Measure.Method(
                    () =>
                    {
                        for (int repeat = kPlaybackLoopCount; repeat != 0; --repeat)
                            cmds.Playback(m_Manager);
                    })
                .Definition("Playback")
                .Run();
            cmds.Dispose();
        }
    }
}

