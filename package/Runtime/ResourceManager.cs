using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using ResourceManagement.Util;
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

        //used to look up locations of instatiated objects in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> m_instanceToLocationMap = new Dictionary<object, IResourceLocation>();

        //used to look up locations of assets in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> m_assetToLocationMap = new Dictionary<object, IResourceLocation>();

        static public bool m_postEvents = true;
        class InternalAsyncOp<TObject> : AsyncOperationBase<TObject>
        {
            Action<IAsyncOperation<TObject>> action;
            public IAsyncOperation<TObject> Start(IAsyncOperation<IResourceLocation> locationOp, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunc)
            {
                m_context = locationOp.context;

                if (action == null)
                    action = (op2) =>
                    {
                        SetResult(op2.result);
                        InvokeCompletionEvent();
                    };

                locationOp.completed += (IAsyncOperation<IResourceLocation> op) =>
                    {
                        asyncFunc(op.result).completed += action;
                    };

                return this;
            }
        }

        class InternalAsyncListOp<TObject> : AsyncOperationBase<IList<TObject>>
        {
            Action<IAsyncOperation<IList<TObject>>> action;
            public IAsyncOperation<IList<TObject>> Start(IAsyncOperation<IList<IResourceLocation>> locationOp, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
            {
                m_context = locationOp.context;

                if (action == null)
                    action = (op2) =>
                    {
                        SetResult(op2.result);
                        InvokeCompletionEvent();
                    };

                locationOp.completed += (IAsyncOperation<IList<IResourceLocation>> op) =>
                    {
                        asyncFunc(op.result).completed += action;
                    };

                return this;
            }
        }

        static void Release_Internal<TObject>(IResourceLocation loc, TObject obj)
            where TObject : class
        {
            if (loc.dependencies != null)
            {
                for (int i = 0; i < loc.dependencies.Count; i++)
                    Release_Internal(loc.dependencies[i], default(object));
            }

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.Release, loc, Time.frameCount);
            GetResourceProvider<TObject>(loc).Release(loc, obj);
        }

        static Dictionary<Type, object> m_loadAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IResourceLocation, IAsyncOperation<TObject>> GetLoadAsyncInternalFunc<TObject>() where TObject : class
        {
            object res;
            if (!m_loadAsyncInternalCache.TryGetValue(typeof(TObject), out res))
                m_loadAsyncInternalCache.Add(typeof(TObject), res = (Func<IResourceLocation, IAsyncOperation<TObject>>)LoadAsync_Internal<TObject>);
            return res as Func<IResourceLocation, IAsyncOperation<TObject>>;
        }


        static Dictionary<Type, object> m_loadAsyncInternalMapCache = new Dictionary<Type, object>();
        static IAsyncOperation<TObject> LoadAsync_Internal<TObject>(IResourceLocation loc)
            where TObject : class
        {
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, loc, Time.frameCount);
            var groupOp = StartLoadGroupOperation(loc.dependencies, GetLoadAsyncInternalFunc<object>(), null);
            var op = GetResourceProvider<TObject>(loc).ProvideAsync<TObject>(loc, groupOp);

            Debug.Assert(op.context is IResourceLocation, "IAsyncOperation.context is not an IResourceLocation for " + loc.id);

            object res;
            if (!m_loadAsyncInternalMapCache.TryGetValue(typeof(TObject), out res))
            {
                Action<IAsyncOperation<TObject>> action = (op2) =>
                {
                    if (op2.result != null && !m_assetToLocationMap.ContainsKey(op2.result))
                        m_assetToLocationMap.Add(op2.result, op2.context as IResourceLocation);
                };
                m_loadAsyncInternalMapCache.Add(typeof(TObject), res = action);
            }

            op.completed += res as Action<IAsyncOperation<TObject>>;
            return op;
        }

        static Dictionary<Type, object> m_instantiateAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IResourceLocation, IAsyncOperation<TObject>> GetInstantiateAsyncInternalFunc<TObject>() where TObject : Object
        {
            object res;
            if (!m_instantiateAsyncInternalCache.TryGetValue(typeof(TObject), out res))
                m_instantiateAsyncInternalCache.Add(typeof(TObject), res = (Func<IResourceLocation, IAsyncOperation<TObject>>)InstantiateAsync_Internal<TObject>);
            return res as Func<IResourceLocation, IAsyncOperation<TObject>>;
        }

        static Dictionary<Type, object> m_instantiateAsyncInternalMapCache = new Dictionary<Type, object>();
        static IAsyncOperation<TObject> InstantiateAsync_Internal<TObject>(IResourceLocation loc)
            where TObject : Object
        {
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncRequest, loc, Time.frameCount);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, loc, Time.frameCount);

            var groupOp = StartLoadGroupOperation(loc.dependencies, GetLoadAsyncInternalFunc<object>(), null);
            var op = instanceProvider.ProvideInstanceAsync<TObject>(GetResourceProvider<TObject>(loc), loc, groupOp);

            Debug.Assert(op.context is IResourceLocation, "IAsyncOperation.context is not an IResourceLocation for " + loc.id);

            object res;
            if (!m_instantiateAsyncInternalMapCache.TryGetValue(typeof(TObject), out res))
            {
                Action<IAsyncOperation<TObject>> action = (op2) =>
                {
                    if (op2.result != null && !m_instanceToLocationMap.ContainsKey(op2.result))
                        m_instanceToLocationMap.Add(op2.result, op2.context as IResourceLocation);
                };
                m_instantiateAsyncInternalMapCache.Add(typeof(TObject), res = action);
            }

            op.completed += res as Action<IAsyncOperation<TObject>>;
            return op;
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

        static IAsyncOperation<IList<TObject>> StartInternalAsyncOp<TObject, TAddress>(IList<TAddress> addressList, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
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

        static Dictionary<Type, object> m_releaseCache = new Dictionary<Type, object>();
        static LoadGroupOperation<TObject> StartLoadGroupOperation<TObject>(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
            where TObject : class
        {
            LoadGroupOperation<TObject> groupOp;

            if (locations != null && locations.Count > 0)
                groupOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            else
                groupOp = AsyncOperationCache.Instance.Acquire<EmptyGroupOperation<TObject>, TObject>();
            
            object releaseAction = null;
            if (!m_releaseCache.TryGetValue(typeof(TObject), out releaseAction))
                m_releaseCache.Add(typeof(TObject), releaseAction = (Action<IAsyncOperation<IList<TObject>>>)AsyncOperationCache.Instance.Release<TObject>);

            groupOp.Start(locations, loadFunc, onComplete).completed += releaseAction as Action<IAsyncOperation<IList<TObject>>>;
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
        /// <param name="obj">Object to release.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        public static void Release<TObject>(TObject obj)
            where TObject : class
        {
            IResourceLocation loc = null;
            if (!m_assetToLocationMap.TryGetValue(obj, out loc))
            {
                Debug.LogWarning("Unable to find location for instantiated object " + obj);
                return;
            }

            Release_Internal(loc, obj);
        }

        /// <summary>
        /// Releases resources belonging to the prefab instance.
        /// </summary>
        /// <param name="inst">Instance to release.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        public static void ReleaseInstance<TObject>(TObject inst)
            where TObject : Object
        {
            IResourceLocation loc = null;
            if (!m_instanceToLocationMap.TryGetValue(inst, out loc))
            {
                Debug.LogWarning("Unable to find location for instantiated object " + inst);
                return;
            }

            m_instanceToLocationMap.Remove(inst);
            if (loc.dependencies != null)
            {
                for (int i = 0; i < loc.dependencies.Count; i++)
                    Release_Internal(loc.dependencies[i], default(object));
            }

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseInstance, loc, Time.frameCount);
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
            return StartInternalAsyncOp(address, GetLoadAsyncInternalFunc<TObject>());
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
            return GetLoadAsyncInternalFunc<TObject>()(location);
        }

        /// <summary>
        /// Asynchronously load all objects in the given collection of <paramref name="addresses"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="addresses">Address to load.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<TObject>> LoadAllAsync<TObject, TAddress>(IList<TAddress> addresses, Action<IAsyncOperation<TObject>> callback)
            where TObject : class
        {
            return StartInternalAsyncOp(addresses, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<TObject>(), callback); });
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
        public static IAsyncOperation<IList<object>> PreloadDependenciesAllAsync<TAddress>(IList<TAddress> addresses, Action<IAsyncOperation<object>> callback)
        {
            List<IResourceLocation> dependencyLocations = new List<IResourceLocation>();
            foreach (TAddress adr in addresses)
            {
                IResourceLocation loc = GetResourceLocation(adr);
                dependencyLocations.AddRange(loc.dependencies);
            }

            return StartInternalAsyncOp(dependencyLocations, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<object>(), callback); });
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
            return StartInternalAsyncOp(address, GetInstantiateAsyncInternalFunc<TObject>());
        }

        /// <summary>
        /// Instantiate all prefabs (GameObjects) in the given collection of <paramref name="addresses"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async instatiate operations are complete.</returns>
        /// <param name="addresses">Collection of addresses to instantiate.</param>
        /// <param name="callback">This callback will be invoked once for each object that is instantiated.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TAddress">Address type.</typeparam>
        public static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TAddress>(IList<TAddress> addresses, Action<IAsyncOperation<TObject>> callback)
            where TObject : Object
        {
            return StartInternalAsyncOp(addresses, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetInstantiateAsyncInternalFunc<TObject>(), callback); });
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
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncRequest, loc, 1);
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, loc, 0);

                    var groupOp = StartLoadGroupOperation(loc.dependencies, GetLoadAsyncInternalFunc<object>(), null);
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
