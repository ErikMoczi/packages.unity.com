using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

namespace Unity.AI.Planner
{
    struct StateHorizonKey : IEquatable<StateHorizonKey>
    {
        public int Horizon;
        public HashCode StateHash;

        public bool Equals(StateHorizonKey other)
        {
            return Horizon == other.Horizon && StateHash == other.StateHash;
        }

        public override int GetHashCode()
        {
            return Horizon + StateHash.GetHashCode();
        }
    }

    struct PolicyGraphNodeStatePair : IEquatable<PolicyGraphNodeStatePair>
    {
        public Entity PolicyGraphNodeEntity;
        public Entity StateEntity;

        public bool Equals(PolicyGraphNodeStatePair other)
        {
            return PolicyGraphNodeEntity.Equals(other.PolicyGraphNodeEntity) && StateEntity.Equals(other.StateEntity);
        }
    }

    /// <summary>
    /// Planner context for an action that is provided to an operational action (e.g. <see cref="Unity.AI.Planner.Agent.IOperationalAction"/>)
    /// </summary>
    public struct ActionContext : IEquatable<ActionContext>
    {
        /// <summary>
        /// The name of the planner action system that corresponds to this operational action
        /// </summary>
        public string Name => m_ActionSystem.Name;

        /// <summary>
        /// The entity for the planner action node in the policy graph
        /// </summary>
        public Entity ActionEntity;

        /// <summary>
        /// The entity for the state in which the action is performed
        /// </summary>
        public Entity StateEntity;

        IActionSystem m_ActionSystem;

        internal ActionContext(Entity actionEntity, Entity stateEntity, IActionSystem actionSystem)
        {
            ActionEntity = actionEntity;
            StateEntity = stateEntity;
            m_ActionSystem = actionSystem;
        }

        /// <summary>
        /// Return the trait for a specific argument (i.e. Entity) that was specified with the action
        /// </summary>
        /// <param name="argumentIndex">Index of the argument (corresponds with the parameter index in the Plan Definition)</param>
        /// <typeparam name="T">Trait type</typeparam>
        /// <returns>The specified trait data</returns>
        public T GetTrait<T>(int argumentIndex) where T : struct, IComponentData
        {
            return m_ActionSystem.GetTrait<T>(ActionEntity, StateEntity, argumentIndex);
        }

        /// <inheritdoc />
        public bool Equals(ActionContext other)
        {
            return ActionEntity.Equals(other.ActionEntity) && StateEntity.Equals(other.StateEntity) && Equals(m_ActionSystem, other.m_ActionSystem);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActionContext other && Equals(other);
        }

        /// <summary>
        /// Returns true if two ActionContexts are equal and false otherwise
        /// </summary>
        /// <param name="x">An ActionContext to compare.</param>
        /// <param name="y">An ActionContext to compare.</param>
        /// <returns>Returns true if two ActionContexts are equal.</returns>
        public static bool operator ==(ActionContext x, ActionContext y) => x.Equals(y);

        /// <summary>
        /// Returns true if two ActionContexts are not equal and false otherwise
        /// </summary>
        /// <param name="x">An ActionContext to compare.</param>
        /// <param name="y">An ActionContext to compare.</param>
        /// <returns>Returns true if two ActionContexts are not equal.</returns>
        public static bool operator !=(ActionContext x, ActionContext y) => !x.Equals(y);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ActionEntity.GetHashCode();
                hashCode = (hashCode * 397) ^ StateEntity.GetHashCode();
                hashCode = (hashCode * 397) ^ (m_ActionSystem != null ? m_ActionSystem.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    class PolicyGraphContainer : IDisposable, IPolicyGraphInternal
    {
        public World World {get; private set;}
        public Entity PolicyGraphRootEntity { get; private set; }
        public int CleanupHorizon { get; set; } = 3; // todo name?

        public IPolicyGraphNode RootNode
        {
            get
            {
                if (PolicyGraphRootEntity != default && EntityManager != null && EntityManager.IsCreated)
                    return EntityManager.GetComponentData<PolicyGraphNode>(PolicyGraphRootEntity);

                return null;
            }
        }

        public ActionContext NextAction
        {
            get
            {
                if (PolicyGraphRootEntity != default)
                {
                    var policyGraphNode = EntityManager.GetComponentData<PolicyGraphNode>(PolicyGraphRootEntity);
                    var optimalActionEntity = policyGraphNode.OptimalActionEntity;
                    if (optimalActionEntity != default)
                    {
                        var actionEntity = policyGraphNode.OptimalActionEntity;
                        var actionNode = EntityManager.GetComponentData<ActionNode>(actionEntity);
                        return new ActionContext(
                            actionEntity,
                            policyGraphNode.StateEntity,
                            m_PolicyGraphUpdateSystem.GetActionSystem(actionNode.ActionSystemGuid)
                        );
                    }
                }
                return default;
            }
        }

        public int MaxHorizonFromRoot => m_FarthestHorizon - m_RootHorizon;
        int m_RootHorizon;
        int m_FarthestHorizon;

        NativeMultiHashMap<HashCode, Entity> m_StateBinLookup;
        NativeMultiHashMapIterator<HashCode> m_StateBinLookupIterator;

        NativeMultiHashMap<StateHorizonKey, PolicyGraphNodeStatePair> m_PolicyGraphNodeLookup;
        NativeMultiHashMapIterator<StateHorizonKey> m_PolicyGraphNodeLookupIterator;

        NativeMultiHashMap<int, PolicyGraphNodeStatePair> m_PolicyGraphNodesByHorizon;
        NativeMultiHashMapIterator<int> m_PolicyGraphNodeHorizonIterator;

        List<Entity> m_EntitiesToDestroy = new List<Entity>();

        PolicyGraphUpdateSystem m_PolicyGraphUpdateSystem;
        EntityManager EntityManager => m_PolicyGraphUpdateSystem?.EntityManager;

        public PolicyGraphContainer(int capacity, PolicyGraphUpdateSystem updateSystem, World world)
        {
            m_StateBinLookup = new NativeMultiHashMap<HashCode, Entity>(capacity, Allocator.Persistent);
            m_PolicyGraphNodeLookup = new NativeMultiHashMap<StateHorizonKey, PolicyGraphNodeStatePair>(capacity, Allocator.Persistent);
            m_PolicyGraphNodesByHorizon = new NativeMultiHashMap<int, PolicyGraphNodeStatePair>(capacity, Allocator.Persistent);
            m_StateBinLookupIterator = default;
            m_PolicyGraphNodeLookupIterator = default;
            m_PolicyGraphNodeHorizonIterator = default;

            PolicyGraphRootEntity = default;
            m_RootHorizon = 0;
            m_FarthestHorizon = 0;

            m_PolicyGraphUpdateSystem = updateSystem;
            World = world;
        }


        public void Dispose()
        {
            if (m_StateBinLookup.IsCreated)
                m_StateBinLookup.Dispose();

            if (m_PolicyGraphNodeLookup.IsCreated)
                m_PolicyGraphNodeLookup.Dispose();

            if (m_PolicyGraphNodesByHorizon.IsCreated)
                m_PolicyGraphNodesByHorizon.Dispose();
        }

        void UpdateRoot(Entity stateEntity, int searchHorizon)
        {
            var stateCopyEntity = TraitBasedDomain.CopyState(EntityManager, stateEntity);
            var stateHash = (HashCode)m_PolicyGraphUpdateSystem.HashState(stateCopyEntity);
            EntityManager.SetComponentData(stateCopyEntity, stateHash);

            // The root may be updated because the plan is being recomputed or because an action has completed,
            // so attempt to match at the specified search horizon (e.g. 0 for the former, 1 for the latter)
            if (MatchPolicyGraphNode(stateCopyEntity, stateHash, searchHorizon, out var matchedStateEntity,
                out var policyGraphNodeStatePair))
            {
                // We can destroy the existing state because we don't need the policy graph update system to
                // process the state into a new policy graph node
                m_PolicyGraphUpdateSystem.DestroyState(stateCopyEntity, true);
            }
            else
            {
                TraitBasedDomain.PropagateState(EntityManager, stateCopyEntity, searchHorizon);

                // No policy graph node was found, so create a new one
                m_PolicyGraphUpdateSystem.Update();

                var stateHorizonKey = new StateHorizonKey { Horizon = searchHorizon, StateHash = stateHash };

                Assert.IsTrue(m_PolicyGraphUpdateSystem.LookupState(stateHash, stateCopyEntity, out matchedStateEntity));
                Assert.IsTrue(m_PolicyGraphUpdateSystem.LookupPolicyGraphNodeAtHorizon(stateHorizonKey, matchedStateEntity, out policyGraphNodeStatePair),
                    "No policy graph node found for updated root state.");
            }

            PolicyGraphRootEntity = policyGraphNodeStatePair.PolicyGraphNodeEntity;
        }

        public void UpdatePlan(Entity stateEntity)
        {
            m_RootHorizon++;
            UpdateRoot(stateEntity, m_RootHorizon);

            CleanupPastPolicyNodes();
        }

        public void Reset(Entity stateEntity)
        {
            m_StateBinLookup.Clear();
            m_PolicyGraphNodeLookup.Clear();
            m_PolicyGraphNodesByHorizon.Clear();

            m_RootHorizon = 0;
            m_FarthestHorizon = 0;
            UpdateRoot(stateEntity, 0);
        }

        void CleanupPastPolicyNodes()
        {
            var targetHorizon = m_RootHorizon - CleanupHorizon - 1;
            var hashCodeLookup = m_PolicyGraphUpdateSystem.GetComponentDataFromEntity<HashCode>();

            // Collect all entities at target horizon
            if (m_PolicyGraphNodesByHorizon.TryGetFirstValue(targetHorizon, out var pgnStatePair,
                out m_PolicyGraphNodeHorizonIterator))
            {
                do
                {
                    var pgnEntity = pgnStatePair.PolicyGraphNodeEntity;

                    m_EntitiesToDestroy.Add(pgnEntity);
                    m_PolicyGraphNodeLookup.Remove(new StateHorizonKey
                        {
                            Horizon = targetHorizon,
                            StateHash = hashCodeLookup[pgnStatePair.StateEntity]
                        });

                    var actionRefs = EntityManager.GetBuffer<ActionNodeReference>(pgnEntity);
                    for (int i = 0; i < actionRefs.Length; i++)
                    {
                        m_EntitiesToDestroy.Add(actionRefs[i].ActionNodeEntity);
                    }

                }
                while (m_PolicyGraphNodesByHorizon.TryGetNextValue(out pgnStatePair, ref m_PolicyGraphNodeHorizonIterator));
            }
            m_PolicyGraphNodesByHorizon.Remove(targetHorizon);

            // Destroy all entities at target horizon
            var entityArray = new NativeArray<Entity>(m_EntitiesToDestroy.Count, Allocator.Temp);
            for (int i = 0; i < m_EntitiesToDestroy.Count; i++)
            {
                entityArray[i] = m_EntitiesToDestroy[i];
            }

            EntityManager.DestroyEntity(entityArray);
            entityArray.Dispose();
            m_EntitiesToDestroy.Clear();

            // Reset parent references at target horizon + 1
            if (m_PolicyGraphNodesByHorizon.TryGetFirstValue(targetHorizon + 1, out pgnStatePair,
                out m_PolicyGraphNodeHorizonIterator))
            {
                do
                    EntityManager.GetBuffer<PolicyGraphNodeParent>(pgnStatePair.PolicyGraphNodeEntity).Clear();
                while (m_PolicyGraphNodesByHorizon.TryGetNextValue(out pgnStatePair,
                    ref m_PolicyGraphNodeHorizonIterator));
            }
        }

        public void AddState(HashCode stateHash, Entity stateEntity)
        {
            m_StateBinLookup.Add(stateHash, stateEntity);
        }

        public void AddPolicyNode(StateHorizonKey stateHorizonKey, PolicyGraphNodeStatePair pgnStatePair)
        {
            m_PolicyGraphNodeLookup.Add(stateHorizonKey, pgnStatePair);
            m_FarthestHorizon = Math.Max(m_FarthestHorizon, stateHorizonKey.Horizon);
            m_PolicyGraphNodesByHorizon.Add(stateHorizonKey.Horizon, pgnStatePair);
        }

        public bool TryGetFirstState(HashCode stateHash, out Entity stateWithMatchingHash)
        {
            return m_StateBinLookup.TryGetFirstValue(stateHash, out stateWithMatchingHash, out m_StateBinLookupIterator);
        }

        public bool TryGetNextState(out Entity stateWithMatchingHash)
        {
            return m_StateBinLookup.TryGetNextValue(out stateWithMatchingHash, ref m_StateBinLookupIterator);
        }

        public bool TryGetFirstPolicyGraphNode(StateHorizonKey stateHorizon, out PolicyGraphNodeStatePair matchingPGN)
        {
            return m_PolicyGraphNodeLookup.TryGetFirstValue(stateHorizon, out matchingPGN, out m_PolicyGraphNodeLookupIterator);
        }

        public bool TryGetNextPolicyGraphNode(out PolicyGraphNodeStatePair matchingPGN)
        {
            return m_PolicyGraphNodeLookup.TryGetNextValue(out matchingPGN, ref m_PolicyGraphNodeLookupIterator);
        }

        bool MatchPolicyGraphNode(Entity stateEntity, HashCode stateHash, int horizon, out Entity matchedStateEntity,
            out PolicyGraphNodeStatePair policyGraphNodeStatePair)
        {
            policyGraphNodeStatePair = default;

            // Match the state first in order to search for a corresponding policy graph node
            if (m_PolicyGraphUpdateSystem.LookupState(stateHash, stateEntity, out matchedStateEntity))
            {
                var stateHorizonKey = new StateHorizonKey { Horizon = horizon, StateHash = stateHash };
                if (m_PolicyGraphUpdateSystem.LookupPolicyGraphNodeAtHorizon(stateHorizonKey, matchedStateEntity, out policyGraphNodeStatePair))
                    return true;
            }
            else
            {
                matchedStateEntity = stateEntity;

#if PLANNER_DEBUG
                Debug.LogWarning("No matching state found.");
#endif
            }
#if PLANNER_DEBUG
            Debug.LogWarning("No matching policy node found.");
#endif

            return false;
        }
    }

    /// <summary>
    /// A system which governs the exploration of a planning domain state space by extending a policy graph
    /// </summary>
    [UpdateAfter(typeof(EntityManager))]
    [UpdateBefore(typeof(ActionSystemGroup))]
    [DisableAutoCreation]
    public sealed class PlannerSystem : ComponentSystem
    {
        internal PolicyGraphContainer PolicyGraph { get; set; }

        Func<PolicyGraphNode, float> m_ExplorationFactor = node => Math.Abs(node.PolicyValue) + 1;
        Func<PolicyGraphNode, float> m_ExpectedUtilFactor = node => 0.5f;
        List<(ActionNode, Entity)> m_MaxActionNodes = new List<(ActionNode, Entity)>();
        static Random s_Random = new Random();

        /// <summary>
        /// Initializes the planner system for the given world (usually a single domain)
        /// </summary>
        /// <param name="world">The world governing this component system</param>
        /// <returns>Returns the initialized PlannerSystem</returns>
        public static PlannerSystem Initialize(World world)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                IEnumerable<Type> allTypes;
                try
                {
                    allTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    allTypes = e.Types.Where(t => t != null);
                    Debug.LogWarning($"Planner initialization failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
                }

                // Create all ComponentSystem
                CreateBehaviourManagersForMatchingTypes(false, allTypes, world);
            }

            var plannerSystem = world.GetOrCreateManager<PlannerSystem>();
            return plannerSystem;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // Check for lack of policy/policy root.
            if (PolicyGraph == null || PolicyGraph.PolicyGraphRootEntity == default)
            {
#if PLANNER_DEBUG
                Debug.LogWarning("No policy root found.");
#endif
                return;
            }

            var actionNodeLookup = GetComponentDataFromEntity<ActionNode>();
            var policyNodeLookup = GetComponentDataFromEntity<PolicyGraphNode>();
            var policyNodeEntity = PolicyGraph.PolicyGraphRootEntity;
            var policyNodeInfo = policyNodeLookup[policyNodeEntity];

            if (policyNodeInfo.Complete)  // Completed root; no need to expand.
                return;

            var actionReferenceBuffer = EntityManager.GetBuffer<ActionNodeReference>(policyNodeEntity);
            while (actionReferenceBuffer.Length > 0)
            {
                // Update node iteration count
                policyNodeInfo.Iterations += 1;
                policyNodeLookup[policyNodeEntity] = policyNodeInfo;

                // Select an action via UCT.
                var (actionNodeInfo, actionNodeEntity) = SelectAction(policyNodeInfo, actionNodeLookup, actionReferenceBuffer);

                // Update action count.
                actionNodeInfo.VisitCount += 1;
                actionNodeLookup[actionNodeEntity] = actionNodeInfo;

                // Sample a node from the action results.
                (policyNodeInfo, policyNodeEntity) = SelectNode(actionNodeEntity, policyNodeLookup);

                // Grab action references (criteria for loop)
                actionReferenceBuffer = EntityManager.GetBuffer<ActionNodeReference>(policyNodeEntity);
            }

            if (!EntityManager.HasComponent<Selected>(policyNodeEntity))
                EntityManager.AddComponent(policyNodeEntity, typeof(Selected));
        }

        (ActionNode actionNode, Entity actionNodeEntity) SelectAction(PolicyGraphNode node, ComponentDataFromEntity<ActionNode> actionNodeLookup, DynamicBuffer<ActionNodeReference> actionReferenceBuffer)
        {
            // Calculate and keep max UCT over actions
            float maxUCTValue = float.MinValue;
            m_MaxActionNodes.Clear();
            for (var i = 0; i < actionReferenceBuffer.Length; i++)
            {
                var actionNodeEntity = actionReferenceBuffer[i].ActionNodeEntity;
                var actionNode = actionNodeLookup[actionNodeEntity];
                if (actionNode.Complete)
                    continue;

                float expFactor = m_ExplorationFactor(node);
                float utilFactor = m_ExpectedUtilFactor(node);
                float uctValue = actionNode.ActionValue * utilFactor + expFactor *
                                 (float)Math.Sqrt(Math.Log(node.Iterations) / actionNode.VisitCount);

                if (uctValue > maxUCTValue)
                {
                    maxUCTValue = uctValue;
                    m_MaxActionNodes.Clear();
                    m_MaxActionNodes.Add((actionNode, actionNodeEntity));
                }
                else if (Math.Abs(uctValue - maxUCTValue) < float.Epsilon)
                {
                    m_MaxActionNodes.Add((actionNode, actionNodeEntity));
                }
            }

            // Sample one state/node from selected action. If tied on actions, break uniformly.
            return m_MaxActionNodes[s_Random.Next(m_MaxActionNodes.Count)];
        }

        (PolicyGraphNode node, Entity nodeEntity) SelectNode(Entity actionNodeEntity, ComponentDataFromEntity<PolicyGraphNode> policyNodeLookup)
        {
            // Sample successor node to for traversal
            var actionResultBuffer = EntityManager.GetBuffer<ActionResult>(actionNodeEntity);
            float totalProbability = 0f;
            for (var actionIndex = 0; actionIndex < actionResultBuffer.Length; actionIndex++)
            {
                var actionResult = actionResultBuffer[actionIndex];
                if (!policyNodeLookup[actionResult.PolicyGraphNodeEntity].Complete) // Don't sample complete nodes.
                    totalProbability += actionResult.Probability;
            }

            var chosen = s_Random.NextDouble() * totalProbability;
            totalProbability = 0;
            for(var actionIndex = 0; actionIndex < actionResultBuffer.Length; actionIndex++)
            {
                var actionResult = actionResultBuffer[actionIndex];
                var resultingPolicyGraphNodeEntity = actionResult.PolicyGraphNodeEntity;
                var resultingPolicyNode = policyNodeLookup[resultingPolicyGraphNodeEntity];

                // Don't sample complete nodes.
                if (resultingPolicyNode.Complete)
                    continue;

                totalProbability += actionResult.Probability;
                if (totalProbability >= chosen)
                    return (resultingPolicyNode, resultingPolicyGraphNodeEntity);
            }

            return  (default, default);
        }

        static void CreateBehaviourManagersForMatchingTypes(bool editorWorld, IEnumerable<Type> allTypes, World world)
        {
            var systemTypes = allTypes.Where(t =>
                t.IsSubclassOf(typeof(ComponentSystemBase))
                && !t.IsAbstract
                && !t.ContainsGenericParameters
                && !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith(world.Name));

            foreach (var type in systemTypes)
            {
                if (editorWorld && type.GetCustomAttributes(typeof(ExecuteInEditMode), true).Length == 0)
                    continue;

                GetBehaviourManagerAndLogException(world, type);
            }
        }

        static void GetBehaviourManagerAndLogException(World world, Type type)
        {
            try
            {
                world.GetOrCreateManager(type);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    /// <summary>
    /// A system which extends a policy graph given newly explored actions and newly reached states
    /// </summary>
    [UpdateAfter(typeof(ActionSystemGroup))]
    [DisableAutoCreation]
    public abstract class PolicyGraphUpdateSystem : ComponentSystem
    {
        internal PolicyGraphContainer PolicyGraph { private get; set; }
        internal IStateTermination[] StateTerminations { private get; set; }
        internal Func<Entity, float> StateEvaluationHeuristic { private get; set; } = stateEntity => 0f;
        internal new EntityManager EntityManager => base.EntityManager; // todo change once PolicyGraph can access an EM

        const float k_DiscountFactor = 0.95f;
        const float k_PolicyValueTolerance = 10e-10f;

        ComponentGroup m_ExpansionList;
        ComponentGroup m_CreatedStateInfo;


        SortedSet<(Entity, int, Entity)> m_PolicyGraphNodesToUpdate = new SortedSet<(Entity, int, Entity)>(
            Comparer<(Entity, int, Entity)>.Create((x,y) =>
            {
                // Only allow unique horizon/states in the set
                var stateEntityX = x.Item1;
                var stateEntityY = y.Item1;

                var comparison = x.Item2.CompareTo(y.Item2);
                if (comparison == 0 && !stateEntityX.Equals(stateEntityY))
                    return stateEntityX.Index.CompareTo(stateEntityY.Index);

                return comparison;
            }));

        static EntityArchetype s_PolicyGraphNodeArchetype;


        /// <inheritdoc />
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_ExpansionList = GetComponentGroup(ComponentType.ReadOnly<PolicyGraphNode>(),
                ComponentType.ReadOnly<Selected>());

            m_CreatedStateInfo = GetComponentGroup(ComponentType.ReadOnly<State>(), typeof(HashCode),
                ComponentType.ReadOnly<CreatedStateInfo>());

            s_PolicyGraphNodeArchetype = EntityManager.CreateArchetype(typeof(PolicyGraphNode),
            typeof(PolicyGraphNodeParent), typeof(ActionNodeReference));
        }

        /// <inheritdoc />
        protected override void OnDestroyManager()
        {
            PolicyGraph.Dispose();
        }

        internal abstract HashCode HashState(Entity stateEntity);

        /// <summary>
        /// Determine if two states are equal
        /// </summary>
        /// <param name="lhsStateEntity">Entity for the left-hand side of the state equality comparison</param>
        /// <param name="rhsStateEntity">Entity for the right-hand side of the state equality comparison</param>
        /// <returns>Whether the states are equal</returns>
        protected abstract bool StateEquals(Entity lhsStateEntity, Entity rhsStateEntity);

        /// <summary>
        /// Overridable method (with default support for <see cref="Unity.AI.Planner.DomainLanguage.TraitBased.IStateTermination"/>)
        /// that determines if a state is terminal (i.e. should not be expanded further)
        /// </summary>
        /// <param name="stateEntity">Entity for the state</param>
        /// <returns>Whether the state is terminal or not</returns>
        protected virtual bool IsTerminal(Entity stateEntity)
        {
            if (StateTerminations != null && StateTerminations.Length > 0)
            {
                var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
                for (var i = 0; i < domainObjectBuffer.Length; i++)
                {
                    var entity = domainObjectBuffer[i].DomainObjectEntity;
                    var componentTypes = EntityManager.GetComponentTypes(entity);

                    foreach (var stateTermination in StateTerminations)
                    {
                        if (TraitBasedDomain.ContainsRequiredComponentTypes(componentTypes, stateTermination.ComponentTypes)
                            && stateTermination.ShouldTerminate(EntityManager, entity))
                            return true;
                    }
                }
            }

            return false;
        }

        internal void DestroyState(Entity stateEntity, bool immediate = false)
        {
            var domainObjectBuffer = EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            var domainObjectReferences = new NativeArray<DomainObjectReference>(domainObjectBuffer.AsNativeArray(), Allocator.Temp);

            for (var i = 0; i < domainObjectReferences.Length; i++)
            {
                var domainObjectEntity = domainObjectReferences[i].DomainObjectEntity;
                if (immediate)
                    EntityManager.DestroyEntity(domainObjectEntity);
                else
                    PostUpdateCommands.DestroyEntity(domainObjectEntity);
            }

            if (immediate)
                EntityManager.DestroyEntity(stateEntity);
            else
                PostUpdateCommands.DestroyEntity(stateEntity);

            domainObjectReferences.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var entities = m_ExpansionList.GetEntityArray();
            for (var i = 0; i < entities.Length; i++)
            {
                PostUpdateCommands.RemoveComponent<Selected>(entities[i]);
            }

            try
            {
                UpdateGraph();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Remove all entities that we've just processed
                entities = m_CreatedStateInfo.GetEntityArray();
                for (var i = 0; i < entities.Length; i++)
                {
                    PostUpdateCommands.RemoveComponent<CreatedStateInfo>(entities[i]);
                }
            }
        }

        internal bool LookupState(HashCode stateHash, Entity stateEntity, out Entity matchedStateEntity)
        {
            var matched = false;

            // Compare against other states with same hash code
            if (PolicyGraph.TryGetFirstState(stateHash, out matchedStateEntity))
            {
                do
                    matched = StateEquals(stateEntity, matchedStateEntity);
                while (!matched && PolicyGraph.TryGetNextState(out matchedStateEntity));
            }

            return matched;
        }

        internal bool LookupPolicyGraphNodeAtHorizon(StateHorizonKey stateHorizonKey, Entity stateEntity,
            out PolicyGraphNodeStatePair policyGraphNodeStatePair)
        {
            var matched = false;

            if (PolicyGraph.TryGetFirstPolicyGraphNode(stateHorizonKey, out policyGraphNodeStatePair))
            {
                do
                    matched = stateEntity.Equals(policyGraphNodeStatePair.StateEntity);
                while (!matched && PolicyGraph.TryGetNextPolicyGraphNode(out policyGraphNodeStatePair));
            }

            return matched;
        }

        static Entity CreatePolicyGraphNode(EntityManager entityManager, Entity stateEntity, int horizon, bool terminal, float initialPolicyValue=0f)
        {
            var nodeEntity = entityManager.CreateEntity(s_PolicyGraphNodeArchetype);
            entityManager.SetComponentData(nodeEntity, new PolicyGraphNode
            {
                Complete = terminal,
                StateEntity = stateEntity,
                Horizon = horizon,
                PolicyValue = initialPolicyValue,
            });

            return nodeEntity;
        }

        static void CreateActionResult(EntityManager entityManager, Entity policyGraphNodeEntity,
            Entity actionNodeEntity, float probability, float transitionUtilityValue)
        {
            var actionResultBuffer = entityManager.GetBuffer<ActionResult>(actionNodeEntity);
            actionResultBuffer.Add(new ActionResult
            {
                Probability = probability,
                TransitionUtilityValue = transitionUtilityValue,
                PolicyGraphNodeEntity = policyGraphNodeEntity,
            });
        }

        internal IActionSystem GetActionSystem(Guid actionSystemGuid)
        {
            foreach (var manager in World.BehaviourManagers)
            {
                if (manager is IActionSystem actionSystem && actionSystem.ActionSystemGuid == actionSystemGuid)
                    return actionSystem;
            }

            return null;
        }

        void UpdateGraph()
        {
            m_PolicyGraphNodesToUpdate.Clear();

            var createdStateInfos = m_CreatedStateInfo.GetComponentDataArray<CreatedStateInfo>();
            var createdStateHashCodes = m_CreatedStateInfo.GetComponentDataArray<HashCode>();
            var createdStateEntities = m_CreatedStateInfo.GetEntityArray();

            // Hash each of the new states.
            for (var i = 0; i < createdStateEntities.Length; i++)
            {
                createdStateHashCodes[i] = HashState(createdStateEntities[i]);
            }

            while (createdStateEntities.Length > 0)
            {
                var createdStateEntity = createdStateEntities[0];
                var createdStateInfo = createdStateInfos[0];
                var horizon = createdStateInfo.StateHorizon;
                var stateHash = createdStateHashCodes[0];
                Entity policyGraphNodeEntity;
                var stateHorizonKey = new StateHorizonKey { Horizon = horizon, StateHash = stateHash };

                if (LookupState(stateHash, createdStateEntity, out var matchedStateEntity))
                {
                    // Remove created state (duplicate of existing state)
                    DestroyState(createdStateEntity);

                    // Find the corresponding policy graph node for the existing match
                    if (LookupPolicyGraphNodeAtHorizon(stateHorizonKey, matchedStateEntity, out var policyGraphNodeStatePair))
                    {
                        policyGraphNodeEntity = policyGraphNodeStatePair.PolicyGraphNodeEntity;
                    }
                    else
                    {
                        // Heuristic evaluation of state
                        float heuristicValue = StateEvaluationHeuristic(matchedStateEntity);

                        // Did not find a match at the new horizon. Create a new policy graph node for the horizon.
                        policyGraphNodeEntity = CreatePolicyGraphNode(EntityManager, matchedStateEntity,
                            horizon, IsTerminal(matchedStateEntity), heuristicValue);
                        PolicyGraph.AddPolicyNode(stateHorizonKey, new PolicyGraphNodeStatePair
                        {
                            PolicyGraphNodeEntity = policyGraphNodeEntity,
                            StateEntity = matchedStateEntity
                        });
                    }
                }
                else // new state
                {
                    matchedStateEntity = createdStateEntity;

                    // Store new state for reference later when creating other new graph nodes with the same state.
                    PolicyGraph.AddState(stateHash, matchedStateEntity);

                    // Heuristic evaluation of state
                    float heuristicValue = StateEvaluationHeuristic(matchedStateEntity);

                    // Create new node
                    policyGraphNodeEntity = CreatePolicyGraphNode(EntityManager, matchedStateEntity, horizon,
                        IsTerminal(matchedStateEntity), heuristicValue);
                    PolicyGraph.AddPolicyNode(stateHorizonKey, new PolicyGraphNodeStatePair
                    {
                        PolicyGraphNodeEntity = policyGraphNodeEntity,
                        StateEntity = matchedStateEntity
                    });
                }

                var parentPolicyGraphNodeEntity = createdStateInfo.ParentPolicyGraphNodeEntity;
                if (parentPolicyGraphNodeEntity != default)
                {
                    // Create forward link
                    CreateActionResult(EntityManager,  policyGraphNodeEntity, createdStateInfo.ActionNodeEntity,
                        createdStateInfo.Probability, createdStateInfo.TransitionUtilityValue);

                    // Create reverse link
                    // First, check for existing parent link
                    var policyGraphParents = EntityManager.GetBuffer<PolicyGraphNodeParent>(policyGraphNodeEntity);
                    var parentLinkExists = false;
                    for (var p = 0; p < policyGraphParents.Length; p++)
                    {
                        if (policyGraphParents[p].ParentPolicyGraphNodeEntity.Equals(parentPolicyGraphNodeEntity))
                        {
                            parentLinkExists = true;
                            break;
                        }
                    }

                    // No existing parent link found. Create one.
                    if (!parentLinkExists)
                    {
                        policyGraphParents.Add(new PolicyGraphNodeParent
                        {
                            ParentPolicyGraphNodeEntity = parentPolicyGraphNodeEntity
                        });
                    }

                    // Add parent node to set of nodes to update.
                    m_PolicyGraphNodesToUpdate.Add((createdStateInfo.ParentStateEntity,
                        createdStateInfo.StateHorizon - 1,
                        parentPolicyGraphNodeEntity));
                }

                // Because the arrays are changing underneath, it's necessary to remove components as we process this list
                EntityManager.RemoveComponent<CreatedStateInfo>(createdStateEntity);
                createdStateEntities = m_CreatedStateInfo.GetEntityArray();
                createdStateHashCodes = m_CreatedStateInfo.GetComponentDataArray<HashCode>();
                createdStateInfos = m_CreatedStateInfo.GetComponentDataArray<CreatedStateInfo>();
            }

            Backup();
        }

        void Backup()
        {
            var policyGraphNodeLookup = GetComponentDataFromEntity<PolicyGraphNode>();
            var actionNodeLookup = GetComponentDataFromEntity<ActionNode>();

            while (m_PolicyGraphNodesToUpdate.Count > 0)
            {
                var nextToUpdate = m_PolicyGraphNodesToUpdate.Max; // Grab items at the farthest horizon first
                m_PolicyGraphNodesToUpdate.Remove(nextToUpdate);

                var policyGraphNodeEntity = nextToUpdate.Item3;
                var policyGraphNode = policyGraphNodeLookup[policyGraphNodeEntity];

                // Get all action nodes for the state and update
                UpdateActionValues(policyGraphNodeEntity, actionNodeLookup, policyGraphNodeLookup);

                if (UpdatePolicyValue(ref policyGraphNode, policyGraphNodeEntity, actionNodeLookup))
                {
                    policyGraphNodeLookup[policyGraphNodeEntity] = policyGraphNode;
                    var policyGraphParents = EntityManager.GetBuffer<PolicyGraphNodeParent>(policyGraphNodeEntity);
                    for (var p = 0; p < policyGraphParents.Length; p++)
                    {
                        var parentPolicyGraphNodeEntity = policyGraphParents[p].ParentPolicyGraphNodeEntity;
                        var parentNode = policyGraphNodeLookup[parentPolicyGraphNodeEntity];

                        m_PolicyGraphNodesToUpdate.Add((parentNode.StateEntity, parentNode.Horizon,
                            parentPolicyGraphNodeEntity));
                    }
                }
            }
        }

        bool UpdatePolicyValue(ref PolicyGraphNode node, Entity pgnEntity, ComponentDataFromEntity<ActionNode> actionNodeLookup)
        {
            var actionNodeReferenceBuffer = EntityManager.GetBuffer<ActionNodeReference>(pgnEntity);

            if (actionNodeReferenceBuffer.Length == 0)
                // No child nodes, therefore a leaf node. Do not change policy value, as it is the heuristic value.
                return false;

            // Store previous node value for comparison
            float lastValue = node.PolicyValue;
            var lastOptimalActionEntity = node.OptimalActionEntity;

            // Pick max action
            Entity optimalActionEntity = default;
            float maxValue = float.MinValue;
            bool nodeCompleteStart = node.Complete;
            var nodeComplete = true;
            for (var i = 0; i < actionNodeReferenceBuffer.Length; i++)
            {
                var actionNodeEntity = actionNodeReferenceBuffer[i].ActionNodeEntity;
                var actionNode = actionNodeLookup[actionNodeEntity];
                if (actionNode.ActionValue > maxValue)
                {
                    optimalActionEntity = actionNodeEntity;
                    maxValue = actionNode.ActionValue;
                }

                if (!actionNode.Complete)
                    nodeComplete = false;
            }

            var policyValue = k_DiscountFactor * maxValue;
            node.PolicyValue = policyValue;
            node.OptimalActionEntity = optimalActionEntity;
            node.Complete = nodeComplete;

            // Check if freshly expanded node. If so, set initial iteration count.
            if (node.Iterations == 0)
                node.Iterations = actionNodeReferenceBuffer.Length;

            // Don't continue updating upward when the node has not changed in a significant way
            return node.OptimalActionEntity != lastOptimalActionEntity || Math.Abs(policyValue - lastValue) > k_PolicyValueTolerance || nodeCompleteStart != nodeComplete;
        }

        void UpdateActionValues(Entity policyGraphNodeEntity, ComponentDataFromEntity<ActionNode> actionNodeLookup, ComponentDataFromEntity<PolicyGraphNode> policyGraphNodeLookup)
        {
            var actionNodeReferenceBuffer = EntityManager.GetBuffer<ActionNodeReference>(policyGraphNodeEntity);
            for (var i = 0; i < actionNodeReferenceBuffer.Length; i++)
            {
                var actionNodeEntity = actionNodeReferenceBuffer[i].ActionNodeEntity;
                var actionNode = actionNodeLookup[actionNodeEntity];
                actionNode.ActionValue = 0f;
                actionNode.Complete = true;

                var actionResultBuffer = EntityManager.GetBuffer<ActionResult>(actionNodeEntity);
                for(var actionIndex = 0; actionIndex < actionResultBuffer.Length; actionIndex++)
                {
                    var actionResult = actionResultBuffer[actionIndex];

                    var policyNode = policyGraphNodeLookup[actionResult.PolicyGraphNodeEntity];
                    actionNode.ActionValue += actionResult.Probability * (actionResult.TransitionUtilityValue
                        + policyNode.PolicyValue);

                    if (!policyNode.Complete)
                        actionNode.Complete = false;
                }

                // Write back component data
                actionNodeLookup[actionNodeEntity] = actionNode;
            }
        }
    }
}
