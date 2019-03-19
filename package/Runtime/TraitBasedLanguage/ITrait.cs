using System;
using Unity.Entities;
using Unity.Properties;

namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// The interface denoting that the container is a trait. Base interface for <see cref="ITrait{T}"/>.
    /// </summary>
    public interface ITrait : IComponentData, IPropertyContainer
    {
        /// <summary>
        /// Sets the component on a given domain object
        /// </summary>
        /// <param name="entityManager">The entity manager for the world containing the domain object entity</param>
        /// <param name="domainObjectEntity">The domain object for which the trait is set</param>
        void SetComponentData(EntityManager entityManager, Entity domainObjectEntity);

        /// <summary>
        /// Sets the flag corresponding to the trait in an object's trait mask
        /// </summary>
        /// <param name="entityManager">The entity manager for the world in which the domain object entity exists</param>
        /// <param name="domainObjectEntity">The domain object entity for which the trait is set</param>
        void SetTraitMask(EntityManager entityManager, Entity domainObjectEntity);
    }

    /// <summary>
    /// The interface denoting the container is a trait
    /// </summary>
    /// <typeparam name="T">Trait type</typeparam>
    public interface ITrait<T> : ITrait, IStructPropertyContainer<T> where T : struct, IPropertyContainer {}
}
