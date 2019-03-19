using System.Collections.Generic;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;

namespace Unity.AI.Planner.Tests
{
    class KeyDomain : TraitBasedDomain
    {
        public static DomainObjectID Agent;
        public static DomainObjectID BlackKey;
        public static DomainObjectID WhiteKey;
        public static DomainObjectID StartRoom;
        public static DomainObjectID FirstRoom;

        public static Entity InitialStateEntity;

        static EntityArchetype s_RoomArchetype;
        static EntityArchetype s_KeyArchetype;
        static EntityArchetype s_AgentArchetype;

        public static void Initialize(World world)
        {
            var entityManager = world.GetOrCreateManager<EntityManager>();

            s_RoomArchetype = entityManager.CreateArchetype(typeof(LockableTrait), typeof(ColorTrait));
            s_KeyArchetype = entityManager.CreateArchetype(typeof(CarriableTrait), typeof(ColorTrait));
            s_AgentArchetype = entityManager.CreateArchetype(typeof(CarrierTrait), typeof(LocalizedTrait));

            var stateEntity = CreateState(entityManager, propagate:false);

            var startRoomEntity = CreateRoom(entityManager, stateEntity, Color.Black, false);
            var blackKeyEntity = CreateKey(entityManager, stateEntity, Color.Black);

            var firstRoomEntity = CreateRoom(entityManager, stateEntity, Color.White);
            var whiteKeyEntity = CreateKey(entityManager, stateEntity, Color.White);
            var agentEntity = CreateAgent(entityManager, stateEntity, startRoomEntity, blackKeyEntity);

            Agent = entityManager.GetComponentData<DomainObjectTrait>(agentEntity).ID;
            BlackKey = entityManager.GetComponentData<DomainObjectTrait>(blackKeyEntity).ID;
            WhiteKey = entityManager.GetComponentData<DomainObjectTrait>(whiteKeyEntity).ID;
            StartRoom = entityManager.GetComponentData<DomainObjectTrait>(startRoomEntity).ID;
            FirstRoom = entityManager.GetComponentData<DomainObjectTrait>(firstRoomEntity).ID;

            InitialStateEntity = stateEntity;
        }

        public static Entity CreateRoom(EntityManager entityManager, Entity stateEntity, Color color, bool locked = true)
        {
            var room = CreateDomainObject(entityManager, stateEntity, s_RoomArchetype);
            entityManager.SetComponentData(room, new LockableTrait { Locked = locked });
            entityManager.SetComponentData(room, new ColorTrait { Color = color });

            return room;
        }

        static Entity CreateKey(EntityManager entityManager, Entity stateEntity, Color color)
        {
            var key = CreateDomainObject(entityManager, stateEntity, s_KeyArchetype);
            entityManager.SetComponentData(key, new CarriableTrait { Carrier = DomainObjectID.None });
            entityManager.SetComponentData(key, new ColorTrait { Color = color });

            return key;
        }

        static Entity CreateAgent(EntityManager entityManager, Entity stateEntity, Entity room, Entity key)
        {
            var agent = CreateDomainObject(entityManager, stateEntity, s_AgentArchetype);
            entityManager.SetComponentData(agent, new CarrierTrait { CarriedObject = entityManager.GetComponentData<DomainObjectTrait>(key).ID });
            entityManager.SetComponentData(agent, new LocalizedTrait { Location = entityManager.GetComponentData<DomainObjectTrait>(room).ID });
            entityManager.SetComponentData(key, new CarriableTrait { Carrier = entityManager.GetComponentData<DomainObjectTrait>(agent).ID });

            return agent;
        }
    }

    class KeyDomainUpdateSystem : PolicyGraphUpdateSystem
    {
        List<Entity> m_EntityListLHS = new List<Entity>();
        List<Entity> m_EntityListRHS = new List<Entity>();

        ComponentType EndTrait;
        ComponentType ColorTrait;
        ComponentType CarrierTrait;
        ComponentType CarriableTrait;
        ComponentType LocalizedTrait;
        ComponentType LockableTrait;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            EndTrait = ComponentType.Create<EndTrait>();
            ColorTrait = ComponentType.Create<ColorTrait>();
            CarrierTrait = ComponentType.Create<CarrierTrait>();
            CarriableTrait = ComponentType.Create<CarriableTrait>();
            LocalizedTrait = ComponentType.Create<LocalizedTrait>();
            LockableTrait = ComponentType.Create<LockableTrait>();
        }

        protected override bool IsTerminal(Entity stateEntity) => EntityManager.HasComponent(stateEntity, EndTrait);

        internal override HashCode HashState(Entity stateEntity)
        {
            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);

            var colorLookup = GetComponentDataFromEntity<ColorTrait>(true);
            var carrierLookup = GetComponentDataFromEntity<CarrierTrait>(true);
            var carriableLookup = GetComponentDataFromEntity<CarriableTrait>(true);
            var localizedLookup = GetComponentDataFromEntity<LocalizedTrait>(true);
            var lockableLookup = GetComponentDataFromEntity<LockableTrait>(true);

            var hash = 19;
            for (var i = 0; i < domainObjectBuffer.Length; i++)
            {
                var entity = domainObjectBuffer[i].DomainObjectEntity;

                if (EntityManager.HasComponent(entity, ColorTrait))
                    hash += colorLookup[entity].GetHashCode();
                if (EntityManager.HasComponent(entity, CarrierTrait))
                    hash += carrierLookup[entity].GetHashCode();
                if (EntityManager.HasComponent(entity, CarriableTrait))
                    hash += carriableLookup[entity].GetHashCode();
                if (EntityManager.HasComponent(entity, LocalizedTrait))
                    hash += localizedLookup[entity].GetHashCode();
                if (EntityManager.HasComponent(entity, LockableTrait))
                    hash += lockableLookup[entity].GetHashCode();
            }

            return new HashCode { Value = hash, TraitMask = 0 };
        }

        protected override bool StateEquals(Entity lhsStateEntity, Entity rhsStateEntity)
        {
            // Check for same number of domain objects.
            var lhsObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(lhsStateEntity);
            var rhsObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(rhsStateEntity);
            if (lhsObjectBuffer.Length != rhsObjectBuffer.Length)
                return false;

            // Check for same End status.
            var lhsHasEndTrait = EntityManager.HasComponent(lhsStateEntity, EndTrait);
            var rhsHasEndTrait = EntityManager.HasComponent(rhsStateEntity, EndTrait);
            if (lhsHasEndTrait != rhsHasEndTrait)
                return false;

            // Set up entity lists.
            m_EntityListLHS.Clear();
            m_EntityListRHS.Clear();
            for (int i = 0; i < lhsObjectBuffer.Length; i++)
            {
                m_EntityListLHS.Add(lhsObjectBuffer[i].DomainObjectEntity);
            }
            for (int i = 0; i < rhsObjectBuffer.Length; i++)
            {
                m_EntityListRHS.Add(rhsObjectBuffer[i].DomainObjectEntity);
            }

            var colors = GetComponentDataFromEntity<ColorTrait>();
            var carriers = GetComponentDataFromEntity<CarrierTrait>();
            var carriables = GetComponentDataFromEntity<CarriableTrait>();
            var localizeds = GetComponentDataFromEntity<LocalizedTrait>();
            var lockables = GetComponentDataFromEntity<LockableTrait>();

            while (m_EntityListLHS.Count > 0)
            {
                var entityLHS = m_EntityListLHS[0];

                var hasColor = EntityManager.HasComponent(entityLHS, ColorTrait);
                var hasCarrier = EntityManager.HasComponent(entityLHS, CarrierTrait);
                var hasCarriable = EntityManager.HasComponent(entityLHS, CarriableTrait);
                var hasLocalized = EntityManager.HasComponent(entityLHS, LocalizedTrait);
                var hasLockable = EntityManager.HasComponent(entityLHS, LockableTrait);
                var hasEnd = EntityManager.HasComponent(entityLHS, EndTrait);

                var foundMatch = false;
                var lhsTypes = EntityManager.GetComponentTypes(entityLHS);
                for (int rhsIndex = 0; rhsIndex < m_EntityListRHS.Count; rhsIndex++)
                {
                    var entityRHS = m_EntityListRHS[rhsIndex];

                    var rhsTypes = EntityManager.GetComponentTypes(entityRHS);
                    if (!TraitBasedDomain.ContainsRequiredComponentTypes(lhsTypes, rhsTypes))
                    {
                        rhsTypes.Dispose();
                        continue;
                    }

                    rhsTypes.Dispose();

                    if (hasColor && !colors[entityLHS].Equals(colors[entityRHS]))
                        continue;

                    if (hasCarrier && !carriers[entityLHS].Equals(carriers[entityRHS]))
                        continue;

                    if (hasCarriable && !carriables[entityLHS].Equals(carriables[entityRHS]))
                        continue;

                    if (hasLocalized && !localizeds[entityLHS].Equals(localizeds[entityRHS]))
                        continue;

                    if (hasLockable && !lockables[entityLHS].Equals(lockables[entityRHS]))
                        continue;

                    if (hasEnd && !EntityManager.HasComponent<EndTrait>(entityRHS))
                        continue;

                    m_EntityListLHS.RemoveAt(0);
                    m_EntityListRHS.RemoveAt(rhsIndex);
                    foundMatch = true;
                    break;
                }

                lhsTypes.Dispose();

                if (!foundMatch)
                    return false;
            }

            return true;
        }
    }
}
