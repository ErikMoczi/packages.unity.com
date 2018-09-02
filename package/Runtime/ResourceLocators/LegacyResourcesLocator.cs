using System;
using System.Collections.Generic;
using ResourceManagement.ResourceLocations;

namespace ResourceManagement.ResourceLocators
{
    public class LegacyResourcesLocator : IResourceLocator<string>
    {
        public IResourceLocation Locate(string key)
        {
            return new LegacyResourcesLocation(key);
        }
    }
}
