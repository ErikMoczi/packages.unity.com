using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Planner;
using Unity.AI.Planner.Agent;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Serialization;
using UnityObject = UnityEngine.Object;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    delegate uint TraitMaskDelegate(params Type[] traitFilter);

    [Serializable]
    class PlanningDomainData : IDisposable
    {
        public string Name => m_DomainDefinition.name;
        public IStateTermination[] StateTerminations => m_StateTerminations;

        public Dictionary<string, IOperationalAction> ActionMapping = new Dictionary<string, IOperationalAction>();

        public TraitMaskDelegate GetTraitMask;

        // These fields are assigned in the editor, so ignore the warning that they are never assigned to
        #pragma warning disable 0649

        [FormerlySerializedAs("m_BehaviorDefinition")]
        [SerializeField]
        PlanDefinition m_PlanDefinition;

        [SerializeField]
        List<DomainObjectData> m_InitialDomainObjectData;

        [SerializeField]
        List<TraitObjectData> m_InitialStateTraitData;

        #pragma warning restore 0649

        DomainDefinition m_DomainDefinition;

        // Forward simulation
        IStateTermination[] m_StateTerminations;

        public void Initialize()
        {
            m_DomainDefinition = m_PlanDefinition.DomainDefinition;
            InitializeActionMapping();
            InitializeStateTerminations();
            InitializeGetTraitMask();
        }

        void InitializeActionMapping()
        {
            foreach (var actionDefinition in m_PlanDefinition.ActionDefinitions)
            {
                IOperationalAction operationalAction = null;
                var gameLogicType = m_DomainDefinition.GetType(actionDefinition.OperationalActionType);
                if (gameLogicType != null)
                    operationalAction = (IOperationalAction)Activator.CreateInstance(gameLogicType);

                ActionMapping[actionDefinition.Name] = operationalAction;
            }
        }

        void InitializeStateTerminations()
        {
            var stateTerminations = new List<IStateTermination>();
            foreach (var stateTerminationDefinition in m_PlanDefinition.StateTerminationDefinitions)
            {
                var terminationType = m_PlanDefinition.GetType(stateTerminationDefinition.Name);
                var instance = Activator.CreateInstance(terminationType) as IStateTermination;
                stateTerminations.Add(instance);
            }

            m_StateTerminations = stateTerminations.ToArray();
        }

        void InitializeGetTraitMask()
        {
            var traitMaskUtilityType = m_DomainDefinition.GetType("TraitMaskUtility");
            var methodInfo = traitMaskUtilityType.GetMethod("GetTraitMask", BindingFlags.Public | BindingFlags.Static);
            GetTraitMask = (TraitMaskDelegate)Delegate.CreateDelegate(typeof(TraitMaskDelegate), methodInfo);
        }

        public void Dispose()
        {
            if (m_StateTerminations != null)
            {
                foreach (var stateTermination in m_StateTerminations)
                {
                    stateTermination.Dispose();
                }

                m_StateTerminations = null;
            }
        }

        public Entity GetInitialState(EntityManager entityManager)
        {
            if (m_DomainDefinition == null)
            {
                if (m_PlanDefinition == null)
                    throw new Exception("Plan definition not specified.");

                m_DomainDefinition = m_PlanDefinition.DomainDefinition;
            }

            var stateEntity = TraitBasedDomain.CreateState(entityManager);

            var objectLookup = new Dictionary<string, Entity>(m_InitialDomainObjectData.Count);

            // First pass - initialize objects (for linking in second pass)
            foreach (var objectData in m_InitialDomainObjectData)
            {
                var traits = objectData.TraitData;
                var traitTypes = traits
                    .Select(t => new ComponentType(m_PlanDefinition.GetType(t.TraitDefinitionName)))
                    .Concat(new []{new ComponentType(typeof(HashCode))})
                    .ToArray();

                var domainObjectEntity = TraitBasedDomain.CreateDomainObject(entityManager, stateEntity, traitTypes);
                objectLookup[objectData.Name] = domainObjectEntity;
            }

            // Second pass - set all properties
            foreach (var objectData in m_InitialDomainObjectData)
            {
                var domainObjectHashCodeValue = 0;
                var domainObjectEntity = objectLookup[objectData.Name];

                foreach (var traitData in objectData.TraitData)
                {
                    var trait = InitializeTrait(entityManager, traitData, objectLookup);
                    ComponentType componentType = trait.GetType();

                    if (!componentType.IsZeroSized)
                        trait.SetComponentData(entityManager, domainObjectEntity);
                    else
                        trait.SetTraitMask(entityManager, domainObjectEntity);

                    domainObjectHashCodeValue += trait.GetHashCode();
                }

                // Set hash code component
                var objectHash = entityManager.GetComponentData<HashCode>(domainObjectEntity);
                objectHash.Value = domainObjectHashCodeValue;
                entityManager.SetComponentData(domainObjectEntity, objectHash);
            }

            // Initialize state traits
            foreach (var traitData in m_InitialStateTraitData)
            {
                var trait = InitializeTrait(entityManager, traitData, objectLookup);
                ComponentType componentType = trait.GetType();

                entityManager.AddComponent(stateEntity, componentType);
                if (!componentType.IsZeroSized)
                    trait.SetComponentData(entityManager, stateEntity);
                else
                    trait.SetTraitMask(entityManager, stateEntity);
            }

            return stateEntity;
        }

        public PolicyGraphUpdateSystem GetPolicyGraphUpdateSystem(World world)
        {
            var updateSystemType = m_DomainDefinition.GetType(m_DomainDefinition.PolicyGraphUpdateSystemName);
            return (PolicyGraphUpdateSystem)world.GetOrCreateManager(updateSystemType);
        }

        ITrait InitializeTrait(EntityManager entityManager, TraitObjectData traitData, Dictionary<string, Entity> objectLookup)
        {
            var traitMatch = m_DomainDefinition.TraitDefinitions.First(td => td.Name == traitData.TraitDefinitionName);
            var trait = (IPropertyContainer)Activator.CreateInstance(m_DomainDefinition.GetType(traitMatch.Name));

            traitData.InitializeFieldValues(traitMatch, m_DomainDefinition);
            foreach (var field in traitMatch.Fields)
            {
                var fieldType = m_DomainDefinition.GetType(field.Type);

                if (fieldType == typeof(DomainObjectID))
                {
                    // Lookup domain objects by name
                    if (traitData.TryGetValue(field.Name, out string objectName) && objectName != null)
                    {
                        objectLookup.TryGetValue(objectName, out var targetObject);
                        var id = entityManager.GetComponentData<DomainObjectTrait>(targetObject).ID;
                        (trait.PropertyBag.FindProperty(field.Name) as IValueStructProperty)?.SetObjectValue(ref trait, id);
                    }
                }
                else
                {
                    // NOTE: GetValue returns a boxed object and a UnityObject for any null values (even if the field
                    // type is Transform), so we have to check for "fake null" UnityObjects that won't properly downcast
                    // to a more specific type (e.g. Transform)
                    var value = traitData.GetValue(field.Name);
                    var unityObject = value as UnityObject;
                    if (value is UnityObject && !unityObject)
                        continue;

                    (trait.PropertyBag.FindProperty(field.Name) as IValueStructProperty)?.SetObjectValue(ref trait, value);
                }
            }

            return (ITrait)trait;
        }
    }
}
