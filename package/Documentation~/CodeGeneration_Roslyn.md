# Properties Code Generation with Roslyn

## Table of Contents

*  [Introduction](#introduction)
*  [Dependencies](#dependencies)
*  [API](#api)
*  [Future Work](#future-work)

# Introduction

The Unity Properties API offers a systematic way to describe your data types. Opting in and benefiting from that data description
structure requires a certain amount of boilerplate code that your types need to implement.

To limit the amount of boilerplate code and ease up the transition to properties, one can easily use code attributes to declaratively
describe the targetted data types and drive the properties boilerplate code generation at compile time.

# Dependencies

The Roslyn backend for Unity Properties code generation depends on the following packages:

- `com.unity.incrementalcompiler` with a version > `0.0.42-preview.21`,

Modify your project's `manifest.json` file dependencies accordingly or use the package manager.

# API

Once the required dependencies are setup, a set of attributes can be used to decorate your types and control
the code generation process.

Those attributes are defined in the `Unity.Properties.Codegen.Experimental` namespace.

## The [GeneratePropertyContainer] attribute

The `[GeneratePropertyContainer]` attribute controls the generation of the code required by the Unity Properties package
for describing your types.

At compile time, types decorated with the `[GeneratePropertyContainer]` attribute will go through an additional
analyzis phase and Unity Properties compatible data description code will be generated when necessary.

Code will be generated for:

- public fields,
- public .NET properties,

A simple example for a very simple class:

```csharp
using Unity.Properties;
using Unity.Properties.Codegen.Experimental;

[GeneratePropertyContainer]
public class MyData
{
    public float Value;
};

// MyData will then be able to leverage the introspection & visitation Unity Properties capabilities
// For example:

public class Test : MonoBehaviour
{
    void Start()
    {
        var md = new MyData();

		// Use the Unity Properties Json serialization capabilities
        Debug.Log(JsonSerializer.Serialize(md));
    }
}

```

To have a fine grained control over the generated code, a set of attribute parameters can be used.

### The IncludeAccessor attribute parameter

Controls the generation of public .NET properties for a given field backed property.

The default value is `false`.

```csharp

[GeneratePropertyContainer(IncludeAccessor=true)]
public class MyData
{
    public float Value;
};

// You can then use:
// var md = new MyData();
// md.ValueProperty

```

# Future Work

* Adding proxy support for code generation,
