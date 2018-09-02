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
        public class InternalOp : AsyncOperationBase<Scene>
        {
            IResourceLocation m_location;

            public InternalOp(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode) : base(loc.id)
            {
                m_location = loc;
                loadDependencyOperation.completed += (obj) => SceneManager.LoadSceneAsync(m_location.id, loadMode).completed += OnSceneLoaded;
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
            return new InternalOp(loc, loadDependencyOperation, loadMode);
        }

        public class InternalReleaseOp : AsyncOperationBase<Scene>
        {
            IResourceLocation location;

            public InternalReleaseOp(IResourceLocation loc) : base(loc.id)
            {
                location = loc;
                m_result = SceneManager.GetSceneByPath(loc.id);

                if (m_result.isLoaded)
                {
                    ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.ReleaseSceneAsyncRequest, location, 0);
                    SceneManager.UnloadSceneAsync(loc.id).completed += OnSceneUnloaded;
                }
                else
                {
                    Debug.LogWarning("Tried to unload a scene that was not loaded:" + loc.id);
                }
            }

            void OnSceneUnloaded(AsyncOperation op)
            {
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.ReleaseSceneAsyncCompletion, location, 0);
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, location, 0);
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
            return new InternalReleaseOp(loc);
        }

        public void ReleaseAll()
        {
        }
    }
}
