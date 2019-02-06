
namespace UnityEditor.PackageManager.ValidationSuite
{
    public interface IHttpWebRequest
    {
        string Method { get; set; }
        int Timeout { get; set; }
        string UserAgent { get; set; }

        IHttpWebResponse GetResponse();
    }
}
