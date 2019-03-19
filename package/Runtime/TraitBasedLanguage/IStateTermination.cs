using System;
using Unity.Collections;
using Unity.Entities;


namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// The interface for state termination criteria
    /// </summary>
    public interface IStateTermination : IDisposable
    {
        /// <summary>
        /// The set of components used to filter domain objects on which the termination check operates
        /// </summary>
        NativeArray<ComponentType> ComponentTypes { get; }

        /// <summary>
        /// Determines if the state meets termination criteria if a domain object satisfies the required conditions
        /// </summary>
        /// <param name="entityManager">The entity manager for the world in which the entity exists</param>
        /// <param name="domainObjectEntity">The domain object entity for which the termination criteria is evaluated</param>
        /// <returns>Whether the termination criteria is satisfied for the given domain object</returns>
        bool ShouldTerminate(EntityManager entityManager, Entity domainObjectEntity);
    }
}
