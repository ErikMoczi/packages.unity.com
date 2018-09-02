The ResourceManager is an extendable high level API that asynchronously loads and unloads assets.

The specific method and location of loading assets is abstracted. With the proper extension, assets can be loading from a variety of locations (Resources, Bundles, etc) all through a single API. 

The overall goal is that regardless of what your setup is, or where you are loading from, you always load in the same way. For example, you can call 
ResourceManager.LoadAsync<Texture, string>("myTexture");
 and have that be loaded regardless of where it came from. 

This package can function as a standalone package, but will be extended in the future via high-level packages that add custom IResourceLocator and IResourceProvider interfaces. See the Samples directory for help on how to use it as a standalone package. Future high-level packages will come with locators and providers, and will handle the initialization themselves. The intent being that users need not know about the above interfaces.
