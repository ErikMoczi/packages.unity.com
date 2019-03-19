using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace UnityEditor.AI.Planner.Utility
{
    // Forked from Unity.Labs.Utils to minimize dependencies
    static class TypeExtensions
    {
        static readonly List<FieldInfo> k_Fields = new List<FieldInfo>();

        /// <summary>
        /// Add all types assignable to this one to a list, using an optional predicate test
        /// </summary>
        /// <param name="type">The type to which assignable types will be matched</param>
        /// <param name="list">The list to which assignable types will be appended</param>
        /// <param name="predicate">Custom delegate to allow user filtering of type list.
        /// Return false to ignore given type</param>
        public static void GetAssignableTypes(this Type type, List<Type> list, Func<Type, bool> predicate = null)
        {
            ReflectionUtils.ForEachType(t =>
            {
                if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && (predicate == null || predicate(t)))
                    list.Add(t);
            });
        }

        /// <summary>
        /// Find all types that implement the given interface type, and append them to a list
        /// If the input type is not an interface type, no action is taken.
        /// </summary>
        /// <param name="type">The interface type whose implementors will be found</param>
        /// <param name="list">The list to which implementors will be appended</param>
        public static void GetImplementationsOfInterface(this Type type, List<Type> list)
        {
            if (type.IsInterface)
                GetAssignableTypes(type, list);
        }

        /// <summary>
        /// Search through all interfaces implemented by this type and, if any of them match the given generic interface
        /// append them to a list
        /// </summary>
        /// <param name="type">The type whose interfaces will be searched</param>
        /// <param name="genericInterface">The generic interface used to match implemented interfaces</param>
        /// <param name="interfaces">The list to which generic interfaces will be appended</param>
        public static void GetGenericInterfaces(this Type type, Type genericInterface, List<Type> interfaces)
        {
            foreach (var typeInterface in type.GetInterfaces())
            {
                if (typeInterface.IsGenericType)
                {
                    var genericType = typeInterface.GetGenericTypeDefinition();
                    if (genericType == genericInterface)
                        interfaces.Add(typeInterface);
                }
            }
        }

        /// <summary>
        /// Gets a specific field of the Type or any of its base Types
        /// </summary>
        /// <param name="type">The type which will be searched for fields</param>
        /// <param name="name">Name of the data field to get</param>
        /// <param name="bindingAttr">A bitmask specifying how the search is conducted</param>
        /// <returns>An object representing the field that matches the specified requirements, if found; otherwise, null</returns>
        public static FieldInfo GetFieldRecursively(this Type type, string name, BindingFlags bindingAttr)
        {
            var field = type.GetField(name, bindingAttr);
            if (field != null)
                return field;

            var baseType = type.BaseType;
            if (baseType != null)
                field = type.BaseType.GetFieldRecursively(name, bindingAttr);

            return field;
        }

        /// <summary>
        /// Gets all fields of the Type or any of its base Types
        /// </summary>
        /// <param name="type">Class type we are going to get fields on</param>
        /// <param name="fields">A list to which all fields of this type will be added</param>
        /// <param name="bindingAttr">A bitmask specifying how the search is conducted</param>
        public static void GetFieldsRecursively(this Type type, List<FieldInfo> fields,
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            foreach (var field in type.GetFields(bindingAttr))
            {
                fields.Add(field);
            }

            var baseType = type.BaseType;
            if (baseType != null)
                baseType.GetFieldsRecursively(fields, bindingAttr);
        }

        /// <summary>
        /// Gets the field info on a collection of classes that are from a collection of interfaces.
        /// </summary>
        /// <param name="classes">Collection of classes to get fields from.</param>
        /// <param name="fields">A list to which matching fields will be added</param>
        /// <param name="interfaceTypes">Collection of interfaceTypes to check if field type implements any interface type.</param>
        /// <param name="bindingAttr">Binding flags of fields.</param>
        public static void GetInterfaceFieldsFromClasses(this IEnumerable<Type> classes, List<FieldInfo> fields,
            IEnumerable<Type> interfaceTypes, BindingFlags bindingAttr)
        {
            foreach (var type in interfaceTypes)
            {
                if (!type.IsInterface)
                    throw new ArgumentException(string.Format("Type {0} in interfaceTypes is not an interface!", type));
            }

            foreach (var type in classes)
            {
                if(!type.IsClass)
                    throw new ArgumentException(string.Format("Type {0} in classes is not a class!", type));

                k_Fields.Clear();
                type.GetFieldsRecursively(k_Fields, bindingAttr);
                foreach (var field in k_Fields)
            {
                    var interfaces = field.FieldType.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        if (interfaceTypes.Contains(@interface))
                        {
                            fields.Add(field);
                            break;
            }
        }
                }
            }
        }

        /// <summary>
        /// Gets the first attribute of a given type.
        /// </summary>
        /// <typeparam name="TAttribute">Attribute type to return</typeparam>
        /// <param name="type">The type whose attribute will be returned</param>
        /// <param name="inherit">Whether to search this type's inheritance chain to find the attribute</param>
        public static TAttribute GetAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            Assert.IsTrue(type.IsDefined(typeof(TAttribute), inherit), "Attribute not found");
            return (TAttribute)type.GetCustomAttributes(typeof(TAttribute), inherit)[0];
        }

        /// <summary>
        /// Returns an array of types from the current type back to the declaring type that includes an inherited attribute.
        /// </summary>
        /// <typeparam name="TAttribute">Type of attribute we are checking if is defined.</typeparam>
        /// <param name="type">Type that has the attribute or inherits the attribute.</param>
        /// <param name="types">A list to which matching types will be added</param>
        public static void IsDefinedGetInheritedTypes<TAttribute>(this Type type, List<Type> types) where TAttribute : Attribute
        {
            while (type != null && type.IsDefined(typeof(TAttribute), true))
            {
                types.Add(type);
                type = type.BaseType;
            }
        }

        /// <summary>
        /// Search by name through a fields of a type and its base types and return the field if one is found
        /// </summary>
        /// <param name="type">The type to search</param>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <returns>The field, if found</returns>
        public static FieldInfo GetFieldInTypeOrBaseType(this Type type, string fieldName)
        {
            while (true)
            {
                if (type == null)
                    return null;

                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                if (field != null)
                    return field;

                type = type.BaseType;
            }
        }

        /// <summary>
        /// Returns a human-readable name for a class with its generic arguments filled in
        /// </summary>
        /// <param name="type">The type to get a name for</param>
        /// <returns>The human-readable name</returns>
        public static string GetNameWithGenericArguments(this Type type)
        {
            var name = type.Name;
            if (!type.IsGenericType)
                return name;

            // Trim off `1
            name = name.Split('`')[0];

            var arguments = type.GetGenericArguments();
            var length = arguments.Length;
            var stringArguments = new string[length];
            for (var i = 0; i < length; i++)
            {
                stringArguments[i] = arguments[i].GetNameWithGenericArguments();
            }

            return string.Format("{0}<{1}>", name, string.Join(", ", stringArguments));
        }
    }
}
