#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class TypeResolverTests
    {
        [Test]
        public void PrimitiveTypesArePromitiveTypes()
        {
            Assert.IsTrue(TypeResolver.IsPrimitiveType("int"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(int).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("bool"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(bool).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("char"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(char).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("uint"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(uint).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("long"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(long).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("ulong"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(ulong).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("byte"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(byte).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("sbyte"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(sbyte).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("short"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(short).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("ushort"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(ushort).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("float"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(float).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("double"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(double).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("string"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(string).FullName));

            Assert.IsTrue(TypeResolver.IsPrimitiveType("object"));
            Assert.IsTrue(TypeResolver.IsPrimitiveType(typeof(object).FullName));
        }

        [Test]
        public void ListIsResolvedAsList()
        {
            Assert.IsTrue(new TypeResolver(null, null).Resolve(new ContainerTypeTreePath(), "List") == PropertyTypeNode.TypeTag.List);
        }

        [Test]
        public void WhenInvalidContext_Resolve_ReturnsError()
        {
            Assert.IsTrue(new TypeResolver(null, null).Resolve(null, "type") == PropertyTypeNode.TypeTag.Unknown);
        }

        [Test]
        public void WhenInvalidTypename_Resolve_ReturnsError()
        {
            Assert.IsTrue(new TypeResolver().Resolve(null, "") == PropertyTypeNode.TypeTag.Unknown);
            Assert.IsTrue(new TypeResolver().Resolve(null, null) == PropertyTypeNode.TypeTag.Unknown);
            Assert.IsNull(new TypeResolver().ResolveType(""));
        }

        [TestCase("my.namespace.roottype/nested/type/roottype", "type", ExpectedResult = PropertyTypeNode.TypeTag.Class)]
        [TestCase("my.namespace.roottype/nested/type", "nested", ExpectedResult = PropertyTypeNode.TypeTag.Struct)]
        [TestCase("my.namespace.roottype/nested", "roottype", ExpectedResult = PropertyTypeNode.TypeTag.Class)]
        [TestCase("my.namespace.roottype", "type", ExpectedResult = PropertyTypeNode.TypeTag.Unknown)]
        [TestCase("my.namespace.roottype/nested", "mytype", ExpectedResult = PropertyTypeNode.TypeTag.Class)]
        [TestCase("my.namespace.roottype/nested/type", "topleveltype", ExpectedResult = PropertyTypeNode.TypeTag.Enum)]
        public PropertyTypeNode.TypeTag WhenContextAndBuiltinTypes_Resolve_Works(
            string contextTypePath,
            string typeName)
        {
            var builtinTypes = new Dictionary<string, PropertyTypeNode.TypeTag>
            {
                { "my.namespace.roottype/nested/type/roottype", PropertyTypeNode.TypeTag.Enum },
                { "my.namespace.roottype/nested/type", PropertyTypeNode.TypeTag.Class },
                { "my.namespace.roottype/nested", PropertyTypeNode.TypeTag.Struct },
                { "my.namespace.roottype", PropertyTypeNode.TypeTag.Class },
                { "my.namespace.mytype", PropertyTypeNode.TypeTag.Class },
                { "topleveltype", PropertyTypeNode.TypeTag.Enum },
            };

            return new TypeResolver()
                .WithBuiltinSymbols(builtinTypes)
                .Resolve(
                    ContainerTypeTreePath.CreateFromString(contextTypePath),
                    typeName
                    );
        }
    }
}

#endif
