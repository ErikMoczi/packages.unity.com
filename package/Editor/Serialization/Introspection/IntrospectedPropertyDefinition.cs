#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Editor.Serialization
{
    public class IntrospectedPropertyDefinition
    {
        public enum PropertyWrapperType
        {
            Unknown,
            Property,
            StructProperty,
            ContainerProperty,
            MutableContainerProperty,
            StructContainerProperty,
            StructMutableContainerProperty,
            ListProperty,
            StructListProperty,
            ContainerListProperty,
            StructContainerListProperty,
            MutableContainerListProperty,
            StructMutableContainerListProperty,
            EnumProperty,
            StructEnumProperty,
            EnumListProperty,
            StructEnumListProperty,
        }

        public bool IsValid { get; internal set; } = false;

        public PropertyWrapperType WrapperType { get; internal set; } = PropertyWrapperType.Unknown;

        public string TypeID { get; internal set; } = string.Empty;

        public PropertyTypeNode.TypeTag TypeTag { get; internal set; } = PropertyTypeNode.TypeTag.Unknown;

        public PropertyTypeNode ListOf { get; internal set; } = null;

        public bool IsReadonly { get; internal set; } = false;

        public bool IsPublic { get; internal set; } = false;

        public bool IsValueType { get; internal set; } = false;

        private static string ListTypeId { get; } = "List";

        protected bool SetUpTypesFrom(
            string innerTypeName)
        {
            TypeTag = PropertyTypeNode.TypeTag.Primitive;

            switch (WrapperType)
            {
                // --- Plain wrappers
                case PropertyWrapperType.StructProperty:
                case PropertyWrapperType.Property:
                    TypeID = innerTypeName;
                    break;
                case PropertyWrapperType.ContainerProperty:
                case PropertyWrapperType.StructContainerProperty:
                    TypeTag = PropertyTypeNode.TypeTag.Class;
                    TypeID = innerTypeName;
                    break;
                case PropertyWrapperType.MutableContainerProperty:
                case PropertyWrapperType.StructMutableContainerProperty:
                    TypeTag = PropertyTypeNode.TypeTag.Struct;
                    TypeID = innerTypeName;
                    break;

                // --- Lists
                case PropertyWrapperType.ListProperty:
                case PropertyWrapperType.StructListProperty:
                    TypeID = ListTypeId;
                    TypeTag = PropertyTypeNode.TypeTag.List;
                    ListOf = new PropertyTypeNode()
                    {
                        TypeName = innerTypeName,
                        Tag = PropertyTypeNode.TypeTag.Primitive
                    };
                    break;
                case PropertyWrapperType.StructContainerListProperty:
                case PropertyWrapperType.ContainerListProperty:
                    TypeID = ListTypeId;
                    TypeTag = PropertyTypeNode.TypeTag.List;
                    ListOf = new PropertyTypeNode()
                    {
                        TypeName = innerTypeName,
                        Tag = PropertyTypeNode.TypeTag.Primitive
                    };
                    break;
                case PropertyWrapperType.EnumListProperty:
                case PropertyWrapperType.StructEnumListProperty:
                    TypeID = ListTypeId;
                    TypeTag = PropertyTypeNode.TypeTag.List;
                    ListOf = new PropertyTypeNode()
                    {
                        TypeName = innerTypeName,
                        Tag = PropertyTypeNode.TypeTag.Enum
                    };
                    break;
                case PropertyWrapperType.MutableContainerListProperty:
                case PropertyWrapperType.StructMutableContainerListProperty:
                    TypeID = ListTypeId;
                    TypeTag = PropertyTypeNode.TypeTag.List;
                    ListOf = new PropertyTypeNode()
                    {
                        TypeName = innerTypeName,
                        Tag = PropertyTypeNode.TypeTag.Struct
                    };
                    break;

                // --- Enums
                case PropertyWrapperType.EnumProperty:
                    TypeTag = PropertyTypeNode.TypeTag.Enum;
                    TypeID = innerTypeName;
                    break;
                case PropertyWrapperType.StructEnumProperty:
                    TypeTag = PropertyTypeNode.TypeTag.Enum;
                    TypeID = innerTypeName;
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected static bool IsReadonlyPropertyType(PropertyWrapperType t)
        {
            bool isReadonly = false;
            switch (t)
            {
                case PropertyWrapperType.StructProperty:
                case PropertyWrapperType.ContainerProperty:
                case PropertyWrapperType.StructContainerProperty:
                case PropertyWrapperType.ListProperty:
                case PropertyWrapperType.StructListProperty:
                case PropertyWrapperType.ContainerListProperty:
                    isReadonly = true;
                    break;
            }
            return isReadonly;
        }

        protected static bool IsValuePropertyType(PropertyWrapperType t)
        {
            bool isValueType = false;
            switch (t)
            {
                case PropertyWrapperType.MutableContainerProperty:
                case PropertyWrapperType.StructMutableContainerProperty:
                case PropertyWrapperType.StructEnumListProperty:
                    isValueType = true;
                    break;
            }
            return isValueType;
        }

        protected static PropertyWrapperType TypeEnumFromType(string typeName)
        {
            if (!Enum.GetNames(typeof(PropertyWrapperType)).Contains(typeName))
            {
                return PropertyWrapperType.Unknown;
            }

            PropertyWrapperType t = PropertyWrapperType.Unknown;
            if (!Enum.TryParse(typeName, out t))
            {
                return PropertyWrapperType.Unknown;
            }

            return t;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
