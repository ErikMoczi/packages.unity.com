using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PGN = Unity.AI.Planner.PolicyGraphNode;

namespace Unity.AI.Planner.Tests
{
    class KeyDomainTestFixture : ECSTestsFixture
    {
        protected EntityManager m_EntityManager;

        protected PickupKeyAction m_PickupKeyAction;
        protected MoveAction m_MoveAction;
        protected UnlockRoomAction m_UnlockRoomAction;

        protected PolicyGraphContainer m_PolicyGraph;

        public override void Setup()
        {
            base.Setup();

            KeyDomain.Initialize(World);

            // It's necessary to create systems manually in edit mode
            m_EntityManager = World.GetExistingManager<EntityManager>();
            m_MoveAction = World.GetOrCreateManager<MoveAction>();
            m_PickupKeyAction = World.GetOrCreateManager<PickupKeyAction>();
            m_UnlockRoomAction = World.GetOrCreateManager<UnlockRoomAction>();

            // Establish components
            var plannerSystem = World.GetOrCreateManager<PlannerSystem>();
            var keyDomainUpdateSystem = World.GetOrCreateManager<KeyDomainUpdateSystem>();
            m_PolicyGraph = new PolicyGraphContainer(128, keyDomainUpdateSystem, World);

            // Hook up references
            plannerSystem.PolicyGraph = m_PolicyGraph;
            keyDomainUpdateSystem.PolicyGraph = m_PolicyGraph;
            m_PolicyGraph.Reset(KeyDomain.InitialStateEntity);

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World);
        }

        public override void TearDown()
        {
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);
            base.TearDown();
        }

        protected static (Entity, Entity) GetResultsFromAction(EntityManager entityManager, Entity policyGraphNodeEntity, Guid actionSystemGuid)
        {
            var actionNodeReferences = entityManager.GetBuffer<ActionNodeReference>(policyGraphNodeEntity);
            for (var i = 0; i < actionNodeReferences.Length; i++)
            {
                var actionNodeReference = actionNodeReferences[i];
                var actionNodeEntity = actionNodeReference.ActionNodeEntity;
                var actionNode = entityManager.GetComponentData<ActionNode>(actionNodeEntity);
                if (actionNode.ActionSystemGuid == actionSystemGuid)
                {
                    var actionResults = entityManager.GetBuffer<ActionResult>(actionNodeEntity);
                    var pgnEntity = actionResults[0].PolicyGraphNodeEntity;
                    var resultPGN = entityManager.GetComponentData<PolicyGraphNode>(pgnEntity);
                    return (pgnEntity, resultPGN.StateEntity);
                }
            }

            return (default, default);
        }

        protected static IEnumerator UpdateSystems()
        {
#if UNITY_EDITOR
            EditorApplication.QueuePlayerLoopUpdate();
#endif
            yield return null;
        }

        protected IEnumerator PrewarmSystems()
        {
            if (m_PolicyGraph.PolicyGraphRootEntity == default)
            {
                // Let a frame pass, so the first policy graph node gets created.
                yield return UpdateSystems();
            }

            // And then another frame, so the root entity gets updated and selected.
            yield return UpdateSystems();

            Assert.IsTrue(m_PolicyGraph.PolicyGraphRootEntity != default);
        }
    }

    [TestFixture]
    class KeyDomainTests : KeyDomainTestFixture
    {
        [UnityTest]
        public IEnumerator VariableBinding()
        {
            yield return PrewarmSystems();

            var rootEntity = m_PolicyGraph.PolicyGraphRootEntity;

            var node = m_EntityManager.GetComponentData<PGN>(rootEntity);
            var stateObjects = m_EntityManager.GetBuffer<DomainObjectReference>(node.StateEntity);
            var actionNodes = m_EntityManager.GetBuffer<ActionNodeReference>(rootEntity);

            // Test
            Assert.AreEqual(5, stateObjects.Length);
            Assert.AreEqual(2, actionNodes.Length);
        }

        [UnityTest]
        public IEnumerator KeyDomainTransitions()
        {
            yield return PrewarmSystems();

            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;

            // Step 1: Pickup Key. Result => Agent has White Key
            // Act
            var (resultingPGNEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager, policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Test
            Assert.True(resultingPGNEntity != default);
            Assert.True(resultingStateEntity != default);

            var keys = new List<(Entity, int)>();
            var agents = new List<(Entity, int)>();

            m_PickupKeyAction.FilterObjects(resultingStateEntity,
                (m_PickupKeyAction.KeyTypes, keys),
                (m_PickupKeyAction.AgentTypes, agents));
            Assert.AreEqual(1, agents.Count);
            var agentCarrier = m_EntityManager.GetComponentData<CarrierTrait>(agents[0].Item1);

            CarriableTrait whiteKeyCarriable = default;
            foreach (var key in keys)
            {
                var keyEntity = key.Item1;
                if (m_EntityManager.GetComponentData<DomainObjectTrait>(keyEntity).ID.Equals(KeyDomain.WhiteKey))
                    whiteKeyCarriable = m_EntityManager.GetComponentData<CarriableTrait>(keyEntity);
            }

            Assert.AreNotEqual(whiteKeyCarriable, default);

            Assert.True(agentCarrier.CarriedObject == KeyDomain.WhiteKey);
            Assert.True(whiteKeyCarriable.Carrier == KeyDomain.Agent);

            // Step 2: Move to Locked Room. Result => Agent in Room 1
            // Act
            m_EntityManager.AddComponent(resultingPGNEntity, typeof(Selected));
            yield return UpdateSystems();
            (resultingPGNEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager, resultingPGNEntity, MoveAction.SystemGuid);

            // Test
            Assert.True(resultingPGNEntity != default);
            Assert.True(resultingStateEntity != default);

            m_MoveAction.FilterObjects(resultingStateEntity, (m_MoveAction.AgentTypes, agents));
            Assert.AreEqual(1, agents.Count);
            var agentLocalized = m_EntityManager.GetComponentData<LocalizedTrait>(agents[0].Item1);
            Assert.True(agentLocalized.Location == KeyDomain.FirstRoom);

            // Step 3: Unlock Room. Result => New Room
            // Act
            m_EntityManager.AddComponent(resultingPGNEntity, typeof(Selected));
            yield return UpdateSystems();
            (resultingPGNEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager, resultingPGNEntity, UnlockRoomAction.SystemGuid);

            // Test
            Assert.True(resultingPGNEntity != default);
            Assert.True(resultingStateEntity != default);

            var stateObjects = m_EntityManager.GetBuffer<DomainObjectReference>(resultingStateEntity);
            Assert.AreEqual(6, stateObjects.Length);

            var rooms = new List<(Entity, int)>();
            m_UnlockRoomAction.FilterObjects(resultingStateEntity, (m_UnlockRoomAction.RoomTypes, rooms), (m_UnlockRoomAction.AgentTypes, agents));
            Assert.AreEqual(1, agents.Count);
            agentLocalized = m_EntityManager.GetComponentData<LocalizedTrait>(agents[0].Item1);
            Assert.True(agentLocalized.Location != KeyDomain.FirstRoom);
            Assert.True(agentLocalized.Location != KeyDomain.StartRoom);

            LockableTrait firstRoomLockable = default;
            foreach (var room in rooms)
            {
                var roomEntity = room.Item1;
                if (m_EntityManager.GetComponentData<DomainObjectTrait>(roomEntity).ID.Equals(KeyDomain.FirstRoom))
                    firstRoomLockable = m_EntityManager.GetComponentData<LockableTrait>(roomEntity);
            }

            Assert.AreNotEqual(firstRoomLockable, default);
            Assert.False(firstRoomLockable.Locked);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithPredictedState()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithUnexpectedStateRemovedObject()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);
            var domainObjectBuffer = m_EntityManager.GetBuffer<DomainObjectReference>(newStateEntity);
            domainObjectBuffer.RemoveAt(0);
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreNotEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithUnexpectedStateAddedObject()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);

            var domainObjectEntity = TraitBasedDomain.CreateDomainObject(m_EntityManager, newStateEntity, typeof(ColorTrait));
            var domainObjectBuffer = m_EntityManager.GetBuffer<DomainObjectReference>(newStateEntity);
            domainObjectBuffer.Add(new DomainObjectReference { DomainObjectEntity = domainObjectEntity });
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreNotEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithUnexpectedStateRemovedTrait()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);
            var domainObjectBuffer = m_EntityManager.GetBuffer<DomainObjectReference>(newStateEntity);
            var objEntity = domainObjectBuffer[0].DomainObjectEntity;
            m_EntityManager.RemoveComponent<ColorTrait>(objEntity);
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreNotEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithUnexpectedStateChangedTraitOnObject()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);
            var domainObjectBuffer = m_EntityManager.GetBuffer<DomainObjectReference>(newStateEntity);
            var objEntity = domainObjectBuffer[0].DomainObjectEntity;
            var lockedTrait = m_EntityManager.GetComponentData<LockableTrait>(objEntity);
            lockedTrait.Locked = !lockedTrait.Locked;
            m_EntityManager.SetComponentData(objEntity, lockedTrait);
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreNotEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }

        [UnityTest]
        public IEnumerator UpdateKeyDomainPolicyWithUnexpectedStateAddedTrait()
        {
            yield return PrewarmSystems();

            // Pickup Key. Result => Agent has White Key
            var policyGraphRootEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var (resultingPolicyGraphNodeEntity, resultingStateEntity) = GetResultsFromAction(m_EntityManager,
                policyGraphRootEntity, PickupKeyAction.SystemGuid);

            // Update planner with predicted state
            var newStateEntity = TraitBasedDomain.CopyState(m_EntityManager, resultingStateEntity);
            var domainObjectBuffer = m_EntityManager.GetBuffer<DomainObjectReference>(newStateEntity);
            var objEntity = domainObjectBuffer[0].DomainObjectEntity;
            m_EntityManager.AddComponentData(objEntity, new EndTrait());
            m_PolicyGraph.UpdatePlan(newStateEntity);

            // Test for
            Assert.AreNotEqual(resultingPolicyGraphNodeEntity, m_PolicyGraph.PolicyGraphRootEntity);
        }
    }
}
