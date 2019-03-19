using NUnit.Framework;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.AI.Planner.Tests
{
    [DisableAutoCreation]
    class EmptySystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle dep) { return dep; }


        new public ComponentGroup GetComponentGroup(params EntityArchetypeQuery[] queries)
        {
            return base.GetComponentGroup(queries);
        }

        new public ComponentGroup GetComponentGroup(params ComponentType[] componentTypes)
        {
            return base.GetComponentGroup(componentTypes);
        }
#if !UNITY_ZEROPLAYER
        new public ComponentGroupArray<T> GetEntities<T>() where T : struct
        {
            return base.GetEntities<T>();
        }
#endif
    }

    class ECSTestsFixture
    {
        protected World m_PreviousWorld;
        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected int StressTestEntityCount = 1000;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.Active;
            World = World.Active = new World("Test World");

            m_Manager = World.GetOrCreateManager<EntityManager>();
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

#if !UNITY_2019_2_OR_NEWER
            // Not raising exceptions can easily bring unity down with massive logging when tests fail.
            // From Unity 2019.2 on this field is always implicitly true and therefore removed.

            UnityEngine.Assertions.Assert.raiseExceptions = true;
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (m_Manager != null)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                var system = World.GetExistingManager<ComponentSystemBase>();
                while (system != null)
                {
                    system.Enabled = false;
                    World.DestroyManager(system);
                    system = World.GetExistingManager<ComponentSystemBase>();
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;

                World.Active = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = null;
            }
        }

        public EmptySystem EmptySystem
        {
            get
            {
                return World.Active.GetOrCreateManager<EmptySystem>();
            }
        }
    }
}
