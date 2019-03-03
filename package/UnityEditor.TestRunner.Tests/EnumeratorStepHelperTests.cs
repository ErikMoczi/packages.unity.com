using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner;

namespace FrameworkTests.CustomRunner
{
    public class EnumeratorStepHelperTests
    {
        [Test]
        public void GetCurrentPC()
        {
            var enumerator = GetEnumerable().GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();

            var enumeratorPc = EnumeratorStepHelper.GetEnumeratorPC(enumerator);

            Assert.AreEqual(enumeratorPc, 2);
        }

        [Test]
        public void SetCurrentPC()
        {
            var enumerator = GetEnumerable().GetEnumerator();

            EnumeratorStepHelper.SetEnumeratorPC(3);
            var isUpdated = EnumeratorStepHelper.UpdateEnumeratorPcIfNeeded(enumerator);

            enumerator.MoveNext();
            Assert.IsTrue(isUpdated);
            Assert.AreEqual("Too", enumerator.Current);
        }

        private IEnumerable GetEnumerable()
        {
            yield return "Foo";
            yield return "Bar";
            yield return "Baz";
            yield return "Too";
            yield return "Tada";
        }
    }
}
