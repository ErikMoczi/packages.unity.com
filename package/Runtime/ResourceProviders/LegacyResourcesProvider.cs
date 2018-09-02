using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;

namespace ResourceManagement.ResourceProviders
{
    public class LegacyResourcesProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                loadDependencyOperation.completed += (obj) => Resources.LoadAsync<Object>(loc.id).completed += OnComplete;
                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as ResourceRequest).asset as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
            var obj = asset as Object;

            if (obj != null)
            {
                Resources.UnloadAsset(obj);
                return true;
            }

            return false;
        }
    }
}
