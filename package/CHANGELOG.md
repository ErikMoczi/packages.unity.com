# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.0-preview] - 2018-10-01
- Fixed an issue where a string hash was being used instead of a file hash causing data to not rebuild

## [1.0.1-preview] - 2018-08-24
- removed compile warning
- Fixed an issue where we were not using the addressableNames field of the AssetBundleBuild struct

## [1.0.0-preview] - 2018-08-20
- Fixed an issue in  ArchiveAndCompressBundles where previous output location was being used failing to copy cached bundles to the new location
- Fixed an issue in BuildCache were built in plugin dlls did not have a hash version causing cache misses
- Fixed invalid access errors when reading access controlled files for hashing.
- Fixed an issue where you could not force rebuild asset bundles using LegacyBuildPipeline
- Implemented IEquatable<T> on public structs
- Breaking API Change: LegacyBuildPipeline.BuildAssetBundles now returns LegacyAssetBundleManifest
	- LegacyAssetBundleManifest's API is identical to AssetBundleManifest

## [0.2.0-preview] - 2018-07-23
- Removed ProjectInCleanState & ValidateBundleAssignments tasks and integrated them directly indo the data validation or running methods
- Added build task to append hash to asset bundle files
- Large rework of how IBuildTasks are implemented. Now using dependency injection to handle passing data.
- Added reusable BuildUsageCache for usage tag calculation performance improvements
- - Unity minimum version now 2018.2.0b9
- Improved asset bundle hash version calculation to be unity version agnostic

## [0.1.0-preview] - 2018-06-06
- Added support for Cache Server integration of the Build Cache
- Refactored Build Cache internals for even more performance gains

## [0.0.15-preview] - 2018-05-21
- Hardened build cache against failures due to invalid data

## [0.0.14-preview] - 2018-05-03
- temporarily removed progress bar as it causes a recompile on mac.  Will attempt to re-add selectively later.

## [0.0.13-preview] - 2018-05-03
- fixed hash serialization bug.

## [0.0.12-preview] - 2018-05-02
- Added build task for generating extra data for sprite loading edge case

## [0.0.11-preview] - 2018-05-01
- Updated BuildAssetBundles API to also take custom context objects
- Added support for extracting Built-In Shaders to a common bundle

## [0.0.10-preview] - 2018-04-26
- Added BuildAssetBundles API that takes a custom task list
- Added null checks for BuildSettings.typeDB & AssetBundleBuild.addressableNames

## [0.0.9-preview] - 2018-04-04
- Added documentation for IWriteOperation implementations
- Added documentation for ReturnCodes, LegacyBuildPipeline, and ContentPipeline
- Ran Unity code analysis & cleaned up warnings (boxing, performance issues, name consistency)
- Breaking api change: Changed build tasks' public static run method to private.
- Added null checks and ArgumentNullExceptions

## [0.0.8-preview] - 2018-03-27
- Test rename & meta file cleanup
- Added documentation for shared classes / structs
- Updated inconsistent interface / class names
- Added missing parameter to IBuildParameters
- Ran spell check
- Moved IWriteOperation to Interfaces
- Update IWriteOperation properties to PascalCase
- Added IWriteOperation documentation

## [0.0.6-preview] - 2018-03-20
- doc updates

## [0.0.5-preview] - 2018-02-08
- Initial submission for package distribution

