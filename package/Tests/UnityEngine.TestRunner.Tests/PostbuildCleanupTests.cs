using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PostbuildCleanupTests : IPostBuildCleanup
    {
        bool m_CleanupDone;
        [Test]
        public void VerifyPostbuildCleanup()
        {
            Assert.IsFalse(m_CleanupDone, "Cleanup executed too early.");
        }

        public void Cleanup()
        {
            m_CleanupDone = true;
        }
    }
}
