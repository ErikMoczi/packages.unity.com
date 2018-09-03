#if COMPILER_TESTS
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace UnityEditor.Compilation
{
    public class RoslynCompilerTests : CompilerTestFixture
    {
        [Test]
        public void CanConnectToIncrementalCompiler()
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService())
            {
                Assert.NotNull(compiler, "Could not create incremental compiler instance");
            }
        }

        [Test]
        public void CompilerReturnsProperlyFormattedErrorMessages()
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService())
            {
                var sourceFile = SourceCode("class myclass {\nfoo bar;}");
                var result = compiler.InvokeIncrementalCompilerAsync(System.Guid.NewGuid().ToString(), CompilerCommandLine(sourceFile)).Result;
                Assert.AreEqual(2, result.compilationMessages.Length, "compiler was suppose to generate one warning and one error but did not");
                Assert.AreEqual(2, result.compilationMessages.First(x => x.severity == IncrementalCompiler.CompilationMessage.MessageSeverity.Error).lineNumber, "the compiler did not return the proper error line number");
            }
        }

        [Test]
        public void CompilerCompilesIncrementally()
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService())
            {
                var sourceFiles = GenerateManyValidSourceFiles();

                var compilationId = System.Guid.NewGuid().ToString();
                var compilerCommandLine = CompilerCommandLine(sourceFiles);

                var result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.AreEqual(0, result.compilationMessages.Length, "compiler should not have returned errors or warnings");
                Assert.IsNull(result.sourceFilesChanges, "compiler should not have reported any source files that have changed on initial compilation");

                for (int t = 0; t < 4; t++)
                    Touch(sourceFiles[t]);

                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.AreEqual(4, result.sourceFilesChanges.Length, "compiler did compile incrementally");

                for (int t = 0; t < 1; t++)
                    Touch(sourceFiles[t]);

                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.AreEqual(1, result.sourceFilesChanges.Length, "compiler did compile incrementally");

                compilerCommandLine = CompilerCommandLine(sourceFiles, "-o", System.IO.Path.GetTempFileName());
                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.IsFalse(result.compilationWasCached, "changing the commandline of a previous compilation session did not cause the compiler to rebuild all, which it should have");
            }
        }

        [Test]
        public void ChangingTheCommandLineOfAnIncrementalCompilationCausesAFullRebuild()
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService())
            {
                var sourceFiles = GenerateManyValidSourceFiles();

                var compilationId = System.Guid.NewGuid().ToString();
                var compilerCommandLine = CompilerCommandLine(sourceFiles);

                var result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                
                Assert.AreEqual(0, result.compilationMessages.Length, "compiler should not have returned errors or warnings");
                Assert.IsNull(result.sourceFilesChanges, "compiler should not have reported any source files that have changed on initial compilation");

                for (int t = 0; t < 4; t++)
                    Touch(sourceFiles[t]);

                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.AreEqual(4, result.sourceFilesChanges.Length, "compiler did compile incrementally");

                // change the command line by adding 'allow unsafe code'
                compilerCommandLine = CompilerCommandLine(sourceFiles, "-unsafe");
                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                Assert.IsFalse(result.compilationWasCached, "changing the commandline of a previous compilation session did not cause the compiler to rebuild all, which it should have");
            }
        }

        static System.Threading.Tasks.Task<long> CompileIncrementallyAsync(CompilerTestFixture fixture, string[] sourceFiles)
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService(1000))
            {
                var compilationTime = System.Diagnostics.Stopwatch.StartNew();
                var compilationId = System.Guid.NewGuid().ToString();
                var compilerCommandLine = fixture.CompilerCommandLine(sourceFiles);

                var result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;

                for (int t = 0; t < 4; t++)
                    fixture.Touch(sourceFiles[t]);

                result = compiler.InvokeIncrementalCompilerAsync(compilationId, compilerCommandLine).Result;
                compilationTime.Stop();

                return compilationTime.ElapsedMilliseconds;
            }
        }

        [Test]
        public void CompilerHandlesConcurrentIncrementalCompilations()
        {
            int concurrentCompilations = 50;
            var compilationTasks = new List<System.Threading.Tasks.Task<long>>();
            for (int a = 0; a < concurrentCompilations; a++)
            {
                compilationTasks.Add(new System.Threading.Tasks.Task<long>(() => CompileIncrementallyAsync(this, GenerateManyValidSourceFiles(200)).Result));
            }

            var totalTimeToCompile = System.Diagnostics.Stopwatch.StartNew();

            foreach (var t in compilationTasks)
            {
                t.Start();
            }

            System.Threading.Tasks.Task.WaitAll(compilationTasks.ToArray());

            totalTimeToCompile.Stop();

            long sumOfCompilationTimes = 0;
            foreach (var a in compilationTasks)
                sumOfCompilationTimes += a.Result;

            Assert.Greater(sumOfCompilationTimes, totalTimeToCompile.ElapsedMilliseconds, "Did not compile concurrently");
        }

        [Test]
        public void CanGetCompilerVersion()
        {
            using (var compiler = CompilerConnection.instance.CreateIncrementalCompilerService())
            {
                var compilerVersion = compiler.GetVersion();
                Assert.IsFalse(String.IsNullOrEmpty(compilerVersion), "could not get compiler version");
            }
        }
    }
}
#endif