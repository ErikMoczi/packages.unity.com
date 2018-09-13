# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.0-preview.1] - 2018-06-21

### This is the first release of *Unity Package XR SDK Management*.


## [0.1.0-preview.2] - 2018-06-22

### Update build settings management

Changed XRBuildData froma class to an attribute. This allows providers to use simpler SO classes for build data and not forece them to subclass anything.
Added a SettingsProvider subclass that wraps each of these attribute tagged classes. We use the display name from the attribute to populate the path in Unified Settings. The key in the attribute is used to store a single instance of the build settings SO in EditorBuildSettings as a single point to manage the instance.
Added code to auto create the first SO settings instance using a file panel since the Editor build settings container requires stored instances be backed in the Asset DB. There is no UI for creating the settings (unless added by the Provider) so this should allow us to maintain the singleton settings. Even if a user duplicates the settings instance, since it won't be in the Editor build settings container we won't honor it.

## [0.1.0-preview.3] - 2018-07-17

## [0.1.0-preview.4] - 2018-07-17

### Added samples and abiity to load settings

This change adds a full fledged sample base that shows how to work with XR Management from start to finish, across run and build. This includes serializing and de-serializing the settings.
