#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

using Unity.Properties.Editor.Serialization;
using Unity.Properties.Editor.Serialization.Experimental;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class CecilGenerationBackendTests
    {
        private class FileDisposer : IDisposable
        {
            private string _filename = string.Empty;

            public FileDisposer(string filename)
            {
                _filename = filename;
            }

            public void Dispose()
            {
                if (File.Exists(_filename))
                {
                    File.Delete(_filename);
                }
            }
        }

        [Test]
        [Ignore("WIP")]
        public void WhenEmptyPropertyNodes_CecilGenerationBackend_ReturnsAnEmptyContainerList()
        {
            var cecilBackend = new CecilGenerationBackend();
            cecilBackend.GenerateContainer(null);

            Assert.IsNull(cecilBackend.GeneratedContainerTypeDefinition);
        }

        [Test]
        [Ignore("WIP")]
        public void WhenBlankPropertyContainer_CecilGenerationBackend_ReturnsAnEmptyContainerList()
        {
            var cecilBackend = new CecilGenerationBackend();

            string assemblyFilePath;
            string errors;

            const string code = @"
            using System.Collections.Generic;
            using Unity.Properties;
            public class HelloWorld : IPropertyContainer
            {
                public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

                private static PropertyBag sBag = new PropertyBag(IntValueProperty);
                public IPropertyBag PropertyBag => sBag;
            };
            ";

            Assert.IsTrue(
                CompileTestUtils.TryCompileToFile(
                    code, out assemblyFilePath, out errors)
                , errors);

            var types = new List<string>();
            {
                var assembly = Assembly.LoadFrom(assemblyFilePath);

                // var container = (IPropertyContainer) Activator.CreateInstance(type);
                // Assert.IsNotNull(container);

                var modules = assembly.Modules.ToList();
                foreach (var module in modules)
                {
                    types.AddRange(module.GetTypes().Select(t => t.Name).ToList());
                }

                var nodes = ReflectionPropertyTree.Read(assemblyFilePath);
                Assert.NotZero(nodes.Count);

                cecilBackend.AssemblyFilePath = assemblyFilePath;
                cecilBackend.GenerateContainer(nodes.First());

                Assert.NotNull(cecilBackend.GeneratedContainerTypeDefinition);

                Assert.NotNull(
                    cecilBackend.GeneratedContainerTypeDefinition.Module.Types.FirstOrDefault(
                        t => t.Name == "HelloWorldPropertyContainer"));
            }
        }

        [Test]
        [Ignore("WIP")]
        public void WhenPropertiesDefined_CecilGenerationBackend_GeneratesAValidContainer()
        {
            var cecilBackend = new CecilGenerationBackend();

            string assemblyFilePath;
            string errors;

            const string code = @"
            using System.Collections.Generic;
            using Unity.Properties;
            public class HelloWorld : IPropertyContainer
            {
                private int _intValue;

                public static readonly Property<HelloWorld, int> IntValueProperty =
                    new Property<HelloWorld, int>(nameof(IntValue),
                        c => c._intValue,
                        (c, v) => c._intValue = v);

                public int IntValue
                {
                    get { return IntValueProperty.GetValue(this); }
                    set { IntValueProperty.SetValue(this, value); }
                }

                public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

                private static PropertyBag sBag = new PropertyBag(IntValueProperty);
                public IPropertyBag PropertyBag => sBag;
            };
            ";

            Assert.IsTrue(
                CompileTestUtils.TryCompileToFile(
                    code, out assemblyFilePath, out errors)
                , errors);

            var types = new List<string>();
            {
                var assembly = Assembly.LoadFrom(assemblyFilePath);

                // var container = (IPropertyContainer) Activator.CreateInstance(type);
                // Assert.IsNotNull(container);

                var modules = assembly.Modules.ToList();
                foreach (var module in modules)
                {
                    types.AddRange(module.GetTypes().Select(t => t.Name).ToList());
                }

                var nodes = ReflectionPropertyTree.Read(assemblyFilePath);
                Assert.NotZero(nodes.Count);

                cecilBackend.AssemblyFilePath = assemblyFilePath;
                cecilBackend.GenerateContainer(nodes.First());

                Assert.NotNull(cecilBackend.GeneratedContainerTypeDefinition);

                Assert.NotNull(
                    cecilBackend.GeneratedContainerTypeDefinition.Module.Types.FirstOrDefault(
                        t => t.Name == "HelloWorldPropertyContainer"));
            }
            {
                var assembly = Assembly.LoadFrom(assemblyFilePath);
                var type = assembly.GetType("HelloWorldPropertyContainer");

                Assert.NotNull(type);
            }
        }
    }
}

#endif // USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)
