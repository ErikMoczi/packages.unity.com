using System;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;

namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// Dynamics systems run on all expanded states independently of actions
    ///
    /// See also: <seealso cref="Unity.AI.Planner.DomainLanguage.TraitBased.BaseAction{T}"/>
    /// </summary>
    [UpdateAfter(typeof(ActionSystemGroup))]
    [DisableAutoCreation]
    public abstract class BaseDynamicsSystem : ComponentSystem
    {
        ComponentGroup m_CreatedStateInfo;

        /// <inheritdoc/>
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CreatedStateInfo = GetComponentGroup(ComponentType.ReadOnly<State>(),
                ComponentType.ReadOnly<CreatedStateInfo>());
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var createdStateEntities = m_CreatedStateInfo.GetEntityArray();
            var createdStateInfos = m_CreatedStateInfo.GetComponentDataArray<CreatedStateInfo>();
            for (var i = 0; i < createdStateEntities.Length; i++)
            {
                var parentStateEntity = createdStateInfos[i].ParentStateEntity;
                var createdStateEntity = createdStateEntities[i];

                OnStateUpdate(parentStateEntity, createdStateEntity);
            }
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
        /// Implement this method in the derived class to modify the newly created state
        /// </summary>
        /// <param name="parentStateEntity">Entity for the predecessor state</param>
        /// <param name="createdStateEntity">Entity for the newly created state that will be modified</param>
        protected abstract void OnStateUpdate(Entity parentStateEntity, Entity createdStateEntity);
    }
}
