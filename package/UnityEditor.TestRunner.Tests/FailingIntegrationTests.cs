using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class FailingIntegrationTests
    {
        [UnityTestExpectedToFail]
        public IEnumerator EnteringPlaymodeTwice_FailsTheTest()
        {
            yield return new EnterPlayMode();
            yield return new EnterPlayMode();
        }

        [UnityTestExpectedToFail]
        public IEnumerator ExitingPlaymodeWhenNotInPlaymode_FailsTheTest()
        {
            yield return new ExitPlayMode();
        }

        [UnityTestExpectedToFail]
        public IEnumerator EnteringPlaymode_WillFailWithErrorLog_Before()
        {
            Debug.LogError("error log");
            yield return new EnterPlayMode();
        }

        [UnityTestExpectedToFail]
        public IEnumerator EnteringPlaymode_WillFailWithErrorLog_After()
        {
            yield return new EnterPlayMode();
            Debug.LogError("error log");
        }

        [UnityTestExpectedToFail]
        public IEnumerator EnteringPlaymode_CanAssertAfterEnteringPlaymode()
        {
            yield return new EnterPlayMode();
            Assert.Fail();
        }

        [UnityTestExpectedToFail]
        public IEnumerator RecompilingScript_WhenNothingToRecompile_WillFailTheTest()
        {
            yield return new RecompileScripts();
        }

        [UnityTestExpectedToFail]
        public IEnumerator WaitingForDomainReload_WhenNothingToCompile_WillFailTheTest()
        {
            yield return new WaitForDomainReload();
        }
    }
}
