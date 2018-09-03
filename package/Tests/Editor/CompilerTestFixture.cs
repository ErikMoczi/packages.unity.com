using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace UnityEditor.Compilation
{
    public class CompilerTestFixture
    {

        TempFileProvider tempFileProvider;
        [SetUp]
        public void Setup()
        {
            tempFileProvider = new TempFileProvider();
        }

        [TearDown]
        public void TearDown()
        {
            tempFileProvider.Dispose();
        }

        public string SourceCode(string code)
        {
            return tempFileProvider.SourceCode(code);
        }

        public void Touch(string file)
        {
            if (!System.IO.File.Exists(file))
                return;
            System.IO.File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        }

        public string CompilerCommandLine(string sourceFile, params string[] additional)
        {
            return CompilerCommandLine(new[] { sourceFile }, additional);
        }
        public string CompilerCommandLine(string[] sourceFile, params string[] additional)
        {
            return CompilerCommandLine(sourceFile, new string[0], new string[0], additional);
        }
        public string CompilerCommandLine(string[] sourceFile, string[] refs, params string[] additional)
        {
            return CompilerCommandLine(sourceFile, refs, new string[0], additional);
        }
        public string CompilerCommandLine(string[] sourceFiles, string[] references, string[] defines, string[] additional)
        {
            var args = new List<string>();
            args.AddRange(sourceFiles.SelectMany(x => new string[] { "-i", x }));
            args.AddRange(references.SelectMany(x => new string[] { "-r", x }));
            args.AddRange(defines.SelectMany(x => new string[] { "-d", x }));
            args.AddRange(additional);
            if (!args.Any(x => x.IndexOf("-o") == 0))
                args.AddRange(new[] { "-o", tempFileProvider.NewTempFile() });

            return string.Join(" ", args);
        }

        public string[] GenerateManyValidSourceFiles(int files)
        {
            var ret = new List<string>();
            var classTemplate = "class myclass%id% {\nint bar = 1;\n int getBar() { return bar; } }";
            for (int a = 0; a < files; a++)
                ret.Add(SourceCode(classTemplate.Replace("%id%", a.ToString())));

            return ret.ToArray();
        }

        public string[] GenerateManyValidSourceFiles()
        {
            return GenerateManyValidSourceFiles(8);
        }
        
        class TempFileProvider : IDisposable
        {
            ConcurrentBag<string> m_TempFiles = new ConcurrentBag<string>();

            public string NewTempFile()
            {
                var nf = System.IO.Path.GetTempFileName(); ;
                m_TempFiles.Add(nf);
                return nf;
            }

            public void Dispose()
            {
                foreach (var f in m_TempFiles)
                {
                    try
                    {
                        System.IO.File.Delete(f);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            public string SourceCode(string code)
            {
                var tmpFile = NewTempFile();
                System.IO.File.WriteAllText(tmpFile, code);
                return tmpFile;
            }
        }
    }

    
}