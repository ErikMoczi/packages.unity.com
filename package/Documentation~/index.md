# About Memory Profiler

Use the Memory Profiler package to identify potential areas in your Unity project (and the Unity Editor itself) where you can reduce [memory usage](https://github.com/google/cadvisor/issues/913#issuecomment-150663233). For example, use the Memory Profiler to capture, inspect, and compare [memory snapshots](https://en.wikipedia.org/wiki/Snapshot_(computer_storage)). 

The Memory Profiler package has a [window](memory-profiler-window.md) in the Editor with an overview of native and managed memory allocations and can help you detect [memory leaks](https://en.wikipedia.org/wiki/Memory_leak) and [fragmentation](https://en.wikipedia.org/wiki/Fragmentation_(computing)).

The Memory Profiler package creates a unified solution allowing you to profile both small projects on mobile devices and big AAA projects on high-end machines. 

You can also import snapshots taken from the [Bitbucket Profiler](https://bitbucket.org/Unity-Technologies/memoryprofiler) and use them within the Memory Profiler.

## Memory Profiler vs. Profiler

The Memory Profiler package is entirely separate from the inbuilt Unity [Profiler](https://docs.unity3d.com/Manual/Profiler.html), even though they share some terminology. 

## Who can use the Memory Profiler?

The Memory Profiler package is mostly for advanced Unity users that desire to understand the memory used in their Project. However, no knowledge of memory profiling tools is required to make use of this package. 

# Installing Memory Profiler

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html). 

>**Note**: While this package is in preview, the Package Manager needs to be configured to show __Preview Packages__. (Under the __Advanced__ drop-down menu, enable __Show preview packages__.) Then search for the Memory Profiler package.

<a name="UsingPackageName"></a>

# Using Memory Profiler

To learn how to use the Memory Profiler package in your project, read the [manual](manual.md).

# Technical details
## Requirements
This version of the Memory Profiler is compatible with the following versions of the Unity Editor:

* 2018.3 and later.

> **Note**:  When you install the Memory Profiler package it will automatically install the [Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@0.0/manual/index.html) package as a dependency.

## Known limitations
Memory Profiler version 0.1.0-preview.2 includes the following known limitations:

* Only development builds, and the Editor can use the Memory Profiler. This limitation means that users can’t profile release builds. (This is the same limitation as Profiler.)
* The capture itself allocates memory so in some cases the system may run out of memory and crash the application. 
* Taking a capture may temporarily freeze the capture target (the Editor or remote application). The length of this freeze depends on the complexity and memory usage of the application.
* Single threaded platforms, such as WebGL, are not supported. 
* Creating a capture currently does not take a screenshot to be sent along as metadata.

## Package contents
The following table indicates the root folders in the package where you can find useful resources:

|Location|Description|
|---|---|
|`Documentation~`|Contains the documentation for the package.|
|`Tests`|Contains the unit tests for the package.|

## Document revision history
|Date|Reason|
|---|---|
|Dec 12, 2018|Updated documentation. Matches package version 0.1.0-preview.2|
|Nov 15, 2018|Documentation created. Matches package version 0.1.0-preview.2|