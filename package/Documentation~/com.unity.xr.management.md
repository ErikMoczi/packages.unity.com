# About *XR SDK Management* package

Use the **XR SDK Management** package to help streamline **XR SDK** lifecycle management and potentially provide users with build time UI through the Unity **Unified Settings** system.

## Lifecycle Management

This package provides for management of **XR SDK** subsystem lifecycles without the need for boilerplate code. The **XRManager** class provides a component that can be added to a game object in the scene that will manage initialization, start, stop and denitialization of a set of subsystems defined by an **XRLoader** instance. The **XRManager** instance can handle all of the lifecycle management, init/deinit only, subsystem start/stop only or can leave all of it up to the user.

A provider will create a subclass of **XRLoader** to provide a loader for their particular runtime scheme. An **XRLoader** is simply a **ScriptableObject** and as such, the user is able to create and instance (or more if the want) of the loader. Each **XRLoader** subclass defines the subsystems and their load order and is responsible for managing the set of subsystems they require. A user will add instances of all the **XRLoaders** instances they created to the Loaders property on the **XRManager**, arranging them in the load order that they desire.

When asked to initialize, **XRManager** will call each **XRLoader** instance it has a reference to in the order it has and attempt to initialize each one. The first loader that succeeds initialization becomes the active loader and all further attempts to initialize are stopped. From this point the user can ask for the static **XRManager.ActiveLoader** instance to get access to the active loader. If initialization fails for all loaders, **ActiveLoader** is set to null.

Automatic lifecycle management hooks into the following **MonoBehaviour** callback points:

|Callback|Lifecycle Step|
|---|---|
|OnEnable|Find the first loader that succeeds intiialization and set ActiveLoader.|
|Start|Start all subsystems|
|OnDisable|Stop all subsystems|
|OnDestroy|Deintialize all subsystems and remove the ActiveLoader instance.|


## Build settings through *Unified Settings*

A provider may need optionl settings to help manage build issues. They can do this by adding the **XRBuildData** attribute to their ScriptableObject and providing the set of properties they want to surface for users to control build. Build settings will be surfaced in the **Unified Settings** window under the **XR** top level entry. We will manage the lifecycle for one instance of the class marked with the attribute. If no special UI is provided, the **Unified Settings** window will display the build settings using the standard **ScriptableObject** UI Inspector. A provider can extend the UI by creating a custom **Editor** for their build settings type and that will be used in the **Unified Settings** window instead.

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
* **XRBuildData** - This is an attribute that allows for build settings to be hosted within the **Unified Settings** window. All build settings instance will be hosted under the top level **XR** entry within the **Unified Settings** window. The management package will maintain and manage the lifecycle for one instance of the build settings using **EditorBuildSettings**. At any time, the provider or the user is free access the builds settings instance by asking **EditorBuildSettings** for the instance associated with the chosen key (set in the attribute).

## Document revision history
 
|Date|Reason|
|---|---|
|June 22, 2018|Added updated information about the XRBuildData Attribute.|
|June 21, 2018|Document created.|
