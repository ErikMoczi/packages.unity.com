using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public static class ResourceManager
    {

        public class UnknownResourceProviderException : Exception
        {
            public IResourceLocation Location { get; private set; }
            public UnknownResourceProviderException(IResourceLocation location)
            {
                Location = location;
            }
        }
        public class UnknownResourceLocationException<TAddress> : Exception
        {
            public TAddress Address { get; private set; }
            public UnknownResourceLocationException(TAddress address)
            {
                Address = address;
            }
        }
        public class ResourceProviderFailedException : Exception
        {
            public IResourceLocation Location { get; private set; }
            public IResourceProvider Provider { get; private set; }
            public IAsyncOperation<IList<object>> DependencyOperation { get; private set; }

            public ResourceProviderFailedException(IResourceProvider provider, IResourceLocation location, IAsyncOperation<IList<object>> dependencyOperation)
            {
                Provider = provider;
                Location = location;
                DependencyOperation = dependencyOperation;
            }
        }

        static List<IResourceLocator> s_resourceLocators = new List<IResourceLocator>();
        static List<IResourceProvider> s_resourceProviders = new List<IResourceProvider>();
        static List<IAsyncOperation> s_initializationOperations = new List<IAsyncOperation>();
        static Action s_initializationComplete;
        //used to look up locations of instatiated objects in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> s_instanceToLocationMap = new Dictionary<object, IResourceLocation>();

        //used to look up locations of assets in order to find the provider when they are released
        static Dictionary<object, IResourceLocation> s_assetToLocationMap = new Dictionary<object, IResourceLocation>();

        static public bool s_postEvents = true;

        /// <summary>
        /// Event that can be used to determine when the initialization operations have completed
        /// </summary>
        /// <value>The event.</value>
        static public event Action InitializationComplete
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (s_initializationOperations.Count == 0)
                    DelayedActionManager.AddAction(value, null);
                else
                    s_initializationComplete += value;
            }
            remove
            {
                s_initializationComplete -= value;
            }
        }

        static void Release_Internal<TObject>(IResourceLocation location, TObject asset)
            where TObject : class
        {
            Debug.Assert(location != null, "ResourceManager.Release_Internal - location == null.");
            if (location.Dependencies != null)
            {
                for (int i = 0; i < location.Dependencies.Count; i++)
                    Release_Internal(location.Dependencies[i], default(object));
            }

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.Release, location, Time.frameCount);
            GetResourceProvider<TObject>(location).Release(location, asset);
        }

        static Dictionary<Type, object> s_loadAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IResourceLocation, IAsyncOperation<TObject>> GetLoadAsyncInternalFunc<TObject>() where TObject : class
        {
            Debug.Assert(s_loadAsyncInternalCache != null, "ResourceManager.GetLoadAsyncInternalFunc - s_loadAsyncInternalCache == null.");

            object result;
            if (!s_loadAsyncInternalCache.TryGetValue(typeof(TObject), out result))
                s_loadAsyncInternalCache.Add(typeof(TObject), result = (Func<IResourceLocation, IAsyncOperation<TObject>>)LoadAsync_Internal<TObject>);
            return result as Func<IResourceLocation, IAsyncOperation<TObject>>;
        }

        static Dictionary<Type, object> s_loadAsyncInternalMapCache = new Dictionary<Type, object>();
        static IAsyncOperation<TObject> LoadAsync_Internal<TObject>(IResourceLocation location)
            where TObject : class
        {
            Debug.Assert(s_loadAsyncInternalMapCache != null, "ResourceManager.LoadAsync_Internal - s_loadAsyncInternalMapCache == null.");
            if (location == null)
                throw new ArgumentNullException("location");

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, location, Time.frameCount);
            var groupOp = StartLoadGroupOperation(location.Dependencies, GetLoadAsyncInternalFunc<object>(), null);
            Debug.Assert(groupOp != null, "ResourceManager.LoadAsync_Internal - groupOp == null.");

            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                throw new UnknownResourceProviderException(location);

            var operation = provider.ProvideAsync<TObject>(location, groupOp);
            operation.Validate();
            if (operation == null)
                throw new ResourceProviderFailedException(provider, location, groupOp);

            //Debug.Assert(operation.Context is IResourceLocation, "IAsyncOperation.context is not an IResourceLocation for " + location.InternalId + ", op.context=" + operation.Context);

            object result;
            if (!s_loadAsyncInternalMapCache.TryGetValue(typeof(TObject), out result))
            {
                Action<IAsyncOperation<TObject>> action = (op2) =>
                {
                    if (op2.Result != null && !s_assetToLocationMap.ContainsKey(op2.Result))
                        s_assetToLocationMap.Add(op2.Result, op2.Context as IResourceLocation);
                };
                s_loadAsyncInternalMapCache.Add(typeof(TObject), result = action);
            }

            operation.Completed += result as Action<IAsyncOperation<TObject>>;
            return operation;
        }

        static Dictionary<Type, object> s_instantiateAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> GetInstantiateAsyncInternalFunc<TObject>() where TObject : Object
        {
            Debug.Assert(s_instantiateAsyncInternalCache != null, "ResourceManager.GetInstantiateAsyncInternalFunc - s_instantiateAsyncInternalCache == null.");

            object result;
            if (!s_instantiateAsyncInternalCache.TryGetValue(typeof(TObject), out result))
                s_instantiateAsyncInternalCache.Add(typeof(TObject), result = (Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>>)InstantiateAsync_Internal<TObject>);
            return result as Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>>;
        }

        static Dictionary<Type, object> s_instantiateAsyncInternalMapCache = new Dictionary<Type, object>();
        static IAsyncOperation<TObject> InstantiateAsync_Internal<TObject>(IResourceLocation location, InstantiationParameters instantiationParameters)
            where TObject : Object
        {
            Debug.Assert(s_instantiateAsyncInternalMapCache != null, "ResourceManager.InstantiateAsync_Internal - s_instantiateAsyncInternalMapCache == null.");

            if (location == null)
                throw new ArgumentNullException("location");

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncRequest, location, Time.frameCount);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, location, Time.frameCount);

            var groupOp = StartLoadGroupOperation(location.Dependencies, GetLoadAsyncInternalFunc<object>(), null);
            Debug.Assert(groupOp != null, "ResourceManager.InstantiateAsync_Internal - groupOp == null.");
            groupOp.Validate();

            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                throw new UnknownResourceProviderException(location);

            var operation = InstanceProvider.ProvideInstanceAsync<TObject>(provider, location, groupOp, instantiationParameters);
            if (operation == null)
                throw new ResourceProviderFailedException(provider, location, groupOp);

            Debug.Assert(operation.Context is IResourceLocation, "IAsyncOperation.context is not an IResourceLocation for " + location.InternalId + ", op.context=" + operation.Context);
            operation.Validate();

            object result;
            if (!s_instantiateAsyncInternalMapCache.TryGetValue(typeof(TObject), out result))
            {
                Action<IAsyncOperation<TObject>> action = (op2) =>
                {
                    if (op2.Result != null)
                    {
                        if (!s_instanceToLocationMap.ContainsKey(op2.Result))
                        {
                            s_instanceToLocationMap.Add(op2.Result, op2.Context as IResourceLocation);
                        }
                        else
                        {
                            Debug.Log("FAILED Added instance " + op2.Result.GetInstanceID() + " loc: " + op2.Context);
                        }
                    }
                };
                s_instantiateAsyncInternalMapCache.Add(typeof(TObject), result = action);
            }

            operation.Completed += result as Action<IAsyncOperation<TObject>>;
            operation.Validate();
            return operation;
        }

       // static Dictionary<Type, object> s_releaseCache = new Dictionary<Type, object>();
        static LoadGroupOperation<TObject> StartLoadGroupOperation<TObject>(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunction, Action<IAsyncOperation<TObject>> onComplete)
            where TObject : class
        {
       //     Debug.Assert(s_releaseCache != null, "ResourceManager.StartLoadGroupOperation - s_releaseCache == null.");

            LoadGroupOperation<TObject> groupOp;

            if (locations != null && locations.Count > 0)
                groupOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            else
                groupOp = AsyncOperationCache.Instance.Acquire<EmptyGroupOperation<TObject>, TObject>();
            /*
            object releaseAction = null;
            if (!s_releaseCache.TryGetValue(typeof(TObject), out releaseAction))
                s_releaseCache.Add(typeof(TObject), releaseAction = (Action<IAsyncOperation<IList<TObject>>>)AsyncOperationCache.Instance.Release<TObject>);
                */
            groupOp.Start(locations, loadFunction, onComplete);//.Completed += releaseAction as Action<IAsyncOperation<IList<TObject>>>;
            return groupOp;
        }

        /// <summary>
        /// Gets the list of configured <see cref="IResourceLocator"/> objects. Resource Locators are used to find <see cref="IResourceLocation"/> objects from user-defined typed keys.
        /// </summary>
        /// <value>The resource locators list.</value>
        public static IList<IResourceLocator> ResourceLocators
        {
            get
            {
                Debug.Assert(s_resourceLocators != null);
                return s_resourceLocators;
            }
        }

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
        /// Queues a resource locator load operation. While there are unfinished load operatiosn for resource locations, all provide
        /// load operations will be defered until the queue is empty.
        /// </summary>
        /// <param name="operation">An IAsyncOperation that loads an IResourceLocator</param>
        public static void QueueInitializationOperation(IAsyncOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            s_initializationOperations.Add(operation);
            operation.Completed += (op2) => RemoveInitializationOperation(op2);
        }

        internal static void RemoveInitializationOperation(IAsyncOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");
            s_initializationOperations.Remove(operation);
            if (s_initializationOperations.Count == 0 && s_initializationComplete != null)
                s_initializationComplete();
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
            for (int i = 0; i < ResourceLocators.Count; i++)
            {
                var locator = ResourceLocators[i] as IResourceLocator<TKey>;
                if (locator == null)
                    continue;

                var l = locator.Locate(key);
                if (l != null)
                    return l as IResourceLocation;
            }
            return null;
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
            for (int i = 0; i < ResourceProviders.Count; i++)
            {
                var p = ResourceProviders[i];
                if (p.CanProvide<TObject>(location))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Release resources belonging to the <paramref name="asset"/> at the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="asset">Object to release.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        public static void Release<TObject>(TObject asset)
            where TObject : class
        {
            Debug.Assert(s_assetToLocationMap != null, "ResourceManager.Release - s_assetToLocationMap == null.");
            if (asset == null)
                throw new ArgumentNullException("asset");

            IResourceLocation loc = null;
            if (!s_assetToLocationMap.TryGetValue(asset, out loc))
            {
                Debug.LogWarning("Unable to find location for instantiated object " + asset);
                return;
            }

            Release_Internal(loc, asset);
        }

        /// <summary>
        /// Releases resources belonging to the prefab instance.
        /// </summary>
        /// <param name="instance">Instance to release.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        public static void ReleaseInstance<TObject>(TObject instance)
            where TObject : Object
        {
            Debug.Assert(s_instanceToLocationMap != null, "ResourceManager.ReleaseInstance - s_instanceToLocationMap == null.");

            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");
            if (instance == null)
                throw new ArgumentNullException("instance");
            IResourceLocation loc = null;

            if (!s_instanceToLocationMap.TryGetValue(instance, out loc))
            {
                Debug.LogWarning("Unable to find location for instantiated object " + instance.GetInstanceID());
                return;
            }


            s_instanceToLocationMap.Remove(instance);
            if (loc.Dependencies != null)
            {
                for (int i = 0; i < loc.Dependencies.Count; i++)
                    Release_Internal(loc.Dependencies[i], default(object));
            }

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseInstance, loc, Time.frameCount);
            InstanceProvider.ReleaseInstance(GetResourceProvider<TObject>(loc), loc, instance);
        }

        class LocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            int dependencyCount;
            TKey m_key;
            Func<IResourceLocation, IAsyncOperation<TObject>> m_asyncFunc;
            public IAsyncOperation<TObject> Start(TKey key, List<IAsyncOperation> dependencies, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunction)
            {
                Validate();
                m_key = key;
                m_asyncFunc = asyncFunction;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.Completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                Validate();
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    m_asyncFunc(GetResourceLocation(m_key)).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                }
            }
        }

        class InstanceLocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            int dependencyCount;
            TKey m_key;
            Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> m_asyncFunc;
            InstantiationParameters m_instParams;

            public IAsyncOperation<TObject> Start(TKey key, List<IAsyncOperation> dependencies, Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> asyncFunction, InstantiationParameters instantiateParameters)
            {
                Validate();
                m_key = key;
                m_instParams = instantiateParameters;
                m_asyncFunc = asyncFunction;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.Completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                Validate();
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    m_asyncFunc(GetResourceLocation(m_key), m_instParams).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                }
            }
        }

        class InstanceLocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            int dependencyCount;
            IList<TKey> m_keys;
            InstantiationParameters m_instParams;
            Action<IAsyncOperation<TObject>> m_callback;
            Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, List<IAsyncOperation> dependencies, Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>> asyncFunction, Action<IAsyncOperation<TObject>> callback, InstantiationParameters instantiateParameters)
            {
                Validate();
                m_instParams = instantiateParameters;
                m_callback = callback;
                m_keys = keys;
                m_asyncFunc = asyncFunction;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.Completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation operation)
            {
                Validate();
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));

                    m_asyncFunc(locList, m_callback, m_instParams).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                }
            }
        }


        static IAsyncOperation<TObject> StartInternalAsyncOp<TObject, TKey>(TKey key, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunction)
        {
            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");
            var loc = GetResourceLocation(key);
            if (loc == null && s_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<LocationOperation<TKey, TObject>, object>();
                return locationOp.Start(key, s_initializationOperations, asyncFunction);
            }

            return asyncFunction(loc);
        }

        static IAsyncOperation<TObject> StartInternalAsyncInstantiateOp<TObject, TKey>(TKey key, Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> asyncFunction, InstantiationParameters instantiateParameters)
        {
            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");
            var loc = GetResourceLocation(key);
            if (loc == null && s_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<InstanceLocationOperation<TKey, TObject>, object>();
                return locationOp.Start(key, s_initializationOperations, asyncFunction, instantiateParameters);
            }

            return asyncFunction(loc, instantiateParameters);
        }

        class LocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            int dependencyCount;
            IList<TKey> m_keys;
            Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, List<IAsyncOperation> dependencies, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunction)
            {
                Validate();
                m_keys = keys;
                m_asyncFunc = asyncFunction;
                dependencyCount = dependencies.Count;
                foreach (var d in dependencies)
                    d.Completed += OnDependenciesComplete;
                return this;
            }

            void OnDependenciesComplete(IAsyncOperation op)
            {
                Validate();
                dependencyCount--;
                if (dependencyCount == 0)
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));

                    m_asyncFunc(locList).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                }
            }
        }

        static IAsyncOperation<IList<TObject>> StartInternalAsyncOp<TObject, TKey>(IList<TKey> keyList, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunction)
        {
            Debug.Assert(s_initializationOperations != null, "ResourceManager.StartInternalAsyncOp - s_initializationOperations == null.");

            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");

            if (s_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<LocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keyList, s_initializationOperations, asyncFunction);
            }

            var locList = new List<IResourceLocation>(keyList.Count);
            foreach (var key in keyList)
                locList.Add(GetResourceLocation(key));

            return asyncFunction(locList);
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
            return StartInternalAsyncOp(key, GetLoadAsyncInternalFunc<TObject>()).Acquire();
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
            return GetLoadAsyncInternalFunc<TObject>()(location).Acquire();
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
            if (keys == null)
                throw new ArgumentNullException("keys");

            return StartInternalAsyncOp(keys, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<TObject>(), callback); }).Acquire();
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
            return PreloadDependenciesAllAsync(new List<TKey> { key }, callback).Acquire();
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
            if (keys == null)
                throw new ArgumentNullException("keys");

            List<IResourceLocation> dependencyLocations = new List<IResourceLocation>();
            foreach (TKey adr in keys)
            {
                IResourceLocation loc = GetResourceLocation(adr);
                dependencyLocations.AddRange(loc.Dependencies);
            }

            return StartInternalAsyncOp(dependencyLocations, (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<object>(), callback); }).Acquire();
        }

        /// <summary>
        /// Asynchronouslly instantiate a prefab (GameObject) at the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Async operation that will complete when the prefab is instantiated.</returns>
        /// <param name="key">key of the prefab.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        /// <typeparam name="TKey">key type.</typeparam>
        internal static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), instantiateParameters).Acquire();
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Transform parent = null, bool instantiateInWorldSpace = false) where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), new InstantiationParameters(parent, instantiateInWorldSpace)).Acquire();
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Vector3 position, Quaternion rotation, Transform parent = null) where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), new InstantiationParameters(position, rotation, parent)).Acquire();
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
            if(keys == null)
                throw new ArgumentNullException("keys");

            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");

            return InstantiateAllAsync<TObject, TKey>(keys, callback, new InstantiationParameters(parent, instantiateInWorldSpace)).Acquire();
        }

        internal static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TKey>(IList<TKey> keys, Action<IAsyncOperation<TObject>> callback, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            Debug.Assert(s_initializationOperations != null, "ResourceManager.InstantiateAllAsync - s_initializationOperations == null.");

            if (s_initializationOperations.Count > 0)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<InstanceLocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keys, s_initializationOperations, GetInstantiateAllAsyncInternalFunc<TObject>(), callback, instantiateParameters);
            }

            var locList = new List<IResourceLocation>(keys.Count);
            foreach (var key in keys)
                locList.Add(GetResourceLocation(key));

            return GetInstantiateAllAsyncInternalFunc<TObject>()(locList, callback, instantiateParameters);
        }

        static Dictionary<Type, object> s_instantiateAllAsyncInternalCache = new Dictionary<Type, object>();
        static Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>> GetInstantiateAllAsyncInternalFunc<TObject>() where TObject : Object
        {
            Debug.Assert(s_instantiateAllAsyncInternalCache != null, "ResourceManager.GetInstantiateAllAsyncInternalFunc - s_instantiateAllAsyncInternalCache == null.");

            object res;
            if (!s_instantiateAllAsyncInternalCache.TryGetValue(typeof(TObject), out res))
                s_instantiateAllAsyncInternalCache.Add(typeof(TObject), res = (Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>> , InstantiationParameters, IAsyncOperation<IList<TObject>>>)InstantiateAllAsync_Internal<TObject>);
            return res as Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>>;
        }

        static IAsyncOperation<IList<TObject>> InstantiateAllAsync_Internal<TObject>(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, InstantiationParameters instantiateParameters) where TObject : Object
        {
            var instAllOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            return instAllOp.Start(locations, (loc) => { return InstantiateAsync_Internal<TObject>(loc, instantiateParameters); }, callback);
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
            if (SceneProvider == null)
                throw new NullReferenceException("ResourceManager.SceneProvider is null.  Assign a valid ISceneProvider object before using.");

            return StartInternalAsyncOp(key, (IResourceLocation location) => {
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncRequest, location, 1);
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, location, 0);

                    var groupOp = StartLoadGroupOperation(location.Dependencies, GetLoadAsyncInternalFunc<object>(), null);
                    return SceneProvider.ProvideSceneAsync(location, groupOp, loadMode);
                }).Acquire();
        }

        /// <summary>
        /// Asynchronously unloads the scene.
        /// </summary>
        /// <returns>Async operation for the scene unload.</returns>
        /// <param name="key">key of the scene to unload.</param>
        /// <typeparam name="TKey">key type.</typeparam>
        public static IAsyncOperation<Scene> UnloadSceneAsync<TKey>(TKey key)
        {
            if (SceneProvider == null)
                throw new NullReferenceException("ResourceManager.SceneProvider is null.  Assign a valid ISceneProvider object before using.");

            return StartInternalAsyncOp(key, SceneProvider.ReleaseSceneAsync).Acquire();
        }
    }
}
