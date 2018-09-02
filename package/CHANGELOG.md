# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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

