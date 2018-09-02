using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public class SceneProvider : ISceneProvider
    {
        class InternalOp : AsyncOperationBase<Scene>
        {
            LoadSceneMode m_loadMode;
            public IAsyncOperation<Scene> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
            {
                m_context = location;
                m_loadMode = loadMode;
                loadDependencyOperation.completed += (obj) => OnBundlesLoaded(obj);
                return this;
            }

            void OnBundlesLoaded(IAsyncOperation<IList<object>> operation)
            {
                var loc = m_context as IResourceLocation;
                SceneManager.LoadSceneAsync(loc.InternalId, m_loadMode).completed += OnSceneLoaded;
            }

            void OnSceneLoaded(AsyncOperation op)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncCompletion, m_context, 1);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, m_context, 100);
                Result = SceneManager.GetActiveScene();
                InvokeCompletionEvent();
            }

            public override bool IsDone
            {
                get
                {
                    return base.IsDone && Result.isLoaded;
                }
            }
        }

        public IAsyncOperation<Scene> ProvideSceneAsync(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp, Scene>();
            return r.Start(location, loadDependencyOperation, loadMode);
        }

        class InternalReleaseOp : AsyncOperationBase<Scene>
        {
            public IAsyncOperation<Scene> Start(IResourceLocation location)
            {
                m_context = location;
                Result = SceneManager.GetSceneByPath(location.InternalId);
                if (Result.isLoaded)
                {
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncRequest, m_context, 0);
                    SceneManager.UnloadSceneAsync(location.InternalId).completed += OnSceneUnloaded;
                }
                else
                {
                    Debug.LogWarning("Tried to unload a scene that was not loaded:" + location.InternalId);
                }

                return this;
            }

            void OnSceneUnloaded(AsyncOperation operation)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncCompletion, m_context, 0);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, m_context, 0);
            }

            public override bool IsDone
            {
                get
                {
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
            var r = AsyncOperationCache.Instance.Acquire<InternalReleaseOp, Scene>();
            return r.Start(location);
        }
    }
}
