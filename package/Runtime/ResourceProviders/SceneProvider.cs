using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides Scene objects.
    /// </summary>
    public class SceneProvider : ISceneProvider
    {
        class InternalOp : AsyncOperationBase<Scene>
        {
            LoadSceneMode m_LoadMode;
            Scene m_Scene;
            Action<IAsyncOperation<IList<object>>> m_Action;
            IAsyncOperation m_DependencyOperation;
            AsyncOperation m_RequestOperation;

            public InternalOp()
            {
                m_Action = op =>
                {
                    if ( (op == null || op.Status == AsyncOperationStatus.Succeeded) && Context is IResourceLocation)
                    {
                        if (op != null)
                        {
                            foreach (var result in op.Result)
                            {
                                AssetBundle bundle = result as AssetBundle;
                                if (bundle == null)
                                {
                                    var handler = result as DownloadHandlerAssetBundle;
                                    if (handler != null)
                                        bundle = handler.assetBundle;
                                }
                            }
                        }

                        m_RequestOperation = SceneManager.LoadSceneAsync((Context as IResourceLocation).InternalId, m_LoadMode);
                        m_Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                        if (m_RequestOperation == null || m_RequestOperation.isDone)
                            DelayedActionManager.AddAction((Action<AsyncOperation>)OnSceneLoaded, 0, m_RequestOperation);
                        else
                            m_RequestOperation.completed += OnSceneLoaded;
                    }
                    else
                    {
                        if(op != null)
                            m_Error = op.OperationException;
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

                    float reqPer = m_RequestOperation == null ? 0 : m_RequestOperation.progress;
                    if (m_DependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }

            public IAsyncOperation<Scene> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, LoadSceneMode loadMode)
            {
                Validate();
                m_RequestOperation = null;
                m_DependencyOperation = loadDependencyOperation;
                Context = location;
                m_LoadMode = loadMode;
                if (loadDependencyOperation == null)
                    m_Action(null);
                else
                    loadDependencyOperation.Completed += m_Action;
                return this;
            }

            void OnSceneLoaded(AsyncOperation operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadSceneAsyncCompletion, Context, 1);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 100);
                SetResult(m_Scene);
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
            Scene m_Scene;
            public IAsyncOperation<Scene> Start(IResourceLocation location, Scene scene)
            {
                Validate();
                m_Scene = scene;
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
                SetResult(m_Scene);
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
