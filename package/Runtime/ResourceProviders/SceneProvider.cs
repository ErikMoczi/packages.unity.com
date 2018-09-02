using UnityEngine;
using UnityEngine.SceneManagement;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Diagnostics;
using System;
using System.Collections.Generic;

namespace ResourceManagement.ResourceProviders
{
    public class SceneProvider : ISceneProvider
    {
        class InternalOp : AsyncOperationBase<Scene>
        {
            IResourceLocation m_location;

            public InternalOp() : base(""){}

            public IAsyncOperation<Scene> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
            {
                m_location = loc;
                loadDependencyOperation.completed += (obj) => SceneManager.LoadSceneAsync(m_location.id, loadMode).completed += OnSceneLoaded;
                return this;
            }

            void OnSceneLoaded(AsyncOperation op)
            {
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.LoadSceneAsyncCompletion, m_location, 1);
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, m_location, 100);
                m_result = SceneManager.GetActiveScene();
                InvokeCompletionEvent(this);
            }

            public override bool isDone
            {
                get
                {
                    return base.isDone && m_result.isLoaded;
                }
            }
        }

        public IAsyncOperation<Scene> ProvideSceneAsync(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp, Scene>();
            return r.Start(loc, loadDependencyOperation, loadMode);
        }

        class InternalReleaseOp : AsyncOperationBase<Scene>
        {
            IResourceLocation m_location;

            public InternalReleaseOp() : base("") {}

            public IAsyncOperation<Scene> Start(IResourceLocation loc)
            {
                m_location = loc;
                m_result = SceneManager.GetSceneByPath(loc.id);

                if (m_result.isLoaded)
                {
                    ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.ReleaseSceneAsyncRequest, m_location, 0);
                    SceneManager.UnloadSceneAsync(loc.id).completed += OnSceneUnloaded;
                }
                else
                {
                    Debug.LogWarning("Tried to unload a scene that was not loaded:" + loc.id);
                }

                return this;
            }

            void OnSceneUnloaded(AsyncOperation op)
            {
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.ReleaseSceneAsyncCompletion, m_location, 0);
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, m_location, 0);
            }

            public override bool isDone
            {
                get
                {
                    return base.isDone && !m_result.isLoaded;
                }
            }
        }

        public bool ReleaseScene(IResourceLocation loc)
        {
            ReleaseSceneAsync(loc);
            return true;
        }

        public IAsyncOperation<Scene> ReleaseSceneAsync(IResourceLocation loc)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalReleaseOp, Scene>();
            return r.Start(loc);
        }
    }
}
