using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    public interface IPreloadRequired
    {
        IAsyncOperation PreloadOperation { get; }
    }
}