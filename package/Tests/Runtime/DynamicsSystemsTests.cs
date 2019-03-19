using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using UnityEngine.TestTools;

namespace Unity.AI.Planner.Tests
{
    [TestFixture]
    class DynamicsSystemsTests : KeyDomainTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            World.GetOrCreateManager<CounterDynamicsSystem>();
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World);

            var allEntities = m_EntityManager.GetAllEntities();
            foreach (var entity in allEntities)
                if (m_EntityManager.HasComponent<State>(entity)
                    || m_EntityManager.HasComponent<CarrierTrait>(entity))
                    m_EntityManager.AddComponentData(entity, new Counter());
            allEntities.Dispose();
        }

        struct Counter : IComponentData
        {
            public int Count;
        }

        [UpdateAfter(typeof(ActionSystemGroup))]
        [UpdateBefore(typeof(KeyDomainUpdateSystem))]
        [DisableAutoCreation]
        class CounterDynamicsSystem : ComponentSystem
        {
            ComponentGroup m_CreatedStateInfo;

            protected override void OnCreateManager()
            {
                base.OnCreateManager();

                m_CreatedStateInfo = GetComponentGroup(ComponentType.ReadOnly<State>(),
                    ComponentType.ReadOnly<CreatedStateInfo>());
            }

            protected override void OnUpdate()
            {
                var counterLookup = GetComponentDataFromEntity<Counter>();

                var createdStateEntities = m_CreatedStateInfo.GetEntityArray();
                for (var i = 0; i < createdStateEntities.Length; i++)
                {
                    var createdStateEntity = createdStateEntities[i];
                    var counter = counterLookup[createdStateEntity];
                    counter.Count++;
                    counterLookup[createdStateEntity] = counter;

                    var objectBuffer = EntityManager.GetBuffer<DomainObjectReference>(createdStateEntity);
                    for (var o = 0; o < objectBuffer.Length; o++)
                    {
                        var domainObjectEntity = objectBuffer[o].DomainObjectEntity;
                        if (EntityManager.HasComponent(domainObjectEntity, typeof(Counter)))
                        {
                            counter = counterLookup[domainObjectEntity];
                            counter.Count++;
                            counterLookup[domainObjectEntity] = counter;
                        }
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator StateGetsUpdatesFromDynamicsSystem()
        {
            yield return PrewarmSystems();

            var policyGraphNodeEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var agents = new List<(Entity, int)>();

            var actionSystem = PickupKeyAction.SystemGuid;

            const int testIterations = 2;
            for (var i = 0; i < testIterations; i++)
            {
                var (resultingPGNEntity, resultingStateEntity) =
                    GetResultsFromAction(m_EntityManager, policyGraphNodeEntity, actionSystem);

                Assert.True(resultingPGNEntity != default);
                Assert.True(resultingStateEntity != default);

                m_EntityManager.AddComponent(resultingPGNEntity, typeof(Selected));
                policyGraphNodeEntity = resultingPGNEntity;
                actionSystem = MoveAction.SystemGuid;

                // Only the state and the agent (w/ CarrierTrait) should have the ActionCount trait
                int expectedCount = i + 1;
                Assert.AreEqual(expectedCount,
                    m_EntityManager.GetComponentData<Counter>(resultingStateEntity).Count);

                m_PickupKeyAction.FilterObjects(resultingStateEntity, (m_PickupKeyAction.AgentTypes, agents));
                Assert.AreEqual(1, agents.Count);

                var counter = m_EntityManager.GetComponentData<Counter>(agents[0].Item1);
                Assert.AreEqual(expectedCount, counter.Count);

                yield return UpdateSystems();
            }
        }
    }
}
