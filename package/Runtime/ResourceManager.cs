using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Entry point for ResourceManager API
    /// </summary>
    public static partial class ResourceManager
    {
        static List<IResourceProvider> s_resourceProviders = new List<IResourceProvider>();

        /// <summary>
        /// Gets or sets the <see cref="IInstanceProvider"/>. The instance provider handles instatiating and releasing prefabs.
        /// </summary>
        /// <value>The instance provider.</value>
        public static IInstanceProvider InstanceProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISceneProvider"/>. The scene provider handles load and release operations for scenes.
        /// </summary>
        /// <value>The scene provider.</value>
        public static ISceneProvider SceneProvider { get; set; }

        /// <summary>
        /// Global exception handler.  This will be called whenever an IAsyncOperation.OperationException is set to a non-null value.
        /// </summary>
        public static Action<IAsyncOperation, Exception> ExceptionHandler { get; set; }

        /// <summary>
        /// Gets the list of configured <see cref="IResourceProvider"/> objects. Resource Providers handle load and release operations for <see cref="IResourceLocation"/> objects.
        /// </summary>
        /// <value>The resource providers list.</value>
        public static IList<IResourceProvider> ResourceProviders
        {
            get
            {
                Debug.Assert(s_resourceProviders != null);
                return s_resourceProviders;
            }
        }

        /// <summary>
        /// Used to set the function that will be used to resolve any runtime variables embedded in location internal ids.
        /// </summary>
        public static Func<string, string> OnResolveInternalId { internal get; set; }
        public static string ResolveInternalId(string id)
        {
            if (OnResolveInternalId == null)
                return id;
            return OnResolveInternalId(id);
        }


        /// <summary>
        /// Gets the appropriate <see cref="IResourceProvider"/> for the given <paramref name="location"/>.
        /// </summary>
        /// <returns>The resource provider.</returns>
        /// <param name="location">The resource location.</param>
        /// <typeparam name="TObject">The desired object type to be loaded from the provider.</typeparam>
        public static IResourceProvider GetResourceProvider<TObject>(IResourceLocation location)
            where TObject : class
        {
            if (location == null)
                return null;

            for (int i = 0; i < ResourceProviders.Count; i++)
            {
                var p = ResourceProviders[i];
                if (p.CanProvide<TObject>(location))
                    return p;
            }
            return null;
        }


        /// <summary>
        /// Load the <typeparamref name="TObject"/> at the specified <paramref name="location"/>.
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="location">Location to load.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        public static IAsyncOperation<TObject> ProvideResource<TObject>(IResourceLocation location)
            where TObject : class
        {
            if (location == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new ArgumentNullException("location"));

            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new UnknownResourceProviderException(location));
            return provider.Provide<TObject>(location, LoadDependencies(location)).Retain();
        }

        /// <summary>
        /// Asynchronously load all objects in the given collection of <paramref name="keys"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="locations">locations to load.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<IList<TObject>> ProvideResources<TObject>(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback)
            where TObject : class
        {
            if (locations == null)
                return new CompletedOperation<IList<TObject>>().Start(null, locations, null, new ArgumentNullException("locations"));
            return AsyncOperationCache.Instance.Acquire<GroupOperation<TObject>>().Start(locations, callback, ProvideResource<TObject>).Retain();
        }

        /// <summary>
        /// Release resources belonging to the <paramref name="asset"/> at the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="asset">Object to release.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        public static void ReleaseResource<TObject>(TObject asset, IResourceLocation location)
            where TObject : class
        {
            if (location == null)
                return;
            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                return;
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.Release, location, Time.frameCount);
            provider.Release(location, asset);
            if (location.HasDependencies)
            {
                foreach (var dep in location.Dependencies)
                    ReleaseResource<object>(null, dep);
            }
        }

        /// <summary>
        /// Asynchronouslly instantiate a prefab (GameObject) at the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation that will complete when the prefab is instantiated.</returns>
        /// <param name="key">key of the prefab.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<TObject> ProvideInstance<TObject>(IResourceLocation location, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            if (location == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new ArgumentNullException("location"));
            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new UnknownResourceProviderException(location));

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncRequest, location, Time.frameCount);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, location, Time.frameCount);

            return InstanceProvider.ProvideInstanceAsync<TObject>(provider, location, LoadDependencies(location), instantiateParameters).Retain();
        }

        /// <summary>
        /// Asynchronouslly instantiate mullitple prefab (GameObject) at the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation that will complete when the prefab is instantiated.</returns>
        /// <param name="locations">locations of prefab asset</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        public static IAsyncOperation<IList<TObject>> ProvideInstances<TObject>(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            if (locations == null)
                return new CompletedOperation<IList<TObject>>().Start(null, locations, null, new ArgumentNullException("locations"));

            return AsyncOperationCache.Instance.Acquire<GroupOperation<TObject>>().Start(locations, callback, ProvideInstance<TObject>, instantiateParameters).Retain();
        }

        /// <summary>
        /// Releases resources belonging to the prefab instance.
        /// </summary>
        /// <param name="instance">Instance to release.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        public static void ReleaseInstance(Object instance, IResourceLocation location)
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");
            if (location == null)
                return;

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseInstance, location, Time.frameCount);
            if (InstanceProvider.ReleaseInstance(GetResourceProvider<Object>(location), location, instance))
                ReleaseResource<object>(null, location);
        }

        /// <summary>
        /// Asynchronously loads the scene a the given <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation for the scene.</returns>
        /// <param name="key">key of the scene to load.</param>
        /// <param name="loadMode">Scene Load mode.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<Scene> ProvideScene(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            if (SceneProvider == null)
                throw new NullReferenceException("ResourceManager.SceneProvider is null.  Assign a valid ISceneProvider object before using.");
            if (location == null)
                return new CompletedOperation<Scene>().Start(location, location, default(Scene), new ArgumentNullException("location"));

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncRequest, location, 1);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, location, 0);

            return SceneProvider.ProvideSceneAsync(location, LoadDependencies(location), loadMode).Retain();
        }

        /// <summary>
        /// Asynchronously unloads the scene.
        /// </summary>
        /// <param name="scene">The scene to unload.</param>
        /// <param name="location">key of the scene to unload.</param>
        /// <returns>Async operation for the scene unload.</returns>
        public static IAsyncOperation<Scene> ReleaseScene(Scene scene, IResourceLocation location)
        {
            if (SceneProvider == null)
                throw new NullReferenceException("ResourceManager.SceneProvider is null.  Assign a valid ISceneProvider object before using.");
            if (location == null)
                return new CompletedOperation<Scene>().Start(location, location, default(Scene), new ArgumentNullException("location"));
            return SceneProvider.ReleaseSceneAsync(location, scene).Retain();
        }

        /// <summary>
        /// Asynchronously unloads the scene.
        /// </summary>
        /// <param name="location">The location of the scene to unload.</param>
        /// <param name="scene">The scene to unload.</param>
        /// <returns>Async operation for the scene unload.</returns>
        [Obsolete("Use ReleaseScene(Scene scene, IResourceLocation location) instead.  The parameter order has been changed to be consistent with other ResourceManager API.")]
        public static IAsyncOperation<Scene> ReleaseScene(IResourceLocation location, Scene scene)
        {
            return ReleaseScene(scene, location);
        }

        /// <summary>
        /// Asynchronously dependencies of a location.
        /// </summary>
        /// <returns>Async operation for the dependency loads.</returns>
        /// <param name="location">location to load dependencies for.</param>
        public static IAsyncOperation<IList<object>> LoadDependencies(IResourceLocation location)
        {
            if (location == null || !location.HasDependencies)
                return null;
            return ProvideResources<object>(location.Dependencies, null);
        }
    }
}
