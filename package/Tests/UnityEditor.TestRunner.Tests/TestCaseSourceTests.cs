using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Assets.editor
{
    public class TestCaseSourceTests
    {
        [Test]
        [TestCaseSource(nameof(GetTestData))]
        public void TestCaseSourceWithFontCompiles(TestData data)
        {
        }

        static IEnumerable<TestData> GetTestData()
        {
            yield return new TestData() {data = 5};
            yield return new TestData() {data = Time.deltaTime };
            yield return new TestData() {data = Texture2D.blackTexture};
        }

        public struct TestData
        {
            public object data;
        }
    }
}
