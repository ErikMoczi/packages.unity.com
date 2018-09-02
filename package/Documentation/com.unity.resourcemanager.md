# Unity Resource Manager

The __ResourceManager__ is a high level API that asynchronously provides and releases resources. Resources can be anything from an asset bundle to a single boolean.

The purpose of the __ResourceManager__ API is to provide a consistent way to access resources while abstracting out specific loading implementations.  It also provides dependency resolution and memory management functionality to simplify game code that needs to deal with loading assets.  To accomplish this, __ResourceManager__ implements a single API call which allows you to load assets from a variety of locations (Resources, Bundles, etc). For example, you can call `ResourceManager.LoadAsync<Texture>(myResourceLocation);` and have the resource loaded regardless of where it came from.

The __ResourceManager__ package can function as a standalone package and contains providers for all common asset types.  It can be extended to handle more complex and custom loading scenarios.  For example on how to use __ResourceManager__ as a standalone package, see the Samples directory.

### Requirements

Unity 2018.1 or greater

## Locations and Providers

The __ResourceManager__ API defines two interfaces:

* `IResourceLocation`: The interface a class must implement to specify the location, loading method, and dependencies of an asset resource. The instantiated object contains the information needed to load an asset. Creating a resource location does not load the asset.

* `IResourceProvider`: The interface a class must implement to act as a resource provider. A resource provider loads an asset described by an `IResourceLocation`. When called to load a location, the resource manager queries the registered providers by calling the `CanProvide` method on the provider and passing in the resource location of the asset. When a resource provider responds that it can retrieve an asset based on the type of location specified, the resource manager stops its search. It calls the `Provide` method on the resource provider to start an asynchronous operation that returns the requested resource. Typically this is accomplished by executing a `load` operation, but the provider could also create an asset to fulfill the request. For example, a custom provider could create composite or generated textures rather than simply loading them.