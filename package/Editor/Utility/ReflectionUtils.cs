using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.AI.Planner.Utility
{
    // Forked from Unity.Labs.Utils to minimize dependencies
    static class ReflectionUtils
    {
        static readonly List<Type[]> k_TypesPerAssembly = new List<Type[]>();
        static readonly List<Dictionary<string, Type>> k_AssemblyTypeMaps = new List<Dictionary<string, Type>>();

        /// <summary>
        /// Iterate through all assemblies and execute a method on each one
        /// Catches ReflectionTypeLoadExceptions in each iteration of the loop
        /// </summary>
        /// <param name="callback">The callback method to execute for each assembly</param>
        public static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }
        }

        /// <summary>
        /// Execute a callback for each type in every assembly
        /// </summary>
        /// <param name="callback">The callback to execute</param>
        public static void ForEachType(Action<Type> callback)
        {
            ForEachAssembly(assembly =>
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    callback(t);
                }
            });
        }

        /// <summary>
        /// Search all assemblies for a type that matches a given predicate delegate
        /// </summary>
        /// <param name="predicate">The predicate; Returns true for the type that matches the search</param>
        /// <returns>The type found, or null if no matching type exists</returns>
        public static Type FindType(Func<Type, bool> predicate)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (predicate(t))
                            return t;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }

            return null;
        }

        /// <summary>
        /// Search all assemblies for a set of types that matches a set of predicates
        /// </summary>
        /// <param name="predicates">The predicates; Returns true for the type that matches each search</param>
        /// <returns>The type found, or null if no matching type exists</returns>
        public static void FindTypesBatch(List<Func<Type, bool>> predicates, List<Type> resultList)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    k_TypesPerAssembly.Add(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }

            for (var i = 0; i < predicates.Count; i++)
            {
                var predicate = predicates[i];
                foreach (var assemblyTypes in k_TypesPerAssembly)
                {
                    foreach (var type in assemblyTypes)
                    {
                        if (predicate(type))
                            resultList[i] = type;
                    }
                }
            }
        }

        /// <summary>
        /// Search all assemblies for a set of types that matches a set of names
        /// </summary>
        /// <param name="predicates">The predicates; Returns true for the type that matches each search</param>
        /// <returns>The type found, or null if no matching type exists</returns>
        public static void FindTypesByNameBatch(List<string> typeNames, List<Type> resultList)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            k_TypesPerAssembly.Clear();
            foreach (var assembly in assemblies)
            {
                try
                {
                    k_TypesPerAssembly.Add(assembly.GetTypes());

                    var typeMap = new Dictionary<string, Type>();
                    foreach (var type in assembly.GetTypes())
                    {
                        typeMap[type.Name] = type;
                    }

                    k_AssemblyTypeMaps.Add(typeMap);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }

            foreach (var typeName in typeNames)
            {
                var found = false;
                foreach (var typeMap in k_AssemblyTypeMaps)
                {
                    Type type;
                    if (typeMap.TryGetValue(typeName, out type))
                    {
                        resultList.Add(type);
                        found = true;
                        break;
                    }
                }

                // If a type can't be found, add a null entry to the list to ensure indexes match
                if (!found)
                    resultList.Add(null);
            }
        }
    }
}
