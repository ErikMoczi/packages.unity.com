# About the Property API

Use the Property API package to include the `Unity.Properties` namespace in your project. This namespace contains interfaces and classes you can use to:
 * describe .Net types as property containers
 * resolve properties in nested property containers
 * implement visitors to traverse property containers and perform tasks (e.g. schema generation, serialization, UI generation, etc.)

The Property API differs from the available .Net RTTI in multiple aspects:
 * it was designed with performance in mind
 * it supports dynamic type trees - you can create properties at runtime
 * it can be extended for application-specific use cases

# Installing the Property API

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html). 

# Using the Property API

**TODO**

# Technical details
## Requirements

This version of the Property API is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

To use this package, you must use the following project settings:
* .Net 4.x: To configure, open *Edit > Project Settings > Player*, and set the *Scripting Runtime Version* to *Experimental (.Net 4.x Equivalent*. Additionally, make sure *Api Compatibility Level* is set to *.Net 4.x*.

## Known limitations

**TODO**

## Document revision history

|Date|Reason|
|---|---|
|Feb 14, 2018|Document created. Matches package version 0.1.0|