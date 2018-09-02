using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
           where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                if (loadDependencyOperation != null)
                {
                    loadDependencyOperation.Completed += (obj) =>
                    {
                        AssetBundle bundle;
                        if (obj.Result != null && obj.Result.Count > 0 && (bundle = obj.Result[0] as AssetBundle) != null)
                            bundle.LoadAssetAsync<TObject>(location.InternalId).completed += OnComplete;
                        else
                            OnComplete();
                    };
                }
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as AssetBundleRequest).asset as TObject;
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
            // Bundled assets are exclusively unloaded by unloading their parent asset bundle
            return true;
        }
    }
}
