using System.Collections.Generic;

namespace UnityEditor.Build.Interfaces
{
    public interface IBuildContent : IContextObject
    {
        List<GUID> Assets { get; }
        List<GUID> Scenes { get; }
    }

    public interface IBundleContent : IBuildContent
    {
        Dictionary<string, List<GUID>> BundleLayout { get; }
        Dictionary<GUID, string> Addresses { get; }
    }
}