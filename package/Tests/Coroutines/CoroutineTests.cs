


using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class CoroutineTests
    {
        [UnityTest]
        public IEnumerator CanCompleteNestedCoroutines()
        {
            var expected = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var list = new List<int>();
            var coroutine = Coroutines.StartCoroutine(GenerateUsingNestedCoroutine(list, 0, 10));

            while (!coroutine.HasCompleted)
            {
                yield return null;
            }

            Assert.AreEqual(expected.Count, list.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        [UnityTest]
        public IEnumerator CoroutineSupportsYieldingNull()
        {
            var expected = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var list = new List<int>();
            var coroutine = Coroutines.StartCoroutine(GenerateUsingYieldNull(list, 0, 10));

            // Run 11 times
            for (var i = 0; i <= 10; ++i)
            {
                Assert.AreEqual(list.Count, i);
                Assert.IsFalse(coroutine.HasCompleted);
                yield return null;
            }

            yield return null;
            Assert.IsTrue(coroutine.HasCompleted);
            Assert.AreEqual(expected.Count, list.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        [Test]
        public void CanCancelNestedCoroutines()
        {
            var expected = new List<int> { 0 };

            var list = new List<int>();
            var coroutine = Coroutines.StartCoroutine(GenerateUsingNestedCoroutine(list, 0, 10));
            coroutine.MoveNext();
            coroutine.Cancel();

            Assert.AreEqual(expected.Count, list.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        [UnityTest]
        public IEnumerator CanWaitTest()
        {
            var initial = EditorApplication.timeSinceStartup;
            var coroutine = Coroutines.StartCoroutine(DoWait(2));

            while (!coroutine.HasCompleted)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(EditorApplication.timeSinceStartup, initial + 2);
        }

        [UnityTest]
        public IEnumerator CanStopAllCoroutines()
        {
            var expected = new List<int>{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var list = new List<int>();
            for (int i = 0; i < 10; ++i)
            {
                Coroutines.StartCoroutine(GenerateUsingNestedCoroutine(list, 0, 10));
            }

            yield return null;
            Coroutines.StopAllCoroutines();
            yield return null;

            Assert.AreEqual(expected.Count, list.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        [UnityTest]
        public IEnumerator CanStopCoroutineUsingTarget()
        {
            var expectedAfterOnce = new List<int>{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expectedAtEnd = new List<int>{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 3, 5, 7, 9 };
            var target = new object();
            var list = new List<int>();
            for (int i = 0; i < 10; ++i)
            {
                Coroutines.StartCoroutine(DoubleAdd(list, i), i % 2 == 0 ? target : null);
            }

            // Run once
            yield return null;
            Assert.AreEqual(expectedAfterOnce.Count, list.Count);
            for (var i = 0; i < expectedAfterOnce.Count; ++i)
            {
                Assert.AreEqual(expectedAfterOnce[i], list[i]);
            }

            Coroutines.StopAllCoroutines(target);
            // Run again
            yield return null;

            Assert.AreEqual(expectedAtEnd.Count, list.Count);
            for (var i = 0; i < expectedAtEnd.Count; ++i)
            {
                Assert.AreEqual(expectedAtEnd[i], list[i]);
            }
        }

        [UnityTest]
        public IEnumerator ThrowingDoesNotBreakTest()
        {
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Operation is not valid due to the current state of the object.");
            var expected = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var list = new List<int>();
            var coroutine = Coroutines.StartCoroutine(GenerateUsingYieldNull(list, 0, 10));

            var expectedToThrow = new List<int>{ 0, 1, 2, 3, 4 };
            var throwingList = new List<int>();
            var throwing = Coroutines.StartCoroutine(AddAndThrow(throwingList, 0, 10));

            while (Coroutines.HasCoroutinesRunning)
            {
                yield return null;
            }

            Assert.IsNull(coroutine.ThrownDuringExecution);
            Assert.IsNotNull(throwing.ThrownDuringExecution);
            Assert.AreSame(typeof(InvalidOperationException), throwing.ThrownDuringExecution.GetType());

            Assert.AreEqual(expectedToThrow.Count, throwingList.Count);
            for (var i = 0; i < expectedToThrow.Count; ++i)
            {
                Assert.AreEqual(expectedToThrow[i], throwingList[i]);
            }

            Assert.AreEqual(expected.Count, list.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        [UnityTest]
        public IEnumerator NestedThrowingStopsExecutionTest()
        {
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Operation is not valid due to the current state of the object.");

            var expectedToThrow = new List<int>{ 0, 1, 2, 3, 4 };
            var throwingList = new List<int>();
            var throwing = Coroutines.StartCoroutine(NestedAddAndThrow(throwingList, 0, 10));

            while (Coroutines.HasCoroutinesRunning)
            {
                yield return null;
            }

            Assert.IsNotNull(throwing.ThrownDuringExecution);

            Assert.AreEqual(expectedToThrow.Count, throwingList.Count);
            for (var i = 0; i < expectedToThrow.Count; ++i)
            {
                Assert.AreEqual(expectedToThrow[i], throwingList[i]);
            }
        }

        private static IEnumerator GenerateUsingYieldNull(List<int> list, int initial, int until)
        {
            while (initial <= until)
            {
                list.Add(initial);
                ++initial;
                yield return null;
            }
        }

        private static IEnumerator GenerateUsingNestedCoroutine(List<int> list, int initial, int until)
        {
            if (initial <= until)
            {
                list.Add(initial);
                yield return Coroutines.StartCoroutine(GenerateUsingNestedCoroutine(list, initial + 1, until));
            }
        }

        private static IEnumerator DoWait(double time)
        {
            yield return new TinyWaitForSeconds(time);
        }

        private static IEnumerator DoubleAdd(List<int> list, int toAdd)
        {
            list.Add(toAdd);
            yield return null;
            list.Add(toAdd);
        }

        private static IEnumerator AddAndThrow(List<int> list, int initial, int until)
        {
            while (initial <= until)
            {
                if (initial == 5)
                {
                    throw new InvalidOperationException();
                }
                list.Add(initial);
                ++initial;
                yield return null;
            }
        }

        private static IEnumerator NestedAddAndThrow(List<int> list, int initial, int until)
        {
            yield return Coroutines.StartCoroutine(AddAndThrow(list, initial, until));
            list.Add(until + 1);
        }
    }
}

