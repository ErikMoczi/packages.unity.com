#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class ContainerTypeTreePathTests
    {
        [Test]
        public void Create_WithEmptyPath()
        {
            var p = ContainerTypeTreePath.CreateFromString(string.Empty);
            Assert.AreEqual(p.FullPath, string.Empty);
        }

        [TestCase("roottype", ExpectedResult = "roottype")]
        [TestCase("my.namespace.roottype", ExpectedResult = "my.namespace.roottype")]
        [TestCase("my.namespace.roottype/nested", ExpectedResult = "my.namespace.roottype/nested")]
        [TestCase("my.namespace.roottype/nested/types", ExpectedResult = "my.namespace.roottype/nested/types")]
        [TestCase("roottype/nested/types", ExpectedResult = "roottype/nested/types")]
        [TestCase("roottype/nested", ExpectedResult = "roottype/nested")]
        public string Create_WithFullPathIsIdempotent(string fullPath)
        {
            return ContainerTypeTreePath.CreateFromString(fullPath).FullPath;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
