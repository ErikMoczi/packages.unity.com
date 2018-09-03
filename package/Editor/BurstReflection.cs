using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Burst.Compiler.IL;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    internal static class BurstReflection
    {
        public static List<BurstCompileTarget> FindExecuteMethods(AssembliesType assemblyTypes)
        {
            var result = new List<BurstCompileTarget>();

            var valueTypes = new List<Type>();
            var interfaceToProducer = new Dictionary<Type, Type>();

            var assemblyList = GetAssemblyList(assemblyTypes);
            //Debug.Log("Filtered Assembly List: " + string.Join(", ", assemblyList.Select(assembly => assembly.GetName().Name)));

            // Find all ways to execute job types (via producer attributes)
            foreach (var assembly in assemblyList)
            {
                foreach (var t in assembly.GetTypes())
                {
                    // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                    // We are only working on plain type or generic type instance!
                    if (t.IsValueType)
                    {
                        if (!t.IsGenericTypeDefinition)
                            valueTypes.Add(t);
                    }
                    else if (t.IsInterface)
                    {
                        object[] attrs = t.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                        if (attrs.Length == 0)
                            continue;

                        JobProducerTypeAttribute attr = (JobProducerTypeAttribute)attrs[0];

                        interfaceToProducer.Add(t, attr.ProducerType);

                        //Debug.Log($"{t} has producer {attr.ProducerType}");
                    }
                }
            }


            //Debug.Log($"Mapped {interfaceToProducer.Count} producers; {valueTypes.Count} value types");

            // Revisit all types to find things that are compilable using the above producers.
            foreach (var type in valueTypes)
            {
                Type executeType = null;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    var genericLessInterface = interfaceType;
                    if (interfaceType.IsGenericType)
                        genericLessInterface = interfaceType.GetGenericTypeDefinition();

                    Type foundProducer;
                    if (interfaceToProducer.TryGetValue(genericLessInterface, out foundProducer))
                    {
                        var genericParams = new List<Type>();
                        genericParams.Add(type);
                        if (interfaceType.IsGenericType)
                            genericParams.AddRange(interfaceType.GenericTypeArguments);

                        executeType = foundProducer.MakeGenericType(genericParams.ToArray());

                        break;
                    }
                }

                if (null == executeType)
                    continue;

                try
                {
                    var executeMethod = executeType.GetMethod("Execute");
                    if (executeMethod == null)
                    {
                        throw new InvalidOperationException($"Burst reflection error. The type `{executeType}` does not contain an `Execute` method");
                    }

                    var target = new BurstCompileTarget(executeMethod, type);
                    result.Add(target);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return result;
        }

        // TODO: This is not working with method. We have to change this
        public static bool ExtractBurstCompilerOptions(Type type, out string optimizationFlags)
        {
            optimizationFlags = null;

            if (!BurstEditorOptions.EnableBurstCompilation)
            {
                return false;
            }

            var attr = type.GetCustomAttribute<ComputeJobOptimizationAttribute>();
            if (attr == null)
                return false;

            var builder = new StringBuilder();

            AddOption(builder, BurstEditorOptions.EnableBurstSafetyChecks ? GetOption(OptionSafetyChecks) : GetOption(OptionDisableSafetyChecks));

            if (attr.CompileSynchronously)
                AddOption(builder, GetOption(OptionJitEnableSynchronousCompilation));

            if (attr.Accuracy != Accuracy.Std)
                AddOption(builder, GetOption(OptionFastMath));

            //Debug.Log($"ExtractBurstCompilerOptions: {type} {optimizationFlags}");

            // AddOption(builder, GetOption(OptionJitEnableModuleCachingDebugger));
            // AddOption(builder, GetOption(OptionJitCacheDirectory, "Library/BurstCache"));

            optimizationFlags = builder.ToString();
            //Debug.Log(optimizationFlags);

            return true;
        }

        private static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append(' ');

            builder.Append(option);
        }

        /// <summary>
        /// Collects all assemblies - transitively that are valid for the specified type `Player` or `Editor`
        /// </summary>
        /// <param name="assemblyTypes">The assembly type</param>
        /// <returns>The list of assemblies valid for this platform</returns>
        private static List<System.Reflection.Assembly> GetAssemblyList(AssembliesType assemblyTypes)
        {
            // TODO: Not sure there is a better way to match assemblies returned by CompilationPipeline.GetAssemblies
            // with runtime assemblies contained in the AppDomain.CurrentDomain.GetAssemblies()

            // Filter the assemblies
            var assemblyList = CompilationPipeline.GetAssemblies(assemblyTypes);

            var assemblyNames = new HashSet<string>();
            foreach (var assembly in assemblyList)
            {
                CollectAssemblyNames(assembly, assemblyNames);
            }

            var allAssemblies = new HashSet<System.Reflection.Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assemblyNames.Contains(assembly.GetName().Name))
                {
                    continue;
                }
                CollectAssembly(assembly, allAssemblies);
            }

            return allAssemblies.ToList();
        }

        private static void CollectAssembly(System.Reflection.Assembly assembly, HashSet<System.Reflection.Assembly> collect)
        {
            if (!collect.Add(assembly))
            {
                return;
            }

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                CollectAssembly(System.Reflection.Assembly.Load(assemblyName), collect);
            }
        }

        private static void CollectAssemblyNames(UnityEditor.Compilation.Assembly assembly, HashSet<string> collect)
        {
            if (assembly == null || assembly.name == null) return;

            if (!collect.Add(assembly.name))
            {
                return;
            }

            foreach (var assemblyRef in assembly.assemblyReferences)
            {
                CollectAssemblyNames(assemblyRef, collect);
            }
        }
    }
}
