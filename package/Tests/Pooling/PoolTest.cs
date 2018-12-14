using System.Collections.Generic;
using NUnit.Framework;
using System;

namespace Unity.Tiny.Test
{
    internal class PoolTest
    {
        [Test]
        public void CanGetAndReleasePooledList()
        {
            var list = ListPool<int>.Get();
            Assert.IsNotNull(list);
            Assert.DoesNotThrow(() => ListPool<int>.Release(list));
        }

        [Test]
        public void ReleasingAPooledListClearsIt()
        {
            const int count = 5;
            var list = ListPool<int>.Get();
            Assert.IsNotNull(list);
            Assert.IsTrue(list.Count == 0);
            for( int i = 0; i < count; ++i)
            {
                list.Add(i);
            }
            
            Assert.IsTrue(list.Count == count);
            Assert.DoesNotThrow(() => ListPool<int>.Release(list));
            Assert.IsTrue(list.Count == 0);
        }

        [Test]
        public void ReleasingUnownedPooledListThrows()
        {
            Assert.Throws<InvalidOperationException>(() => ListPool<int>.Release(new List<int>()));
        }

        [Test]
        public void ReleasingMultipleTimesThrows()
        {
            var list = ListPool<int>.Get();
            Assert.DoesNotThrow(() => ListPool<int>.Release(list));
            Assert.Throws<InvalidOperationException>(() => ListPool<int>.Release(list));
        }

        [Test]
        public void CanGetAndReleaseInDifferentOrder()
        {
            var list = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();
            Assert.DoesNotThrow(() => ListPool<int>.Release(list2));
            Assert.DoesNotThrow(() => ListPool<int>.Release(list));
        }

        [Test]
        public void MultipleGetResultsInDifferentPooledLists()
        {
            var list = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();

            Assert.AreNotSame(list, list2);

            Assert.DoesNotThrow(() => ListPool<int>.Release(list));
            Assert.DoesNotThrow(() => ListPool<int>.Release(list2));
        }

        [Test]
        public void PooledListAreIndeedPooled()
        {
            // Note that this is an implementation detail, release a pooled list will put it in a stack
            var list1 = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();
            var list3 = ListPool<int>.Get();

            Assert.DoesNotThrow(() => ListPool<int>.Release(list1));
            Assert.DoesNotThrow(() => ListPool<int>.Release(list2));
            Assert.DoesNotThrow(() => ListPool<int>.Release(list3));

            var matchesList3 = ListPool<int>.Get();
            var matchesList2 = ListPool<int>.Get();
            var matchesList1 = ListPool<int>.Get();

            Assert.AreSame(list1, matchesList1);
            Assert.AreSame(list2, matchesList2);
            Assert.AreSame(list3, matchesList3);

            Assert.DoesNotThrow(() => ListPool<int>.Release(matchesList2));
            Assert.DoesNotThrow(() => ListPool<int>.Release(matchesList3));
            Assert.DoesNotThrow(() => ListPool<int>.Release(matchesList1));
        }

        [Test]
        public void InvalidLifetimePolicyThrows()
        {
            Assert.Throws<InvalidOperationException>(() => ListPool<int>.Get((LifetimePolicy)5));
        }

        [Test]
        public void ReleaseNullListThrows()
        {
            Assert.Throws<InvalidOperationException>(() => ListPool<int>.Release(null));
        }
    }
}

