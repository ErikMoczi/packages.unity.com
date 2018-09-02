using System;
using System.Collections.Generic;
using ResourceManagement;
using ResourceManagement.AsyncOperations;
using UnityEngine;

internal class ResourceLocationCollectionLoadOperation<TAddress> : AsyncOperationBase<IList<IResourceLocation>>
{
    IList<IResourceLocator> m_resourceLocators;
    IList<IAsyncOperation<IResourceLocator>> m_locatorLoadOps;
    Func<TAddress, IResourceLocation> m_getLocationFunc;
    List<TAddress> m_addressList;
    int m_depLoadCount;
    Action<IAsyncOperation<IResourceLocator>> m_onCompleteAction;
    public ResourceLocationCollectionLoadOperation()
    {
        m_addressList = new List<TAddress>();
        m_result = new List<IResourceLocation>();
        m_onCompleteAction = OnLocatorLoadComplete;
    }

    public virtual ResourceLocationCollectionLoadOperation<TAddress> Start(IList<TAddress> addressList, IList<IAsyncOperation<IResourceLocator>> locatorLoadOperations, IList<IResourceLocator> locators, Func<TAddress, IResourceLocation> getLocationFunc)
    {
        m_addressList.Clear();
        m_result.Clear();

        m_addressList.AddRange(addressList);
        m_resourceLocators = locators;
        m_locatorLoadOps = locatorLoadOperations;
        m_getLocationFunc = getLocationFunc;
        m_depLoadCount = locatorLoadOperations.Count;

        if (m_depLoadCount > 0)
        {
            for(int i = 0; i < locatorLoadOperations.Count; i++)
                locatorLoadOperations[i].completed += m_onCompleteAction;
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
        foreach (var address in m_addressList)
            m_result.Add(m_getLocationFunc(address));

        InvokeCompletionEvent();
    }
}
