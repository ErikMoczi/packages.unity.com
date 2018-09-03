#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngineInternal;

namespace Unity.Properties.Editor.Serialization
{
    //
    // Schema semantics from http://json-schema.org/specification.html
    //

    public class JsonSchemaValidator
    {
        public static readonly string Schema = @"
{
  ""type"": ""object"",
  ""properties"":
  {
      ""Version"": { ""type"": ""string"" },
      ""RequiredAssemblies"":
      {
        ""type"": ""array"",
        ""items"": { ""type"": ""string"" }
      },
      ""UsingAssemblies"":
      {
        ""type"": ""array"",
        ""items"": { ""type"": ""string"" }
      },
      ""Namespace"": { ""type"": ""string"" },
      ""Types"":
      {
        ""type"": ""array"",
        ""items"":
        {
          ""id"": ""property_container_node"",
          ""type"": ""object"",
          ""properties"":
          {
            ""Name"": { ""type"": ""string"" },
            ""Namespace"": { ""type"": ""string"" },
            ""Properties"":
            {
              ""type"": ""array""
              ""items"":
              {
                ""type"": ""object"",
                ""properties"":
                {
                  ""Name"": { ""type"": ""string"" },
                  ""Type"": { ""type"": ""string"" },
                  ""ItemType"": { ""type"": ""string"" },
                  ""BackingField"": { ""type"": ""string"" },
                  ""DefaultValue"": { ""type"": ""string"" },
                  ""IsPublic"": { ""type"": ""boolean"" },
                  ""IsCustom"": { ""type"": ""boolean"" },
                  ""IsReadonlyProperty"": { ""type"": ""boolean"" },
                  ""DontInitializeBackingField"": { ""type"": ""boolean"" },
                },
                ""required"": [""Name"", ""Type""]
              }
            },
            ""GeneratedUserHooks"": { ""type"": ""string"" },
            ""OverrideDefaultBaseClass"": { ""type"": ""string"" },
            ""IsAbstractClass"": { ""type"": ""boolean"" },
            ""IsStruct"": { ""type"": ""boolean"" },
            ""NoDefaultImplementation"": { ""type"": ""boolean"" },
            ""ConstructedFrom"": { ""type"": ""string"" },
            ""NestedTypes"": {
              ""type"": ""array"",
              ""items"":
              {
                ""$ref"": ""property_container_node""
              }
            }
          }
          ""required"": [""Name""]
        }
      }
  },
  ""required"": [""Version"", ""Types""]
}
";

        public class Diagnostic
        {
            public bool IsValid { get; internal set;  } = false;
            public string Error { get; internal set; }
        }

        public Diagnostic ValidatePropertyDefinition(IDictionary<string, object> definition)
        {
            if (RootValidator == null)
            {
                throw new Exception("Could not find valid validator for schema validation.");
            }

            return new Diagnostic()
            {
                IsValid = RootValidator.IsValid(definition),
                Error = RootValidator.Status.Error
            };
        }

        public Diagnostic ValidatePropertyDefinition(string json)
        {
            object obj;
            if (!Properties.Serialization.Json.TryDeserializeObject(json, out obj))
            {
                throw new Exception("Invalid JSON data.");
            }
            if (RootValidator == null)
            {
                throw new Exception("Could not find valid validator for schema validation.");
            }
            return new Diagnostic()
            {
                IsValid = RootValidator.IsValid(obj as IDictionary<string, object>),
                Error = RootValidator.Status.Error
            };
        }

        public static List<string> CollectAllObjectValidatorKeys()
        {
            return DoCollectAllObjectValidatorKeys(RootValidator, new List<IValidator>());
        }

        private static List<string> DoCollectAllObjectValidatorKeys(
            IValidator validator, List<IValidator> visitedValidators)
        {
            var keys = new List<string>();

            if (visitedValidators.Contains(validator))
            {
                return keys.ToList();
            }

            visitedValidators.Add(validator);

            if (validator is ArrayValidator)
            {
                var av = validator as ArrayValidator;
                keys.AddRange(DoCollectAllObjectValidatorKeys(av.ItemValidator, visitedValidators));
                return keys.ToList();
            }

            var ov = validator as ObjectValidator;
            if (ov == null)
            {
                return keys.ToList();
            }

            foreach (var childValidator in ov.Validators)
            {
                keys.Add(childValidator.Key);

                keys.AddRange(DoCollectAllObjectValidatorKeys(childValidator.Value, visitedValidators));
            }

            return keys.ToList();
        }

        private static IValidator RootValidator { get; set; } = null;

        static JsonSchemaValidator()
        {
            GenerateValidator(Schema);
        }

        private interface IValidator
        {
            Diagnostic Status { get; }
            bool IsValid(object node);
        }

        private class ObjectValidator : IValidator
        {
            public List<string> Requires { get; set; } = new List<string>();

            public Dictionary<string, IValidator> Validators { get; set; } = new Dictionary<string, IValidator>();

            public Diagnostic Status { get; } = new Diagnostic();

            public bool IsValid(object node)
            {
                var typedNode = node as IDictionary<string, object>;
                if (typedNode == null)
                {
                    Status.IsValid = false;
                    Status.Error = $"Expected an object type but got '{node}'";
                    return false;
                }

                var keys = typedNode.Keys;
                foreach (var require in Requires)
                {
                    if (!keys.Contains(require))
                    {
                        Status.IsValid = false;
                        Status.Error = $"Missing required key an object type but got '{require}'";
                        return false;
                    }
                }
                
                foreach (var validator in Validators)
                {
                    if (typedNode.ContainsKey(validator.Key))
                    {
                        if ( ! validator.Value.IsValid(typedNode[validator.Key]))
                        {
                            Status.IsValid = false;
                            Status.Error = $"Failed validator for key '{validator.Key}' : {validator.Value.Status.Error}";
                            return false;
                        }
                    }
                }

                Status.IsValid = true;
                return true;
            }
        }

        private class StringTypeValidator : IValidator
        {
            public Diagnostic Status { get; } = new Diagnostic();

            public bool IsValid(object value)
            {
                if (value == null || value as string == null)
                {
                    Status.IsValid = false;
                    Status.Error = $"Expected a string type but got '{value}'";
                    return false;
                }

                Status.IsValid = true;

                return true;
            }
        }

        private class ArrayValidator : IValidator
        {
            public Diagnostic Status { get; } = new Diagnostic();

            public IValidator ItemValidator { get; set; }

            public bool IsValid(object node)
            {
                var typedNodes = node as IEnumerable;
                if (typedNodes == null)
                {
                    Status.IsValid = false;
                    return false;
                }

                Status.IsValid = false;

                return typedNodes.Cast<object>().All(typedNode => ItemValidator.IsValid(typedNode));
            }
        }
        
        private class BooleanTypeValidator : IValidator
        {
            public Diagnostic Status { get; } = new Diagnostic();

            public bool IsValid(object node)
            {
                Status.IsValid = node is bool;
                return Status.IsValid;
            }
        }

        private static IValidator GenerateArrayItemValidator(IDictionary<string, object> o)
        {
            if ( ! o.ContainsKey("items"))
            {
                throw new Exception("Invalid 'array' type, no 'items' field.");
            }

            var items = o["items"] as IDictionary<string, object>;
            if (items == null)
            {
                throw new Exception("Invalid 'array' type, invalid 'items' field.");
            }

            string referenceId;
            if (IsReferenceNode(items, out referenceId))
            {
                if (!IdToValidatorMap.ContainsKey(referenceId))
                {
                    throw new Exception($"Invalid validator schema: reference Id does not exists {referenceId}");
                }

                return IdToValidatorMap[referenceId];
            }
            else

            if ( ! items.ContainsKey("type") || ! (items["type"] is string))
            {
                throw new Exception("Invalid 'array' type, invalid 'items' field : no 'type'.");
            }

            return ValidatorFromTypeString((string) items["type"], items);
        }

        private static Dictionary<string, IValidator> IdToValidatorMap = new Dictionary<string, IValidator>();

        private static IValidator ValidatorFromTypeString(string type, IDictionary<string, object> o)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new Exception("Invalid type string used in validator creation.");
            }

            IValidator validator = null;

            switch (type)
            {
                case "object":
                    validator = new ObjectValidator();
                    break;
                case "string":
                    validator = new StringTypeValidator();
                    break;
                case "boolean":
                    validator = new BooleanTypeValidator();
                    break;
                case "array":
                    validator = new ArrayValidator();
                    break;
            }

            if (o.ContainsKey("id")
                && o["id"] is string
                && validator != null)
            {
                IdToValidatorMap.Add((string) o["id"], validator);
            }

            // Post fix

            if (type == "array")
            {
                ((ArrayValidator) validator).ItemValidator = GenerateArrayItemValidator(o);
            }
            else if (type == "object")
            {
                var v = GenerateObjectValidator(o) as ObjectValidator;
                ((ObjectValidator) validator).Requires = v.Requires;
                ((ObjectValidator) validator).Validators = v.Validators;
            }

            return validator;
        }

        private static bool IsReferenceNode(IDictionary<string, object> o, out string referenceId)
        {
            referenceId = string.Empty;

            if (o.ContainsKey("$ref") && o["$ref"] is string && ((string)o["$ref"]).Length > 0)
            {
                referenceId = (string) o["$ref"];
                return true;
            }

            return false;
        }

        private static IValidator GenerateObjectValidator(IDictionary<string, object> o)
        {
            var validator = new ObjectValidator();

            if (o.ContainsKey("required"))
            {
                var required = o["required"] as IEnumerable;
                if (required == null)
                {
                    throw new Exception("Invalid 'required' key used in validator creation.");
                }

                validator.Requires = new List<string>();
                foreach (var require in required)
                {
                    validator.Requires.Add((string)require);
                }
            }

            if ( ! o.ContainsKey("type") || (string) o["type"] != "object")
            {
                throw new Exception("Invalid validator schema: object with no valid 'type'");
            }

            if ( ! o.ContainsKey("properties"))
            {
                throw new Exception("Invalid validator schema: object with no 'properties'");
            }

            var properties = o["properties"] as IDictionary<string, object>;
            if (properties == null)
            {
                throw new Exception("Invalid validator schema: object with no valid 'properties'");
            }

            foreach (var k in properties)
            {
                var p = properties[k.Key] as IDictionary<string, object>;
                if (p == null)
                {
                    throw new Exception("Invalid validator schema: object with not valid 'properties'");
                }

                string referenceId;
                if (IsReferenceNode(p, out referenceId))
                {
                    if ( ! IdToValidatorMap.ContainsKey(referenceId))
                    {
                        throw new Exception($"Invalid validator schema: reference Id does not exists {referenceId}");
                    }

                    validator.Validators.Add(k.Key, IdToValidatorMap[referenceId]);
                }
                else
                {

                    if (!p.ContainsKey("type") && !(p["type"] is string))
                    {
                        throw new Exception("Invalid validator schema: object with no valid 'type'");
                    }

                    var type = (string)p["type"];

                    validator.Validators.Add(k.Key, ValidatorFromTypeString(type, p));
                }
            }

            return validator;
        }

        private static void GenerateValidator(string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return;
            }

            object obj;
            if ( ! Properties.Serialization.Json.TryDeserializeObject(schema, out obj))
            {
                return;
            }

            var root = obj as IDictionary<string, object>;
            if (root == null)
            {
                return;
            }

            RootValidator = GenerateObjectValidator(root);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)