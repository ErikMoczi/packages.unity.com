using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides assets stored in an asset bundle.
    /// </summary>
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
           where TObject : class
        {
            IAsyncOperation m_DependencyOperation;
            AssetBundleRequest m_RequestOperation;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_Result = null;
                m_RequestOperation = null;
                m_DependencyOperation = loadDependencyOperation;
                loadDependencyOperation.Completed += op =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        AssetBundle bundle = op.Result[0] as AssetBundle;
                        if (bundle == null)
                        {
                            var handler = op.Result[0] as DownloadHandlerAssetBundle;
                            if (handler != null)
                                bundle = handler.assetBundle;
                        }

                        if (bundle == null)
                        {
                            //TODO - handle error case properly
                            SetResult(default(TObject));
                            OnComplete();   
                        }
                        else
                        {
                            var t = typeof(TObject);
                            if (t.IsArray)
                                m_RequestOperation = bundle.LoadAssetWithSubAssetsAsync(location.InternalId, t.GetElementType());
                            else if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
                                m_RequestOperation = bundle.LoadAssetWithSubAssetsAsync(location.InternalId, t.GetGenericArguments()[0]);
                            else
                                m_RequestOperation = bundle.LoadAssetAsync<TObject>(location.InternalId);

                            if (m_RequestOperation.isDone)
                                DelayedActionManager.AddAction((Action<AsyncOperation>)OnComplete, 0, m_RequestOperation);
                            else
                                m_RequestOperation.completed += OnComplete;
                        }
                    }
                    else
                    {
                        m_Error = op.OperationException;
                        SetResult(default(TObject));
                        OnComplete();
                    }
                };
                return base.Start(location);
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

            internal override TObject ConvertResult(AsyncOperation op)
            {
                var t = typeof(TObject);
                try
                {
                    var req = op as AssetBundleRequest;
                    if (req == null)
                        return null;
                    
                    if (t.IsArray)
                        return ResourceManagerConfig.CreateArrayResult<TObject>(req.allAssets);
                    if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
                        return ResourceManagerConfig.CreateListResult<TObject>(req.allAssets);
                    return req.asset as TObject;
                }
                catch (Exception e)
                {
                    OperationException = e;
                    return null;
                }
            }
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (loadDependencyOperation == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new ArgumentNullException("IAsyncOperation<IList<object>> loadDependencyOperation"));

            return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>().Start(location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            return true;
        }
    }
}
