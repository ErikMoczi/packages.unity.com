
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IPersistentObject : IRegistryObject, IVersioned
    {
        /// <summary>
        /// Unity object asset name
        /// </summary>
        new string Name { get; set; }
        
        /// <summary>
        /// Handle to the unity asset guid
        /// </summary>
        string PersistenceId { get; set; }
      
        /// <summary>
        /// Asset format version
        /// </summary>
        int SerializedVersion { get; set; }
        
        /// <summary>
        /// Enumerates all property container objects within this persistent object
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPropertyContainer> EnumerateContainers();
    }
}
