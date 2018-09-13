# About *XR SDK Management* package

Use the **XR SDK Management** package to help streamline **XR SDK** lifecycle management and potentially provide users with build time UI through the Unity **Unified Settings** system.

# End Users

## Add an **XRManager** instance to your scene

1) Create a new empty Game Object in your scene.
2) Add an instance of the **XRManager** component to this new Game Object.
3) Use the **XRManager** component Inspector UI to add/create, remove and reorder the loaders you widh to use.

## Customize build and runtime settings

Any package that needs build or runtime settings should provide a settings datatype for use. This will be surfaces in the **Unified Settings** UI window underneath a top leve **XR** node. By default a custom settings data instance will not be created. If you wish to modify build or runtime settings for the package you must go to the package authors entry in **Unified Settings** and select **Create**. This will create an instance of the settings that you can then modify inside of **Unified Settings**.

# Package Authors

## Lifecycle Management

This package provides for management of **XR SDK** subsystem lifecycles without the need for boilerplate code. The **XRManager** class provides a component that can be added to a game object in the scene that will manage initialization, start, stop and denitialization of a set of subsystems defined by an **XRLoader** instance. The **XRManager** instance can handle all of the lifecycle management, init/deinit only, subsystem start/stop only or can leave all of it up to the user.

A provider will create a subclass of **XRLoader** to provide a loader for their particular runtime scheme. An **XRLoader** is simply a **ScriptableObject** and as such, the user is able to create and instance (or more if they want) of the loader. Each **XRLoader** subclass defines the subsystems and their load order and is responsible for managing the set of subsystems they require. A user will add all the **XRLoaders** instances they created to the Loaders property on the **XRManager**, arranging them in the load order that they desire.

**_NOTE_**: _At this time there is no way for a provider to ship an instance of their loader in their package that the user can find. To get around this, you will need to provide a means of allowing the user to create the necessary instance of your loader. The recommendation for this is to add a CreateAssetMenu attribute to your loader class and set the menu location to "XR/Loaders/[your loader name]"._

When asked to initialize, **XRManager** will call each **XRLoader** instance it has a reference to in the order it has and attempt to initialize each one. The first loader that succeeds initialization becomes the active loader and all further attempts to initialize are stopped. From this point the user can ask for the static **XRManager.ActiveLoader** instance to get access to the active loader. If initialization fails for all loaders, **ActiveLoader** is set to null.

Automatic lifecycle management hooks into the following **MonoBehaviour** callback points:

|Callback|Lifecycle Step|
|---|---|
|OnEnable|Find the first loader that succeeds intiialization and set ActiveLoader.|
|Start|Start all subsystems|
|OnDisable|Stop all subsystems|
|OnDestroy|Deintialize all subsystems and remove the ActiveLoader instance.|


## Build and Runtime settings through *Unified Settings*

A provider may need optionl settings to help manage build issues or runtime configuration. They can do this by adding the **XRConfigurationData** attribute to a ScriptableObject and providing the set of properties they want to surface for users to control configuration. Configuration options will be surfaced in the **Unified Settings** window under the **XR** top level entry. We will manage the lifecycle for one instance of the class marked with the attribute through the EditorBuildSettings config object API. If no special UI is provided, the **Unified Settings** window will display the configuration settings using the standard **ScriptableObject** UI Inspector. A provider can extend the UI by creating a custom **Editor** for their configuration settings type and that will be used in the **Unified Settings** window instead.

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
* **Samples** - There is a samples folder in the package that contains an implementation of all parts of XR Management. Copy that folder to a location in your project/package to get started with implementing XR Management for your needs.

## Document revision history

|Date|Reason|
|---|---|
|July 25, 2018|Update docs to reflect API changes.|
|June 22, 2018|Added updated information about the XRBuildData Attribute.|
|June 21, 2018|Document created.|
