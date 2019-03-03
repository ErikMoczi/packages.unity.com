using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace FrameworkTests
{
#if !UNITY_INCLUDE_TESTS
    public class IncludeTestsDefineTest
    {
        [Test]
        public void FailTestIfItsExecuted()
        {
            Assert.Fail("This test should never be executed because of the define");
        }
    }
#endif
}
