using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResourceManagerUtilityTests
{
    class TestOperation<TObject> : AsyncOperationBase<TObject>
    {
        public static int instanceCount = 0;
        public TestOperation()
        {
            instanceCount++;
        }
    }
    [UnityTest]
    public IEnumerator AsyncOperationCacheReusesReleasedOperation()
    {
        TestOperation<int>.instanceCount = 0;
        AsyncOperationCache.Instance.Clear();
        IAsyncOperation op = AsyncOperationCache.Instance.Acquire<TestOperation<int>>();
        Assert.AreEqual(1, TestOperation<int>.instanceCount);
        AsyncOperationCache.Instance.Release(op);
        op = AsyncOperationCache.Instance.Acquire<TestOperation<int>>();
        Assert.AreEqual(1, TestOperation<int>.instanceCount);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AsyncOperationCacheReturnsCorrectType()
    {
        AsyncOperationCache.Instance.Clear();
        IAsyncOperation op = AsyncOperationCache.Instance.Acquire<TestOperation<int>>();
        Assert.IsNotNull(op);
        Assert.AreEqual(op.GetType(), typeof(TestOperation<int>));
        op = AsyncOperationCache.Instance.Acquire<TestOperation<string>>();
        Assert.IsNotNull(op);
        Assert.AreEqual(op.GetType(), typeof(TestOperation<string>));
        yield return null;
    }

    class DAMTest
    {
        public int frameInvoked;
        public float timeInvoked;
        public void Method()
        {
            frameInvoked = Time.frameCount;
            timeInvoked = Time.realtimeSinceStartup;
        }

        public void MethodWithParams(int p1, string p2, bool p3, float p4)
        {
            Assert.AreEqual(p1, 5);
            Assert.AreEqual(p2, "testValue");
            Assert.AreEqual(p3, true);
            Assert.AreEqual(p4, 3.14f);
        }

    }

    [UnityTest]
    public IEnumerator DelayedActionManagerInvokeSameFrame()
    {
        var testObj = new DAMTest();
        int frameCalled = Time.frameCount;
        DelayedActionManager.AddAction((Action)testObj.Method);
        yield return null;
        Assert.AreEqual(frameCalled, testObj.frameInvoked);
    }

    [UnityTest]
    public IEnumerator DelayedActionManagerInvokeDelayed()
    {
        var testObj = new DAMTest();
        float timeCalled = Time.realtimeSinceStartup;
        DelayedActionManager.AddAction((Action)testObj.Method, .25f);
        yield return new WaitForSeconds(.5f);
        Assert.LessOrEqual(timeCalled + .25f, testObj.timeInvoked);
    }

    [UnityTest]
    public IEnumerator DelayedActionManagerInvokeWithParameters()
    {
        var testObj = new DAMTest();
        DelayedActionManager.AddAction((Action<int, string, bool, float>)testObj.MethodWithParams, 0, 5, "testValue", true, 3.14f);
        yield return null;
    }
}
