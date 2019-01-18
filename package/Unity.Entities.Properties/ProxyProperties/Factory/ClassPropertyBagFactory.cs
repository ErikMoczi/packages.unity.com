﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Unity.Properties;

namespace Unity.Entities.Properties
{
    internal static class ClassPropertyBagFactory
    {
        public static ClassPropertyBag<ObjectContainerProxy> GetPropertyBagForObject(
            object o,
            string objectDisplayName,
            HashSet<Type> primitiveTypes)
        {
            Type type = o.GetType();

            if (_propertyBagCache.ContainsKey(type))
            {
                return _propertyBagCache[type];
            }

            var bag = new ClassPropertyBag<ObjectContainerProxy>();

            bag.AddProperty(new TypeIdClassProperty((ObjectContainerProxy c) => objectDisplayName));

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    object value = f.GetValue(o);
                    if (value == null)
                    {
                        continue;
                    }

                    if (IsPrimitiveValue(primitiveTypes, f.FieldType))
                    {
                        var objectProperty = typeof(FieldObjectProperty<>).MakeGenericType(f.FieldType);

                        bag.AddProperty((IClassProperty<ObjectContainerProxy>)Activator.CreateInstance(objectProperty, f));
                    }
                    // TODO: only class type for now
                    else if (f.FieldType.IsClass)
                    {
                        if (f.FieldType.Name != type.Name)
                        {
                            string displayName = f.Name;

                            bag.AddProperty(new ClassObjectProxyProperty(
                                f.FieldType, displayName, value, primitiveTypes)
                                );
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (var f in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    if (IsPrimitiveValue(primitiveTypes, f.PropertyType))
                    {
                        var objectProperty = typeof(CSharpPropertyObjectProperty<>).MakeGenericType(f.PropertyType);

                        bag.AddProperty((IClassProperty<ObjectContainerProxy>)Activator.CreateInstance(objectProperty, f));
                    }
                    // TODO: only class type for now
                }
                catch (Exception)
                {
                }
            }

            _propertyBagCache[type] = bag;

            return bag;
        }

        public static ClassPropertyBag<ObjectContainerProxy> GetPropertyBagForObject(object o, HashSet<Type> primitiveTypes)
        {
            return GetPropertyBagForObject(o, o.GetType().Name, primitiveTypes);
        }

        private static bool IsPrimitiveValue(ICollection<Type> primitiveTypes, Type t)
        {
            return primitiveTypes.Contains(t) || t.IsEnum;
        }

        private static readonly Dictionary<Type, ClassPropertyBag<ObjectContainerProxy>> _propertyBagCache =
            new Dictionary<Type, ClassPropertyBag<ObjectContainerProxy>>();
    }
}
