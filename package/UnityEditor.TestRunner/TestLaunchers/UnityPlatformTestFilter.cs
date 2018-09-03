using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.TestTools.TestRunner
{
    internal class UnityPlatformTestFilter : ITestFilter
    {
        private ITestFilter m_InnnerTestFilter;
        private RuntimePlatform? m_TargetPlatform;
        public UnityPlatformTestFilter(ITestFilter innnerTestFilter, BuildTarget? buildTarget)
        {
            m_InnnerTestFilter = innnerTestFilter;
            if (buildTarget != null)
            {
                m_TargetPlatform = BuildTargetConverter.TryConvertToRuntimePlatform(buildTarget.Value);
            }
            else
            {
                m_TargetPlatform = Application.platform;
            }
        }

        public TNode ToXml(bool recursive)
        {
            return m_InnnerTestFilter.ToXml(recursive);
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            return m_InnnerTestFilter.AddToXml(parentNode, recursive);
        }

        public bool IsExplicitMatch(ITest test)
        {
            return m_InnnerTestFilter.IsExplicitMatch(test);
        }

        public bool Pass(ITest test)
        {
            var innerResult = m_InnnerTestFilter.Pass(test);
            if (innerResult == false || m_TargetPlatform == null)
            {
                return innerResult;
            }

            return IsPlatformSupported(GetAttributeOnMethod(test)) && IsPlatformSupported(GetAttributeOnClass(test)) && IsPlatformSupported(GetAttributeOnAssembly(test));
        }

        private UnityPlatformAttribute GetAttributeOnMethod(ITest test)
        {
            return test.Method.GetCustomAttributes<UnityPlatformAttribute>(true).FirstOrDefault();
        }

        private UnityPlatformAttribute GetAttributeOnClass(ITest test)
        {
            return test.Method.TypeInfo.GetCustomAttributes<UnityPlatformAttribute>(true).FirstOrDefault();
        }

        private UnityPlatformAttribute GetAttributeOnAssembly(ITest test)
        {
            return test.Method.TypeInfo.Assembly.GetCustomAttributes(typeof(UnityPlatformAttribute), true).OfType<UnityPlatformAttribute>().FirstOrDefault();
        }

        private bool IsPlatformSupported(UnityPlatformAttribute platformAttribute)
        {
            if (platformAttribute == null)
            {
                return true;
            }

            if (platformAttribute.include.Any() && !platformAttribute.include.Any(x => x == m_TargetPlatform))
            {
                return false;
            }

            if (platformAttribute.exclude.Any(x => x == m_TargetPlatform))
            {
                return false;
            }

            return true;
        }
    }
}
