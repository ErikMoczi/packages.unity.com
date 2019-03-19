using System;
using System.Collections.Generic;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Collections;
using Unity.Entities;

namespace Unity.AI.Planner.Tests
{
    class PickupKeyAction : BaseAction<PickupKeyAction.Permutation>
    {
        List<(Entity, int)> m_AgentEntities = new List<(Entity, int)>();
        List<(Entity, int)> m_KeyEntities = new List<(Entity, int)>();
        List<(Entity, int)> m_RoomEntities = new List<(Entity, int)>();
        public NativeArray<ComponentType> AgentTypes { get; private set; }
        public NativeArray<ComponentType> KeyTypes { get; private set; }
        public NativeArray<ComponentType> RoomTypes { get; private set; }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            AgentTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LocalizedTrait>(),
                ComponentType.ReadOnly<CarrierTrait>(),
                ComponentType.ReadOnly<DomainObjectTrait>()
            }, Allocator.Persistent);

            KeyTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<CarriableTrait>(),
                ComponentType.ReadOnly<DomainObjectTrait>(),
                ComponentType.ReadOnly<ColorTrait>()
            }, Allocator.Persistent);

            RoomTypes = new NativeArray<ComponentType>(new[]
            {
                ComponentType.ReadOnly<LockableTrait>(),
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

            if (KeyTypes.IsCreated)
            {
                KeyTypes.Dispose();
                KeyTypes = default;
            }

            if (RoomTypes.IsCreated)
            {
                RoomTypes.Dispose();
                RoomTypes = default;
            }
        }

        protected override void GenerateArgumentPermutations(Entity stateEntity)
        {
            var carriables = GetComponentDataFromEntity<CarriableTrait>();
            var carriers = GetComponentDataFromEntity<CarrierTrait>();
            var localizeds = GetComponentDataFromEntity<LocalizedTrait>();
            var objectTraitLookup = GetComponentDataFromEntity<DomainObjectTrait>();

            FilterObjects(stateEntity);

            if (m_RoomEntities.Count <= 0)
                return;

            var firstRoom = EntityManager.GetComponentData<DomainObjectTrait>(m_RoomEntities[0].Item1).ID;

            // Get argument permutation and check preconditions
            foreach ((var keyEntity, int keyIndex) in m_KeyEntities)
            {
                if (carriables[keyEntity].Carrier != DomainObjectID.None)
                    continue;

                foreach ((var agentEntity, int agentIndex) in m_AgentEntities)
                {
                    if (carriers[agentEntity].CarriedObject == objectTraitLookup[keyEntity].ID)
                        continue;

                    if (localizeds[agentEntity].Location != firstRoom)
                        continue;

                    m_ArgumentPermutations.Add(new Permutation { KeyIndex = keyIndex, AgentIndex = agentIndex });
                }
            }
        }

        protected override void ApplyEffects(Permutation permutation, Entity parentPolicyGraphNodeEntity, Entity originalStateEntity, int horizon)
        {
            var actionNodeEntity = CreateActionNode(parentPolicyGraphNodeEntity);
            var stateCopyEntity = CopyState(originalStateEntity);

            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateCopyEntity);

            var carriableLookup = GetComponentDataFromEntity<CarriableTrait>();
            var objectTraitLookup = GetComponentDataFromEntity<DomainObjectTrait>();
            var carrierLookup = GetComponentDataFromEntity<CarrierTrait>();

            // Action effects
            var agentEntity = domainObjectBuffer[permutation.AgentIndex].DomainObjectEntity;
            var keyEntity = domainObjectBuffer[permutation.KeyIndex].DomainObjectEntity;
            var agentCarrier = carrierLookup[agentEntity];
            var oldKeyID = agentCarrier.CarriedObject;

            if (oldKeyID != DomainObjectID.None)
            {
                FilterObjects(stateCopyEntity, (KeyTypes, m_KeyEntities));
                foreach ((var otherKeyEntity, int _) in m_KeyEntities)
                    if (objectTraitLookup[otherKeyEntity].ID == oldKeyID)
                    {
                        var carriableTrait = carriableLookup[otherKeyEntity];
                        carriableTrait.Carrier = DomainObjectID.None;
                        carriableLookup[otherKeyEntity] = carriableTrait;
                        break;
                    }
            }

            var keyCarriable = carriableLookup[keyEntity];
            keyCarriable.Carrier = objectTraitLookup[agentEntity].ID;
            agentCarrier.CarriedObject = objectTraitLookup[keyEntity].ID;

            // Update components
            carriableLookup[keyEntity] = keyCarriable;
            carrierLookup[agentEntity] = agentCarrier;

            SetActionData(stateCopyEntity, originalStateEntity, parentPolicyGraphNodeEntity, horizon + 1, actionNodeEntity, -1f);
        }

        public struct Permutation
        {
            public int KeyIndex;
            public int AgentIndex;
        }
    }
}
