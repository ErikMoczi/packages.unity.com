# Unity Properties package

Interfaces and utilities to describe and visit data containers efficiently.

## Getting Started

TODO

## Test Suite

The Properties package uses an extensive test suite to make sure that code is covered and functionality stable.

The test suite can be run from the Unity Editor as part of the Unity.Properties.EditorTests assembly.

## API Examples

**NOTE** : The API changes a lot and is not stable yet.

### Properties Runtime

A property container is a "host" for a list of properties. It contains a property 'bag' that is a list of the hosted properties.

The end result is a typed property tree for which nodes are other property containers and leaves are 'primitives'.

#### Properties Creation

Creating a property container means creating an IPropertyContainer derived class

```

// A plain data property container

class FloatDataComponentContainer : IPropertyContainer
{
    private int m_FloatData;

    public static readonly IProperty<FloatDataComponentContainer, float> FloatDataProperty = new Property<FloatDataComponentContainer, float>(
        nameof(FloatData), c => c.m_FloatData, (c, v) => c.m_FloatData = v);

    public int FloatData
    {
        get { return FloatDataProperty.GetValue(this); }
        set { FloatDataProperty.SetValue(this, value); }
	}

    public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

    public IPropertyBag PropertyBag => sBag;
    private static PropertyBag sBag = new PropertyBag(FloatDataProperty);
}

```

A property container can itself contain a property container.

```

class DataProxyContainer : IPropertyContainer
{
    private MyDataContainer m_MyDataContainer;

    public static readonly IProperty<DataProxyContainer, DataComponentContainer> DataContainerProperty =
		new ContainerProperty<DataProxyContainer, DataComponentContainer>(
		        nameof(DataContainer),
				c => c.m_MyDataContainer,
				(c, v) => c.m_MyDataContainer = v);

    public int DataContainer
    {
        get { return DataContainerProperty.GetValue(this); }
        set { DataContainerProperty.SetValue(this, value); }
	}

    public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

    public IPropertyBag PropertyBag => sBag;
    private static PropertyBag sBag = new PropertyBag(DataContainerProperty);
}

```

#### Properties Accessors

```

var container = new DataComponentContainer { FloatData = 1.0f };

var value = DataComponentContainer.FloatDataProperty.GetValue(container);
DataComponentContainer.FloatDataProperty.SetValue(container, 2.0f);

var proxyContainer = new DataProxyContainer { DataContainer = container };

var dataContainerValue = DataProxyContainer.DataContainerProperty.GetValue(proxyContainer);

DataComponentContainer.FloatDataProperty.SetValue(dataContainerValue, 3.0f);

```

#### Properties Paths

A very convenient way to access properties in a given property trees is to use property paths.

```

class DataComponentContainer : IPropertyContainer
{
    private List<float> m_floats = new List<float>();

    public static readonly ListProperty<DataProxyContainer, List<float>, float> FloatsProperty =
        new ListProperty<DataProxyContainer, List<float>, float>(
			nameof(Floats),
            c => c.m_floats,
            null,
            null);

    public List<float> Floats
    {
        get { return FloatsProperty.GetValue(this); }
    }

    public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

    public IPropertyBag PropertyBag => sBag;
    private static PropertyBag sBag = new PropertyBag(FloatsProperty);
}

class DataProxyContainer : IPropertyContainer
{
    public static readonly IProperty<DataProxyContainer, DataComponentContainer> DataContainerProperty =
		new ContainerProperty<DataProxyContainer, DataComponentContainer>(
		        nameof(DataContainer),
				c => c.m_MyDataContainer,
				(c, v) => c.m_MyDataContainer = v);

    public int DataContainer
    {
        get { return DataContainerProperty.GetValue(this); }
        set { DataContainerProperty.SetValue(this, value); }
	}

    public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

    public IPropertyBag PropertyBag => sBag;
    private static PropertyBag sBag = new PropertyBag(DataContainerProperty);
}

var proxyContainer = new DataProxyContainer
{
	DataContainer = new DataComponentContainer { Floats = new List<float> { 1.0f, 2.0f, 3.0f } }
};

var result = PropertyPath.Parse("DataContainer.Floats[1]").Resolve(proxyContainer);
if (result.success)
{
	var myFloat = result.value;
	var myProperty = result.property;

	// Direct property accessor

	myProperty.SetValue(proxyContainer.DataContainer, 10);
	myProperty.GetValue(proxyContainer.DataContainer);
}

```

#### Properties Visit

```

var container = new FloatDataComponentContainer { FloatData = 1.0f };

private class DataComponentContainerVisitor : PropertyVisitor, ICustomVisit<float>
{
	protected override void Visit<TValue>(TValue value)
    {
		// Generic visit method
	}
	
	void ICustomVisit<float>.CustomVisit(float value)
	{
		// Access to FloatData value from DataComponentContainer
	}
}

var visitor = new DataComponentContainerVisitor();
container.Visit(visitor);

```

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

