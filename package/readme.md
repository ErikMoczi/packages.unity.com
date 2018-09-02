ResourceManager will provide a high level API for managing assets in Unity.  It provides a simple API of Load, LoadAsync, and Release for now.
All methods are templated by requested type and templated async operations are returned, which themselves have templated completion events.

The specific method and location of loading assets is abstracted and all loads are done via addressable asset names.  
These names will be assignable in the editor via by assigning an asset to a bundle, using it as a target of an AssetReference, or putting it in a Resources folder.

Assets that are in bundles will trigger the loading of bundles and its dependencies.  Results are cached in memory in order until they are not needed.
Releasing assets will decrement reference counts and remove objects once there are not references.  References to dependencies are propogated and may cause them to unload as well.

The system is designed to be extensible to allow for custom locators and providers of assets.  A possible custom provider could do some image compositing and return
the result using the same API as a normal texture load.  
