﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Serialization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        private PropertyInfo Info { get; set;  }

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
            yield break;
            if (_t == null)
            {
                yield break;
            }
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

        public PropertyBag Parse(Type t)
        {
            return DoParse(t, new TypeTreeParsingContext());
        }

        private PropertyBag DoParse(Type t, TypeTreeParsingContext context)
        {
            if (_propertyBagCache.ContainsKey(t))
            {
                return _propertyBagCache[t];
            }

            Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>()
            {
                { ComponentIdProperty.Name, ComponentIdProperty }
            };

            foreach (var member in new TypeFieldIterator(t).Get(
                TypeFieldIterator.Specifier.Public | TypeFieldIterator.Specifier.ValueType))
            {
                IProperty property = VisitType(member, new TypeTreeParsingContext() { Offset = context.Offset + member.GetOffset() });
                if (!properties.ContainsKey(property.Name))
                {
                    properties[property.Name] = property;
                }
            }

            foreach (var member in new TypePropertyIterator(t).Get(
                TypePropertyIterator.Specifier.Public | TypePropertyIterator.Specifier.ValueType))
            {
                try
                {
                    IProperty property = VisitType(member, new TypeTreeParsingContext() { Offset = context.Offset });

                    // TODO Unknown types: GameObjects, etc
                    if (property != null)
                    {
                        if (!properties.ContainsKey(property.Name))
                        {
                            properties[property.Name] = property;
                        }
                    }
                }
                catch (Exception)
                { }
            }

            _propertyBagCache[t] = new PropertyBag(properties.Values.ToList());

            return _propertyBagCache[t];
        }

        private IProperty VisitType(ITypedMemberDescriptor d, TypeTreeParsingContext context)
        {
            Type memberType = d.GetMemberType();
            if (!typeof(IComponentData).IsAssignableFrom(memberType))
            {
                if (memberType.IsEnum)
                {
                    // Same hack as below
                    // TODO: this is a hack until we have code gen
                    var propertyType = typeof(EnumPrimitiveProperty<>).MakeGenericType(memberType);
                    return (IProperty)Activator.CreateInstance(propertyType, d);
                }
                else if (PrimitiveTypes.Contains(d.GetMemberType()))
                {
                    var propertyType = typeof(PrimitiveProperty<>).MakeGenericType(memberType);
                    return (IProperty)Activator.CreateInstance(propertyType, d);
                }

                if (memberType.IsPrimitive)
                {
                    throw new NotSupportedException($"Primitive field type {memberType} is not supported");
                }
            }

            return new NestedProxyProperty(d) { PropertyBag = Parse(memberType) };
        }

        private class TypeIdProperty : StructProperty<StructProxy, string>
        {
            public TypeIdProperty(GetValueMethod getValue) : base("$TypeId", getValue, null)
            {
            }
        }

        private static readonly IProperty ComponentIdProperty = new TypeIdProperty(
            (ref StructProxy c) => c.type.FullName);

        private readonly Dictionary<Type, PropertyBag> _propertyBagCache = new Dictionary<Type, PropertyBag>();

        private unsafe class NestedProxyProperty : StructMutableContainerProperty<StructProxy, StructProxy>
        {
            public int FieldOffset { get; }
            public Type ComponentType { get; }
            public PropertyBag PropertyBag { get; set; }

            public NestedProxyProperty(ITypedMemberDescriptor member)
                : base(member.Name, null, null, null)
            {
                FieldOffset = member.GetOffset();
                ComponentType = member.GetMemberType();
                RefAccess = GetChildRef;
            }

            private void GetChildRef(ref StructProxy container, RefVisitMethod refVisitMethod, IPropertyVisitor visitor)
            {
                var val = GetValue(ref container);
                refVisitMethod(ref val, visitor);
            }

            public override StructProxy GetValue(ref StructProxy container)
            {
                return new StructProxy()
                {
                    data = container.data + FieldOffset,
                    bag = PropertyBag,
                    type = ComponentType
                };
            }
        }

        private unsafe class PrimitiveProperty<TValue> : StructProperty<StructProxy, TValue>
            where TValue : struct
        {
            // TODO only temporary for property wrappers
            private MethodInfo PropertyGetMethod { get; }
            private MethodInfo PropertySetMethod { get; }

            private int FieldOffset { get; }

            public override bool IsReadOnly => false;

            public PrimitiveProperty(ITypedMemberDescriptor member) : base(member.Name, null, null)
            {
                FieldOffset = member.GetOffset();

                /*
                Type myType = typeof(PrimitiveProperty<>.).MakeGenericType(p.PropertyType);
                PropertyGetMethod = new DynamicMethod(p.PropertyType.Name, p.PropertyType, new Type[] { typeof(void*) });
                ILGenerator gen = PropertyGetMethod.GetILGenerator();
                LocalBuilder outputValue = gen.DeclareLocal(p.PropertyType);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldloc_0);
                gen.Emit(OpCodes.Call, UnsafeUtility.CopyPtrToStructure);
                gen.Emit(OpCodes.Ldarg_0); // ??
                gen.Emit(OpCodes.Call, p.GetMethod);
                gen.Emit(OpCodes.Ret);
                */
            }

            public override TValue GetValue(ref StructProxy container)
            {
                TValue v = default(TValue);
                UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
                return v;
            }

            public override void SetValue(ref StructProxy container, TValue value)
            {
                // @TODO ComponentJobSafetyManager.CompleteReadAndWriteDependency
                UnsafeUtility.CopyStructureToPtr(ref value, container.data + FieldOffset);
            }
        }

        private unsafe class EnumPrimitiveProperty<TValue> : StructEnumProperty<StructProxy, TValue>
            where TValue : struct, IComparable, IFormattable, IConvertible
        {
            private int FieldOffset { get; }

            public EnumPrimitiveProperty(ITypedMemberDescriptor member) : base(member.Name, null, null)
            {
                FieldOffset = member.GetOffset();
            }

            public override TValue GetValue(ref StructProxy container)
            {
                TValue v = default(TValue);
//              UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
                UnsafeUtility.MemCpy(container.data + FieldOffset, UnsafeUtility.AddressOf(ref v), UnsafeUtility.SizeOf<TValue>());
                return v;
            }

            public override void SetValue(ref StructProxy container, TValue value)
            {
                // @TODO ComponentJobSafetyManager.CompleteReadAndWriteDependency
                UnsafeUtility.CopyStructureToPtr(ref value, container.data + FieldOffset);
            }
        }
    }

    public static class TypeInformation
    {
        private static PropertyCodeReflectionParser _propertyTypeParser = new PropertyCodeReflectionParser();

        public static PropertyBag GetOrCreate(Type componentType, HashSet<Type> primitiveTypes)
        {
            _propertyTypeParser.PrimitiveTypes = primitiveTypes;
            return _propertyTypeParser.Parse(componentType);
        }
    }
}
