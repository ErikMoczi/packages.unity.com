using System;
using System.Collections.Generic;
using ResourceManagement.ResourceLocations;

namespace ResourceManagement.ResourceLocators
{
    public class LegacyResourcesLocator : IResourceLocator<string>
    {
        public IResourceLocation Locate(string address)
        {
            return new LegacyResourcesLocation(address);
        }
    }
}
