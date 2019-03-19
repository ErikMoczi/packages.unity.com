using System.Collections.Generic;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Collections;
using Unity.Entities;

namespace Unity.AI.Planner.Tests
{
    class MoveAction : BaseAction<MoveAction.Permutation>
    {
        List<(Entity, int)> m_AgentEntities = new List<(Entity, int)>();
        List<(Entity, int)> m_RoomEntities = new List<(Entity, int)>();
        public NativeArray<ComponentType> AgentTypes { get; private set; }
        public NativeArray<ComponentType> RoomTypes { get; private set; }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            AgentTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LocalizedTrait>()
            }, Allocator.Persistent);

            RoomTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LockableTrait>(),
                ComponentType.ReadOnly<DomainObjectTrait>()
            }, Allocator.Persistent);

            m_FilterTuples = new[] { (AgentTypes, m_AgentEntities), (RoomTypes, m_RoomEntities) };
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
                AgentTypes = default;
            }
        }

        protected override void GenerateArgumentPermutations(Entity stateEntity)
        {
            var agentLocations = GetComponentDataFromEntity<LocalizedTrait>(true);
            var roomIDs = GetComponentDataFromEntity<DomainObjectTrait>(true);

            FilterObjects(stateEntity);

            // Get argument permutation and check preconditions
            foreach (var (roomEntity, roomIndex) in m_RoomEntities)
            {
                foreach (var (agentEntity, agentIndex) in m_AgentEntities)
                {
                    if (agentLocations[agentEntity].Location == roomIDs[roomEntity].ID)
                        continue;

                    m_ArgumentPermutations.Add(new Permutation() { AgentIndex = agentIndex, RoomIndex = roomIndex});
                }
            }
        }

        protected override void ApplyEffects(Permutation permutation, Entity parentPolicyGraphNodeEntity, Entity originalStateEntity, int horizon)
        {
            var actionNodeEntity = CreateActionNode(parentPolicyGraphNodeEntity);
            var stateCopyEntity = TraitBasedDomain.CopyState(EntityManager, originalStateEntity);

            var objectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateCopyEntity);
            var localizedLookup = GetComponentDataFromEntity<LocalizedTrait>();
            var objectTraitLookup = GetComponentDataFromEntity<DomainObjectTrait>();

            // Action effects
            var agentEntity = objectBuffer[permutation.AgentIndex].DomainObjectEntity;
            var agentLocation = localizedLookup[agentEntity];
            agentLocation.Location = objectTraitLookup[objectBuffer[permutation.RoomIndex].DomainObjectEntity].ID;

            localizedLookup[agentEntity] = agentLocation;

            SetActionData(stateCopyEntity, originalStateEntity, parentPolicyGraphNodeEntity, horizon + 1, actionNodeEntity, -1f);
        }

        public struct Permutation
        {
            public int AgentIndex;
            public int RoomIndex;
        }
    }
}
