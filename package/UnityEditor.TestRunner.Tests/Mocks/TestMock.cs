using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;

namespace FrameworkTests
{
    public class TestMock : ITest
    {
        public TestMock() : this(new MethodInfoMock())
        {
        }

        public TestMock(IMethodInfo methodInfo)
        {
            Method = methodInfo;
            IsSuite = false;
            HasChildren = false;
        }

        public TestMock(IEnumerable<ITest> children)
        {
            Tests = children.ToList();
            IsSuite = true;
            HasChildren = true;
        }

        public TNode ToXml(bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public ITypeInfo TypeInfo { get; set; }
        public IMethodInfo Method { get; set; }
        public RunState RunState { get; set; }
        public int TestCaseCount { get; set; }
        public IPropertyBag Properties { get; set; }
        public ITest Parent { get; set; }
        public bool IsSuite { get; set; }
        public bool HasChildren { get; set; }
        public IList<ITest> Tests { get; set; }
        public object Fixture { get; set; }
    }
}
