using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement
{
    public interface ISceneProvider
    {
        /// <summary>
        /// Asynchronously loads a scene.
        /// </summary>
        /// <returns>An async operation for the scene.</returns>
        /// <param name="location">Location to load.</param>
        /// <param name="loadDependencyOperation">Async load operation for scene dependencies.</param>
        /// <param name="loadMode">Scene load mode.</param>
        IAsyncOperation<Scene> ProvideSceneAsync(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode);

        /// <summary>
        /// Release any resources associated with the scene at the given <typeparamref name="loc"/>
        /// </summary>
        /// <returns>An async operation for the scene, completed when the scene is unloaded.</returns>
        /// <param name="location">Location to unload.</param>
        IAsyncOperation<Scene> ReleaseSceneAsync(IResourceLocation location, Scene scene);
    }
}
