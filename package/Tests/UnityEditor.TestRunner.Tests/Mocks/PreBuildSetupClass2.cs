using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PreBuildSetupClass2 : IPrebuildSetup
    {
        public static int SetupCalledCount;
        public void Setup()
        {
            SetupCalledCount++;
        }
    }
}
