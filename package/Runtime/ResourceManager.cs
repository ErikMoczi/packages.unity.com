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
            public override string Message
            {
                get
                {
                    return base.Message + ", Location=" + Location;
                }
            }
        }
        public class UnknownResourceLocationException<TAddress> : Exception
        {
            public TAddress Address { get; private set; }
            public UnknownResourceLocationException(TAddress address)
            {
                Address = address;
            }
            public override string Message
            {
                get
                {
                    return base.Message + ", Address=" + Address;
                }
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
            public override string Message
            {
                get
                {
                    return base.Message + ", Provider=" + Provider + ", Location=" + Location;
                }
            }
        }

        static List<IResourceLocator> s_resourceLocators = new List<IResourceLocator>();
        static List<IResourceProvider> s_resourceProviders = new List<IResourceProvider>();
        static Action s_initializationCompletionCallback;
        static bool s_initializationCompleted = false;

        static Dictionary<int, HashSet<GameObject>> s_sceneToInstanceIds = new Dictionary<int, HashSet<GameObject>>();
        public static void OnSceneUnloaded(Scene scene)
        {
            HashSet<GameObject> instanceIds = null;
            if (s_sceneToInstanceIds.TryGetValue(scene.GetHashCode(), out instanceIds))
            {
                foreach (var go in instanceIds)
                {
                    var i = go.GetInstanceID();
                    IResourceLocation loc;
                    if (s_instanceToLocationMap.TryGetValue(i, out loc))
                    {
                        if (!s_instanceToSceneMap.Remove(i))
                            Debug.LogFormat("Scene not found for instance {0}", i);
                        s_instanceToLocationMap.Remove(i);
                        ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseInstance, loc, Time.frameCount);

                        if (InstanceProvider.ReleaseInstance(GetResourceProvider<object>(loc), loc, null))
                            Release_Internal<Object>(loc, null);
                    }
                    else
                    {
                        //object has already been released
                        Debug.LogFormat("Object instance {0} has already been released.", i);
                    }
                }
                s_sceneToInstanceIds.Remove(scene.GetHashCode());
            }
        }

        static void RecordInstanceSceneChange(GameObject go, int previousScene, int currentScene)
        {
            HashSet<GameObject> instanceIds = null;
            if (!s_sceneToInstanceIds.TryGetValue(previousScene, out instanceIds))
                Debug.LogFormat("Unable to find instance table for instance {0}.", go.GetInstanceID());
            else
                instanceIds.Remove(go);
            if (!s_sceneToInstanceIds.TryGetValue(currentScene, out instanceIds))
                s_sceneToInstanceIds.Add(currentScene, instanceIds = new HashSet<GameObject>());
            instanceIds.Add(go);

            s_instanceToSceneMap[go.GetInstanceID()] = currentScene;

        }
        /// <summary>
        /// Notify the ResourceManager that a tracked instance has changed scenes so that it can be released properly when the scene is unloaded.
        /// </summary>
        public static void RecordInstanceSceneChange(GameObject go, Scene previousScene, Scene currentScene)
        {
            RecordInstanceSceneChange(go, previousScene.GetHashCode(), currentScene.GetHashCode());
        }

        /// <summary>
        /// This is used to tell the resourcemanager that all initialization events have completed. It will normally be called from loading catalogs, but tests will need to call it before running.
        /// </summary>

        static public void SetReady()
        {
            if(!s_initializationCompleted)
                DelayedActionManager.AddAction((Action)InvokeReadyCallbacks);
        }

        static void InvokeReadyCallbacks()
        {
            s_initializationCompleted = true;
            try
            {
                if (s_initializationCompletionCallback != null)
                    s_initializationCompletionCallback();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            s_initializationCompletionCallback = null;
        }

        //used to look up locations of instatiated objects in order to find the provider when they are released
        static Dictionary<int, IResourceLocation> s_instanceToLocationMap = new Dictionary<int, IResourceLocation>();
        static Dictionary<int, int> s_instanceToSceneMap = new Dictionary<int, int>();

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

                if (s_initializationCompleted)
                    DelayedActionManager.AddAction(value, 0);
                else
                    s_initializationCompletionCallback += value;
            }
            remove
            {
                s_initializationCompletionCallback -= value;
            }
        }

        static void Release_Internal<TObject>(IResourceLocation location, TObject asset)
            where TObject : class
        {
            Debug.Assert(location != null, "ResourceManager.Release_Internal - location == null.");

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.Release, location, Time.frameCount);
            GetResourceProvider<TObject>(location).Release(location, asset);
            if (location.Dependencies != null)
            {
                for (int i = 0; i < location.Dependencies.Count; i++)
                    Release_Internal(location.Dependencies[i], default(object));
            }
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
            if (operation == null)
                throw new ResourceProviderFailedException(provider, location, groupOp);
            operation.Validate();

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
            if (location == null)
                throw new ArgumentNullException("location");

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncRequest, location, Time.frameCount);
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncRequest, location, Time.frameCount);

            var groupOp = StartLoadGroupOperation(location.Dependencies, GetLoadAsyncInternalFunc<object>(), null);
            groupOp.Validate();

            var provider = GetResourceProvider<TObject>(location);
            if (provider == null)
                throw new UnknownResourceProviderException(location);

            var operation = InstanceProvider.ProvideInstanceAsync<TObject>(provider, location, groupOp, instantiationParameters);
            if (operation == null)
                throw new ResourceProviderFailedException(provider, location, groupOp);

            operation.Validate();

            object result;
            if (!s_instantiateAsyncInternalMapCache.TryGetValue(typeof(TObject), out result))
            {
                Action<IAsyncOperation<TObject>> action = (op2) =>
                {
                    if (op2.Result != null)
                    {
                        if (!s_instanceToLocationMap.ContainsKey(op2.Result.GetInstanceID()))
                        {
                            s_instanceToLocationMap.Add(op2.Result.GetInstanceID(), op2.Context as IResourceLocation);
                        }
                        else
                        {
                            Debug.LogErrorFormat("s_instanceToLocationMap.Add failed for {0}, context={1}", op2.Result.GetInstanceID(), op2.Context);
                        }
                        var go = op2.Result as GameObject;
                        if (go != null)
                        {
                            Scene targetScene = go.scene;
                            HashSet<GameObject> instancesInScene;
                            if (!s_sceneToInstanceIds.TryGetValue(targetScene.GetHashCode(), out instancesInScene))
                                s_sceneToInstanceIds.Add(targetScene.GetHashCode(), instancesInScene = new HashSet<GameObject>());
                            instancesInScene.Add(go);
                            s_instanceToSceneMap.Add(op2.Result.GetInstanceID(), targetScene.GetHashCode());
                        }
                    }
                };
                s_instantiateAsyncInternalMapCache.Add(typeof(TObject), result = action);
            }

            operation.Completed += result as Action<IAsyncOperation<TObject>>;
            operation.Validate();
            return operation;
        }

        static LoadGroupOperation<TObject> StartLoadGroupOperation<TObject>(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunction, Action<IAsyncOperation<TObject>> onComplete)
            where TObject : class
        {
            LoadGroupOperation<TObject> groupOp;

            if (locations != null && locations.Count > 0)
            {
                groupOp = AsyncOperationCache.Instance.Acquire<LoadGroupOperation<TObject>, TObject>();
            }
            else
            {
                groupOp = AsyncOperationCache.Instance.Acquire<EmptyGroupOperation<TObject>, TObject>();
                AsyncOperationCache.Instance.Release<TObject>(groupOp);//release right away since it does nothing and can be shared...
            }
            groupOp.Start(locations, loadFunction, onComplete);
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
                throw new ArgumentNullException("asset",  "Cannot release null asset.  It is possible that the object has been destroyed.");

            IResourceLocation loc = null;
            if (!s_assetToLocationMap.TryGetValue(asset, out loc))
            {
                var obj = asset as Object;
                if (obj != null && s_instanceToLocationMap.ContainsKey(obj.GetInstanceID()))
                    Debug.LogWarningFormat("ResourceManager.Release() - parameter asset {0} is an instance. Instances must be released using ReleaseInstance().", asset);
                else
                    Debug.LogWarningFormat("ResourceManager.Release() - unable to find location for asset {0}.", asset);
                return;
            }

            Release_Internal(loc, asset);
        }

        public static void ReleaseInstance<TObject>(TObject instance, float delay)
            where TObject : Object
        {
            if (instance == null)
                throw new ArgumentNullException("instance", "Cannot release null instance.  It is possible that the object has been destroyed.");
            if (delay <= 0)
                ReleaseInstance(instance);
            else
                DelayedActionManager.AddAction(new Action<TObject>(ReleaseInstance), delay, instance);
        }

        /// <summary>
        /// Releases resources belonging to the prefab instance.
        /// </summary>
        /// <param name="instance">Instance to release.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        public static void ReleaseInstance<TObject>(TObject instance)
            where TObject : Object
        {
            if (InstanceProvider == null)
                throw new NullReferenceException("ResourceManager.InstanceProvider is null.  Assign a valid IInstanceProvider object before using.");
            if (instance == null)
                throw new ArgumentNullException("instance");
            IResourceLocation loc = null;
            if (!s_instanceToLocationMap.TryGetValue(instance.GetInstanceID(), out loc))
            {
                if (s_assetToLocationMap.ContainsKey(instance))
                    Debug.LogWarningFormat("ResourceManager.ReleaseInstance() - parameter instance {0} is an asset. Assets must be released using Release().", instance.GetInstanceID());
                else
                    Debug.LogWarningFormat("ResourceManager.ReleaseInstance() - unable to find location for instance {0}.", instance.GetInstanceID());
                return;
            }

            int sceneID = 0;
            if (!s_instanceToSceneMap.TryGetValue(instance.GetInstanceID(), out sceneID))
            {
                Debug.LogWarningFormat("Unable to find scene for instance {0}", instance.GetInstanceID());
            }
            else
            {
                var go = instance as GameObject;
                if (go != null)
                {
                    HashSet<GameObject> instances;
                    if (!s_sceneToInstanceIds.TryGetValue(sceneID, out instances))
                    {
                        Debug.LogFormat("Instance {0} was not found in scene table for scene {1},  use ResourceManager.RecordInstanceSceneChange to ensure proper asset reference counts.", instance.GetInstanceID(), sceneID);
                    }
                    else
                    {
                        if (!instances.Remove(go))
                        {
                            Debug.LogFormat("Instance {0} was not found in scene table for scene {1},  use ResourceManager.RecordInstanceSceneChange to ensure proper asset reference counts.", instance.GetInstanceID(), sceneID);
                        }
                    }
                }
                s_instanceToSceneMap.Remove(instance.GetInstanceID());
            }
            
            s_instanceToLocationMap.Remove(instance.GetInstanceID());

            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseInstance, loc, Time.frameCount);
            if (InstanceProvider.ReleaseInstance(GetResourceProvider<TObject>(loc), loc, instance))
                Release_Internal(loc, default(object));
        }

        class LocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            TKey m_key;
            Func<IResourceLocation, IAsyncOperation<TObject>> m_asyncFunc;
            public IAsyncOperation<TObject> Start(TKey key, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunction)
            {
                Validate();
                m_key = key;
                m_asyncFunc = asyncFunction;
                InitializationComplete += () =>
                {
                    m_asyncFunc(GetResourceLocation(m_key)).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                };
                return this;
            }
        }

        class LocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            IList<TKey> m_keys;
            Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunction)
            {
                Validate();
                m_keys = keys;
                m_asyncFunc = asyncFunction;
                InitializationComplete += () =>
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));
                    m_asyncFunc(locList).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                };
                return this;
            }
        }

        class InstanceLocationOperation<TKey, TObject> : AsyncOperationBase<TObject>
        {
            TKey m_key;
            Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> m_asyncFunc;
            InstantiationParameters m_instParams;
            public IAsyncOperation<TObject> Start(TKey key, InstantiationParameters inst, Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> asyncFunction)
            {
                Validate();
                m_instParams = inst;
                m_key = key;
                m_asyncFunc = asyncFunction;
                InitializationComplete += () =>
                {
                    m_asyncFunc(GetResourceLocation(m_key), m_instParams).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                };
                return this;
            }
        }

        class InstanceLocationListOperation<TKey, TObject> : AsyncOperationBase<IList<TObject>>
        {
            IList<TKey> m_keys;
            InstantiationParameters m_instParams;
            Action<IAsyncOperation<TObject>> m_callback;
            Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>> m_asyncFunc;
            public IAsyncOperation<IList<TObject>> Start(IList<TKey> keys, InstantiationParameters instParams, 
                Func<IList<IResourceLocation>, Action<IAsyncOperation<TObject>>, InstantiationParameters, IAsyncOperation<IList<TObject>>> asyncFunction,
                Action<IAsyncOperation<TObject>> callback)
            {
                Validate();
                m_keys = keys;
                m_instParams = instParams;
                m_callback = callback;
                m_asyncFunc = asyncFunction;
                InitializationComplete += () =>
                {
                    var locList = new List<IResourceLocation>(m_keys.Count);
                    foreach (var key in m_keys)
                        locList.Add(GetResourceLocation(key));
                    m_asyncFunc(locList, m_callback, m_instParams).Completed += (loadOp) =>
                    {
                        SetResult(loadOp.Result);
                        InvokeCompletionEvent();
                    };
                };
                return this;
            }
        }


        static IAsyncOperation<TObject> StartInternalAsyncOp<TObject, TKey>(TKey key, Func<IResourceLocation, IAsyncOperation<TObject>> asyncFunction)
        {
            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");
            var loc = GetResourceLocation(key);
            if (loc == null && !s_initializationCompleted)
                return AsyncOperationCache.Instance.Acquire<LocationOperation<TKey, TObject>, object>().Start(key, asyncFunction);

            return asyncFunction(loc);
        }

        static IAsyncOperation<TObject> StartInternalAsyncInstantiateOp<TObject, TKey>(TKey key, Func<IResourceLocation, InstantiationParameters, IAsyncOperation<TObject>> asyncFunction, InstantiationParameters instantiateParameters)
        {
            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");
            var loc = GetResourceLocation(key);
            if (loc == null && !s_initializationCompleted)
                return AsyncOperationCache.Instance.Acquire<InstanceLocationOperation<TKey, TObject>, object>().Start(key, instantiateParameters, asyncFunction);

            return asyncFunction(loc, instantiateParameters);
        }


        static IAsyncOperation<IList<TObject>> StartInternalAsyncOp<TObject, TKey>(IList<TKey> keyList, Func<IList<IResourceLocation>, IAsyncOperation<IList<TObject>>> asyncFunction)
        {
            if (asyncFunction == null)
                throw new ArgumentNullException("asyncFunction");

            if (!s_initializationCompleted)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<LocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keyList, asyncFunction);
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

            var dependencyLocations = new HashSet<IResourceLocation>();
            foreach (TKey adr in keys)
            {
                IResourceLocation loc = GetResourceLocation(adr);
                foreach(var d in loc.Dependencies)
                    dependencyLocations.Add(d);
            }
            return StartInternalAsyncOp(new List<IResourceLocation>(dependencyLocations), (IList<IResourceLocation> locs) => { return StartLoadGroupOperation(locs, GetLoadAsyncInternalFunc<object>(), callback); }).Acquire();
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
            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), instantiateParameters).Acquire();
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Transform parent = null, bool instantiateInWorldSpace = false) where TObject : Object
        {
            return StartInternalAsyncInstantiateOp(key, GetInstantiateAsyncInternalFunc<TObject>(), new InstantiationParameters(parent, instantiateInWorldSpace)).Acquire();
        }

        public static IAsyncOperation<TObject> InstantiateAsync<TObject, TKey>(TKey key, Vector3 position, Quaternion rotation, Transform parent = null) where TObject : Object
        {
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

            return InstantiateAllAsync<TObject, TKey>(keys, callback, new InstantiationParameters(parent, instantiateInWorldSpace)).Acquire();
        }

        internal static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject, TKey>(IList<TKey> keys, Action<IAsyncOperation<TObject>> callback, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            if (!s_initializationCompleted)
            {
                var locationOp = AsyncOperationCache.Instance.Acquire<InstanceLocationListOperation<TKey, TObject>, object>();
                return locationOp.Start(keys, instantiateParameters, GetInstantiateAllAsyncInternalFunc<TObject>(), callback);
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

        static void ValidateSceneInstances()
        {
            var objectsThatNeedToBeFixed = new List<KeyValuePair<int, GameObject>>();
            foreach (var kvp in s_sceneToInstanceIds)
            {
                foreach (var go in kvp.Value)
                {
                    if (go == null)
                    {
                        Debug.LogWarningFormat("GameObject instance has been destroyed, use ResourceManager.ReleaseInstance to ensure proper reference counts.");
                    }
                    else
                    {
                        if (go.scene.GetHashCode() != kvp.Key)
                        {
                            Debug.LogWarningFormat("GameObject instance {0} has been moved to from scene {1} to scene {2}.  When moving tracked instances, use ResourceManager.RecordInstanceSceneChange to ensure that reference counts are accurate.", go, kvp.Key, go.scene.GetHashCode());
                            objectsThatNeedToBeFixed.Add(new KeyValuePair<int, GameObject>(kvp.Key, go));
                        }
                    }
                }
            }

            foreach (var go in objectsThatNeedToBeFixed)
                RecordInstanceSceneChange(go.Value, go.Key, go.Value.scene.GetHashCode()); 
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

            if (loadMode == LoadSceneMode.Single)
                ValidateSceneInstances();

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
            ValidateSceneInstances();
            return StartInternalAsyncOp(key, SceneProvider.ReleaseSceneAsync).Acquire();
        }
    }
}
