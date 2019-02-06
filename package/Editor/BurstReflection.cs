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
            var typesVisited = new HashSet<string>();
            var typesToVisit = new HashSet<string>();
            foreach (var assembly in assemblyList)
            {
                var types = new List<Type>();
                try
                {
                    types.AddRange(assembly.GetTypes());
                    // Collect all generic type instances (excluding indirect instances)
                    CollectGenericTypeInstances(assembly, types);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Unexpected exception while collecting types in assembly `" + assembly.FullName + "` Exception: " + ex);
                }

                for (var i = 0; i < types.Count; i++)
                {
                    var t = types[i];
                    if (typesToVisit.Add(t.FullName))
                    {
                        // Because the list of types returned by CollectGenericTypeInstances does not detect nested generic classes that are not
                        // used explicitly, we need to create them if a declaring type is actually used
                        // so for example if we have:
                        // class MyClass<T> { class MyNestedClass { } }
                        // class MyDerived : MyClass<int> { }
                        // The CollectGenericTypeInstances will return typically the type MyClass<int>, but will not list MyClass<int>.MyNestedClass
                        // So the following code is correcting this in order to fully query the full graph of generic instance types, including indirect types
                        var nestedTypes = t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var nestedType in nestedTypes)
                        {
                            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                            {
                                var parentGenericTypeArguments = t.GetGenericArguments();
                                // Only create nested types that are closed generic types (full generic instance types)
                                // It happens if for example the parent class is `class MClass<T> { class MyNestedGeneric<T1> {} }`
                                // In that case, MyNestedGeneric<T1> is closed in the context of MClass<int>, so we don't process them
                                if (nestedType.GetGenericArguments().Length == parentGenericTypeArguments.Length)
                                {
                                    var instanceNestedType = nestedType.MakeGenericType(parentGenericTypeArguments);
                                    types.Add(instanceNestedType);
                                }
                            }
                            else
                            {
                                types.Add(nestedType);
                            }

                        }
                    }
                }

                foreach (var t in types)
                {
                    // If the type has been already visited, don't try to visit it
                    if (!typesVisited.Add(t.FullName) || (t.IsGenericTypeDefinition && !t.IsInterface))
                    {
                        continue;
                    }

                    try
                    {
                        if (t.IsInterface)
                        {
                            object[] attrs = t.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                            if (attrs.Length == 0)
                                continue;

                            JobProducerTypeAttribute attr = (JobProducerTypeAttribute)attrs[0];

                            interfaceToProducer.Add(t, attr.ProducerType);

                            //Debug.Log($"{t} has producer {attr.ProducerType}");
                        }
                        else if (t.IsValueType)
                        {
                            // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                            // We are only working on plain type or generic type instance!
                            if (!t.IsGenericTypeDefinition)
                                valueTypes.Add(t);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Unexpected exception while inspecting type `" + t +
                                  "` IsConstructedGenericType: " + t.IsConstructedGenericType +
                                  " IsGenericTypeDef: " + t.IsGenericTypeDefinition +
                                  " IsGenericParam: " + t.IsGenericParameter +
                                  " Exception: " + ex);
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

        public static bool ExtractBurstCompilerOptionsBasic(MemberInfo member, StringBuilder flagsOut)
        {
            if (!BurstEditorOptions.EnableBurstCompilation)
            {
                return false;
            }

            var attr = member.GetCustomAttribute<BurstCompileAttribute>();
            if (attr == null)
                return false;

            if (attr.CompileSynchronously || BurstGlobalCompilerOptions.ForceSynchronousCompilation)
                AddOption(flagsOut, GetOption(OptionJitEnableSynchronousCompilation));

            if (attr.FloatMode != FloatMode.Default)
                AddOption(flagsOut, GetOption(OptionFloatMode, attr.FloatMode));

            if (attr.FloatPrecision != FloatPrecision.Standard)
                AddOption(flagsOut, GetOption(OptionFloatPrecision, attr.FloatPrecision));

            // Add custom options
            if (attr.Options != null)
            {
                foreach (var option in attr.Options)
                {
                    if (!string.IsNullOrEmpty(option))
                    {
                        AddOption(flagsOut, option);
                    }
                }
            }

            return true;
        }

        public static bool GetBurstGeneralOptions(StringBuilder flagsOut)
        {
            if (!BurstEditorOptions.EnableBurstCompilation)
            {
                return false;
            }

            if (BurstEditorOptions.EnableBurstSafetyChecks)
            {
                AddOption(flagsOut, GetOption(OptionSafetyChecks));
            }
            else
            {
                AddOption(flagsOut, GetOption(OptionDisableSafetyChecks));
                // Enable NoAlias ahen safety checks are disable
                AddOption(flagsOut, GetOption(OptionNoAlias));
            }

            if (BurstEditorOptions.EnableShowBurstTimings)
            {
                AddOption(flagsOut, GetOption(OptionJitLogTimings));
            }
            return true;
        }

        public static bool ExtractBurstCompilerOptions(MemberInfo member, out string flagsOut)
        {
            flagsOut = null;
            // We don't fail if member == null as this method is being called by native code and doesn't expect to crash
            if (!BurstEditorOptions.EnableBurstCompilation || member == null || BurstGlobalCompilerOptions.DisableCompilation)
            {
                return false;
            }

            var flagsBuilderOut = new StringBuilder();
            if (!ExtractBurstCompilerOptionsBasic(member, flagsBuilderOut))
                return false;

            GetBurstGeneralOptions(flagsBuilderOut);
            flagsOut = flagsBuilderOut.ToString();
            return true;
        }

        private static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append('\n'); // Use \n to separate options

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
                try
                {
                    CollectAssembly(System.Reflection.Assembly.Load(assemblyName), collect);
                }
                catch (Exception)
                {
                    Debug.LogWarning("Could not load assembly " + assemblyName);
                }
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

        /// <summary>
        /// Gets the list of concrete generic type instances used in an assembly.
        /// See remarks
        /// </summary>
        /// <param name="assembly">The assembly</param>
        /// <param name="types"></param>
        /// <returns>The list of generic type instances</returns>
        /// <remarks>
        /// Note that this method fetchs only direct type instanecs but
        /// cannot fetch transitive generic type instances.
        /// </remarks>
        private static void CollectGenericTypeInstances(System.Reflection.Assembly assembly, List<Type> types)
        {
            // From: https://gist.github.com/xoofx/710aaf86e0e8c81649d1261b1ef9590e
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            // Token base id for TypeSpec
            const int mdTypeSpec = 0x1B000000;
            const int mdTypeSpecCount = 1 << 24;
            foreach (var module in assembly.Modules)
            {
                for (int i = 1; i < mdTypeSpecCount; i++)
                {
                    try
                    {
                        var type = module.ResolveType(mdTypeSpec | i);
                        if (type.IsConstructedGenericType && !type.ContainsGenericParameters)
                        {
                            types.Add(type);
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    catch (ArgumentException)
                    {
                        // Can happen on ResolveType on certain generic types, so we continue
                    }
                }
            }
        }
    }
}
