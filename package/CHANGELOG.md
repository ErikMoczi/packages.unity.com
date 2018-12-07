# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2018-11-30

## [1.2.0] - 2018-11-27
- Updating post processing version 2.1.0
- Android: enable Vulkan, disable x86
- Update new project templates to use 4.x scripting runtime

## [1.1.1] - 2018-11-08
- Physics.reuseCollisionCallbacks property now defaults to true.
- Physics2D.reuseCollisionCallbacks property now defaults to true.
- Physics.autoSyncTransforms property now defaults to false.
- Physics2D.autoSyncTransforms property now defaults to false.

## [1.1.0] - 2018-24-10

### Changed
- Updating Post Version to 2.0.16-preview
- AndroidTVCompatibility to false

## [1.0.6] - 2018-24-09

### Changed
- Oculus XR settings default to dash support and depth export enabled.
- Updated default webgl memory size
- updated default upload manager ring buffer size to 16mb
- removing platform overrides for texture presets
- manfiest updates
- post processing increased to 2.0.13-preview


## [1.0.5] - 2018-09-06

### Changed
- Updated PP version number

## [1.0.4] - 2018-07-16

### Changed
- Post Version updated - 2.0.9
- Collider added to ground

## [1.0.3] - 2018-06-06

### Changed
- Migrating old 3d w extras templates into package format 

## [1.0.2] - 2018-06-01

### Changed
- Package version updated
- Static Mesh import settings have been updated to show best options (was default import settings before)
- Texture import settings updated with platform size override (4k for andriod and ios) (all textures already much smaller than this fyi)
- Audio preset updated with platform differences for ios and android. Ios is always MP3 and Android is always Vorbis
- Texture preset max size forces to 4k for androidand ios
- Exit sample added to camera script
- Fixed Timestep in Time Manger updated from 0.0167 to 0.02
- Quality settings have shadows changes to improve banding

## [1.0.1] - 2018-06-01

### Changed
- Package updates
- Settings Polish

## [1.0.0] - 2018-02-25

### Added
- Sample static meshes to show best practices
- Light probs
- Reflection Probs
- Presets added

### Changed
- Removed cinemachine and text mesh pro
- Package version updated
- Additional settings polish
- Basic content removed

## [0.0.5] - 2018-1-29

### Added
- cinemachine and text mesh pro packages

## [0.0.2] - 2018-12-06

### Changed
- Update some lighting and player settings based on feedback notes

## [0.0.1] - 2018-1-?

### Added 
- Created Project for 3D with Post Processing
- Post processing package added 
- Project, lighting, and post settings tuned for best visuals
- Very basic example content added
