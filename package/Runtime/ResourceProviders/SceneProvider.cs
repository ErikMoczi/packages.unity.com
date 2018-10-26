using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides Scene objects.
    /// </summary>
    public class SceneProvider : ISceneProvider
    {
        class InternalOp : AsyncOperationBase<Scene>
        {
            LoadSceneMode m_loadMode;
            Scene m_scene;
            Action<IAsyncOperation<IList<object>>> action;
            IAsyncOperation m_dependencyOperation;
            AsyncOperation m_requestOperation;

            public InternalOp()
            {
                action = (op) =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        m_requestOperation = SceneManager.LoadSceneAsync((Context as IResourceLocation).InternalId, m_loadMode);
                        m_scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                        if (m_requestOperation == null || m_requestOperation.isDone)
                            DelayedActionManager.AddAction((Action<AsyncOperation>)OnSceneLoaded, 0, m_requestOperation);
                        else
                            m_requestOperation.completed += OnSceneLoaded;
                    }
                    else
                    {
                        OperationException = op.OperationException;
                        SetResult(default(Scene));
                        OnSceneLoaded(null);
                    }
                };
            }
            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1;

                    float reqPer = m_requestOperation == null ? 0 : m_requestOperation.progress;
                    if (m_dependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_dependencyOperation.PercentComplete * .75f;
                }
            }

            public IAsyncOperation<Scene> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
            {
                Validate();
                m_requestOperation = null;
                m_dependencyOperation = loadDependencyOperation;
                Context = location;
                m_loadMode = loadMode;
                if (loadDependencyOperation == null)
                    action(null);
                else
                    loadDependencyOperation.Completed += action;
                return this;
            }

            void OnSceneLoaded(AsyncOperation operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncCompletion, Context, 1);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 100);
                SetResult(m_scene);
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
        /// <inheritdoc/>
        public IAsyncOperation<Scene> ProvideSceneAsync(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            return AsyncOperationCache.Instance.Acquire<InternalOp>().Start(location, loadDependencyOperation, loadMode);
        }

        class InternalReleaseOp : AsyncOperationBase<Scene>
        {
            Scene m_scene;
            public IAsyncOperation<Scene> Start(IResourceLocation location, Scene scene)
            {
                Validate();
                m_scene = scene;
                Context = location;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncRequest, Context, 0);
                var unloadOp = SceneManager.UnloadSceneAsync(scene);
                if (unloadOp.isDone)
                    DelayedActionManager.AddAction((Action<AsyncOperation>)OnSceneUnloaded, 0, unloadOp);
                else
                    unloadOp.completed += OnSceneUnloaded;
                return this;
            }

            void OnSceneUnloaded(AsyncOperation operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.ReleaseSceneAsyncCompletion, Context, 0);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 0);
                SetResult(m_scene);
                InvokeCompletionEvent();
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

        /// <inheritdoc/>
        public IAsyncOperation<Scene> ReleaseSceneAsync(IResourceLocation location, Scene scene)
        {
            return AsyncOperationCache.Instance.Acquire<InternalReleaseOp>().Start(location, scene);
        }
    }
}
