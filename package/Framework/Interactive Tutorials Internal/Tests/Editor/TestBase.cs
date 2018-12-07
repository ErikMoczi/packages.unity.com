using System.IO;

namespace Unity.InteractiveTutorials.Tests
{
    public class TestBase
    {
        protected string GetTestAssetPath(string relativeAssetPath)
        {
            return Path.Combine("Packages/com.unity.learn.iet-framework/Framework/Interactive Tutorials Internal/Tests/Editor", relativeAssetPath);
        }
    }
}
