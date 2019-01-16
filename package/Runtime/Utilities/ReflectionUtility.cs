using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.MemoryProfiler
{
    internal class ReflectionUtility
    {
        public static List<Type> GetConcreteDerivedTypes(Type baseType, Assembly assembly)
        {
            // Request all types from the given assembly
            Type[] types = assembly.GetTypes();
            List<Type> derived = new List<Type>();

            for (int i = 0; i < types.Length; ++i)
            {
                Type type = types[i];
                if (type.IsSubclassOf(baseType) && !type.IsAbstract)
                {
                    derived.Add(type);
                }
            }

            return derived;
        }
    }

}
