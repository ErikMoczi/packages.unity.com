using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class LegacyResourcesProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> StartOp(IResourceLocation location)
            {
                m_result = null;
                var reqOp = Resources.LoadAsync<Object>(location.InternalId);
                if (reqOp.isDone)
                    DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                else
                    reqOp.completed += OnComplete;
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as ResourceRequest).asset as TObject;
            }
        }

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>().StartOp(location);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var go = asset as GameObject;
            if (go != null)
            {
                //GameObjects cannot be resleased via Object.Destroy because they are considered an asset
                //but they can't be unloaded via Resources.UnloadAsset since they are NOT an asset?
                return true;
            }
            var obj = asset as Object;
            if (obj != null)
            {
                Resources.UnloadAsset(obj);
                return true;
            }

            return true;
        }
    }
}
