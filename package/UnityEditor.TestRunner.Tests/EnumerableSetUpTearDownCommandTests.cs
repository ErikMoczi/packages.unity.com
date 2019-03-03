using System;
using System.Collections;
using System.Collections.Generic;
using Castle.Components.DictionaryAdapter;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class EnumerableSetUpTearDownCommandTests
    {
        private static List<string> s_Outputs;
        [Test]
        public void MultipleSetUpAndTearDownsAreInvoked()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            var executeEnumerable =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext);
            foreach (var workItemStep in executeEnumerable)
            {
            }

            CollectionAssert.AreEqual(new[] { "Setup1", "Setup2", "PassingTest", "TearDown1", "TearDown2" }, s_Outputs);
        }

        [Test]
        public void MultipleSetUpAndTearDownsAreInvokedWithDomainReload()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            testClass.enterPlaymode = true;
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            var executeEnumerator =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext).GetEnumerator();
            while (executeEnumerator.MoveNext())
            {
                if (executeEnumerator.Current is EnterPlayMode || executeEnumerator.Current is ExitPlayMode)
                {
                    // simulate a reset of everything except test execution context.
                    executeEnumerator =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext).GetEnumerator();
                }
            }

            CollectionAssert.AreEqual(new[]
            {
                "Setup1", "Setup1_InPlaymode", "Setup1_AfterPlaymode",
                "Setup2", "Setup2_InPlaymode", "Setup2_AfterPlaymode",
                "PassingTest",
                "TearDown1", "TearDown1_InPlaymode", "TearDown1_AfterPlaymode",
                "TearDown2", "TearDown2_InPlaymode", "TearDown2_AfterPlaymode",
            }, s_Outputs);
        }

        [Test]
        public void MultipleSetUpAndTearDownsWithErrorInSetup()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            testClass.logErrorInSetup = true;
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            LogAssert.Expect(LogType.Error, "Error in Setup1");

            var executeEnumerable =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext);
            foreach (var workItemStep in executeEnumerable)
            {
            }

            CollectionAssert.AreEqual(new[] { "Setup1", "Setup2", "TearDown1", "TearDown2" }, s_Outputs);
        }

        [Test]
        public void MultipleSetUpAndTearDownsWithExceptionInSetup()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            testClass.throwExceptionInSetup = true;
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            var executeEnumerable =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext);
            foreach (var workItemStep in executeEnumerable)
            {
            }

            CollectionAssert.AreEqual(new[] { "Setup1", "Setup2", "TearDown1", "TearDown2" }, s_Outputs);
        }

        [Test]
        public void MultipleSetUpAndTearDownsWithErrorInTearDown()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            testClass.logErrorInTearDown = true;
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            LogAssert.Expect(LogType.Error, "Error in TearDown1");

            var executeEnumerable =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext);
            foreach (var workItemStep in executeEnumerable)
            {
            }

            CollectionAssert.AreEqual(new[] { "Setup1", "Setup2", "PassingTest", "TearDown1", "TearDown2" }, s_Outputs);
        }

        [Test]
        public void MultipleSetUpAndTearDownsWithExceptionInTearDown()
        {
            var testClass = new ClassWithTestAndMultipleSetupAndTearDown();
            testClass.throwExceptionInTearDown = true;
            var testName = "PassingTest";
            var testExecutionContext = new UnityTestExecutionContext();
            testExecutionContext.SetUpTearDownState = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            s_Outputs = new List<string>();

            var executeEnumerable =  ExecuteTestWithSetupAndTearDown(testClass, testName, testExecutionContext);
            foreach (var workItemStep in executeEnumerable)
            {
            }

            CollectionAssert.AreEqual(new[] { "Setup1", "Setup2", "PassingTest", "TearDown1", "TearDown2" }, s_Outputs);
        }

        private static IEnumerable ExecuteTestWithSetupAndTearDown(object testClass, string testName, UnityTestExecutionContext testExecutionContext)
        {
            testExecutionContext.TestObject = testClass;
            var testMethod = new TestAttribute().BuildFrom(new MethodWrapper(testClass.GetType(), testName), null);
            var testCommand = new TestMethodCommand(testMethod);
            testExecutionContext.CurrentResult = new TestCaseResult(testMethod);

            var commandUnderTest = new EnumerableSetUpTearDownCommand(new UnityLogCheckDelegatingCommand(testCommand));

            return commandUnderTest.ExecuteEnumerable(testExecutionContext);
        }

        public class ClassWithTestAndMultipleSetupAndTearDown
        {
            public bool enterPlaymode;
            public bool throwExceptionInSetup;
            public bool logErrorInSetup;
            public bool throwExceptionInTearDown;
            public bool logErrorInTearDown;

            public ClassWithTestAndMultipleSetupAndTearDown()
            {
                if (s_Outputs == null)
                {
                    s_Outputs = new EditableList<string>();
                }
            }

            [Test]
            public void PassingTest()
            {
                s_Outputs.Add("PassingTest");
            }

            [UnitySetUp]
            public IEnumerator Setup1()
            {
                s_Outputs.Add("Setup1");

                if (throwExceptionInSetup)
                {
                    throw new Exception("Exception in Setup1");
                }
                if (logErrorInSetup)
                {
                    Debug.LogError("Error in Setup1");
                }
                else if (!enterPlaymode)
                {
                    yield return null;
                }
                else
                {
                    yield return new EnterPlayMode();
                    s_Outputs.Add("Setup1_InPlaymode");
                    yield return new ExitPlayMode();
                    s_Outputs.Add("Setup1_AfterPlaymode");
                }
            }

            [UnitySetUp]
            public IEnumerator Setup2()
            {
                s_Outputs.Add("Setup2");
                if (!enterPlaymode)
                {
                    yield return null;
                }
                else
                {
                    yield return new EnterPlayMode();
                    s_Outputs.Add("Setup2_InPlaymode");
                    yield return new ExitPlayMode();
                    s_Outputs.Add("Setup2_AfterPlaymode");
                }
            }

            [UnityTearDown]
            public IEnumerator TearDown1()
            {
                s_Outputs.Add("TearDown1");

                if (throwExceptionInTearDown)
                {
                    throw new Exception("Exception in TearDown1");
                }
                if (logErrorInTearDown)
                {
                    Debug.LogError("Error in TearDown1");
                }
                else if (!enterPlaymode)
                {
                    yield return null;
                }
                else
                {
                    yield return new EnterPlayMode();
                    s_Outputs.Add("TearDown1_InPlaymode");
                    yield return new ExitPlayMode();
                    s_Outputs.Add("TearDown1_AfterPlaymode");
                }
            }

            [UnityTearDown]
            public IEnumerator TearDown2()
            {
                s_Outputs.Add("TearDown2");
                if (!enterPlaymode)
                {
                    yield return null;
                }
                else
                {
                    yield return new EnterPlayMode();
                    s_Outputs.Add("TearDown2_InPlaymode");
                    yield return new ExitPlayMode();
                    s_Outputs.Add("TearDown2_AfterPlaymode");
                }
            }
        }
    }
}
