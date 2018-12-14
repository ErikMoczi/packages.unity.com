using System;
using Unity.Properties;

namespace Unity.Tiny
{
    internal static class PropertyModificationConverter
    {
        private const int UnityObjectTypeCode = -1;
        private const int EntityReferenceTypeCode = -2;
        private const int EnumReferenceTypeCode = -3;

        internal static int GetSerializedTypeId(Type type)
        {
            if (null == type)
            {
                return (int) TypeCode.Empty;
            }
            
            var typeCode = Type.GetTypeCode(type);

            if (typeCode != TypeCode.Object)
            {
                return (int) typeCode;
            }
            
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return UnityObjectTypeCode;
            }
            
            if (typeof(TinyEntity.Reference) == type)
            {
                return EntityReferenceTypeCode;
            }
            
            if (typeof(TinyEnum.Reference) == type)
            {
                return EnumReferenceTypeCode;
            }

            return (int) TypeCode.Object;
        }

        public static IPropertyModification Convert(IPropertyContainer container)
        {
            var typeId = container.GetValue<int>("TypeCode");
            return CreatePropertyModification(container, typeId);
        }
        
        private static IPropertyModification CreatePropertyModification(IPropertyContainer container, int id)
        {
            switch (id)
            {
                case (int) TypeCode.SByte:
                    return CreatePropertyModification<sbyte>(container);
                case (int) TypeCode.Int16:
                    return CreatePropertyModification<short>(container);
                case (int) TypeCode.Int32:
                    return CreatePropertyModification<int>(container);
                case (int) TypeCode.Int64:
                    return CreatePropertyModification<long>(container);
                case (int) TypeCode.Byte:
                    return CreatePropertyModification<byte>(container);
                case (int) TypeCode.UInt16:
                    return CreatePropertyModification<ushort>(container);
                case (int) TypeCode.UInt32:
                    return CreatePropertyModification<uint>(container);
                case (int) TypeCode.UInt64:
                    return CreatePropertyModification<ulong>(container);
                case (int) TypeCode.Single:
                    return CreatePropertyModification<float>(container);
                case (int) TypeCode.Double:
                    return CreatePropertyModification<double>(container);
                case (int) TypeCode.Boolean:
                    return CreatePropertyModification<bool>(container);
                case (int) TypeCode.String:
                    return CreatePropertyModification<string>(container);
                case UnityObjectTypeCode:
                    return CreatePropertyModification<UnityEngine.Object>(container);
                case EntityReferenceTypeCode:
                    return CreateContainerPropertyModification<TinyEntity.Reference>(container);
                case EnumReferenceTypeCode:
                    return CreateContainerPropertyModification<TinyEnum.Reference>(container);
            }

            return null;
        }

        private static IPropertyModification CreatePropertyModification<TValue>(IPropertyContainer container)
        {
            var target = container.GetValue<TinyType.Reference>("Target");
            var path = container.GetValue<string>("Path");
            var value = container.GetValue<TValue>("Value");
            return new ValuePropertyModification<TValue>(target, path, value);
        }
        
        private static IPropertyModification CreateContainerPropertyModification<TValue>(IPropertyContainer container)
        {
            var target = container.GetValue<TinyType.Reference>("Target");
            var path = container.GetValue<string>("Path");
            var value = container.GetValue<TValue>("Value");
            return new ContainerPropertyModification(target, path, value as IPropertyContainer);
        }
    }
}