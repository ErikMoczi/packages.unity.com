# Unity Resource Manager

The ResourceManager is an extendable high level API that asynchronously loads and unloads assets.

### Requires Unity 2018.1+

The specific method and location of loading assets is abstracted. With the proper extension, assets can be loading from a variety of locations (Resources, Bundles, etc) all through a single API. The overall goal is that regardless of what your setup is, or where you are loading from, you always load in the same way. For example, you can call _ResourceManager.LoadAsync<Texture, string>("myTexture");_ and have that be loaded regardless of where it came from.
This package can function as a standalone package, but will be extended in the future via high-level packages that add custom IResourceLocator and IResourceProvider interfaces. See the Samples directory for help on how to use it as a standalone package.

## Locators, Locations & Providers

The ResourceManager project defines three important interfaces: IResourceLocator, IResourceLocation, and IResourceProvider.  
* An **IResourceLocation** contains the information needed to load an asset; creating a resource location does not actually load the asset.  
* An **IResourceLocator** object is what is used to find IResourceLocation objects.  This lookup is done by providing the locator an  "address".  What this address is (string name, int index, other) is determined by the implementation of the locator.  
* An **IResourceProvider** is what actually does the loading of an asset. When asked to load a location, the resource manager asks all of the providers it knows about if they _CanProvide_ that location. Once one can, the resource manager stops its search, and asks that provider to profide the asset.  In general this will mean a _load_ operation, but in theory it could also be asset creation.  For example a custom provider could create composite or generated textures rather than simply loading them.

The system is designed to be extensible to allow for custom locators and providers of assets.  Future high-level packages will come with locators and providers, and will handle the initialization themselves.  The intent being that users need not know about the above interfaces.


