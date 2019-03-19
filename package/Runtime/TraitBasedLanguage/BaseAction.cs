using System;
using System.Collections.Generic;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Collections;
using Unity.Entities;

namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    class ActionSystemGroup { }

    interface IActionSystem
    {
        string Name { get; }
        Guid ActionSystemGuid { get; }
        T GetTrait<T>(Entity actionEntity, Entity stateEntity, int argumentIndex) where T : struct, IComponentData;
        T GetTrait<T>(Entity entity) where T : struct, IComponentData;
        Entity GetEntityByDomainObjectID(DomainObjectID id, Entity stateEntity);
    }

    /// <summary>
    /// Abstract base class for a planner action (counterpart of <see cref="Unity.AI.Planner.Agent.IOperationalAction"/>).
    ///
    /// See also: <seealso cref="Unity.AI.Planner.ActionContext" />.
    /// </summary>
    /// <typeparam name="TPermutation">Argument permutation type (see generated code or tests for an example)</typeparam>
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [DisableAutoCreation]
    public abstract class BaseAction<TPermutation> : ComponentSystem, IActionSystem where TPermutation: struct
    {
        /// <summary>
        /// List of states marked for expansion
        /// </summary>
        protected ComponentGroup m_ExpansionList;

        /// <summary>
        /// Name for the action system
        /// </summary>
        public string Name => GetType().Name;

        /// <summary>
        /// Globally unique ID for the action system
        /// </summary>
        public Guid ActionSystemGuid => SystemGuid;

        internal static Guid SystemGuid = Guid.NewGuid();

        /// <summary>
        /// Local storage (cache) for argument permutations generated in <see cref="GenerateArgumentPermutations"/>
        /// </summary>
        protected List<TPermutation> m_ArgumentPermutations = new List<TPermutation>();

        /// <summary>
        /// Filtered tuples of entities and indexes in the domain object reference buffer
        /// </summary>
        protected (NativeArray<ComponentType>, List<(Entity, int)>)[] m_FilterTuples;

        static EntityArchetype s_ActionNodeArchetype;

        /// <summary>
        /// Return the trait for a specific argument (i.e. Entity) that was specified with the action
        /// </summary>
        /// <param name="actionEntity">Entity for the planner action node</param>
        /// <param name="stateEntity">Entity for the state in which the action is performed</param>
        /// <param name="argumentIndex">Index of the argument (corresponds with the parameter index in the Plan Definition)</param>
        /// <typeparam name="T">Trait type</typeparam>
        /// <returns>The specified trait data</returns>
        public T GetTrait<T>(Entity actionEntity, Entity stateEntity, int argumentIndex)
            where T : struct, IComponentData
        {
            var argument = EntityManager.GetBuffer<ActionNodeArgument>(actionEntity)[argumentIndex];
            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            var argumentEntity = domainObjectBuffer[argument.DomainObjectReferenceIndex].DomainObjectEntity;
            return EntityManager.GetComponentData<T>(argumentEntity);
        }

        /// <summary>
        /// Return the trait for a specified domain object entity
        /// </summary>
        /// <param name="entity">Entity for the domain object</param>
        /// <typeparam name="T">Trait type</typeparam>
        /// <returns>The specified trait data</returns>
        public T GetTrait<T>(Entity entity) where T : struct, IComponentData
        {
            return EntityManager.GetComponentData<T>(entity);
        }

        /// <summary>
        /// Look up the domain object entity by <see cref="Unity.AI.Planner.DomainLanguage.TraitBased.DomainObjectID"/>
        /// </summary>
        /// <param name="id">ID of the domain object</param>
        /// <param name="stateEntity">Entity for the state</param>
        /// <returns>Entity that matches the DomainObjectID</returns>
        public Entity GetEntityByDomainObjectID(DomainObjectID id, Entity stateEntity)
        {
            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            for (var i = 0; i < domainObjectBuffer.Length; i++)
            {
                var domainObjectEntity = domainObjectBuffer[i].DomainObjectEntity;
                var domainObjectTrait = EntityManager.GetComponentData<DomainObjectTrait>(domainObjectEntity);
                if (domainObjectTrait.ID.Equals(id))
                    return domainObjectEntity;
            }

            return default;
        }

        /// <summary>
        /// Get the entity at specified index of the state
        /// </summary>
        /// <param name="domainObjectBuffer">State buffer of domain object references</param>
        /// <param name="index">Index of domain object in buffer</param>
        /// <param name="stateEntity">Entity for the state</param>
        /// <returns>Entity for the specified domain object</returns>
        protected Entity GetEntity(DynamicBuffer<DomainObjectReference> domainObjectBuffer, int index, Entity stateEntity)
        {
            return index < 0 ? stateEntity : domainObjectBuffer[index].DomainObjectEntity;
        }

        /// <inheritdoc/>
        protected override void OnCreateManager()
        {
            m_ExpansionList = GetComponentGroup(ComponentType.ReadOnly<PolicyGraphNode>(),
                ComponentType.ReadOnly<Selected>());

            s_ActionNodeArchetype = EntityManager.CreateArchetype(typeof(ActionNode), typeof(ActionResult),
                typeof(ActionNodeArgument));
        }

        /// <summary>
        /// Write back a trait for a specific domain object entity
        /// </summary>
        /// <param name="setter">Delegate for modifying trait values (uses ref)</param>
        /// <param name="entity">Entity for the domain object</param>
        /// <typeparam name="T">Trait type</typeparam>
        protected void SetTrait<T>(WriteTrait<T> setter, Entity entity) where T : struct, ITrait
        {
            TraitBasedDomain.SetTrait(EntityManager, setter, entity);
        }

        /// <summary>
        /// Update the newly created state with additional action information
        /// </summary>
        /// <param name="stateCopyEntity">Entity for the copied state for this action permutation</param>
        /// <param name="parentStateEntity">Entity for the state of the policy graph node that is being expanded</param>
        /// <param name="parentPolicyGraphNode">Entity for the policy graph node that is being expanded</param>
        /// <param name="horizon">Horizon of the newly created policy graph node</param>
        /// <param name="actionNodeEntity">Entity for the action node in the policy graph</param>
        /// <param name="value">Transition utility value for following this action</param>
        /// <param name="probability">Probability of this action occuring</param>
        protected void SetActionData(Entity stateCopyEntity, Entity parentStateEntity, Entity parentPolicyGraphNode, int horizon,
            Entity actionNodeEntity, float value, float probability = 1.0f)
        {
            EntityManager.AddComponentData(stateCopyEntity, new CreatedStateInfo
            {
                ParentPolicyGraphNodeEntity = parentPolicyGraphNode,
                ParentStateEntity = parentStateEntity,
                ActionNodeEntity = actionNodeEntity,
                Probability = probability,
                TransitionUtilityValue = value,
                StateHorizon = horizon
            });
        }

        /// <summary>
        /// Method for generating argument permutations (that match preconditions) in order to apply effects later
        /// </summary>
        /// <param name="stateEntity">Entity for the state of the policy graph node that is being expanded</param>
        protected abstract void GenerateArgumentPermutations(Entity stateEntity);

        /// <summary>
        /// Apply effects of this action to a copy of the original state using the arguments (i.e. permutation)
        /// </summary>
        /// <param name="permutation">Permutation of arguments for this action</param>
        /// <param name="parentPolicyGraphNodeEntity">Entity for the policy graph node that is being expanded</param>
        /// <param name="originalStateEntity">Entity for the state of the policy graph node that is being expanded</param>
        /// <param name="horizon">Horizon of the parent policy graph node</param>
        protected abstract void ApplyEffects(TPermutation permutation, Entity parentPolicyGraphNodeEntity, Entity originalStateEntity, int horizon);

        /// <summary>
        /// Process:
        ///
        ///    Input - Entities for policy graph nodes (state) to be expanded
        ///
        ///    1. Iterate over various combinations of parameters by evaluating preconditions
        ///    2. For every valid combination apply effects (must copy state in the process)
        ///
        ///    Output - Resulting states + action node information
        /// </summary>
        protected override void OnUpdate()
        {
            var expansionNodes = m_ExpansionList.GetComponentDataArray<PolicyGraphNode>();
            var pgnEntities = m_ExpansionList.GetEntityArray();
            for (var i = 0; i < expansionNodes.Length; i++) // Can process states in parallel
            {
                var node = expansionNodes[i];
                var stateEntity = node.StateEntity;

                m_ArgumentPermutations.Clear();
                GenerateArgumentPermutations(stateEntity);

                var policyGraphNodeEntity = pgnEntities[i];
                var horizon = node.Horizon;
                for (var p = 0; p < m_ArgumentPermutations.Count; p++)
                {
                    ApplyEffects(m_ArgumentPermutations[p], policyGraphNodeEntity, stateEntity, horizon);
                }

                expansionNodes = m_ExpansionList.GetComponentDataArray<PolicyGraphNode>();
                pgnEntities = m_ExpansionList.GetEntityArray();
            }
        }

        /// <summary>
        /// Create tuples of filtered objects based on parameters specified in the Plan Definition
        /// </summary>
        /// <param name="stateEntity"></param>
        protected void FilterObjects(Entity stateEntity)
        {
            foreach (var (_, list) in m_FilterTuples)
            {
                list.Clear();
            }

            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            for (var i = 0; i < domainObjectBuffer.Length; i++)
            {
                var entity = domainObjectBuffer[i].DomainObjectEntity;
                var componentTypes = EntityManager.GetComponentTypes(entity);

                foreach (var (group, objectList) in m_FilterTuples)
                {
                    if (TraitBasedDomain.ContainsRequiredComponentTypes(componentTypes, group))
                        objectList.Add((entity, i));
                }
            }

            var stateComponentTypes = EntityManager.GetComponentTypes(stateEntity);
            foreach (var (group, objectList) in m_FilterTuples)
            {
                if (TraitBasedDomain.ContainsRequiredComponentTypes(stateComponentTypes, group))
                    objectList.Add((stateEntity, -1));
            }
        }

        internal void FilterObjects(Entity stateEntity, params (NativeArray<ComponentType>, List<(Entity, int)>)[] typeListPairs)
        {
            foreach (var (_, list) in typeListPairs)
            {
                list.Clear();
            }

            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            for (var i = 0; i < domainObjectBuffer.Length; i++)
            {
                var entity = domainObjectBuffer[i].DomainObjectEntity;
                var componentTypes = EntityManager.GetComponentTypes(entity);

                foreach (var (group, objectList) in typeListPairs)
                {
                    if (TraitBasedDomain.ContainsRequiredComponentTypes(componentTypes, group))
                        objectList.Add((entity, i));
                }
            }
        }

        /// <summary>
        /// Create an action node in the policy graph for the expansion
        /// </summary>
        /// <param name="actionGuid">ID of the action system</param>
        /// <param name="parentPolicyGraphNodeEntity">Entity for the policy graph node that is being expanded</param>
        /// <returns>Entity for the newly created action node</returns>
        protected Entity CreateActionNode(Entity parentPolicyGraphNodeEntity)
        {
            var nodeEntity = EntityManager.CreateEntity(s_ActionNodeArchetype);
            EntityManager.SetComponentData(nodeEntity, new ActionNode { VisitCount = 1, ActionSystemGuid = ActionSystemGuid});

            var actionReferenceBuffer = EntityManager.GetBuffer<ActionNodeReference>(parentPolicyGraphNodeEntity);
            actionReferenceBuffer.Add(new ActionNodeReference {ActionNodeEntity = nodeEntity});

            return nodeEntity;
        }

        /// <summary>
        /// Create a copy of the specified state
        /// </summary>
        /// <param name="parentStateEntity">Entity for the parent state to be copied</param>
        /// <returns>Entity for the copied state</returns>
        protected Entity CopyState(Entity parentStateEntity)
        {
            return TraitBasedDomain.CopyState(EntityManager, parentStateEntity);
        }

    }
}
