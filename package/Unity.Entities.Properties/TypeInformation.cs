﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    public interface ITypeVisitor
    {
        IPropertyContainer VisitType(ITypedMemberDescriptor type, out bool recurse);
    }

    public interface ITypedMemberDescriptor
    {
        int GetOffset();
        Type GetMemberType();
        bool IsPrimitive();
        string Name { get; }
    }

    public class FieldMemberDescriptor : ITypedMemberDescriptor
    {
        private FieldInfo Info { get; set; }

        public FieldMemberDescriptor(FieldInfo field)
        {
            Info = field;
        }

        public Type GetMemberType()
        {
            return Info.FieldType;
        }

        public bool IsPrimitive()
        {
            return Info.FieldType.IsPrimitive;
        }

        public int GetOffset()
        {
            return UnsafeUtility.GetFieldOffset(Info);
        }

        public string Name
        {
            get { return Info.Name; }
        }
    }

    public class PropertyMemberDescriptor : ITypedMemberDescriptor
    {
        private PropertyInfo Info { get; set; }

        public PropertyMemberDescriptor(PropertyInfo p)
        {
            Info = p;
        }

        public Type GetMemberType()
        {
            return Info.PropertyType;
        }

        public bool IsPrimitive()
        {
            return Info.PropertyType.IsPrimitive;
        }
        public string Name
        {
            get { return Info.Name; }
        }

        public int GetOffset()
        {
            // TODO(WIP) This is broken for now obviously ... 
            return 0;
        }
    }

    public class TypeFieldIterator
    {
        [Flags]
        public enum Specifier
        {
            Public = 0x01,
            Private = 0x02,
            ValueType = 0x04,
        }

        public TypeFieldIterator(Type t)
        {
            _t = t;
        }

        public IEnumerable<ITypedMemberDescriptor> Get(Specifier d)
        {
            if (_t == null)
            {
                yield return null;
            }
            foreach (var field in _t.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }
                if (field.FieldType.IsValueType != d.HasFlag(Specifier.ValueType))
                {
                    continue;
                }
                if (field.IsPublic && d.HasFlag(Specifier.Public))
                {
                    yield return new FieldMemberDescriptor(field);
                }
                if (!field.IsPublic && d.HasFlag(Specifier.Private))
                {
                    yield return new FieldMemberDescriptor(field);
                }
            }
        }
        private Type _t;
    }

    public class TypePropertyIterator
    {
        [Flags]
        public enum Specifier
        {
            Public = 0x01,
            Private = 0x02,
            ValueType = 0x04,
        }

        public TypePropertyIterator(Type t)
        {
            _t = t;
        }

        public IEnumerable<ITypedMemberDescriptor> Get(Specifier d)
        {
            if (_t == null)
            {
                yield break;
            }
            yield break;
#if ENABLE_CSHARP_PROPERTY_PARSING
            foreach (var p in _t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.GetGetMethod(true).IsPublic)
                {
                    continue;
                }

                // we dont support recursion for now ... 
                if (_t == p.PropertyType)
                {
                    continue;
                }

                if (p.PropertyType.IsValueType != d.HasFlag(Specifier.ValueType))
                {
                    continue;
                }

                yield return new PropertyMemberDescriptor(p);
            }
#endif
        }
        private Type _t;
    }

    public class PropertyCodeReflectionParser
    {
        public HashSet<Type> PrimitiveTypes { get; set; }

        private class TypeTreeParsingContext
        {
            public int Offset { get; set; } = 0;
        }

        public IPropertyBag Parse(Type t, string propertyDisplayName)
        {
            return DoParse(t, new TypeTreeParsingContext(), propertyDisplayName);
        }

        private void AddPropertiesFromIterator(
            IEnumerable<ITypedMemberDescriptor> members,
            TypeTreeParsingContext context,
            Dictionary<string, IStructProperty<StructProxy>> properties)
        {
            foreach (var member in members)
            {
                try
                {
                    IStructProperty<StructProxy> property =
                        VisitType(member, new TypeTreeParsingContext() { Offset = context.Offset + member.GetOffset() });

                    if (property != null && !properties.ContainsKey(property.Name))
                    {
                        properties[property.Name] = property;
                    }
                }
                catch (Exception)
                { }
            }
        }

        private IPropertyBag DoParse(Type t, TypeTreeParsingContext context, string propertyDisplayName)
        {
            if (_propertyBagCache.ContainsKey(t))
            {
                return _propertyBagCache[t];
            }

            string a = propertyDisplayName;
            Dictionary<string, IStructProperty<StructProxy>> properties = new Dictionary<string, IStructProperty<StructProxy>>()
            {
                { ComponentIdProperty.Name, new TypeIdStructProperty((ref StructProxy c) => a) }
            };

            AddPropertiesFromIterator(
                new TypeFieldIterator(t).Get(TypeFieldIterator.Specifier.Public | TypeFieldIterator.Specifier.ValueType),
                context,
                properties);

            // TODO: add support for C# auto/non auto properties

            AddPropertiesFromIterator(
                new TypePropertyIterator(t).Get(TypePropertyIterator.Specifier.Public | TypePropertyIterator.Specifier.ValueType),
                context,
                properties);

            _propertyBagCache[t] = new StructPropertyBag<StructProxy>(properties.Values.ToList());

            return _propertyBagCache[t];
        }

        private IStructProperty<StructProxy> VisitType(
            ITypedMemberDescriptor d,
            TypeTreeParsingContext context)
        {
            Type memberType = d.GetMemberType();

            if (!typeof(IComponentData).IsAssignableFrom(memberType))
            {
                if (memberType.IsEnum)
                {
                    // Same hack as below
                    // TODO: this is a hack until we have code gen
                    var propertyType = typeof(EnumPrimitiveProperty<>).MakeGenericType(memberType);
                    return (IStructProperty<StructProxy>)Activator.CreateInstance(propertyType, d);
                }

                if (PrimitiveTypes.Contains(d.GetMemberType()))
                {
                    var propertyType = typeof(PrimitiveStructProperty<>).MakeGenericType(memberType);
                    return (IStructProperty<StructProxy>)Activator.CreateInstance(propertyType, d);
                }

                if (memberType.IsPrimitive)
                {
                    throw new NotSupportedException($"Primitive field type {memberType} is not supported");
                }
            }

            return new NestedStructProxyProperty(d) { PropertyBag = Parse(memberType, d.Name) };
        }

        private static readonly IStructProperty<StructProxy> ComponentIdProperty = new TypeIdStructProperty(
            (ref StructProxy c) => c.type.Name);

        private readonly Dictionary<Type, IPropertyBag> _propertyBagCache = new Dictionary<Type, IPropertyBag>();
    }

    public static class TypeInformation
    {
        private static PropertyCodeReflectionParser _propertyTypeParser = new PropertyCodeReflectionParser();

        public static IPropertyBag GetOrCreate(Type componentType, HashSet<Type> primitiveTypes)
        {
            _propertyTypeParser.PrimitiveTypes = primitiveTypes;

            string propertyDisplayName = componentType.Name;

            return _propertyTypeParser.Parse(componentType, propertyDisplayName);
        }
    }
}
