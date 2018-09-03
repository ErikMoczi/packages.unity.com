using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestRunner.NUnitExtensions.Filters;

namespace UnityEngine.TestRunner.NUnitExtensions
{
    internal static class ITestExtensions
    {
        private static IEnumerable<string> GetTestCategories(this ITest test)
        {
            var categories = test.Properties[PropertyNames.Category].Cast<string>().ToList();
            if (categories.Count == 0 && test is TestMethod)
            {
                // only mark tests as Uncategorized if the test fixture doesn't have a category,
                // otherwise the test inherits the Fixture category
                var fixtureCategories = test.Parent.Properties[PropertyNames.Category].Cast<string>().ToList();
                if (fixtureCategories.Count == 0)
                    categories.Add(CategoryFilterExtended.k_DefaultCategory);
            }
            return categories;
        }

        public static bool HasCategory(this ITest test, string[] categoryFilter)
        {
            var categories = test.GetAllCategoriesFromTest().Distinct();
            return categoryFilter.Any(c => categories.Any(r => r == c));
        }

        public static List<string> GetAllCategoriesFromTest(this ITest test)
        {
            if (test.Parent == null)
                return test.GetTestCategories().ToList();

            var categories = GetAllCategoriesFromTest(test.Parent);
            categories.AddRange(test.GetTestCategories());
            return categories;
        }

        public static void ParseForNameDuplicates(this ITest test)
        {
            var duplicates = new Dictionary<string, int>();
            for (var i = 0; i < test.Tests.Count; i++)
            {
                var child = test.Tests[i];
                int count;
                if (duplicates.TryGetValue(child.FullName, out count))
                {
                    count++;
                    child.Properties.Add("childIndex", count);
                    duplicates[child.FullName] = count;
                }
                else
                {
                    duplicates.Add(child.FullName, 1);
                }
                ParseForNameDuplicates(child);
            }
        }

        public static int GetChildIndex(this ITest test)
        {
            var index = test.Properties["childIndex"];
            return (int)index[0];
        }

        public static bool HasChildIndex(this ITest test)
        {
            var index = test.Properties["childIndex"];
            return index.Count > 0;
        }
    }
}
