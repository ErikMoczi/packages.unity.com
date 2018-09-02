using System.Collections.Generic;
using System.IO;

namespace UnityEngine.ResourceManagement
{
    public class LocalAssetBundleProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Context = location;
                Result = null;
                loadDependencyOperation.Completed += (obj) =>
                    {
                        var reqOp = AssetBundle.LoadFromFileAsync(Path.Combine("file://", ResourceManagerConfig.ExpandPathWithGlobalVariables(location.InternalId)));
                        if (reqOp.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                        else
                            reqOp.completed += OnComplete;
                    };

                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as AssetBundleCreateRequest).assetBundle as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return operation.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            var bundle = asset as AssetBundle;
            if (bundle != null)
            {
                bundle.Unload(true);
                return true;
            }

            return false;
        }
    }
}
