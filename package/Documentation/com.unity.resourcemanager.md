# Unity Resource Manager


ResourceManager provides a high level API for managing assets in Unity.  All load methods are asynchronous. 

### Requires Unity 2017.3+

The specific method and location of loading assets is abstracted.  The intent is that with basic setup, it can load items from Resources or load Scenes.  With a little more setup, it can load assets from asset bundles.  The overall goal is that regardless of what your setup was, or where you are loading from, that you load the same way.  That you could simly call _ResourceManager.LoadAsync<Texture, string>("myTexture");_ and have that be loaded regardless of where it came from.

This package can function as a stand alone package, but long term is intended to be the foundataion for future, high-level packages.  See the Samples directory for help on how to use it as a stand alone package. 



## Locators, Locations & Providors

The ResourceManager project defines three important interfaces: IResourceLocator, IResourceLocation, and IResourceProvidor.  
* An **IResourceLocation** contains the information needed to load an asset; creating a resource location does not actually load the asset.  
* An **IResourceLocator** object is what is used to find IResourceLocation objects.  This lookup is done by providing the Locator an "address".  What this address is (string name, int index, other) is determined by the implementation of the Locator.  
* An **IResourceProvidor** is what actually does the loading of an asset. When asked to load a location, the resource manager asks all of the providors it knows about if they _CanProvide_ that location. Once one can, the resource manager stops its search, and asks that providor to profide the asset.  In general this will mean a _load_ operation, but in theory it could also be asset creation.  For example a custom providor could create composite or generated textures rather than simply loading them.

The system is designed to be extensible to allow for custom locators and providers of assets.  Future high-level packages will come with locators and providors, and will handle the initialization themselves.  The intent being that users need not know about the above interfaces.


