# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.0-preview.7] 2018-10-29
* Hopefully all CI issues are resolved now.
  
## [0.2.0-preview.4] 2018-10-24
* Merged in gneral settings support. Initial implentation allows for ability to assign an XR Manager instance for loading XR SDK at boot launch time.

## [0.2.0-preview.3] 2018-10-24
* Merged in Unified Settings dependent changes.

## [0.1.0-preview.9] - 2018-07-30
* Add missing .npmignore file

## [0.1.0-preview.8] - 2018-07-30

* Updated UI for XR Manager to allow for adding, removing and reordering loaders. No more need for CreateAssetMenu attributes on loaders.
* Updated code to match formatting and code standards.

## [0.1.0-preview.7] - 2018-07-25

* Fix issue #3: Add ASMDEFs for sample code to get it to compile. No longer need to keep copy in project.
* Fix Issue #4: Update documentation to reflect API changes and to expand information and API documentation.
* Fix Issue #5: Move boilerplate loader code to a common helper base class that can be used if an implementor wants to.

## [0.1.0-preview.6] - 2018-07-17

### Added runtime tests for XRManager

### Updated code to reflect name changes for XR Subsystem types.

## [0.1.0-preview.5] - 2018-07-17

### Simplified settings for build/runtime

Since we are 2018.3 and later only we can take advantage of the new PlayerSettings Preloaded Assets API. This API allows us to stash assets in PLayerSettings that are preloaded at runtime. Now, instead of figuring out where to write a file for which build target we just use the standard Unity engine and code access to get the settings we need when we need them.

## [0.1.0-preview.4] - 2018-07-17

### Added samples and abiity to load settings

This change adds a full fledged sample base that shows how to work with XR Management from start to finish, across run and build. This includes serializing and de-serializing the settings.

## [0.1.0-preview.3] - 2018-07-17


## [0.1.0-preview.2] - 2018-06-22

### Update build settings management

Changed XRBuildData froma class to an attribute. This allows providers to use simpler SO classes for build data and not forece them to subclass anything.
Added a SettingsProvider subclass that wraps each of these attribute tagged classes. We use the display name from the attribute to populate the path in Unified Settings. The key in the attribute is used to store a single instance of the build settings SO in EditorBuildSettings as a single point to manage the instance.
Added code to auto create the first SO settings instance using a file panel since the Editor build settings container requires stored instances be backed in the Asset DB. There is no UI for creating the settings (unless added by the Provider) so this should allow us to maintain the singleton settings. Even if a user duplicates the settings instance, since it won't be in the Editor build settings container we won't honor it.

## [0.1.0-preview.1] - 2018-06-21

### This is the first release of *Unity Package XR SDK Management*.
