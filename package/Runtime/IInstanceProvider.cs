using System.Collections.Generic;

namespace ResourceManagement
{
    public interface IInstanceProvider
    {
        /// <summary>
        /// Determind whether or not this provider can provide for the given <paramref name="loadProvider"/> and <paramref name="location"/>
        /// </summary>
        /// <returns><c>true</c>, if provide instance was caned, <c>false</c> otherwise.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to instantiate.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation location)
        where TObject : UnityEngine.Object;

        /// <summary>
        /// Asynchronously nstantiate the given <paramref name="location"/>
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to instantiate.</param>
        /// <param name="loadDependencyOperation">Async operation for dependency loading.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : UnityEngine.Object;

        /// <summary>
        /// Releases the instance.
        /// </summary>
        /// <returns><c>true</c>, if instance was released, <c>false</c> otherwise.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to release.</param>
        /// <param name="instance">Object instance to release.</param>
        bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation location, UnityEngine.Object instance);
    }
}
