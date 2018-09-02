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
                m_context = location;
                Result = null;
                loadDependencyOperation.completed += (obj) =>
                    {
                        AssetBundle.LoadFromFileAsync(Path.Combine("file://", ResourceManagerConfig.ExpandPathWithGlobalVariables(location.InternalId))).completed += OnComplete;
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
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
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
