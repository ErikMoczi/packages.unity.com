using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    /** Skip it for now
    internal class APIEvaluation : BaseValidation
    {
        public Dictionary<string, int> methodCounts = new Dictionary<string, int>();
        public HashSet<string> localMethods = new HashSet<string>();

        public APIEvaluation()
        {
            TestName = "API Evaluation";
            TestDescription = "Produces report of APIs exposed by package, as well as APIs the package depends on.";
            TestCategory = TestCategory.DataValidation;
        }

        public override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;
            var resolver = new DefaultAssemblyResolver();

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = false,
                SymbolReaderProvider = new DefaultSymbolReaderProvider()
            };

            try
            {
                var scriptsPath = Path.Combine("Library", "ScriptAssemblies");
                var assembly = AssemblyDefinition.ReadAssembly(scriptsPath, readerParameters);

                assembly.Modules.ToList().ForEach(m => {
                    if (m.Types != null)
                    {
                        m.Types.ToList().ForEach(t => {
                            if (t.Methods != null)
                            {
                                t.Methods.ToList().ForEach(mt =>
                                {
                                    localMethods.Add(mt.FullName);
                                });

                                t.Methods.ToList().ForEach(mt =>
                                {
                                    if ((mt != null) && (mt.Body != null))
                                    {
                                        foreach (var instruction in mt.Body.Instructions)
                                        {
                                            if (instruction.OpCode == OpCodes.Call)
                                            {
                                                MethodReference methodCall = instruction.Operand as MethodReference;
                                                if (methodCall != null)
                                                    ProcessMemberReference(methodCall.FullName);
                                            }
                                        }
                                    }
                                });
                            }
                        });
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                TestState = TestState.Failed;
            }
        }

        private void ProcessMemberReference(string fullname)
        {
            // At least filter System calls
            var parts = fullname.Split(' ');
            if (String.IsNullOrEmpty(parts[1]) || parts[1].StartsWith("System.") || parts[1].Contains(".ctor()"))
                return;

            if (localMethods.Contains(fullname))
                return;

            if (methodCounts.ContainsKey(fullname))
            {
                methodCounts[fullname]++;
            }
            else
            {
                methodCounts.Add(fullname, 1);
            }
        }
    }
    **/
}