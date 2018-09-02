using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Contains enough information to load an asset (what/where/how/dependencies)
    /// </summary>
    public interface IResourceLocation
    {
        /// <summary>
        /// Internal name used by the provider to load this location
        /// </summary>
        /// <value>The identifier.</value>
        string InternalId { get; }

        /// <summary>
        /// Matches the provider used to provide/load this location
        /// </summary>
        /// <value>The provider id.</value>
        string ProviderId { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        IList<IResourceLocation> Dependencies { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        bool HasDependencies { get; }

    }
}
