#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;
using System.Collections;
using System.Linq;

using Unity.Properties.Serialization;
using System;

namespace Unity.Properties.Editor.Serialization
{
    public class PropertyConstructor
    {
        public List<KeyValuePair<string, string>> ParameterTypes { get; set; } = new List<KeyValuePair<string, string>>();
    }

    public class PropertyTypeNode
    {
        public enum TypeTag
        {
            Unknown,
            Enum,
            Class,
            Struct,
            List,
            Primitive
        }

        public static bool IsCompositeType(TypeTag t)
        {
            return IsAggregateType(t) || IsEnumerableType(t);
        }

        public static bool IsAggregateType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Class;
        }

        public static bool IsValueType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Primitive;
        }

        public static bool IsEnumerableType(TypeTag t)
        {
            return t == TypeTag.List;
        }

        public static class Defaults
        {
            public static bool IsAbstractClass { get; } = false;
            public static bool IsReadonly { get; } = true;
            public static bool IsPublicProperty { get; } = false;
            public static bool NoDefaultImplementation { get; } = false;
            public static bool IsCustomProperty { get; } = false;
            public static bool DontInitializeBackingField { get; } = false;
        }

        public Type NativeType { get; set; } = null;


        // 
        // Property definition related values
        // 

        public ContainerTypeTreePath TypePath { get; set; } = new ContainerTypeTreePath();

        public bool IsCustomProperty { get; set; } = Defaults.IsCustomProperty;

        // @TODO stored as a string for now, should be better handled
        public string DefaultValue { get; set; } = string.Empty;

        public string PropertyBackingAccessor { get; set; } = string.Empty;

        public bool DontInitializeBackingField { get; set; } = Defaults.DontInitializeBackingField;

        public PropertyTypeNode Of { get; set; }

        public bool IsReadonly { get; set; } = Defaults.IsReadonly;

        public bool IsPublicProperty { get; set; } = false;

        public PropertyConstructor Constructor = new PropertyConstructor();

        public string TypeName
        {
            get { return m_typename; }
            set
            {
                // @TODO remove string mess
                if (value == "list")
                {
                    m_typename = "List";
                }
                else
                {
                    m_typename = value;
                }
            }
        }
        private string m_typename = string.Empty;

        // @TODO a bit convoluted
        public string FullTypeName
        {
            get
            {
                var p = new ContainerTypeTreePath(TypePath);
                p.TypePath.Push(TypeName);
                return p.FullPath;
            }
        }

        public string PropertyName { get; set; } = string.Empty;

        public TypeTag Tag { get; set; } = TypeTag.Unknown;


        // 
        // Container definition related values
        // 

        public bool IsAbstractClass { get; set; } = Defaults.IsAbstractClass;

        public bool NoDefaultImplementation { get; set; } = Defaults.NoDefaultImplementation;

        public string OverrideDefaultBaseClass { get; set; } = string.Empty;

        public UserHookFlags UserHooks { get; set; } = UserHookFlags.None;

        // @TODO split the container vs property simplification used now
        public List<PropertyTypeNode> Properties = new List<PropertyTypeNode>();

        public List<PropertyTypeNode> NestedContainers = new List<PropertyTypeNode>();

        public void Visit(ContainerTypeTreePath path, IContainerTypeTreeVisitor visitor)
        {
            path.TypePath.Push(TypeName);

            visitor.VisitNestedContainer(path, this);

            foreach (var t in NestedContainers)
            {
                t.Visit(path, visitor);
            }

            path.TypePath.Pop();
        }

        public void VisitRoot(IContainerTypeTreeVisitor visitor)
        {
            var context = new ContainerTypeTreePath()
            {
                Namespace = this.TypePath.Namespace,
                TypePath = new Stack<string>(new[] {this.TypeName})
            };

            visitor.VisitContainer(context, this);

            Visit(context, visitor);
        }

        public List<string> DependantAssemblyNames()
        {
            return DependantAssemblyNamesFor(this);
        }

        private static List<string> DependantAssemblyNamesFor(PropertyTypeNode typeNode)
        {
            var assemblyNames = new List<string>();

            if (typeNode == null)
            {
                return assemblyNames;
            }
            if (typeNode.NativeType != null)
            {
                assemblyNames.Add(typeNode.NativeType.Assembly.GetName().Name);
            }
            foreach (var t in typeNode.NestedContainers)
            {
                assemblyNames.AddRange(DependantAssemblyNamesFor(t));
            }
            foreach (var p in typeNode.Properties)
            {
                assemblyNames.AddRange(DependantAssemblyNamesFor(p));
            }
            return assemblyNames;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
