
// ReSharper disable PossibleMultipleEnumeration

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using Unity.Tiny;

using BaseTypeAttribute = TinyAttributeScanner.TypeAttribute<Unity.Tiny.TinyAttribute>;
using BaseMethodAttribute = TinyAttributeScanner.MethodAttribute<Unity.Tiny.TinyAttribute>;

internal static class TinyAttributeScanner
{
    internal struct TypeAttribute<TAttribute>
        where TAttribute : TinyAttribute
    {
        public readonly Type Type;
        public readonly TAttribute Attribute;

        public TypeAttribute(Type type, TAttribute attribute)
        {
            Type = type;
            Attribute = attribute;
        }
    }

    internal struct MethodAttribute<TAttribute>
        where TAttribute : TinyAttribute
    {
        public readonly Type Type;
        public readonly MethodInfo Method;
        public readonly TAttribute Attribute;

        public MethodAttribute(Type type, MethodInfo method, TAttribute attribute)
        {
            Type = type;
            Method = method;
            Attribute = attribute;
        }
    }

    #region Fields
    private static readonly List<BaseTypeAttribute> s_AttributesOnType = new List<BaseTypeAttribute>();
    private static readonly List<BaseMethodAttribute> s_AttributesOnMethod = new List<BaseMethodAttribute>();
    private static readonly List<Type> s_CompiledTypesInEditor = new List<Type>();
    #endregion

    #region Properties
    public static IReadOnlyList<Type> CompiledTypesInEditor => s_CompiledTypesInEditor.AsReadOnly();
    #endregion

    #region API
    public static IEnumerable<TypeAttribute<TAttribute>> GetTypeAttributes<TAttribute>()
        where TAttribute : TinyAttribute
    {
        return s_AttributesOnType
                .Convert<TAttribute>()
                .OrderBy(t => t.Attribute.Order);
    }

    public static IEnumerable<MethodAttribute<TAttribute>> GetMethodAttributes<TAttribute>()
        where TAttribute : TinyAttribute
    {
        return s_AttributesOnMethod
                .Convert<TAttribute>()
                .OrderBy(t => t.Attribute.Order);
    }
    #endregion

    #region Implementation
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void ScanAttributes()
    {
        s_CompiledTypesInEditor.AddRange(GetAllTypesCompiledInTheEditor());
        foreach (var type in s_CompiledTypesInEditor)
        {
            var tinyAttributes = type.GetCustomAttributes<TinyAttribute>();
            foreach (var attribute in tinyAttributes)
            {
                s_AttributesOnType.Add(new BaseTypeAttribute(type, attribute));
            }

            foreach (var method in GetAllMethodInfo(type))
            {
                var tinyMethodAttributes = method.GetCustomAttributes<TinyAttribute>();
                foreach (var attribute in tinyMethodAttributes)
                {
                    s_AttributesOnMethod.Add(new BaseMethodAttribute(type, method, attribute));
                }
            }
        }
    }

    private static IEnumerable<Type> GetAllTypesCompiledInTheEditor()
    {
        IEnumerable<Type> types = new Type[0];

        var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
        foreach (var unityAssembly in assemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.Load(unityAssembly.name);
                types = types.Concat(assembly.GetTypes());
            }
            catch (System.IO.FileNotFoundException)
            {
                // Skip.
            }
        }

        return types;
    }
    #endif

    private static IEnumerable<TypeAttribute<TAttribute>> Convert<TAttribute>(this IEnumerable<BaseTypeAttribute> typeAttributes)
        where TAttribute : TinyAttribute
        => typeAttributes
            .Where(t => t.Attribute is TAttribute)
            .Select(t => new TypeAttribute<TAttribute>(t.Type, (TAttribute)t.Attribute));

    private static IEnumerable<MethodAttribute<TAttribute>> Convert<TAttribute>(this IEnumerable<BaseMethodAttribute> methodAttributes)
        where TAttribute : TinyAttribute
        => methodAttributes
            .Where(t => t.Attribute is TAttribute)
            .Select(t => new MethodAttribute<TAttribute>(t.Type, t.Method, (TAttribute)t.Attribute));

    private static IEnumerable<MethodInfo> GetAllMethodInfo(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }
    #endregion
}
