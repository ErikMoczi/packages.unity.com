using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;

namespace Unity.EditorCoroutines.Editor.Tests
{
    internal class DummyEditorWindow : EditorWindow
    {

    }

    [TestFixture]
    public class EditorCoroutineTests
    {
        const float waitTime = 2.0f; //wait time in seconds
        IEnumerator ExecuteRoutineYieldingAbitraryEnumerator(IEnumerator enumerator)
        {
            Debug.Log("PreExecution");
            yield return enumerator;
            Debug.Log("PostExecution");
        }

        IEnumerator ExecuteRoutineWithWaitForSeconds()
        {
            Debug.Log("PreExecution");
            yield return new EditorWaitForSeconds(waitTime);
            Debug.Log("PostExecution");
        }

        IEnumerator ExecuteNestedOwnerlessRoutineswithWaitForSeconds()
        {
            Debug.Log("Outer PreExecution");
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteRoutineWithWaitForSeconds());
            Debug.Log("Outer PostExecution");
        }

        [UnityTest]
        public IEnumerator Coroutine_LogsStepsAtExpectedTimes()
        {
            var currentWindow = EditorWindow.GetWindow<DummyEditorWindow>();

            currentWindow.StartCoroutine(ExecuteRoutineYieldingAbitraryEnumerator(null));
            yield return null; //the coroutine starts execution the next frame

            yield return null; //coroutine will now yield and log
            LogAssert.Expect(LogType.Log, "PreExecution");

            yield return null;
            LogAssert.Expect(LogType.Log, "PostExecution");

            currentWindow.Close();
        }

        [UnityTest]
        public IEnumerator Coroutine_WaitsForSpecifiedNumberOfSeconds()
        {
            yield return new EnterPlayMode(); //both enter/exit play mode cause domain reload

            var currentWindow = EditorWindow.GetWindow<DummyEditorWindow>();
            currentWindow.StartCoroutine(ExecuteRoutineWithWaitForSeconds());

            yield return null; //one frame has passed and the routine got scheduled

            double targetTime = EditorApplication.timeSinceStartup + waitTime;
            LogAssert.Expect(LogType.Log, "PreExecution");

            while (targetTime > EditorApplication.timeSinceStartup)
            {
                yield return null; //wait until target time is reached
            }

            LogAssert.Expect(LogType.Log, "PostExecution");

            currentWindow.Close();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CoroutineWithAbitraryObject_StopsExecutionIfObjectIsCollected()
        {
            object obj = new object();
            EditorCoroutineUtility.StartCoroutine(ExecuteRoutineWithWaitForSeconds(), obj);

            yield return null; //one frame has passed and the routine got scheduled

            double targetTime = EditorApplication.timeSinceStartup + waitTime;
            LogAssert.Expect(LogType.Log, "PreExecution");

            while (targetTime > EditorApplication.timeSinceStartup)
            {
                if (EditorApplication.timeSinceStartup > targetTime - (waitTime * 0.5f) && obj != null)
                {
                    obj = null;
                    GC.Collect(); //Halfway through the wait, collect the owner object
                }
                yield return null; //wait until target time is reached
            }

            LogAssert.NoUnexpectedReceived();
        }


        [UnityTest]
        public IEnumerator CoroutineWithAbitraryUnityEngineObject_StopsExecutionIfObjectIsCollected()
        {
            GameObject gameObject = new GameObject("TEST");
            EditorCoroutineUtility.StartCoroutine(ExecuteRoutineWithWaitForSeconds(), gameObject);

            yield return null; //one frame has passed and the routine got scheduled

            double targetTime = EditorApplication.timeSinceStartup + waitTime;
            LogAssert.Expect(LogType.Log, "PreExecution");

            while (targetTime > EditorApplication.timeSinceStartup)
            {
                if (EditorApplication.timeSinceStartup > targetTime - (waitTime * 0.5f) && gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                    gameObject = null; //Immediately destroy the gameObject
                }
                yield return null; //wait until target time is reached
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NestedCoroutinesWithoutOwner_WaitForSpecificNumberOfSeconds()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteNestedOwnerlessRoutineswithWaitForSeconds());

            yield return null; //schedule root routine
            LogAssert.Expect(LogType.Log, "Outer PreExecution");

            yield return null; //schedule inner routine
            yield return null; //execute inner coroutine
            double targetTime = EditorApplication.timeSinceStartup + waitTime;
            LogAssert.Expect(LogType.Log, "PreExecution");

            while (targetTime > EditorApplication.timeSinceStartup)
            {
                yield return null; //wait until target time is reached
            }

            LogAssert.Expect(LogType.Log, "PostExecution");
            yield return null; //exit inner coroutine
            yield return null; //run over outer coroutine
            LogAssert.Expect(LogType.Log, "Outer PostExecution");
        }

        private IEnumerator NestedIEnumeratorRoutine()
        {
            Debug.Log("Start of nesting");
            yield return ExecuteRoutineYieldingAbitraryEnumerator(ExecuteRoutineYieldingAbitraryEnumerator(null));
            Debug.Log("End of nesting");
        }

        [UnityTest]
        public IEnumerator CoroutineWithoutOwner_YieldingIEnumerator()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(NestedIEnumeratorRoutine());
            yield return null;

            LogAssert.Expect(LogType.Log, "Start of nesting");

            yield return null; //yield 1st nested IEnumerator
            LogAssert.Expect(LogType.Log, "PreExecution");

            yield return null; //yield 2nd nested IEnumerator
            LogAssert.Expect(LogType.Log, "PreExecution");

            yield return null; //execute 2nd IEnumerator
            LogAssert.Expect(LogType.Log, "PostExecution");

            yield return null; //execute 1st IEnumerator
            LogAssert.Expect(LogType.Log, "PostExecution");

            //return to routine execution
            LogAssert.Expect(LogType.Log, "End of nesting");
        }

        IEnumerator RoutineThrowingGUIException()
        {
            yield return null;
            GUIUtility.ExitGUI();
            LogAssert.Expect(LogType.Exception, "");
        }

        [UnityTest]
        public IEnumerator ThrowingCoroutine_DoesNotHandleExitGUIException() //prefixed test with Z in order to ensure it is last
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(RoutineThrowingGUIException());
            yield return null;
        }
    }
}
