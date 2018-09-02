using System;
using System.Collections.Generic;
using ResourceManagement;
using ResourceManagement.AsyncOperations;

public class ResourceLocationLoadOperation<TAddress> : AsyncOperationBase<IResourceLocation>
{
    IList<IResourceLocator> m_resourceLocators;
    IList<IAsyncOperation<IResourceLocator>> m_locatorLoadOps;
    Func<TAddress, IResourceLocation> m_getLocationFunc;
    TAddress m_address;
    int m_depLoadCount;

    public ResourceLocationLoadOperation() : base("") {}

    public ResourceLocationLoadOperation<TAddress> Start(TAddress address, IList<IAsyncOperation<IResourceLocator>> locatorLoadOperations, IList<IResourceLocator> locators, Func<TAddress, IResourceLocation> getLocationFunc)
    {
        m_address = address;
        m_resourceLocators = locators;
        m_locatorLoadOps = locatorLoadOperations;
        m_getLocationFunc = getLocationFunc;
        m_depLoadCount = locatorLoadOperations.Count;

        if (m_depLoadCount > 0)
        {
            foreach (var op in locatorLoadOperations)
                op.completed += OnLocatorLoadComplete;
        }
        else
        {
            OnComplete();
        }

        return this;
    }

    protected virtual void OnLocatorLoadComplete(IAsyncOperation<IResourceLocator> op)
    {
        var locator = op.result;

        if (!m_resourceLocators.Contains(locator))
            m_resourceLocators.Insert(0, locator);

        if (m_locatorLoadOps.Contains(op))
            m_locatorLoadOps.Remove(op);

        m_depLoadCount--;
        if (m_depLoadCount == 0)
            OnComplete();
    }

    protected virtual void OnComplete()
    {
        SetResult(m_getLocationFunc(m_address));
        InvokeCompletionEvent(this);
    }
}
