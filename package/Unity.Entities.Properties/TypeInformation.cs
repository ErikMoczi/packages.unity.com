using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    public static class TypeInformation
    {
        private static readonly Dictionary<Type, PropertyBag> s_PropertyBagCache = new Dictionary<Type, PropertyBag>();

        public static PropertyBag GetOrCreate(Type componentType)
        {
            PropertyBag result;
            if (s_PropertyBagCache.TryGetValue(componentType, out result))
            {
                return result;
            }

            var properties = new List<IProperty> {ComponentIdProperty};
            foreach (var field in componentType.GetFields())
            {
                // we only support public struct fields in this model
                if (field.IsPublic && field.FieldType.IsValueType)
                {
                    IProperty property;

                    if (typeof(IComponentData).IsAssignableFrom(field.FieldType))
                    {
                        property = new NestedProxyProperty(field);
                    }
                    else
                    {
                        // assumption: use an IOptimizedVisitor everywhere
                        if (OptimizedVisitor.Supports(field.FieldType))
                        {
                            // primitive
                            // TODO: this is a hack until we have code gen
                            var propertyType = typeof(PrimitiveProperty<>).MakeGenericType(field.FieldType);
                            property = (IProperty) Activator.CreateInstance(propertyType, field);
                        }
                        else
                        {
                            if (field.FieldType.IsPrimitive)
                            {
                                throw new NotSupportedException($"Primitive field type {field.FieldType} is not supported");
                            }

                            // composite
                            property = new NestedProxyProperty(field);
                        }
                    }

                    properties.Add(property);
                }
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
            public int FieldOffset { get; }
            public Type ComponentType { get; }
            public PropertyBag PropertyBag { get; }

            public NestedProxyProperty(FieldInfo field)
                : base(field.Name, null, null, null)
            {
                FieldOffset = UnsafeUtility.GetFieldOffset(field);
                ComponentType = field.FieldType;
                PropertyBag = GetOrCreate(ComponentType);
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
            private int FieldOffset { get; }

            public PrimitiveProperty(FieldInfo field) : base(field.Name, null, null)
            {
                FieldOffset = UnsafeUtility.GetFieldOffset(field);
            }

            public override TValue GetValue(ref StructProxy container)
            {
                TValue v;
                UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
                return v;
            }
        }
    }
}
