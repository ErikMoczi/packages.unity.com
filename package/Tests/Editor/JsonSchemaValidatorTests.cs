#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Unity.Properties.Editor.Serialization;
using UnityEngine;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class JsonSchemaValidatorTests
    {
        [Test]
        public void MakeSure_AllSchemaKeys_AreCovered_ByTheValidator()
        {
            var allValidatorKeys = JsonSchemaValidator.CollectAllObjectValidatorKeys();
            allValidatorKeys.Sort();

            var allJsonSchemaKeys = typeof(JsonSchema.Keys).GetFields().Select(f => f.GetValue(null).ToString()).ToList();
            allJsonSchemaKeys.Sort();

            Assert.AreEqual(
                allValidatorKeys,
                allJsonSchemaKeys
                );
        }

        [Test]
        public void WhenInvalidRootVersion_Validator_FailsValidation()
        {
            const string schema = @"
{
  ""Version"": 1,
  ""Using"":[""System"", ""Unity.Tiny.FooBar""],
  ""Types"": []
}
";
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsFalse(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }

        [Test]
        public void WhenAbsentRootTypes_Validator_FailsValidation()
        {
            const string schema = @"
{
  ""Version"": 1,
  ""Using"":[""System"", ""Unity.Tiny.FooBar""],
}
";
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsFalse(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }

        [Test]
        public void WhenTypeWithNoName_Validator_FailsValidation()
        {
            const string schema = @"
{
  ""Version"": 1,
  ""Using"":[""System"", ""Unity.Tiny.FooBar""],
  ""Types"": [ { ""Properties"" : {} } ]
}
";
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsFalse(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }

        [Test]
        public void WhenPropertyNoName_Validator_FailsValidation()
        {
            const string schema = @"
{
  ""Version"": 1,
  ""Using"":[""System"", ""Unity.Tiny.FooBar""],
  ""Types"": [ { ""Name"": 1, ""Properties"" : { } } ]
}
";
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsFalse(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }

        [Test]
        public void WhenPropertyNameNotString_Validator_FailsValidation()
        {
            const string schema = @"
{
  ""Version"": 1,
  ""Using"":[""System"", ""Unity.Tiny.FooBar""],
  ""Types"": [ { ""Name"":  ""Foo"", ""Properties"" : {  ""Name"": 1 } } ]
}
";
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsFalse(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }

        [Test]
        public void WhenCompleteType_Validator_ValidationSucceeds()
        {
            var schema = new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                            .WithProperty("Data", "int", "5")
                            .WithProperty("Floats", "list", "", "float")
                            .WithProperty("MyStruct", "SomeData")
                    )
                    .ToJson();

            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }
            var validator = new JsonSchemaValidator();
            Assert.IsTrue(validator.ValidatePropertyDefinition(obj as IDictionary<string, object>).IsValid);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
