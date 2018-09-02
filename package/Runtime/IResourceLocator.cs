using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public interface IResourceLocator
    {
    }

    public interface IResourceLocator<TKey> : IResourceLocator
    {
        /// <summary>
        /// Resolve an <paramref name="key"/> to an <see cref="IResourceLocation"/>
        /// </summary>
        /// <returns>The resource location.</returns>
        /// <param name="key">key to resolve.</param>
        /// <typeparam name="TKey">The key type</typeparam>
        IResourceLocation Locate(TKey key);
    }
}
