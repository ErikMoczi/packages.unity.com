# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.2] - 2019-01-25
### Added
- Fix unable to rig Sprites created manually
- Remove legacy packing tag
- Default Texture Type is changed to 'Sprite (2D and UI)'
- Default Sprite Mode is changed to 'Multiple'

## [1.0.0-preview.1] - 2018-11-20
### Added
- New release
- ScriptedImporter for importing Adobe Photoshop file
- Supports handling of Adobe Photoshop layers
    - Creates Sprites from individual layers
    - Handles include or exclude hidden layers
- Supports Prefab generation that reconstruct generated Sprites to original art asset layout
    - Prefab generation supports GameObject grouping based on Adobe Photoshop layer grouping
- Supports 2D Animation v2 single character with multiple Sprites workflow