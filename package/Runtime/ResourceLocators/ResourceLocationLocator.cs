using System;
using System.Collections.Generic;
using ResourceManagement.ResourceLocations;

namespace ResourceManagement.ResourceLocators
{
    public class ResourceLocationLocator : IResourceLocator<IResourceLocation>
    {
        public IResourceLocation Locate(IResourceLocation address)
        {
            return address;
        }
    }
}
