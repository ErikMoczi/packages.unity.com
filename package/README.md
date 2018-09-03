# Unity Properties package

Interfaces and utilities to describe and visit data containers efficiently.

## Getting Started

TODO

## Test Suite

The Properties package uses an extensive test suite to make sure that code is covered and functionality stable.

The test suite can be run from the Unity Editor as part of the Unity.Properties.EditorTests assembly.

## API Examples

**NOTE** : The API changes a lot and is not stable yet.

### Properties Serialization

A PropertyContainer describes a type tree structure. A container has a version and is made of Properties, that can either be containers themselves or primitive properties.

In code, the structure of the type tree is represented and described using a specific data structure `PropertyTypeNode`.

The `PropertyTypeNode` class is used as an intermediate representation for the container type tree used when serializing:

* To and From JSON,
* To C# code using `ReflectionJsonSchemaGenerator`,
* From assembly introspection using `CSharpGenerationBackend`,

##### Json Serialization

Serializing From JSON to a `PropertyTypeNode`

```
using Unity.Properties.Editor.Serialization;

var json = $@"
{{
    ""Version"": ""<my-version>"",
    ""Types"": [
    {{
      ""Name"": ""HelloWorld"",
      ""Properties"":
	  [
        {{
	      ""Name"": ""Data"", 
          ""Type"": ""int"",
        }},
      ]
    }}
    ]
}}
";

var propertyTypeTree = JsonSchema.FromJson(json);
```

Serializing To JSON From a `PropertyTypeNode`

```
using Unity.Properties.Editor.Serialization;

var json = $@"
{{
    ""Version"": ""<my-version>"",
    ""Types"": [
        {{
        ""Name"": ""HelloWorld"",
        ""Properties"":
		[
            {{
				""Name"": ""Data"", 
                ""Type"": ""int"",
            }},
        ]
        }}
    ]
}}
";

var propertyTypeTree = JsonSchema.FromJson(json);
var propertyTypeTreeAsJson = JsonSchema.ToJson(propertyTypeTree);

// `json` and `propertyTypeTreeAsJson` should be equivalent
```

##### Json Schema Validation

```
using Unity.Properties.Editor.Serialization;

var json = $@"
{{
    ""Version"": ""<my-version>"",
    ""Types"": [
    {{
      ""Name"": ""HelloWorld"",
      ""Properties"":
	  [
        {{
	      ""Name"": ""Data"", 
          ""Type"": ""int"",
        }},
      ]
    }}
    ]
}}
";

var validator = new JsonSchemaValidator();
var result = validator.ValidatePropertyDefinition(json);
Debug.Log($"Validation result: {result.IsValid ? "success" : "failure"} '{result.Error}'");

```

Serializing To JSON From a `PropertyTypeNode`

```
using Unity.Properties.Editor.Serialization;

var json = $@"
{{
    ""Version"": ""<my-version>"",
    ""Types"": [
    {{
      ""Name"": ""HelloWorld"",
      ""Properties"":
	  [
        {{
	      ""Name"": ""Data"", 
          ""Type"": ""int"",
        }},
      ]
    }}
    ]
}}
";

var propertyTypeTree = JsonSchema.FromJson(json);
var propertyTypeTreeAsJson = JsonSchema.ToJson(propertyTypeTree);

// `json` and `propertyTypeTreeAsJson` should be equivalent
```

##### Code Generation

The `PropertyTypeNode` type tree intermediate representation can be serialized to C# Code.

```
using Unity.Properties.Editor.Serialization;

var json = $@"
{{
    ""Version"": ""<my-version>"",
    ""Types"": [
    {{
      ""Name"": ""HelloWorld"",
      ""Properties"":
	  [
        {{
	      ""Name"": ""Data"", 
          ""Type"": ""int"",
        }},
      ]
    }}
    ]
}}
";

var propertyTypeTree = JsonSchema.FromJson(json);

var generator = new CSharpGenerationBackend();
backend.Generate(propertyTypeTree);

var code = backend.Code.ToString();
```


##### Assembly Introspection


```
using Unity.Properties.Editor.Serialization;

string assemblyFilePath = "<path to the assembly to be introspected>";

var propertyTypeTree = ReflectionPropertyTree.Read(assemblyFilePath);

// The `propertyTypeTree` can then be serialized to json or c#

var generator = new CSharpGenerationBackend();
backend.Generate(propertyTypeTree);


```

