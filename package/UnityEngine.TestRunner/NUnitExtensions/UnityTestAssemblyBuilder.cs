using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UnityEngine.TestTools.NUnitExtensions
{
    internal class UnityTestAssemblyBuilder : DefaultTestAssemblyBuilder
    {
        public ITest Build(Assembly[] assemblies, IDictionary<string, object> options)
        {
            var productName = string.Join("_", Application.productName.Split(Path.GetInvalidFileNameChars()));
            var suite = new TestSuite(productName);
            foreach (var assembly in assemblies)
            {
                var assemblySuite = Build(assembly, options) as TestSuite;
                if (assemblySuite != null && assemblySuite.HasChildren)
                {
                    suite.Add(assemblySuite);
                }
            }
            return suite;
        }

        public static UnityTestAssemblyBuilder GetNUnitTestBuilder(TestPlatform testPlatform)
        {
            var builder = new UnityTestAssemblyBuilder();
            return builder;
        }

        public static Dictionary<string, object> GetNUnitTestBuilderSettings(TestPlatform testPlatform)
        {
            var emptySettings = new Dictionary<string, object>();
            emptySettings.Add(FrameworkPackageSettings.TestParameters, "platform=" + testPlatform);
            return emptySettings;
        }
    }
}
