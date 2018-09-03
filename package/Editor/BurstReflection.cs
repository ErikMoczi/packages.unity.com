using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Burst.Editor
{
    internal static class BurstReflection
    {
        public static List<BurstCompileTarget> FindExecuteMethods()
        {
            var result = new List<BurstCompileTarget>();

            var valueTypes = new List<Type>();
            var interfaceToProducer = new Dictionary<Type, Type>();

            // TODO: filter to make sure that we don't process editor only assemblies

            // Find all ways to execute job types (via producer attributes)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                    // We are only working on plain type or generic type instance!
                    if (t.IsValueType && !t.IsGenericTypeDefinition)
                        valueTypes.Add(t);

                    if (!t.IsInterface)
                        continue;

                    object[] attrs = t.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                    if (attrs.Length == 0)
                        continue;

                    JobProducerTypeAttribute attr = (JobProducerTypeAttribute)attrs[0];

                    interfaceToProducer.Add(t, attr.ProducerType);

                    //Debug.Log($"{t} has producer {attr.ProducerType}");
                }
            }

            //Debug.Log($"Mapped {interfaceToProducer.Count} producers; {valueTypes.Count} value types");

            // Revisit all types to find things that are compilable using the above producers.
            foreach (var type in valueTypes)
            {
                Type foundProducer = null;
                Type foundInterface = null;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceToProducer.TryGetValue(interfaceType, out foundProducer))
                    {
                        foundInterface = interfaceType;
                        break;
                    }
                }

                if (null == foundProducer)
                    continue;

                try
                {
                    var executeType = foundProducer.MakeGenericType(type);
                    var executeMethod = executeType.GetMethod("Execute");
                    if (executeMethod == null)
                    {
                        throw new InvalidOperationException($"Burst reflection error. The type `{executeType}` does not contain an `Execute` method");
                    }

                    var target = new BurstCompileTarget(executeMethod, type, foundInterface);
                    result.Add(target);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return result;
        }

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

            AddOption(builder, BurstEditorOptions.EnableBurstSafetyChecks ? "-enable-safety-checks" : "-disable-safety-checks");

            if (attr.CompileSynchronously)
                AddOption(builder, "-enable-synchronous-compilation");

            if (attr.Accuracy != Accuracy.Std)
                AddOption(builder, "-fast-math");

            //Debug.Log($"ExtractBurstCompilerOptions: {type} {optimizationFlags}");

            // AddOption(builder, "-enable-module-caching-debugger");
            // AddOption(builder, "-cache-directory=Library/BurstCache");

            optimizationFlags = builder.ToString();

            return true;
        }

        private static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append(' ');

            builder.Append(option);
        }
    }
}
