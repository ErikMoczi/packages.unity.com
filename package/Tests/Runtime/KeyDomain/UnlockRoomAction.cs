using System;
using System.Collections.Generic;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Collections;
using Unity.Entities;

namespace Unity.AI.Planner.Tests
{
    class UnlockRoomAction : BaseAction<UnlockRoomAction.Permutation>
    {
        List<(Entity, int)> m_AgentEntities = new List<(Entity, int)>();
        List<(Entity, int)> m_KeyEntities = new List<(Entity, int)>();
        List<(Entity, int)> m_RoomEntities = new List<(Entity, int)>();
        public NativeArray<ComponentType> AgentTypes { get; private set; }
        public NativeArray<ComponentType> RoomTypes { get; private set; }
        public NativeArray<ComponentType> KeyTypes { get; private set; }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            AgentTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LocalizedTrait>(),
                ComponentType.ReadOnly<CarrierTrait>()
            }, Allocator.Persistent);

            RoomTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LockableTrait>(),
                ComponentType.ReadOnly<DomainObjectTrait>(),
                ComponentType.ReadOnly<ColorTrait>()
            }, Allocator.Persistent);

            KeyTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<CarriableTrait>(),
                ComponentType.ReadOnly<DomainObjectTrait>(),
                ComponentType.ReadOnly<ColorTrait>()
            }, Allocator.Persistent);

            m_FilterTuples = new[] { (AgentTypes, m_AgentEntities), (KeyTypes, m_KeyEntities), (RoomTypes, m_RoomEntities) };
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            if (AgentTypes.IsCreated)
            {
                AgentTypes.Dispose();
                AgentTypes = default;
            }

            if (RoomTypes.IsCreated)
            {
                RoomTypes.Dispose();
                RoomTypes = default;
            }

            if (KeyTypes.IsCreated)
            {
                KeyTypes.Dispose();
                KeyTypes = default;
            }
        }

        protected override void GenerateArgumentPermutations(Entity stateEntity)
        {
            var agentLocations = GetComponentDataFromEntity<LocalizedTrait>(true);
            var lockables = GetComponentDataFromEntity<LockableTrait>(true);
            var roomIDs = GetComponentDataFromEntity<DomainObjectTrait>(true);
            var keyIDs = GetComponentDataFromEntity<DomainObjectTrait>(true);
            var carriers = GetComponentDataFromEntity<CarrierTrait>(true);
            var colors = GetComponentDataFromEntity<ColorTrait>(true);

            FilterObjects(stateEntity);

            // Get argument permutation and check preconditions
            foreach ((var roomEntity, int roomIndex) in m_RoomEntities)
            {
                if (!lockables[roomEntity].Locked)
                    continue;

                foreach ((var agentEntity, int agentIndex) in m_AgentEntities)
                {
                    if (agentLocations[agentEntity].Location != roomIDs[roomEntity].ID)
                        continue;

                    foreach (var (keyEntity, _) in m_KeyEntities)
                    {
                        if (carriers[agentEntity].CarriedObject != keyIDs[keyEntity].ID)
                            continue;

                        if (!colors[roomEntity].Color.Equals(colors[keyEntity].Color))
                            continue;

                        m_ArgumentPermutations.Add(new Permutation { AgentIndex = agentIndex, RoomIndex = roomIndex });
                    }
                }
            }
        }

        protected override void ApplyEffects(Permutation permutation, Entity parentPolicyGraphNodeEntity, Entity originalStateEntity, int horizon)
        {
            var actionNodeEntity = CreateActionNode(parentPolicyGraphNodeEntity);

            horizon++;
            var stateA = TraitBasedDomain.CopyState(EntityManager, originalStateEntity);
            UpdateState(stateA, permutation, Color.Black);
            SetActionData(stateA, originalStateEntity, parentPolicyGraphNodeEntity, horizon, actionNodeEntity, 1f, 0.4f);

            var stateB = TraitBasedDomain.CopyState(EntityManager, originalStateEntity);
            UpdateState(stateB, permutation, Color.White);
            SetActionData(stateB, originalStateEntity, parentPolicyGraphNodeEntity, horizon, actionNodeEntity, 1f, 0.4f);

            var stateC = TraitBasedDomain.CopyState(EntityManager, originalStateEntity);
            UpdateState(stateC, permutation, Color.Black, true);
            SetActionData(stateC, originalStateEntity, parentPolicyGraphNodeEntity, horizon, actionNodeEntity, 10f, 0.2f);
        }

        void UpdateState(Entity stateEntity, Permutation permutation, Color roomColor, bool endRoom = false)
        {
            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);

            var roomEntity = domainObjectBuffer[permutation.RoomIndex].DomainObjectEntity;
            var agentEntity = domainObjectBuffer[permutation.AgentIndex].DomainObjectEntity;

            // Action effects
            var newRoom = KeyDomain.CreateRoom(EntityManager, stateEntity, roomColor);
            var lockables = GetComponentDataFromEntity<LockableTrait>();
            var lockable = lockables[roomEntity];
            lockable.Locked = false;
            lockables[roomEntity] = lockable;

            var agentLocations = GetComponentDataFromEntity<LocalizedTrait>();
            var agentLocation = agentLocations[agentEntity];
            agentLocation.Location = EntityManager.GetComponentData<DomainObjectTrait>(newRoom).ID;
            agentLocations[agentEntity] = agentLocation;

            if (endRoom)
                EntityManager.AddComponent(stateEntity, typeof(EndTrait));
        }

        public struct Permutation
        {
            public int AgentIndex;
            public int RoomIndex;
        }
    }
}
