using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Planner;
using Unity.AI.Planner.Agent;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine.Serialization;


namespace UnityEngine.AI.Planner.Agent
{
    /// <summary>
    /// Abstract base class for an agent
    /// </summary>
    /// <typeparam name="TAgent">Agent type</typeparam>
    public abstract class BaseAgent<TAgent> : MonoBehaviour where TAgent : class
    {
        /// <summary>
        /// The controller which executes plans for the agent.
        /// </summary>
        public Controller<TAgent> Controller => m_Controller;

#pragma warning disable 0649
        [SerializeField]
        [FormerlySerializedAs("m_Domain")]
        PlanningDomainData m_DomainData;
#pragma warning restore 0649

        Controller<TAgent> m_Controller;
        protected EntityManager m_EntityManager;

        /// <summary>
        /// Returns domain objects and/or the state entity which possess a set of required traits
        /// </summary>
        /// <param name="stateEntity">The state entity containing objects to be filtered and returned</param>
        /// <param name="traitFilter">The set of traits which objects must possess to be returned</param>
        /// <returns>Returns domain objects and/or the state entity which possess a set of required traits</returns>
        public IEnumerable<Entity> GetObjectEntities(Entity stateEntity, params Type[] traitFilter)
        {
            var traitMask = m_DomainData.GetTraitMask(traitFilter);

            var domainObjectReferences = m_EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            foreach (var domainObjectReference in domainObjectReferences.AsNativeArray())
            {
                var domainObjectEntity = domainObjectReference.DomainObjectEntity;
                var hashCode = m_EntityManager.GetComponentData<HashCode>(domainObjectEntity);
                if ((hashCode.TraitMask & traitMask) == traitMask)
                    yield return domainObjectEntity;
            }

            var stateHashCode = m_EntityManager.GetComponentData<HashCode>(stateEntity);
            if ((stateHashCode.TraitMask & traitMask) == traitMask)
                yield return stateEntity;
        }

        /// <summary>
        /// Returns the entity of the domain object which is assigned the given ID
        /// </summary>
        /// <param name="stateEntity">Entity for the state containing the domain object</param>
        /// <param name="id">ID of the domain object entity to be returned</param>
        /// <returns>Entity that matches the DomainObjectID</returns>
        public Entity GetDomainObjectEntityByID(Entity stateEntity, DomainObjectID id)
        {
            var domainObjectReferences = m_EntityManager.GetBuffer<DomainObjectReference>(stateEntity);
            foreach (var domainObjectReference in domainObjectReferences.AsNativeArray())
            {
                var domainObjectEntity = domainObjectReference.DomainObjectEntity;
                var domainObjectTrait = m_EntityManager.GetComponentData<DomainObjectTrait>(domainObjectEntity);
                if (domainObjectTrait.ID == id)
                    return domainObjectEntity;
            }

            return default;
        }

        /// <summary>
        /// Returns the trait data for a given object entity
        /// </summary>
        /// <param name="objectEntity">Entity for the object from which the trait data is to be read</param>
        /// <typeparam name="T">Trait type</typeparam>
        /// <returns>The specified trait data</returns>
        public T GetObjectTrait<T>(Entity objectEntity) where T : struct, ITrait<T>
        {
            return m_EntityManager.GetComponentData<T>(objectEntity);
        }

        /// <summary>
        /// Sets the given trait data on the given object entity
        /// </summary>
        /// <param name="objectEntity">Entity for the object which the trait data is to be set</param>
        /// <param name="traitData">Trait data to be set on the given object entity</param>
        /// <typeparam name="T">Trait type</typeparam>
        public void SetObjectTrait<T>(Entity objectEntity, T traitData) where T : struct, ITrait<T>
        {
            var hashCode = m_EntityManager.GetComponentData<HashCode>(objectEntity);
            var oldTrait = m_EntityManager.GetComponentData<T>(objectEntity);
            hashCode.Value += traitData.GetHashCode() - oldTrait.GetHashCode();

            m_EntityManager.SetComponentData(objectEntity, hashCode);
            m_EntityManager.SetComponentData(objectEntity, traitData);
        }

        /// <summary>
        /// Overridable method to specify a heuristic
        /// </summary>
        /// <param name="stateEntity">Entity for the state that a heuristic will be provided</param>
        /// <returns>Value estimate</returns>
        protected virtual float Heuristic(Entity stateEntity) => 0f;

        void Start()
        {
            m_DomainData.Initialize();
            InitializeController();
        }

        (Entity, PolicyGraphContainer) InitializePlannerSystem(Func<Entity, float> heuristic = null)
        {

            var world = new World(m_DomainData.Name); //plannerSystem.ActiveWorld);plannerSystem.ActiveWorld;
            m_EntityManager = world.GetOrCreateManager<EntityManager>();
            var plannerSystem = PlannerSystem.Initialize(world);//m_DomainData.Name);

            var initialStateEntity = m_DomainData.GetInitialState(m_EntityManager);
            var updateSystem = m_DomainData.GetPolicyGraphUpdateSystem(world);
            updateSystem.StateTerminations = m_DomainData.StateTerminations;
            updateSystem.StateEvaluationHeuristic = heuristic ?? (state => 0);

            var policy = new PolicyGraphContainer(128, updateSystem, world);

            plannerSystem.PolicyGraph = policy;
            updateSystem.PolicyGraph = policy;
            policy.Reset(initialStateEntity);

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.AllWorlds.ToArray());
            return (initialStateEntity, policy);
        }

        void InitializeController()
        {
            var (initialStateEntity, policy) = InitializePlannerSystem(Heuristic);

            var actionMapping = m_DomainData.ActionMapping.ToDictionary(k => k.Key,
                k => k.Value == null ? NOOPAction<TAgent>.Instance : k.Value as IOperationalAction<TAgent>);

            m_Controller = new Controller<TAgent>(policy, initialStateEntity, this as TAgent, actionMapping);
        }

        void OnDestroy()
        {
            if (m_DomainData is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Overridable Update method (called once per frame)
        /// </summary>
        protected virtual void Update()
        {
            Controller.Update();
        }
    }
}
