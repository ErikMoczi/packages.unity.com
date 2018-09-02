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
        static List<IAsyncOperation> m_initializationOperations = new List<IAsyncOperation>();
        static Action m_initializationComplete;
        //used to look up locations of instatiated objects in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> m_instanceToLocationMap = new Dictionary<object, IResourceLocation>();

        //used to look up locations of assets in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> m_assetToLocationMap = new Dictionary<object, IResourceLocation>();

        static public bool m_postEvents = true;

        /// <summary>
        /// Event that can be used to determine when the initialization operations have completed
        /// </summary>
        /// <value>The event.</value>
        static public event Action initializationComplete
        {
            add
            {
                if (m_initializationOperations.Count == 0)
                {
                    try
                    {
                        value();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else
                    m_initializationComplete += value;
            }
            remove
            {
                m_initializationComplete -= value;
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

            if (!(op.context is IResourceLocation))
                Debug.LogError("IAsyncOperation.context is not an IResourceLocation for " + loc.id + ", op.context=" + op.context);

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
        static Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>> GetInstantiateAsyncInternalFunc<TObject>() where TObject : Object
        {
            object res;
            if (!m_instantiateAsyncInternalCache.TryGetValue(typeof(TObject), out res))
                m_instantiateAsyncInternalCache.Add(typeof(TObject), res = (Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>>)InstantiateAsync_Internal<TObject>);
            return res as Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>>;
        }

        static Dictionary<Type, object> m_instantiateAsyncInternalMapCache = new Dictionary<Type, object>();
        static IAsyncOperation<TObject> InstantiateAsync_Internal<TObject>(IResourceLocation loc, InstantiationParams instParams)
            where TObject : Object
        {
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncRequest, loc, Time.frameCount);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, loc, Time.frameCount);

            var groupOp = StartLoadGroupOperation(loc.dependencies, GetLoadAsyncInternalFunc<object>(), null);
            var op = instanceProvider.ProvideInstanceAsync<TObject>(GetResourceProvider<TObject>(loc), loc, groupOp, instParams);
            if(!(op.context is IResourceLocation))
                Debug.LogError("IAsyncOperation.context is not an IResourceLocation for " + loc.id + ", op.context="+op.context);

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
        /// Gets the list of configured <see cref="IResourceLocator"/> objects. Resource Locators are used to find <see cref="IResourceLocation"/> objects from user-defined typed keys.
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
        public static void QueueInitializationOperation(IAsyncOperation op)
        {
            m_initializationOperations.Add(op);
            op.completed += (op2) => RemoveInitializationOperation(op2);
        }

        internal static void RemoveInitializationOperation(IAsyncOperation op)
        {
            m_initializationOperations.Remove(op);
            if (m_initializationOperations.Count == 0 && m_initializationComplete != null)
                m_initializationComplete();
        }

        /// <summary>
        /// Resolve an <paramref name="key"/> to an <see cref="IResourceLocation"/>
        /// </summary>
        /// <returns>The resource location.</returns>
        /// <param name="key">key to resolve.</param>
        /// <typeparam name="TKey">The key type</typeparam>
        public static IResourceLocation GetResourceLocation<TKey>(TKey key)
        {
            if (key is IResourceLocation)
                return key as IResourceLocation;
            for (int i = 0; i < resourceLocators.Count; i++)
            {
                var locator = resourceLocators[i] as IResourceLocator<TKey>;
                if (locator == null)
                    continue;

                var l = locator.Locate(key);
                if (l != null)
                    return l as IResourceLocation;
            }
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
            return null;
        }

        /// <summary>
        /// Release resources belonging to the <paramref name="obj"/> at the specified <paramref name="key"/>.
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

        class LocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            int dependencyCount;
            TKey m_key;
            Func<IResourceLocation, IAsyncOperation<TObject>> m_asyncFunc;
            public IAsyncOperation<TObject> Start(TKey key, List<IAsyncOperation> dependencies, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunc)
            {
                m_key = key;
                m_asyncFunc = asyncFunc;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    m_asyncFunc(GetResourceLocation(m_key)).completed += (loadOp) =>
                    {
                        SetResult(loadOp.result);
                        InvokeCompletionEvent();
                        AsyncOperationCache.Instance.Release<TObject>(this);
                    };
                }
            }
        }

        class InstanceLocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            int dependencyCount;
            TKey m_key;
            Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>> m_asyncFunc;
            InstantiationParams m_instParams;

            public IAsyncOperation<TObject> Start(TKey key, List<IAsyncOperation> dependencies, Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>> asyncFunc, InstantiationParams instParams)
            {
                m_key = key;
                m_instParams = instParams;
                m_asyncFunc = asyncFunc;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    m_asyncFunc(GetResourceLocation(m_key), m_instParams).completed += (loadOp) =>
                    {
                        SetResult(loadOp.result);
                        InvokeCompletionEvent();
                        AsyncOperationCache.Instance.Release<TObject>(this);
                    };
                }
            }
        }

        class InstanceLocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            int dependencyCount;
            IList<TKey> m_keys;
            InstantiationParams m_instParams;
            Action<IAsyncOperation<TObject>> m_callback;
            Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParams, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, List<IAsyncOperation> dependencies, Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParams, IAsyncOperation<IList<TObject>>> asyncFunc, Action<IAsyncOperation<TObject>> callback, InstantiationParams instParams)
            {
                m_instParams = instParams;
                m_callback = callback;
                m_keys = keys;
                m_asyncFunc = asyncFunc;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));

                    m_asyncFunc(locList, m_callback, m_instParams).completed += (loadOp) =>
                    {
                        SetResult(loadOp.result);
                        InvokeCompletionEvent();
                        AsyncOperationCache.Instance.Release<TObject>(this);
                    };
                }
            }
        }


        static IAsyncOperation<TObject> StartInternalAsyncOp<TObject, TKey>(TKey key, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunc)
        {
            var loc = GetResourceLocation(key);
            if (loc == null && m_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<LocationOperation<TKey, TObject>, object>();
                return locationOp.Start(key, m_initializationOperations, asyncFunc);
            }

            return asyncFunc(loc);
        }

        static IAsyncOperation<TObject> StartInternalAsyncInstantiateOp<TObject, TKey>(TKey key, Func<IResourceLocation, InstantiationParams, IAsyncOperation<TObject>> asyncFunc, InstantiationParams instParams)
        {
            var loc = GetResourceLocation(key);
            if (loc == null && m_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<InstanceLocationOperation<TKey, TObject>, object>();
                return locationOp.Start(key, m_initializationOperations, asyncFunc, instParams);
            }

            return asyncFunc(loc, instParams);
        }

        class LocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            int dependencyCount;
            IList<TKey> m_keys;
            Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, List<IAsyncOperation> dependencies, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
            {
                m_keys = keys;
                m_asyncFunc = asyncFunc;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));

                    m_asyncFunc(locList).completed += (loadOp) =>
                    {
                        SetResult(loadOp.result);
                        InvokeCompletionEvent();
                        AsyncOperationCache.Instance.Release<TObject>(this);
                    };
                }
            }
        }

        static IAsyncOperation<IList<TObject>> StartInternalAsyncOp<TObject, TKey>(IList<TKey> keyList, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunc)
        {
            if (m_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<LocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keyList, m_initializationOperations, asyncFunc);
            }

            var locList = new List<IResourceLocation>(keyList.Count);
            foreach (var key in keyList)
                locList.Add(GetResourceLocation(key));

            return asyncFunc(locList);
        }

        /// <summary>
        /// Load the <typeparamref name="TObject"/> at the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="key">key to load.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<TObject> LoadAsync<TObject, TKey>(TKey key)
            where TObject : class
        {
            return StartInternalAsyncOp(key, GetLoadAsyncInternalFunc<TObject>());
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
        /// Asynchronously load all objects in the given collection of <paramref name="keys"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="keys">key to load.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TObject">Object type to load.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<IList<TObject>> LoadAllAsync<TObject, TKey>(IList<TKey> keys, Action<IAsyncOperation<TObject>> callback)
            where TObject : class
        {
            return StartInternalAsyncOp(keys, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<TObject>(), callback); });
        }

        /// <summary>
        /// Asynchronously loads only the dependencies for the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="key">key for which to load dependencies.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAsync<TKey>(TKey key, Action<IAsyncOperation<object>> callback)
        {
            return PreloadDependenciesAllAsync(new List<TKey> { key }, callback);
        }

        /// <summary>
        /// Asynchronously loads only the dependencies for a collection of <paramref name="keys"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async load operations are complete.</returns>
        /// <param name="keys">Collection of keys for which to load dependencies.</param>
        /// <param name="callback">This callback will be invoked once for each object that is loaded.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAllAsync<TKey>(IList<TKey> keys, Action<IAsyncOperation<object>> callback)
        {
            List<IResourceLocation> dependencyLocations = new List<IResourceLocation>();
            foreach (TKey adr in keys)
            {
                IResourceLocation loc = GetResourceLocation(adr);
                dependencyLocations.AddRange(loc.dependencies);
            }

            return StartInternalAsyncOp(dependencyLocations, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<object>(), callback); });
        }

        /// <summary>
        /// Asynchronouslly instantiate a prefab (GameObject) at the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation that will complete when the prefab is instantiated.</returns>
        /// <param name="key">key of the prefab.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        internal static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, InstantiationParams instParams)
            where TObject : Object
        {
            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), instParams);
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Transform parent = null, bool instantiateInWorldSpace = false) where TObject : Object
        {
            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), new InstantiationParams(parent, instantiateInWorldSpace));
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Vector3 position, Quaternion rotation, Transform parent = null) where TObject : Object
        {
            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), new InstantiationParams(position, rotation, parent));
        }

        /// <summary>
        /// Instantiate all prefabs (GameObjects) in the given collection of <paramref name="keys"/>.
        /// </summary>
        /// <returns>An async operation that will complete when all individual async instatiate operations are complete.</returns>
        /// <param name="keys">Collection of keys to instantiate.</param>
        /// <param name="callback">This callback will be invoked once for each object that is instantiated.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TKey>(IList<TKey> keys, Action<IAsyncOperation<TObject>> callback, Transform parent = null, bool instantiateInWorldSpace = false)
            where TObject : Object
        {
            return InstantiateAllAsync<TObject, TKey>(keys, callback, new InstantiationParams(parent, instantiateInWorldSpace));
        }

        internal static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TKey>(IList<TKey> keys, Action<IAsyncOperation<TObject>> callback, InstantiationParams instParams)
            where TObject : Object
        {
            if (m_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<InstanceLocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keys, m_initializationOperations, GetInstantiateAllAsyncInternalFunc<TObject>(), callback, instParams);
            }

            var locList = new List<IResourceLocation>(keys.Count);
            foreach (var key in keys)
                locList.Add(GetResourceLocation(key));

            return GetInstantiateAllAsyncInternalFunc<TObject>()(locList, callback, instParams);
        }

        static Dictionary<Type, object> m_instantiateAllAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParams, IAsyncOperation<IList<TObject>>> GetInstantiateAllAsyncInternalFunc<TObject>() where TObject : Object
        {
            object res;
            if (!m_instantiateAllAsyncInternalCache.TryGetValue(typeof(TObject), out res))
                m_instantiateAllAsyncInternalCache.Add(typeof(TObject), res = (Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>> , InstantiationParams, IAsyncOperation<IList<TObject>>>)InstantiateAllAsync_Internal<TObject>);
            return res as Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParams, IAsyncOperation<IList<TObject>>>;
        }

        static IAsyncOperation<IList<TObject>> InstantiateAllAsync_Internal<TObject>(IList<IResourceLocation> locs, Action<IAsyncOperation<TObject>> callback, InstantiationParams instParams) where TObject : Object
        {
            var instAllOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            return instAllOp.Start(locs, (loc) => { return InstantiateAsync_Internal<TObject>(loc, instParams); }, callback);
        }


        /// <summary>
        /// Asynchronously loads the scene a the given <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation for the scene.</returns>
        /// <param name="key">key of the scene to load.</param>
        /// <param name="loadMode">Scene Load mode.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<Scene> LoadSceneAsync<TKey>(TKey key, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            return StartInternalAsyncOp(key, (IResourceLocation loc) => {
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
        /// <param name="key">key of the scene to unload.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<Scene> UnloadSceneAsync<TKey>(TKey key)
        {
            return StartInternalAsyncOp(key, sceneProvider.ReleaseSceneAsync);
        }
    }
}
