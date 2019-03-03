using NUnit.Framework.Interfaces;

namespace FrameworkTests
{
    public class TestFilterMock : ITestFilter
    {
        readonly ITest m_TestToExcludel;

        public TestFilterMock(ITest testToExcludel)
        {
            this.m_TestToExcludel = testToExcludel;
        }

        public TNode ToXml(bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public bool Pass(ITest test)
        {
            return test != m_TestToExcludel;
        }

        public bool IsExplicitMatch(ITest test)
        {
            throw new System.NotImplementedException();
        }
    }
}
