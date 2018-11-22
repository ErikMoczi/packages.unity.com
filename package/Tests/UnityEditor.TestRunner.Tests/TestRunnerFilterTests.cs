using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine.TestTools.TestRunner.GUI;

namespace FrameworkTests
{
    [TestFixture]
    public class TestRunnerFilterTests
    {
        [Test]
        public void Pass_ExactNameMatch_ReturnsTrue()
        {
            Assert.IsTrue(FilterByName("TestFullName", "TestFullName"));
        }

        [Test]
        public void Pass_PartialNameMatch_ReturnsTrue()
        {
            Assert.IsTrue(FilterByName("TestFull", "TestFullName"));
        }

        [Test]
        public void Pass_TestNameCaseDoesNotMatch_ReturnsFalse()
        {
            Assert.IsFalse(FilterByName("testFullName", "TestFullName"));
        }

        [Test]
        public void Pass_NotMatchingFilter_ReturnsFalse()
        {
            Assert.IsFalse(FilterByName("NoMatch", "TestFullName"));
        }

        [Test]
        public void Pass_CategoryMatch_ReturnsTrue()
        {
            Assert.IsTrue(FilterByCategory("categoryName", "categoryName"));
        }

        [Test]
        public void Pass_CategoryPartialMatch_ReturnsTrue()
        {
            Assert.IsTrue(FilterByCategory("category", "categoryName"));
        }

        [Test]
        public void Pass_CategoryCaseDoesNotMatch_ReturnsFalse()
        {
            Assert.IsFalse(FilterByCategory("CategoryName", "categoryName"));
        }

        private static bool FilterByCategory(string categoryFilter, string testCategoty)
        {
            var testRunnerFilter = new TestRunnerFilter
            {
                categoryNames = new[] { categoryFilter }
            };

            var testMock = new TestMock();
            var properties = new PropertyBag();
            properties.Set("Category", testCategoty);
            testMock.Properties = properties;

            var nunitFilter = testRunnerFilter.BuildNUnitFilter();
            return nunitFilter.Pass(testMock);
        }

        private static bool FilterByName(string filter, string testName)
        {
            var testRunnerFilter = new TestRunnerFilter
            {
                groupNames = new[] { filter }
            };

            var testMock = new TestMock
            {
                FullName = testName
            };
            var nunitFilter = testRunnerFilter.BuildNUnitFilter();
            return nunitFilter.Pass(testMock);
        }
    }
}
