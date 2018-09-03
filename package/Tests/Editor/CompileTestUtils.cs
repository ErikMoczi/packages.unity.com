#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    public static class CompileTestUtils
    {
        public static bool TryCompile(string code, out Assembly assembly, out string errorMessage)
        {
            try
            {
                var compilation = Compile(code);

                // Make sure that we build all
                using (var ms = new MemoryStream())
                {
                    var result = compilation.Emit(ms);

                    if ( ! result.Success)
                    {
                        var messages = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error)
                            .Select(
                                diagnostic => $"{diagnostic.Id} {diagnostic.GetMessage()} {diagnostic.Location.GetLineSpan().Span.ToString()}"
                                );

                        errorMessage = string.Join("\n", messages);

                        assembly = null;

                        return false;
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                assembly = null;
                errorMessage = e.ToString();

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static bool TryCompileToFile(string code, out string assemblyFilePath, out string errorMessage)
        {
            try
            {
                var compilation = Compile(code);

                assemblyFilePath = Path.ChangeExtension(Path.Combine(AssemblyDirectory, Path.GetRandomFileName()), "dll");

                var result = compilation.Emit(assemblyFilePath);

                if (!result.Success)
                {
                    var messages = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(
                            diagnostic => $"{diagnostic.Id} {diagnostic.GetMessage()} {diagnostic.Location.GetLineSpan().Span.ToString()}"
                            );

                    errorMessage = string.Join("\n", messages);

                    return false;
                }
            }
            catch (Exception e)
            {
                assemblyFilePath = string.Empty;
                errorMessage = e.ToString();

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

        private static CSharpCompilation Compile(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = ReferenceAssemblies.Locations.Select(
                location => MetadataReference.CreateFromFile (location)).ToArray();

            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: new[] {syntaxTree},
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            return compilation;
        }
    }
}

#endif // USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)