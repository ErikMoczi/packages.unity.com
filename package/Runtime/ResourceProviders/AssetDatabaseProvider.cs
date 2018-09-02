#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class AssetDatabaseProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                CompletionUpdater.UpdateUntilComplete(location.ToString(), () => {
                    #if UNITY_EDITOR
                        var res = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(location.InternalId) as TObject;
                        SetResult(res);
                    #endif

                        OnComplete();
                        return true;
                    });

                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            var obj = asset as Object;

            if (obj != null)
                return true;

            return false;
        }
    }
}
#endif
