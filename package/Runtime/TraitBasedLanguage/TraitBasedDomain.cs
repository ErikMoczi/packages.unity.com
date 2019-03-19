using System.Text;
using Unity.AI.Planner.Utility;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Properties;
using Unity.Properties;


namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// Delegate for writing back trait data
    /// </summary>
    /// <param name="trait">The trait for which changes are to be made</param>
    /// <typeparam name="T">Trait type</typeparam>
    public delegate void WriteTrait<T>(ref T trait) where T : struct, ITrait;

    abstract class TraitBasedDomain
    {
        internal static Entity CreateDomainObject(EntityManager entityManager, Entity stateEntity, EntityArchetype objectArchetype)
        {
            var entity = entityManager.CreateEntity(objectArchetype);

            if (!entityManager.HasComponent<DomainObjectTrait>(entity))
                entityManager.AddComponentData(entity, DomainObjectTrait.GetNext());

            var domainObjectReferences = entityManager.GetBuffer<DomainObjectReference>(stateEntity);
            domainObjectReferences.Add(new DomainObjectReference {DomainObjectEntity = entity});

            return entity;
        }

        internal static Entity CreateDomainObject(EntityManager entityManager, Entity stateEntity, params ComponentType[] types)
        {
            var archetype = entityManager.CreateArchetype(types);
            return CreateDomainObject(entityManager, stateEntity, archetype);
        }

        internal static Entity CreateState(EntityManager entityManager, bool propagate = false, int horizon = 0)
        {
            var stateEntity = entityManager.CreateEntity(typeof(State), typeof(HashCode), typeof(DomainObjectReference));

            if (propagate)
                PropagateState(entityManager, stateEntity, horizon);

            return stateEntity;
        }

        internal static void PropagateState(EntityManager entityManager, Entity stateEntity, int horizon = 0)
        {
            entityManager.AddComponentData(stateEntity, new CreatedStateInfo
            {
                ParentPolicyGraphNodeEntity = default,
                ParentStateEntity = default,
                ActionNodeEntity = default,
                Probability = 1,
                TransitionUtilityValue = 0,
                StateHorizon = horizon
            });
        }

        internal static Entity CopyState(EntityManager entityManager, Entity parentStateEntity)
        {
            // Copy state entity
            var stateCopyEntity = entityManager.Instantiate(parentStateEntity);
//            entityManager.SetComponentData(stateCopyEntity, new HashCode());

            // Copy all object entities.
            var objectBuffer = entityManager.GetBuffer<DomainObjectReference>(stateCopyEntity);
            for (var i = 0; i < objectBuffer.Length; i++)
            {
                var objectEntityReference = objectBuffer[i];
                objectEntityReference.DomainObjectEntity = entityManager.Instantiate(objectEntityReference.DomainObjectEntity);
                objectBuffer = entityManager.GetBuffer<DomainObjectReference>(stateCopyEntity);
                objectBuffer[i] = objectEntityReference;
            }

            return stateCopyEntity;
        }

        internal static bool RemoveDomainObject(EntityManager entityManager, Entity stateEntity, DomainObjectID domainObjectID)
        {
            var domainObjectReferences = entityManager.GetBuffer<DomainObjectReference>(stateEntity);
            for (var i = 0; i < domainObjectReferences.Length; i++)
            {
                var domainObjectTrait = entityManager.GetComponentData<DomainObjectTrait>(domainObjectReferences[i].DomainObjectEntity);
                if (domainObjectTrait.ID == domainObjectID)
                {
                    domainObjectReferences.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        internal static bool ContainsRequiredComponentTypes(NativeArray<ComponentType> typeSet, NativeArray<ComponentType> requiredTypes)
        {
            for (var i = 0; i < requiredTypes.Length; i++)
            {
                var requiredType = requiredTypes[i];
                var subtractive = requiredType.AccessModeType == ComponentType.AccessMode.Subtractive;

                bool matches = subtractive;
                for (var j = 0; j < typeSet.Length; j++)
                {
                    if (typeSet[j].TypeIndex == requiredType.TypeIndex)
                    {
                        if (subtractive)
                            return false;

                        matches = true;
                        break;
                    }
                }

                if (!matches)
                    return false;
            }

            return true;
        }

        internal static void SetTrait<T>(EntityManager entityManager, WriteTrait<T> setter, Entity entity) where T : struct, ITrait
        {
            var hashCode = entityManager.GetComponentData<HashCode>(entity);
            var trait = entityManager.GetComponentData<T>(entity);

            hashCode.Value -= trait.GetHashCode();
            setter(ref trait);
            hashCode.Value += trait.GetHashCode();

            entityManager.SetComponentData(entity, hashCode);
            entityManager.SetComponentData(entity, trait);
        }

        internal static string GetStateString(EntityManager entityManager, Entity stateEntity)
        {
            var sb = new StringBuilder();

            var visitor = new ToStringPropertyVisitor();
            var stateEntityContainer = new EntityContainer(entityManager, stateEntity);
            PropertyContainer.Visit(ref stateEntityContainer, visitor);
            sb.AppendLine(visitor.ToString());

            var domainObjectBuffer = entityManager.GetBuffer<DomainObjectReference>(stateEntity);
            for (var i = 0; i < domainObjectBuffer.Length; i++)
            {
                var domainObjectEntity = domainObjectBuffer[i].DomainObjectEntity;
                var entityContainer = new EntityContainer(entityManager, domainObjectEntity);

                visitor.Reset();
                PropertyContainer.Visit(ref entityContainer, visitor);
                var domainObjectInfo = visitor.ToString();

                sb.AppendLine($"DomainObject Entity: {domainObjectEntity.Index}");
                sb.AppendLine(domainObjectInfo);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
