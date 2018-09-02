#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides assets loaded via the AssetDatabase API.  This provider is only available in the editor and is used for fast iteration or to simulate asset bundles when in play mode.
    /// </summary>
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
                m_result = null;
                Context = location;
                DelayedActionManager.AddAction((Action)CompleteLoad, loadDelay);
                return base.Start(location);
            }

            void CompleteLoad()
            {
                var res = UnityEditor.AssetDatabase.LoadAssetAtPath((Context as IResourceLocation).InternalId, typeof(TObject));
                SetResult(res as TObject);
                OnComplete();
            }

            internal override TObject ConvertResult(AsyncOperation op) { return null; }
        }


        /// <inheritdoc/>
        public override bool CanProvide<TObject>(IResourceLocation location)
        {
            if (!base.CanProvide<TObject>(location))
                return false;
            var t = typeof(TObject);
            return t == typeof(object) || typeof(UnityEngine.Object).IsAssignableFrom(t);
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>().Start(location, m_loadDelay);
        }

        /// <inheritdoc/>
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
