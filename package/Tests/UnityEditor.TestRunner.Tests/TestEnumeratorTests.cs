using System;
using System.Collections;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner
{
    public class TestEnumeratorTests
    {
        private Mock<ITestExecutionContext> m_TestExecutionContext;
        private TestCaseResult m_TestCaseResult;

        [SetUp]
        public void Setup()
        {
            m_TestExecutionContext = new Mock<ITestExecutionContext>();
            m_TestCaseResult = new TestCaseResult(null);
            m_TestExecutionContext.SetupProperty(x => x.CurrentResult, m_TestCaseResult);
        }

        [Test]
        public void MethodNotFailingSetsContextSuccess()
        {
            var enumerator = MethodThatJustYields().GetEnumerator();
            var testEnumerator = new TestEnumerator(m_TestExecutionContext.Object, enumerator);

            var execute = testEnumerator.Execute();
            while (execute.MoveNext()) {}

            Assert.AreEqual(m_TestExecutionContext.Object.CurrentResult.ResultState, ResultState.Success);
        }

        [Test]
        public void MethodThatThrowsSetsResultStateError()
        {
            var enumerator = MethodThrowsException().GetEnumerator();
            var testEnumerator = new TestEnumerator(m_TestExecutionContext.Object, enumerator);

            var execute = testEnumerator.Execute();
            while (execute.MoveNext()) {}

            Assert.AreEqual(m_TestExecutionContext.Object.CurrentResult.ResultState, ResultState.Error);
        }

        [Test]
        public void MethodThatFailsShouldNotContinueExecuting()
        {
            var enumerator = MethodThrowsTwoException().GetEnumerator();
            var testEnumerator = new TestEnumerator(m_TestExecutionContext.Object, enumerator);

            var execute = testEnumerator.Execute();
            while (execute.MoveNext()) {}

            Assert.AreEqual(m_TestExecutionContext.Object.CurrentResult.ResultState, ResultState.Failure);
            Assert.That(!m_TestExecutionContext.Object.CurrentResult.Output.Contains("Second exception"), "Did not stop executing after failure");
        }

        [Test]
        public void EachYieldShouldBeTheSameAsTheMethodsYields()
        {
            var enumerator = MethodYieldsDifferentStrings().GetEnumerator();
            var testEnumerator = new TestEnumerator(m_TestExecutionContext.Object, enumerator);

            List<string> yields = new List<string>();
            var execute = testEnumerator.Execute();
            while (execute.MoveNext())
            {
                yields.Add((string)execute.Current);
            }

            Assert.That(yields.Count, Is.EqualTo(3), "More yields than method has");
            Assert.That(yields[0], Is.EqualTo("Foo"));
            Assert.That(yields[1], Is.EqualTo("Bar"));
            Assert.That(yields[2], Is.EqualTo("Baz"));
        }

        public IEnumerable MethodThatJustYields()
        {
            yield return null;
        }

        public IEnumerable MethodThrowsException()
        {
            yield return null;
            throw new Exception("MethodThrowsException");
        }

        public IEnumerable MethodThrowsTwoException()
        {
            yield return null;
            Assert.Fail();

            yield return null;
            throw new Exception("Second exception");
        }

        public IEnumerable MethodYieldsDifferentStrings()
        {
            yield return "Foo";
            yield return "Bar";
            yield return "Baz";
        }
    }
}
