using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;
using System;

namespace ResourceManagement.ResourceProviders
{
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
           where TObject : class
        {
            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                loadDependencyOperation.completed += (obj) =>
                {
                    AssetBundle bundle;
                    if (obj.result != null && obj.result.Count > 0 && (bundle = obj.result[0] as AssetBundle) != null)
                        bundle.LoadAssetAsync<TObject>(loc.id).completed += OnComplete;
                    else
                        OnComplete();
                };

                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as AssetBundleRequest).asset as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
            // Bundled assets are exclusively unloaded by unloading their parent asset bundle
            return true;
        }
    }
}
