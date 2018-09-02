using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using ResourceManagement.Diagnostics;
using ResourceManagement.AsyncOperations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceManagement
{
    public static class ResourceManager
    {
        static List<IResourceLocator> m_resourceLocators = new List<IResourceLocator>();
        static List<IResourceProvider> m_resourceProviders = new List<IResourceProvider>();
        static List<IAsyncOperation<IResourceLocator>> m_resourceLocatorLoadOperations = new List<IAsyncOperation<IResourceLocator>>();

        class InternalAsyncOp<TObject> : AsyncOperationBase<TObject>
        {
            public InternalAsyncOp() : base("") {}

            public IAsyncOperation<TObject> Start(IAsyncOperation<IResourceLocation> locationOp, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunc)
            {
                locationOp.completed += (IAsyncOperation<IResourceLocation> op) =>
                    {
                        asyncFunc(op.result).completed += OnComplete;
                    };

                return this;
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                SetResult(op.result);
                InvokeCompletionEvent(this);
            }
        }

        class InternalAsyncListOp<TObject> : AsyncOperationBase<IList<TObject>>
        {
            public InternalAsyncListOp() : base("") {}

            public IAsyncOperation<IList<TObject>> Start(IAsyncOperation<IList<IResourceLocation>> locationOp, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
            {
                locationOp.completed += (IAsyncOperation<IList<IResourceLocation>> op) =>
                    {
                        asyncFunc(op.result).completed += OnComplete;
                    };

                return this;
            }

            void OnComplete(IAsyncOperation<IList<TObject>> op)
            {
                SetResult(op.result);
                InvokeCompletionEvent(this);
            }
        }

        static void Release_Internal<TObject>(IResourceLocation loc, TObject obj)
            where TObject : class
        {
            foreach (var dep in loc.dependencies)
                Release_Internal(dep, default(object));

            ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.Release, loc, Time.frameCount);
            GetResourceProvider<TObject>(loc).Release(loc, obj);
        }

        static IAsyncOperation<TObject> LoadAsync_Internal<TObject>(IResourceLocation loc)
            where TObject : class
        {
            ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.LoadAsyncRequest, loc, Time.frameCount);
            var groupOp = StartLoadGroupOperation(loc.dependencies, LoadAsync_Internal<object>, null);
            return GetResourceProvider<TObject>(loc).ProvideAsync<TObject>(loc, groupOp);
        }

        static IAsyncOperation<TObject> InstantiateAsync_Internal<TObject>(IResourceLocation loc)
            where TObject : Object
        {
            ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.InstantiateAsyncRequest, loc, Time.frameCount);
            ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.LoadAsyncRequest, loc, Time.frameCount);

            var groupOp = StartLoadGroupOperation(loc.dependencies, LoadAsync_Internal<object>, null);
            return instanceProvider.ProvideInstanceAsync<TObject>(GetResourceProvider<TObject>(loc), loc, groupOp);
        }

        static IAsyncOperation<TObject> StartInternalAsyncOp<TObject, TAddress>(TAddress address, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunc)
        {
            if (m_resourceLocatorLoadOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<ResourceLocationLoadOperation<TAddress>, object>();
                var asyncOp = AsyncOperationCache.Instance.Acquire<InternalAsyncOp<TObject>, TObject>();
                return asyncOp.Start(locationOp.Start(address, m_resourceLocatorLoadOperations, resourceLocators, GetResourceLocation), asyncFunc);
            }

            return asyncFunc(GetResourceLocation(address));
        }

        static IAsyncOperation<IList<TObject>> StartInternalAsyncOp<TObject, TAddress>(ICollection<TAddress> addressList, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
        {
            if (m_resourceLocatorLoadOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<ResourceLocationCollectionLoadOperation<TAddress>, object>();
                var asyncOp = AsyncOperationCache.Instance.Acquire<InternalAsyncListOp<TObject>, TObject>();
                return asyncOp.Start(locationOp.Start(addressList, m_resourceLocatorLoadOperations, resourceLocators, GetResourceLocation), asyncFunc);
            }

            var locList = new List<IResourceLocation>(addressList.Count);
            foreach (var address in addressList)
                locList.Add(GetResourceLocation(address));

            return asyncFunc(locList);
        }

        static LoadGroupOperation<TObject> StartLoadGroupOperation<TObject>(ICollection<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
            where TObject : class
        {
            LoadGroupOperation<TObject> groupOp;

            if (locations != null && locations.Count > 0)
                groupOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            else
                groupOp = AsyncOperationCache.Instance.Acquire<EmptyGroupOperation<TObject>, TObject>();

            groupOp.Start(locations, loadFunc, onComplete).completed += AsyncOperationCache.Instance.Release<object>;
            return groupOp;
        }

        /// <summary>
        /// Gets the list of configured <see cref="IResourceLocator"/> objects. Resource Locators are used to find <see cref="IResourceLocation"/> objects from user-defined typed addresses.
        /// </summary>
        /// <value>The resource locators list.</value>
        public static IList<IResourceLocator> resourceLocators { get { return m_resourceLocators; } }

        /// <summary>
        /// Gets the list of configured <see cref="IResourceProvider"/> objects. Resource Providers handle load and release operations for <see cref="IResourceLocation"/> objects.
        /// </summary>
        /// <value>The resource providers list.</value>
        public static IList<IResourceProvider> resourceProviders { get { return m_resourceProviders; } }

        /// <summary>
        /// Gets or sets the <see cref="IInstanceProvider"/>. The instance provider handles instatiating and releasing prefabs.
        /// </summary>
        /// <value>The instance provider.</value>
        public static IInstanceProvider instanceProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISceneProvider"/>. The scene provider handles load and release operations for scenes.
        /// </summary>
        /// <value>The scene provider.</value>
        public static ISceneProvider sceneProvider { get; set; }

        /// <summary>
        /// Queues a resource locator load operation. While there are unfinished load operatiosn for resource locations, all provide
        /// load operations will be defered until the queue is empty.
        /// </summary>
        /// <param name="op">An IAsyncOperation that loads an IResourceLocator</param>
        public static void QueueResourceLocatorLoadOperation(IAsyncOperation<IResourceLocator> op)
        {
            if (!op.isDone)
                m_resourceLocatorLoadOperations.Add(op);
        }

        /// <summary>
        /// Resolve an <paramref name="address"/> to an <see cref="IResourceLocation"/>
        /// </summary>
        /// <returns>The resource location.</returns>
        /// <param name="address">Address to resolve.</param>
        /// <typeparam name="TAddress">The address type</typeparam>
        public static IResourceLocation GetResourceLocation<TAddress>(TAddress address)
        {
            if (address is IResourceLocation)
                return address as IResourceLocation;
            for (int i = 0; i < resourceLocators.Count; i++)
            {
                var locator = resourceLocators[i] as IResourceLocator<TAddress>;
                if (locator == null)
                    continue;

                var l = locator.Locate(address);
                if (l != null)
                    return l as IResourceLocation;
            }
            Debug.Log("Cannot find location for " + address);
            return null;
        }

        /// <summary>
        /// Gets the appropriate <see cref="IResourceProvider"/> for the given <paramref name="loc"/>.
        /// </summary>
        /// <returns>The resource provider.</returns>
        /// <param name="loc">The resource location.</param>
        /// <typeparam name="TObject">The desired object type to be loaded from the provider.</typeparam>
        public static IResourceProvider GetResourceProvider<TObject>(IResourceLocation loc)
            where TObject : class
        {
            for (int i = 0; i < resourceProviders.Count; i++)
            {
                var p = resourceProviders[i];
                if (p.CanProvide<TObject>(loc))
                    return p;
            }
            Debug.Log("Cannot find provider for " + loc);
            return null;
        }

        /// <summary>
        /// Release resources belonging to the <paramref name="obj"/> at the specified <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Address to release.</param>
        /// <param name="obj">Object to release.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static void Release<TObject, TAddress>(TAddress address, TObject obj)
            where TObject : class
        {
            var loc = GetResourceLocation(address);
            Release_Internal(loc, obj);
        }

        /// <summary>
        /// Releases resources belonging to the prefab instance.
        /// </summary>
        /// <param name="address">Address of the prefab.</param>
        /// <param name="inst">Instance to release.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static void ReleaseInstance<TObject, TAddress>(TAddress address, TObject inst)
            where TObject : Object
        {
            var loc = GetResourceLocation(address);

            if (loc.dependencies != null)
                foreach (var dep in loc.dependencies)
                    Release_Internal(dep, default(object));

            ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.ReleaseInstance, loc, Time.frameCount);
            instanceProvider.ReleaseInstance(GetResourceProvider<TObject>(loc), loc, inst);
        }

        /// <summary>
        /// Load the <typeparamref name="TObject"/> at the specified <paramref name="address"/>.
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="address">Address to load.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<TObject> LoadAsync<TObject, TAddress>(TAddress address)
            where TObject : class
        {
            return StartInternalAsyncOp(address, LoadAsync_Internal<TObject>);
        }

        /// <summary>
        /// Load the <typeparamref name="TObject"/> at the specified <paramref name="location"/>.
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="location">Location to load.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        public static IAsyncOperation<TObject> LoadAsync<TObject>(IResourceLocation location)
            where TObject : class
        {
            return LoadAsync_Internal<TObject>(location);
        }

        /// <summary>
        /// Asynchronously load all objects in the given collection of <paramref name="addresses"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="addresses">Address to load.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<TObject>> LoadAllAsync<TObject, TAddress>(ICollection<TAddress> addresses, Action<IAsyncOperation<TObject>> callback)
            where TObject : class
        {
            return StartInternalAsyncOp(addresses, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, LoadAsync_Internal<TObject>, callback); });
        }

        /// <summary>
        /// Asynchronously loads only the dependencies for the specified <paramref name="address"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="address">Address for which to load dependencies.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAsync<TAddress>(TAddress address, Action<IAsyncOperation<object>> callback)
        {
            return PreloadDependenciesAllAsync(new List<TAddress> { address }, callback);
        }

        /// <summary>
        /// Asynchronously loads only the dependencies for a collection of <paramref name="addresses"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="addresses">Collection of addresses for which to load dependencies.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAllAsync<TAddress>(ICollection<TAddress> addresses, Action<IAsyncOperation<object>> callback)
        {
            List<IResourceLocation> dependencyLocations = new List<IResourceLocation>();
            foreach (TAddress adr in addresses)
            {
                IResourceLocation loc = GetResourceLocation(adr);
                dependencyLocations.AddRange(loc.dependencies);
            }

            return StartInternalAsyncOp(dependencyLocations, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, LoadAsync_Internal<object>, callback); });
        }

        /// <summary>
        /// Asynchronouslly instantiate a prefab (GameObject) at the specified <paramref name="address"/>.
        /// </summary>
        /// <returns>Async operation that will complete when the prefab is instantiated.</returns>
        /// <param name="address">Address of the prefab.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TAddress>(TAddress address)
            where TObject : Object
        {
            return StartInternalAsyncOp(address, InstantiateAsync_Internal<TObject>);
        }

        /// <summary>
        /// Instantiate all prefabs (GameObjects) in the given collection of <paramref name="addresses"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async instatiate operations are complete.</returns>
        /// <param name="addresses">Collection of addresses to instantiate.</param>
        /// <param name="callback">This callback will be invoked once for each object that is instantiated.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TAddress>(ICollection<TAddress> addresses, Action<IAsyncOperation<TObject>> callback)
            where TObject : Object
        {
            return StartInternalAsyncOp(addresses, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, InstantiateAsync_Internal<TObject>, callback); });
        }

        /// <summary>
        /// Asynchronously loads the scene a the given <paramref name="address"/>.
        /// </summary>
        /// <returns>Async operation for the scene.</returns>
        /// <param name="address">Address of the scene to load.</param>
        /// <param name="loadMode">Scene Load mode.</param>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<Scene> LoadSceneAsync<TAddress>(TAddress address, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            return StartInternalAsyncOp(address, (IResourceLocation loc) => {
                    ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.LoadSceneAsyncRequest, loc, 1);
                    ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, loc, 0);

                    var groupOp = StartLoadGroupOperation(loc.dependencies, LoadAsync_Internal<object>, null);
                    return sceneProvider.ProvideSceneAsync(loc, groupOp, loadMode);
                });
        }

        /// <summary>
        /// Asynchronously unloads the scene.
        /// </summary>
        /// <returns>Async operation for the scene unload.</returns>
        /// <param name="address">Address of the scene to unload.</param>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<Scene> UnloadSceneAsync<TAddress>(TAddress address)
        {
            return StartInternalAsyncOp(address, sceneProvider.ReleaseSceneAsync);
        }
    }
}
