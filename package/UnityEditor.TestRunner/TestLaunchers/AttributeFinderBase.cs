using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;

namespace UnityEditor.TestTools.TestRunner
{
    internal abstract class AttributeFinderBase
    {
        public abstract IEnumerable<Type> Search(ITest tests, ITestFilter filter);
    }

    internal abstract class AttributeFinderBase<T1, T2> : AttributeFinderBase where T2 : Attribute
    {
        private readonly Func<T2, Type> m_TypeSelector;
        protected AttributeFinderBase(Func<T2, Type> typeSelector)
        {
            m_TypeSelector = typeSelector;
        }

        public override IEnumerable<Type> Search(ITest tests, ITestFilter filter)
        {
            var selectedTests = new List<ITest>();
            GetMatchingTests(tests, filter, ref selectedTests);

            var result = new List<Type>();
            result.AddRange(GetTypesFromPrebuildAttributes(selectedTests));
            result.AddRange(GetTypesFromInterface(selectedTests));

            return result.Distinct();
        }

        private static void GetMatchingTests(ITest tests, ITestFilter filter, ref List<ITest> resultList)
        {
            foreach (var test in tests.Tests)
            {
                if (test.IsSuite)
                {
                    GetMatchingTests(test, filter, ref resultList);
                }
                else
                {
                    if (filter.Pass(test))
                        resultList.Add(test);
                }
            }
        }

        private IEnumerable<Type> GetTypesFromPrebuildAttributes(IEnumerable<ITest> tests)
        {
            var attributesFromMethods = tests.SelectMany(t => t.Method.GetCustomAttributes<T2>(true).Select(attribute => attribute));
            var attributesFromTypes = tests.SelectMany(t => t.Method.TypeInfo.GetCustomAttributes<T2>(true).Select(attribute => attribute));

            var result = new List<T2>();
            result.AddRange(attributesFromMethods);
            result.AddRange(attributesFromTypes);

            return result.Select(m_TypeSelector).Where(type => type != null);
        }

        private static IEnumerable<Type> GetTypesFromInterface(IEnumerable<ITest> selectedTests)
        {
            var typesWithInterfaces = selectedTests.Where(t => typeof(T1).IsAssignableFrom(t.Method.TypeInfo.Type));
            return typesWithInterfaces.Select(t => t.Method.TypeInfo.Type);
        }
    }
}
