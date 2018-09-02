#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class AssetDatabaseProvider : ResourceProviderBase
    {
        float m_loadDelay = .1f;
        public AssetDatabaseProvider(float delay = .25f)
        {
            m_loadDelay = delay;
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, float loadDelay)
            {
                Result = null;
                Context = location;
                DelayedActionManager.AddAction((Action)CompleteLoad, loadDelay);
                return base.Start(location);
            }

            void CompleteLoad()
            {
                SetResult(UnityEditor.AssetDatabase.LoadAssetAtPath<Object>((Context as IResourceLocation).InternalId) as TObject);
                OnComplete();
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
            return operation.Start(location, m_loadDelay);
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
