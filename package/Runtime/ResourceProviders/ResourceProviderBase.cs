using System.Collections.Generic;
using ResourceManagement.AsyncOperations;
using UnityEngine;
using ResourceManagement.Util;

namespace ResourceManagement.ResourceProviders
{
    public abstract class ResourceProviderBase : IResourceProvider
    {
        protected ResourceProviderBase() {}

        public virtual string providerId
        {
            get { return GetType().FullName; }
        }

        public virtual bool CanProvide<TObject>(IResourceLocation loc)
            where TObject : class
        {
            return providerId.Equals(loc.providerId, System.StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return string.Format("[{0}]", providerId);
        }

        public abstract IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : class;

        public virtual bool Release(IResourceLocation loc, object asset) { return true; }

        protected IAsyncOperation<TObject> CreateProvideAsyncOperation<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, System.Func<IAsyncOperation<IList<object>>, AsyncOperation> start, System.Func<AsyncOperation, TObject> convert) where TObject : class
        {
			var retOp = AsyncOperationCache.Instance.Acquire<AsyncOperationBase<TObject>, TObject>();
			retOp.m_context = loc;
            int startFrame = Time.frameCount;

            loadDependencyOperation.completed += (op) =>
            {
                start(op).completed += (op2) =>
                {
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, loc, Time.frameCount - startFrame);
                    retOp.SetResult(convert(op2));
					retOp.InvokeCompletionEvent();
					AsyncOperationCache.Instance.Release<TObject>(retOp);
				};
            };
            return retOp;
        }

        protected IAsyncOperation<TObject> CreateProvideAsyncOperation<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, System.Func<IAsyncOperation<IList<object>>, IAsyncOperation> start, System.Func<IAsyncOperation, TObject> convert) where TObject : class
        {
			var retOp = AsyncOperationCache.Instance.Acquire<AsyncOperationBase<TObject>, TObject>();
			retOp.m_context = loc;
            int startFrame = Time.frameCount;
            loadDependencyOperation.completed += (depsOp) =>
            {
				var sop = start(depsOp);
				sop.completed += (op2) =>
                {
                    ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, loc, Time.frameCount - startFrame);
                    retOp.SetResult(convert(op2));
                    retOp.InvokeCompletionEvent();
                    AsyncOperationCache.Instance.Release<TObject>(retOp);
				};
            };
            return retOp;
        }
    }
}
