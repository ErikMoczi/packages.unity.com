using System;
using System.Collections;
using System.Reflection;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEditor.TestTools.TestRunner;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner.UnityWorkItems
{
    public class EditorEnumeratorTestWorkItemTests
    {
        [AttributeUsage(AttributeTargets.Method)]
        private class WrapWithEnumeratorCommandAttribute : Attribute, IWrapSetUpTearDown
        {
            public TestCommand Wrap(TestCommand command)
            {
                return new UnityLogCheckDelegatingCommand(new EnumerableTestMethodCommand((TestMethod)command.Test));
            }
        }

        private class SomeTestFixtureClass
        {
            [WrapWithEnumeratorCommand]
            public static IEnumerator SomeTestMethod()
            {
                yield return null;
            }

            [WrapWithEnumeratorCommand]
            public static IEnumerator SomeTestMethodThrowsException()
            {
                yield return null;
                throw new Exception("SomeTestMethodThrowsException");
            }

            [WrapWithEnumeratorCommand]
            public static IEnumerator SomeTestMethodWithFailingAssert()
            {
                yield return null;
                Assert.IsTrue(false);
            }
        }

        private static UnityTestExecutionContext ExecuteMethod(string methodName)
        {
            var methodInfo = typeof(SomeTestFixtureClass).GetMethod(methodName);
            var defaultTestWorkItem = new EditorEnumeratorTestWorkItem(NUnitMockHelpers.GetTestMethod(methodInfo, NUnitMockHelpers.GetFixture(typeof(SomeTestFixtureClass))), NUnitMockHelpers.AlwaysIncludeFilter());

            var mock = new Mock<ITestListener>();
            var unityTestExecutionContext = new UnityTestExecutionContext();
            unityTestExecutionContext.Listener = mock.Object;

            defaultTestWorkItem.InitializeContext(unityTestExecutionContext);
            foreach (var step in defaultTestWorkItem.Execute()) {}
            return unityTestExecutionContext;
        }

        [Test]
        public void Test1()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethod");
            Assert.AreEqual(ResultState.Success, unityTestExecutionContext.CurrentResult.ResultState);
        }

        [Test]
        public void Test2()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethodThrowsException");
            Assert.AreEqual(ResultState.Error, unityTestExecutionContext.CurrentResult.ResultState);
        }

        [Test]
        public void Test3()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethodWithFailingAssert");
            Assert.AreEqual(ResultState.Failure, unityTestExecutionContext.CurrentResult.ResultState);
        }
    }

    public class NUnitMockHelpers
    {
        public static TestFixture GetFixture(Type fixture)
        {
            var mock = new Mock<ITypeInfo>();
            mock.Setup(x => x.Type).Returns(fixture);
            return new TestFixture(mock.Object);
        }

        public static TestMethod GetTestMethod(MethodInfo method, TestFixture fixture)
        {
            var methodInfo = new MethodWrapper(fixture.TypeInfo.Type, method);
            return new TestMethod(methodInfo, fixture);
        }

        public static ITestFilter AlwaysIncludeFilter()
        {
            var mock = new Mock<ITestFilter>();
            mock.Setup(x => x.IsExplicitMatch(Moq.It.IsAny<ITest>())).Returns(true);
            return mock.Object;
        }
    }

    internal class DefaultTestWorkItemTests
    {
        private class SomeTestFixtureClass
        {
            public static void SomeTestMethod()
            {
            }

            public static void SomeTestMethodThrowsException()
            {
                throw new Exception("SomeTestMethodThrowsException");
            }

            public static void SomeTestMethodWithFailingAssert()
            {
                Assert.IsTrue(false);
            }
        }

        [Test]
        public void WhenPerformWorkOnMethodThatThrowsException_ThenContextIsError()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethodThrowsException");

            Assert.AreEqual(unityTestExecutionContext.CurrentResult.ResultState, ResultState.Error);
        }

        private static UnityTestExecutionContext ExecuteMethod(string methodName)
        {
            var methodInfo = typeof(SomeTestFixtureClass).GetMethod(methodName);
            var defaultTestWorkItem = new DefaultTestWorkItem(NUnitMockHelpers.GetTestMethod(methodInfo, NUnitMockHelpers.GetFixture(typeof(SomeTestFixtureClass))), NUnitMockHelpers.AlwaysIncludeFilter());

            var mock = new Mock<ITestListener>();
            var unityTestExecutionContext = new UnityTestExecutionContext();
            unityTestExecutionContext.Listener = mock.Object;

            defaultTestWorkItem.InitializeContext(unityTestExecutionContext);
            foreach (var step in defaultTestWorkItem.Execute()) {}
            return unityTestExecutionContext;
        }

        [Test]
        public void WhenPerformWorkOnMethodThatFailAssert_ThenContextIsFailure()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethodWithFailingAssert");

            Assert.AreEqual(ResultState.Failure, unityTestExecutionContext.CurrentResult.ResultState);
        }

        [Test]
        public void WhenPerformWorkOnMethodWithNoAsserts_ThenContextIsSuccess()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethod");

            Assert.AreEqual(ResultState.Success, unityTestExecutionContext.CurrentResult.ResultState);
        }

        [Test]
        public void WhenPerformWorkOnMethodWithFailingAsserts_ThenContextIsSuccess()
        {
            var unityTestExecutionContext = ExecuteMethod("SomeTestMethodWithFailingAssert");

            Assert.AreEqual(ResultState.Failure, unityTestExecutionContext.CurrentResult.ResultState);
        }
    }

    internal abstract class WorkItemFactoryTests<T> where T : WorkItemFactory, new()
    {
        protected T m_WorkItemFactory;

        [SetUp]
        public void Setup()
        {
            m_WorkItemFactory = new T();
        }

        [Test]
        public void TestIsSuiteThenWeExpectCompositeWorkItem()
        {
            var unityWorkItem = m_WorkItemFactory.Create(new TestSuite("Test suite"), null);

            Assert.That(unityWorkItem, Is.TypeOf<CompositeWorkItem>());
        }

        [Test]
        public void TestIsNormalTestWeExpectDefaultTestWorkItem()
        {
            var methodInfoMock = new Mock<IMethodInfo>();
            var typeInfoMock = new Mock<ITypeInfo>();
            var filterMock = new Mock<ITestFilter>();
            MethodInfo currentMethod = (MethodInfo)MethodInfo.GetCurrentMethod();
            typeInfoMock.Setup(x => x.Type).Returns(currentMethod.ReturnType);
            methodInfoMock.Setup(x => x.TypeInfo).Returns(typeInfoMock.Object);
            methodInfoMock.Setup(x => x.ReturnType).Returns(typeInfoMock.Object);
            methodInfoMock.SetupGet(x => x.MethodInfo).Returns(currentMethod);
            filterMock.Setup(x => x.IsExplicitMatch(Moq.It.IsAny<ITest>())).Returns(true);
            var testMethod = new TestMethod(methodInfoMock.Object);

            var unityWorkItem = m_WorkItemFactory.Create(testMethod, filterMock.Object);

            Assert.That(unityWorkItem, Is.TypeOf<DefaultTestWorkItem>());
        }

        public IEnumerator EnumeratorTest()
        {
            yield return null;
        }
    }
}
