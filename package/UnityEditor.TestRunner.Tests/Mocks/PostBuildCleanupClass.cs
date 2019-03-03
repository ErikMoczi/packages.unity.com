using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PostBuildCleanupClass : IPostBuildCleanup
    {
        public static int CleanUpCalledCount;
        public void Cleanup()
        {
            CleanUpCalledCount++;
        }
    }
}
