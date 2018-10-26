# About *XR SDK Management* package

Use the **XR SDK Management** package to help streamline **XR SDK** lifecycle management and potentially provide users with build time UI through the Unity **Unified Settings** system.

# Installation

To use this package you need to add a reference to it in the Packages/manifest.json file located in your project. There are three ways you can reference a specific version of a package depending and how you are using it.

## Using a production version of the package

For a released version of the package in production, referencing the package is no different than any other released package. If visible in the Package Manager UI you can just select the package and install it. Alternatively, you can edit the manifest.json file to add it manually like this:

```json
	"dependencies": {
        ...
		"com.unity.xr.management":"<full version number>"
	}
```

## Using a staging version of the package

For a pre-released version of the package you will need to edit the manifest.json file directly. You'll also need to add a `registry` entry to make sure that Package Manager knows to look in the staging repository for the package you want. 

```json
    "registry": "https://staging-packages.unity.com",
	"dependencies": {
        ...
		"com.unity.xr.management":"<full version number>"
	}
```

## Using a local clone of the package

If you want to use a cloned version of the package directly, you can point the Package Manager at a local folder as the location from which to get the package from.

```json
	"dependencies": {
        ...
		"com.unity.xr.management":"file:path/to/package/root"
	}
```

NOTE: The package root is not necessarily the root of the cloned repo. The package root the folder where the package's package.json file located.

# End Users

## Add default loader and settings instances if requested

At package install time, the package may prompt you to create an instance of a loader and an instance of settings for the package. This step is entirely optional and is there simply to help the user get things going at installation time.

If you wish not to create this at installation time, the appropriate portion of the editor that require them (**XRManager** and **Unified Settings** will) prompt you to create them as well.

## Add an **XRManager** instance to your scene

* Create a new empty Game Object in your scene.
* Add an instance of the **XRManager** component to this new Game Object.
* Use the **XRManager** component Inspector UI to add/create, remove and reorder the loaders you wish to use.

## Customize build and runtime settings

Any package that needs build or runtime settings should provide a settings datatype for use. This will be surfaced in the **Unified Settings** UI window underneath a top level **XR** node. By default a custom settings data instance will not be created. If you wish to modify build or runtime settings for the package you must go to the package authors entry in **Unified Settings** and select **Create**. This will create an instance of the settings that you can then modify inside of **Unified Settings**.

# Package Authors

## Lifecycle Management

This package provides for management of **XR SDK** subsystem lifecycle without the need for boilerplate code. The **XRManager** class provides a component that can be added to a game object in the scene that will manage initialization, start, stop and de-initialization of a set of subsystems defined by an **XRLoader** instance. The **XRManager** instance can handle all of the lifecycle management, init/de-init only, subsystem start/stop only or can leave all of it up to the user.

A provider will create a subclass of **XRLoader** to provide a loader for their particular runtime scheme. An **XRLoader** is simply a **ScriptableObject** and as such, the user is able to create and instance (or more if they want) of the loader. Each **XRLoader** subclass defines the subsystems and their load order and is responsible for managing the set of subsystems they require. A user will add all the **XRLoaders** instances they created to the Loaders property on the **XRManager**, arranging them in the load order that they desire.

**_NOTE_**: _At this time there is no way for a provider to ship an instance of their loader in their package that the user can find. To get around this, you will need to provide a means of allowing the user to create the necessary instance of your loader. The recommendation for this is to add a CreateAssetMenu attribute to your loader class and set the menu location to "XR/Loaders/[your loader name]"._

When asked to initialize, **XRManager** will call each **XRLoader** instance it has a reference to in the order it has and attempt to initialize each one. The first loader that succeeds initialization becomes the active loader and all further attempts to initialize are stopped. From this point the user can ask for the static **XRManager.ActiveLoader** instance to get access to the active loader. If initialization fails for all loaders, **ActiveLoader** is set to null.

Automatic lifecycle management hooks into the following **MonoBehaviour** callback points:

|Callback|Lifecycle Step|
|---|---|
|OnEnable|Find the first loader that succeeds initialization and set ActiveLoader.|
|Start|Start all subsystems|
|OnDisable|Stop all subsystems|
|OnDestroy|De-initialize all subsystems and remove the ActiveLoader instance.|


## Build and Runtime settings through *Unified Settings*

A provider may need optional settings to help manage build issues or runtime configuration. They can do this by adding the **XRConfigurationData** attribute to a ScriptableObject and providing the set of properties they want to surface for users to control configuration. Configuration options will be surfaced in the **Unified Settings** window under the **XR** top level entry. We will manage the lifecycle for one instance of the class marked with the attribute through the EditorBuildSettings config object API. If no special UI is provided, the **Unified Settings** window will display the configuration settings using the standard **ScriptableObject** UI Inspector. A provider can extend the UI by creating a custom **Editor** for their configuration settings type and that will be used in the **Unified Settings** window instead. 

The provider will need to handle getting the settings from **EditorUserBuildSettings** into the build application. This can be done with a custom build processing script. If all you need for build support is to make sure that you have access to the same settings at runtime you can derive from **XRBuildHelper<T>**. This is a generic abstract base class that handles the necessary work of getting the build settings stored in EditorUserBuildSettings and getting them into the build application for access at runtime. Simplest build script for your package would look like this:

```csharp
public class MyBuildProcessor : XRBuildHelper<MySettings> { }
```

You can override the build processing steps from **IPreprocessBuildWithReport** and **IPostprocessBuildWithReport** but make sure that you call to the base class implementation or else your settings will not be copied.

```csharp
public class MyBuildProcessor : XRBuildHelper<MySettings> 
{ 
    public override void OnPreprocessBuild(BuildReport report)
    {
        base.OnPreprocessBuild(report);
        // Do your work here
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
        base.OnPreprocessBuild(report);
        // Do your work here
    }
}
```

If you need more extensive support and/or absolute control you can copy the **SampleBuildProcessor** in the _Samples/Editor_ folder and work from there.

## Package Initialization

Given the need for **ScriptableObject** instance to support loaders and settings, the user will be forced to create these instances at some point. Both **XRManager** and the **Unified Settings** support can handle creating the necessary instances on demand but it may be beneficial to create these up front at package installation. If you derive a class from **XRPackageInitializationBase** and fill out the interface properties and methods the management system will provide for creation of default instances of your loader and settings at installation. Creation of each one is driven by prompting the user if it is OK to do that and, if so, creating the instance in the appropriate default location in the Assets folder. For loaders this is Assets/XR/Loaders. For settings this is Assets/XR/Settings. While these are the recommended locations for putting these package based objects they do not have to be located there. The user is free to move them around as long as we can find at least one instance of each type for each package.

# Installing *XR SDK Management*

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).

# Technical details

## Requirements

This version of **XR SDK Management** is compatible with the following versions of the Unity Editor:

* 2018.3 and later (recommended)

## Known limitations

* Still in preview.

## Package contents

This version of **XR SDK Management** includes:

* **XRManager** - This is a **MonoBehaviour** that can be added to a **GameObject** in a scene and provides for management of **XRLoader** instances and their lifecycle.
* **XRLoader** - This is the base class all loaders should derive from. It provides a basic the **XRManager** can use to manage lifecycle and a simple API to allow users to request specific subsystems from the loader as and when needed.
* **XRConfigurationData** - This is an attribute that allows for build and runtime settings to be hosted within the **Unified Settings** window. All instances will be hosted under the top level **XR** entry within the **Unified Settings** window under the name supplied as part of the attribute. The management package will maintain and manage the lifecycle for one instance of the build settings using **EditorBuildSettings** config object API, stored with the key provided in the attribute. At any time, the provider or the user is free access the configuration settings instance by asking **EditorBuildSettings** for the instance associated with the chosen key (as set in the attribute).
* **XRPackageInitializationBase** - Helper class to derive from that simplifies some of the busy work for package initialization. Helps to create a default instance of the packages XRLoader and default settings at the time of package installation. Initialization is run once only and the package developer should not depend on the user creating the specified instances.
* **XRBuildHelper** - Abstract class useful for handling some boilerplate around moving settings from Editor -> Runtime. Derive from this class and specific the appropriate settings type and the system will take care of marshaling that type from EditorUserBuildSettings to PlayerSettings to be used at runtime.
* **Samples** - There is a samples folder in the package that contains an implementation of all parts of XR Management. Copy that folder to a location in your project/package to get started with implementing XR Management for your needs.

## Document revision history

|Date|Reason|
|---|---|
|October 8, 2018|Add documentation around package installation.|
|October 5, 2018|Fix typos.|
|October 4, 2018|Add info on package initialization.|
|September 25, 2018|Clarify a bit and correct some spelling.|
|July 25, 2018|Update docs to reflect API changes.|
|June 22, 2018|Added updated information about the XRBuildData Attribute.|
|June 21, 2018|Document created.|
