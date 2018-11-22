using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class IgnoredUnityTests
    {
        [UnityTest]
        public IEnumerator Assert_IgnoreFollowedByYieldReturnNull_IgnoresTheTest()
        {
            Ignore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Assert_YieldReturnNullFollowedByIgnore_IgnoresTheTest()
        {
            yield return null;
            Ignore();
        }

        [UnityTest]
        public IEnumerator Assert_IgnoreFollowedByCoroutine_IgnoresTheTest()
        {
            Ignore();
            return AnotherCoroutine();
        }

        static IEnumerator AnotherCoroutine()
        {
            yield return null;
        }

        private static void Ignore()
        {
            Assert.Ignore("Ignored internationally");
        }
    }
}
