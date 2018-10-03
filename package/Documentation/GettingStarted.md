# Getting started with Scriptable Build Pipeline

## Installing the Scriptable Build Pipeline (SBP) package

Requires Unity 2018.2 or later.

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest).

To build an AssetBundle, use the ContentPipeline.BuildAssetBundles() method. In its simplest form, you supply parameters:

* Build Parameters - A *BuildParameters* object that specifies the BuildTarget, the BuildTargetGroup, the output path, and additional option properties.

* The content to build - A *BundleBuildContent* object that specifies the content to build and its layout.

* A results object - A *BundleBuildResults* object to receive the details about the AssetBun