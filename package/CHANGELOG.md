# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2017-06-01

### Changed
- Lightweight Package version updated to "com.unity.render-pipelines.lightweight": "1.1.10-preview"
- Static Mesh import settings have been updated to show best options (was default import settings before)
- Texture import settings updated with platform size override (4k for andriod and ios) (all textures already much smaller than this fyi)
- Audio preset updated with platform differences for ios and android. Ios is always MP3 and Android is always Vorbis
- Texture preset max size forces to 4k for androidand ios
- Exit sample added to camera script
-  Fixed Timestep in Time Manger updated from 0.0167 to 0.02
- Removed Vertex Lighting from all lightweight assets
- Added soft shadows to Lightweight high quality and medium quality assets

## [1.0.3] - 2017-06-06

### Changed
- Migrating old lightweight templates into package format 

