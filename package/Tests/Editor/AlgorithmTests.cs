using NUnit.Framework;
using System;
using System.Collections;
using Unity.Collections;
using Unity.MemoryProfiler.Containers.Unsafe;
using UnityEngine.TestTools;

namespace Unity.MemoryProfiler.Editor.Tests
{
    [TestFixture]
    internal class NativeArrayAlgorithmsTests
    {
        NativeArray<int> nativeArray;

        int[] kDefaultArray = new int[16] 
        {
            102,
             1,
            -11,
            4,
            2,
            102,
            5,
            -16,
            -2,
            3,
            -10,
            0,
            6,
            90,
            -1,
            -32
        };

        int[] kDefaultArraySorted = new int[16]
        {
            -32,
            -16,
            -11,
            -10,
            -2,
            -1,
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            90,
            102,
            102
        };

        [TearDown]
        public void TearDown()
        {
            if(nativeArray.IsCreated)
                nativeArray.Dispose();
        }

        [Test]
        public void QuickSort_SortsNativeArray()
        {
            nativeArray = new NativeArray<int>(kDefaultArray, Allocator.Temp);

            NativeArrayAlgorithms.IntrospectiveSort(nativeArray, 0, nativeArray.Length);
            for(int i = 0; i < nativeArray.Length; ++i)
            {
                Assert.AreEqual(kDefaultArraySorted[i], nativeArray[i]);
            }
        }

        [Test]
        public void QuickSort_SortsNativeArrayWith2MillionEntries()
        {
            const int itemCount = 2000000;
            nativeArray = new NativeArray<int>(itemCount, Allocator.Persistent);
            Random rnd = new Random(0);

            for(int i = 0; i < nativeArray.Length; ++i)
            {
                nativeArray[i] = rnd.Next(0, itemCount);
            }

            var array = nativeArray.ToArray();

            float x = UnityEngine.Time.realtimeSinceStartup;
            Array.Sort(array);
            UnityEngine.Debug.Log("Managed: " + (UnityEngine.Time.realtimeSinceStartup - x));


            x = UnityEngine.Time.realtimeSinceStartup;
            NativeArrayAlgorithms.IntrospectiveSort(nativeArray, 0, nativeArray.Length);
            UnityEngine.Debug.Log("Native: " + (UnityEngine.Time.realtimeSinceStartup - x));

            for (int i = 0; i < nativeArray.Length; ++i)
            {
                Assert.AreEqual(array[i], nativeArray[i]);
            }
        }

        [UnityTest]
        public IEnumerator QuickSort_withMultipleSeeds_SortsNativeArrayWith500ThousandEntries([Values(-1,0,1)] int seed)
        {
            const int itemCount = 500000;
            nativeArray = new NativeArray<int>(itemCount, Allocator.Persistent);
            Random rnd = new Random(seed);

            for (int i = 0; i < nativeArray.Length; ++i)
            {
                nativeArray[i] = rnd.Next(0, itemCount);
            }

            var array = nativeArray.ToArray();

            float x = UnityEngine.Time.realtimeSinceStartup;
            Array.Sort(array);
            UnityEngine.Debug.Log("Managed: " + (UnityEngine.Time.realtimeSinceStartup - x));


            x = UnityEngine.Time.realtimeSinceStartup;
            NativeArrayAlgorithms.IntrospectiveSort(nativeArray, 0, nativeArray.Length);
            UnityEngine.Debug.Log("Native: " + (UnityEngine.Time.realtimeSinceStartup - x));

            for (int i = 0; i < nativeArray.Length; ++i)
            {
                Assert.AreEqual(array[i], nativeArray[i]);
            }
            yield return null;
        }
    }
}