using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Serialization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Entities.Properties
{
    public static class TypeInformation
    {
        private static readonly Dictionary<Type, PropertyBag> s_PropertyBagCache = new Dictionary<Type, PropertyBag>();

        // TODO obviously refactor those 2 following functions
        private static IProperty VisitField(
            FieldInfo f,
            bool isPublic,
            Type type,
            HashSet<Type> primitiveTypes)
        {
            IProperty property = null;
            
            if (isPublic && type.IsValueType)
            {
                if (typeof(IComponentData).IsAssignableFrom(type))
                {
                    property = new NestedProxyProperty(f, primitiveTypes);
                }
                else
                {
                    if (type.IsEnum)
                    {
                        // Same hack as below
                        // TODO: this is a hack until we have code gen
                        var propertyType = typeof(EnumPrimitiveProperty<>).MakeGenericType(type);
                        property = (IProperty)Activator.CreateInstance(propertyType, f);
                    }
                    else if (primitiveTypes.Contains(type))
                    {
                        var propertyType = typeof(PrimitiveProperty<>).MakeGenericType(type);
                        property = (IProperty)Activator.CreateInstance(propertyType, f);
                    }
                    else
                    {
                        if (type.IsPrimitive)
                        {
                            throw new NotSupportedException($"Primitive field type {type} is not supported");
                        }
                        property = new NestedProxyProperty(f, primitiveTypes);
                    }
                }
            }

            return property;
        }

        private static IProperty VisitProperty(
            PropertyInfo p,
            bool isPublic,
            Type type,
            HashSet<Type> primitiveTypes)
        {
            IProperty property = null;

            if (isPublic && type.IsValueType)
            {
                if (typeof(IComponentData).IsAssignableFrom(type))
                {
                    property = new NestedProxyProperty(p, primitiveTypes);
                }
                else
                {
                    if (type.IsEnum)
                    {
                        // Same hack as below
                        // TODO: this is a hack until we have code gen
                        var propertyType = typeof(EnumPrimitiveProperty<>).MakeGenericType(type);
                        property = (IProperty)Activator.CreateInstance(propertyType, p);
                    }
                    else if (primitiveTypes.Contains(type))
                    {
                        var propertyType = typeof(PrimitiveProperty<>).MakeGenericType(type);
                        property = (IProperty)Activator.CreateInstance(propertyType, p);
                    }
                    else
                    {
                        if (type.IsPrimitive)
                        {
                            throw new NotSupportedException($"Primitive field type {type} is not supported");
                        }
                        property = new NestedProxyProperty(p, primitiveTypes);
                    }
                }
            }

            return property;
        }

        public static PropertyBag GetOrCreate(Type componentType, HashSet<Type> primitiveTypes)
        {
            PropertyBag result;
            if (s_PropertyBagCache.TryGetValue(componentType, out result))
            {
                return result;
            }

            var properties = new List<IProperty> { ComponentIdProperty };

            foreach (var field in componentType.GetFields())
            {
                // we dont support recursion for now ... 
                if (componentType == field.FieldType)
                {
                    continue;
                }

                IProperty property = VisitField(field, field.IsPublic, field.FieldType, primitiveTypes);
                properties.Add(property);
            }

            foreach (var objectProperty in componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!objectProperty.CanRead || !objectProperty.GetGetMethod(true).IsPublic)
                {
                    continue;
                }

                // we dont support recursion for now ... 
                if (componentType == objectProperty.PropertyType)
                {
                    continue;
                }

                try
                {
                    IProperty property = VisitProperty(objectProperty, true, objectProperty.PropertyType, primitiveTypes);

                    // Unknown types: GameObjects, etc
                    if (property != null)
                    {
                        properties.Add(property);
                    }
                }
                catch (Exception e)
                {}
            }

            result = new PropertyBag(properties);
            s_PropertyBagCache[componentType] = result;
            return result;
        }

        private class TypeIdProperty : StructProperty<StructProxy, string>
        {
            public TypeIdProperty(GetValueMethod getValue) : base("$TypeId", getValue, null)
            {
            }
        }

        private static readonly IProperty ComponentIdProperty = new TypeIdProperty(
            (ref StructProxy c) => c.type.FullName);

        private unsafe class NestedProxyProperty : StructMutableContainerProperty<StructProxy, StructProxy>
        {
            private PropertyInfo _propertyInfo;
            public int FieldOffset { get; }
            public Type ComponentType { get; }
            public PropertyBag PropertyBag { get; }

            // @TODO review primitiveTypes
            public NestedProxyProperty(FieldInfo field, HashSet<Type> primitiveTypes)
                : base(field.Name, null, null, null)
            {
                FieldOffset = UnsafeUtility.GetFieldOffset(field);
                ComponentType = field.FieldType;
                PropertyBag = GetOrCreate(ComponentType, primitiveTypes);
                RefAccess = GetChildRef;
            }

            public NestedProxyProperty(PropertyInfo propertyInfo, HashSet<Type> primitiveTypes)
                : base(propertyInfo.Name, null, null, null)
            {
                FieldOffset = 1;// UnsafeUtility.GetFieldOffset(field);
                ComponentType = propertyInfo.PropertyType;
                PropertyBag = GetOrCreate(propertyInfo.PropertyType, primitiveTypes);
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

        private unsafe class AutoImplementedPropertiesPropertyWrapper<TValue> : StructProperty<StructProxy, TValue>
            where TValue : struct
        {
            private int FieldOffset { get; }

            public AutoImplementedPropertiesPropertyWrapper(PropertyInfo p) : base(p.Name, null, null)
            {
                var field = p.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.Name.Contains(p.Name));

                FieldOffset = UnsafeUtility.GetFieldOffset(field.First());
            }

            public override TValue GetValue(ref StructProxy container)
            {
                TValue v;
                UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
                return v;
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

            public PrimitiveProperty(FieldInfo field) : base(field.Name, null, null)
            {
                FieldOffset = UnsafeUtility.GetFieldOffset(field);
            }

            public PrimitiveProperty(PropertyInfo p) : base(p.Name, null, null)
            {
                // TODO this is only a temporary fix for now
                var field = p.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.Name.Contains(p.Name));
                FieldOffset = UnsafeUtility.GetFieldOffset(field.First());

                // TODO cache

                /*
                    Type myType = typeof(PrimitiveProperty<>.).MakeGenericType(p.PropertyType);

                    PropertyGetMethod = new DynamicMethod(p.PropertyType.Name, p.PropertyType, new Type[] { typeof(void*) });

                    ILGenerator gen = PropertyGetMethod.GetILGenerator();

                    LocalBuilder outputValue = gen.DeclareLocal(p.PropertyType);
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldloc_0);
                    gen.Emit(OpCodes.Call, UnsafeUtility.CopyPtrToStructure);

                    //                IL_0004: call void T::CopyPtrToStructure < valuetype S > (void *, !!0 &)

                    gen.Emit(OpCodes.Ldarg_0); // ??
                    gen.Emit(OpCodes.Call, p.GetMethod);
                    gen.Emit(OpCodes.Ret);
                */

                PropertyGetMethod = p.GetMethod;
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

            public EnumPrimitiveProperty(FieldInfo field) : base(field.Name, null, null)
            {
                FieldOffset = UnsafeUtility.GetFieldOffset(field);
            }

            public override TValue GetValue(ref StructProxy container)
            {
                TValue v = default(TValue);
                //                UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
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
}
