using System;
using System.Collections;
using FrameworkTests.CustomRunner.UnityWorkItems;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace FrameworkTests.CustomRunner
{
    internal class PlaymodeWorkItemFactoryTests : WorkItemFactoryTests<PlaymodeWorkItemFactory>
    {
        [Test]
        public void TestIsEnumeratorTestWeExpectEditorEnumeratorTestWorkItem()
        {
            var methodInfoMock = new Mock<IMethodInfo>();
            var typeInfoMock = new Mock<ITypeInfo>();
            var filterMock = new Mock<ITestFilter>();
            var methodInfo = typeof(PlaymodeWorkItemFactoryTests).GetMethod("EnumeratorTest");
            typeInfoMock.Setup(x => x.Type).Returns(methodInfo.ReturnType);
            methodInfoMock.Setup(x => x.TypeInfo).Returns(typeInfoMock.Object);
            methodInfoMock.Setup(x => x.ReturnType).Returns(typeInfoMock.Object);
            methodInfoMock.SetupGet(x => x.MethodInfo).Returns(methodInfo);
            filterMock.Setup(x => x.IsExplicitMatch(Moq.It.IsAny<ITest>())).Returns(true);
            var testMethod = new TestMethod(methodInfoMock.Object);

            var unityWorkItem = m_WorkItemFactory.Create(testMethod, filterMock.Object);

            Assert.That(unityWorkItem, Is.TypeOf<CoroutineTestWorkItem>());
        }
    }
}
