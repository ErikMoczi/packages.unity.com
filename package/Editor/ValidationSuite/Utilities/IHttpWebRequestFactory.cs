using UnityEditor.PackageManager.ValidationSuite;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public interface IHttpWebRequestFactory
    {
        IHttpWebRequest Create(string url);
    }
}
