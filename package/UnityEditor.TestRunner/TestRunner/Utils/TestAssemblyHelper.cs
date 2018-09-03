using System.Reflection;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
using UnityEngine.TestTools.NUnitExtensions;

namespace UnityEditor.TestTools.TestRunner
{
    internal class TestAssemblyHelper
    {
        public static ITest BuildTests(TestPlatform testPlatform, Assembly[] assemblies)
        {
            var settings = UnityTestAssemblyBuilder.GetNUnitTestBuilderSettings(testPlatform);
            var builder = UnityTestAssemblyBuilder.GetNUnitTestBuilder(testPlatform);
            return builder.Build(assemblies, settings);
        }
    }
}
