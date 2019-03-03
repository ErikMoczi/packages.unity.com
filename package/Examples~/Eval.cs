using JetBrains.Annotations;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CSharp;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.QuickSearch
{
    namespace Providers
    {
        [UsedImplicitly]
        static class Eval
        {
            internal static string type = "eval";
            internal static string displayName = "Evaluator";

            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 1,
                    filterId = "$",
                    fetchItems = (context, items, provider) =>
                    {
                        if (!context.searchBoxText.StartsWith(provider.filterId))
                            return;

                        items.Add(provider.CreateItem(GUID.Generate().ToString(), "Evaluate C# expression", context.searchQuery.Trim(), null, null));
                    }
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new[]
                {
                    new SearchAction(type, "locate", null, "locate")
                    {
                        handler = (item, context) =>
                        {
                            var sCSCode = item.description;

                            var c = new CSharpCodeProvider();
                            var icc = c.CreateCompiler();
                            var cp = new CompilerParameters();

                            cp.ReferencedAssemblies.Add("system.dll");
                            cp.ReferencedAssemblies.Add(InternalEditorUtility.GetEngineAssemblyPath());
                            cp.ReferencedAssemblies.Add(InternalEditorUtility.GetEngineCoreModuleAssemblyPath());
                            cp.ReferencedAssemblies.Add(InternalEditorUtility.GetEditorAssemblyPath());

                            /*
                             foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                             {
                                param.ReferencedAssemblies.Add(assembly.Location);
                             }
                             */

                            cp.CompilerOptions = "/t:library";
                            cp.GenerateInMemory = true;

                            CompilerResults cr;
                            if (!CompileSource(sCSCode, icc, cp, out cr, true))
                            {
                                if (!CompileSource(sCSCode, icc, cp, out cr, false))
                                    return;
                            }

                            var a = cr.CompiledAssembly;
                            var o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");

                            var t = o.GetType();
                            var mi = t.GetMethod("EvalCode");

                            var s = mi.Invoke(o, null);
                            if (s != null && s.GetType() != typeof(void))
                                UnityEngine.Debug.Log(s);
                        }
                    }
                };
            }

            private static bool CompileSource(string scriptCode, ICodeCompiler icc, CompilerParameters cp, out CompilerResults cr, bool hasReturnValue)
            {
                var sb = new StringBuilder("");
                sb.Append("using System;\n");
                sb.Append("using System.Collections;\n");
                sb.Append("using System.Collections.Generic;\n");
                sb.Append("using UnityEngine;\n");
                sb.Append("using UnityEditor;\n");

                sb.Append("namespace CSCodeEvaler{ \n");
                sb.Append("public class CSCodeEvaler{ \n");
                if (hasReturnValue)
                {
                    sb.Append("public object EvalCode(){\n");
                    sb.Append("return " + scriptCode + "; \n");
                }
                else
                {
                    sb.Append("public void EvalCode(){\n");
                    sb.Append(scriptCode + "; \n");
                }
                sb.Append("} \n");
                sb.Append("} \n");
                sb.Append("}\n");

                var script = sb.ToString();
                cr = icc.CompileAssemblyFromSource(cp, script);
                if (cr.Errors.Count > 0)
                {
                    if (!hasReturnValue || !cr.Errors[0].ErrorText.Contains("Cannot implicitly convert type `void' to `object'"))
                        UnityEngine.Debug.LogError("Error evaluating C# code: " + cr.Errors[0].ErrorText + "\n\n" + script);
                    return false;
                }

                return true;
            }
        }
    }
}
