# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.4.0] - 2019-03-09
- Updating LWRP version to 6.5.2

## [3.3.0] - 2019-03-07
- Updating LWRP version to 6.5.0

## [3.2.1] - 2019-03-05
- PlayerSettings.displayResolutionDialog property now defaults to false.

## [3.2.0] - 2019-02-21
- Fixing an error in the readme.asset .
- Updating the LW version for bugfixes.

## [3.1.0] - 2019-02-19
-Adding information about package manager to the readme.

## [3.0.0] - 2019-02-12
- Updating LW package version
- Removing deprecated packages from manifest

## [2.1.0] - 2019-02-04
- Corrected some default values in project settings.

## [2.0.0] - 2019-01-30

### Changed
- Enabled HoloLens `depthBufferSharingEnabled` by default.
- LW version updated to 5.3.1

## [1.4.2] - 2019-01-22

### Changed
- Removed unneeded manifest packages

## [1.4.1] - 2018-12-07

### Changed
- LW version updated to 5.2.3

## [1.4.0] - 2018-12-06

### Changed
- LW version updated to 5.2.2
- Directional Light is now set to realtime
- Main light shadow resolution is now 2084px

## [1.3.0] - 2018-11-30

### Changed
- LW version updated to 5.2.1

## [1.2.0] - 2018-11-27

### Changed
- android-vulkan-default
- LW version updated to 5.2.0
- Update new project templates to use 4.x scripting runtime

## [1.1.1] - 2018-11-08

### Fixed
- Physics.reuseCollisionCallbacks property now defaults to true.
- Physics2D.reuseCollisionCallbacks property now defaults to true.
- Physics.autoSyncTransforms property now defaults to false.
- Physics2D.autoSyncTransforms property now defaults to false.

## [1.1.0] - 2018-24-10

### Changed
- Updating LW Version 
- AndroidTVCompatibility to false


## [1.0.6] - 2018-24-09

### Changed
- Oculus XR settings default to dash support and depth export enabled.
- Updating default webgl memory size
- Updating default upload manager ring buffer size to 16 mb
- removing platform overrides for textures presets
- updating lw version to 4.0.0-preview

## [1.0.5] - 2018-09-06

### Changed
- LW Version update to 3.3.0

## [1.0.4] - 2018-07-16

### Changed
- LW Version update to 3.0.0
- Adding collision to floor mesh

## [1.0.3] - 2018-06-06

### Changed
- Migrating old lightweight templates into package format 

## [1.0.2] - 2018-06-01

### Changed
- Lightweight Package version updated to "com.unity.render-pipelines.lightweight": "1.1.10-preview"
- Static Mesh import settings have been updated to show best options (was default import settings before)
- Texture import settings updated with platform size override (4k for andriod and ios) (all textures already much smaller than this fyi)
- Audio preset updated with platform differences for ios and android. Ios is always MP3 and Android is always Vorbis
- Texture preset max size forces to 4k for androidand ios
- Exit sample added to camera script
- Fixed Timestep in Time Manger updated from 0.0167 to 0.02
- Removed Vertex Lighting from all lightweight assets
- Added soft shadows to Lightweight high quality and medium quality assets

## [1.0.0] - 2018-2-25

### Added
- Sample static meshes to show best practices
- Light probs
- Reflection Probs

### Changed
- Removed cinemachine and text mesh pro
- Updated package version for LW 
- Additional setting and lighting polish
- Removing basic content (red cube)

## [0.0.5] - 2018-1-29

### Added
- cinemachine and text mesh pro packages

## [0.0.4] - 2018-1-29

### Added
- cinemachine and text mesh pro packages

## [0.0.3] - 2018-1-26 

### Changed 
- Updating Shadergraph and lightweight to new version
- Updating lighting settings based on internal feedback from lighting team

## [0.0.2] - 2017-12-12
### Added
- Packages for Lightweight SRP and Shadergraph
- Simple example content - red cube 

### Changed
- Project and Lighting Settings adjusted for use with Lightweight Render Pipeline

## [0.0.1] - 2017-12-05

### Added
- Initial Project Creation for Unity Lightweight Project Template \com.unity.template.lightweight.

