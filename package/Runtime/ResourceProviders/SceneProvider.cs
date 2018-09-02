using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;
using System;

namespace UnityEngine.ResourceManagement
{
    public class SceneProvider : ISceneProvider
    {
        static Scene GetSceneFromLocation(IResourceLocation location)
        {
            var path = location.InternalId;
            if (!path.EndsWith(".unity"))
                path = path + ".unity";
            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                    return scene;
            }
            return default(Scene);
        }

        class InternalOp : AsyncOperationBase<Scene>
        {
            LoadSceneMode m_loadMode;
            public IAsyncOperation<Scene> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
            {
                Validate();
                Context = location;
                m_loadMode = loadMode;
                loadDependencyOperation.Completed += (op) =>
                {
                    var reqOp = SceneManager.LoadSceneAsync((Context as IResourceLocation).InternalId, m_loadMode);
                    if (reqOp.isDone)
                        DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnSceneLoaded, 0, reqOp);
                    else
                        reqOp.completed += OnSceneLoaded;
                };
                return this;
            }

            void OnSceneLoaded(AsyncOperation operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncCompletion, Context, 1);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 100);
               
                SetResult(GetSceneFromLocation(Context as IResourceLocation));
                InvokeCompletionEvent();
            }

            public override bool IsDone
            {
                get
                {
                    Validate();
                    return base.IsDone && Result.isLoaded;
                }
            }
        }

        public IAsyncOperation<Scene> ProvideSceneAsync(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if(loadDependencyOperation == null)
                throw new ArgumentNullException("loadDependencyOperation");

            var operation = AsyncOperationCache.Instance.Acquire<InternalOp, Scene>();
            return operation.Start(location, loadDependencyOperation, loadMode);
        }

        class InternalReleaseOp : AsyncOperationBase<Scene>
        {
            public IAsyncOperation<Scene> Start(IResourceLocation location)
            {
                Validate();
                Context = location;
                var scene = GetSceneFromLocation(location);
                if (scene.IsValid())
                {
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncRequest, Context, 0);
                    var unloadOp = SceneManager.UnloadSceneAsync(scene);
                    if (unloadOp != null)
                        unloadOp.completed += OnSceneUnloaded;
                }
                return this;
            }

            void OnSceneUnloaded(AsyncOperation operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncCompletion, Context, 0);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 0);
            }

            public override bool IsDone
            {
                get
                {
                    Validate();
                    return base.IsDone && !Result.isLoaded;
                }
            }
        }

        public bool ReleaseScene(IResourceLocation location)
        {
            ReleaseSceneAsync(location);
            return true;
        }

        public IAsyncOperation<Scene> ReleaseSceneAsync(IResourceLocation location)
        {
            var operation = AsyncOperationCache.Instance.Acquire<InternalReleaseOp, Scene>();
            return operation.Start(location);
        }
    }
}
