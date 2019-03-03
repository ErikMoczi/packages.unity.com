using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PreBuildSetupClass : IPrebuildSetup
    {
        public static int SetupCalledCount;
        public void Setup()
        {
            SetupCalledCount++;
        }
    }
}
