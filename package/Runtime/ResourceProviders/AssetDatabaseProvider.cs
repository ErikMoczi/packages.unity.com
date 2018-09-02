#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class AssetDatabaseProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public override InternalProviderOperation<TObject> Start(IResourceLocation location)
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
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return operation.Start(location);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var obj = asset as Object;

            if (obj != null)
                return true;

            return false;
        }
    }
}
#endif
