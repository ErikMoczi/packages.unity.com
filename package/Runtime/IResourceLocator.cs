using System;
using System.Collections.Generic;

namespace ResourceManagement
{
    public interface IResourceLocator
    {
    }

    public interface IResourceLocator<TAddress> : IResourceLocator
    {
        /// <summary>
        /// Resolve an <paramref name="address"/> to an <see cref="IResourceLocation"/>
        /// </summary>
        /// <returns>The resource location.</returns>
        /// <param name="address">Address to resolve.</param>
        /// <typeparam name="TAddress">The address type</typeparam>
        IResourceLocation Locate(TAddress address);
    }
}
