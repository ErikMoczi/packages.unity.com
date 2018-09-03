# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.6] - 2018-04-25

### Rename ARUtilities to ARFoundation

- This package is now called `ARFoundation`.
- Removed `ARPlaceOnPlane`, `ARMakeAppearOnPlane`, `ARPlaneMeshVisualizer`, and `ARPointCloudParticleVisualizer` as these were deemed sample content.
- Removed setup wizard.
- Renamed `ARRig` to `ARSessionOrigin`.
- `ARSessionOrigin` no longer requires its `Camera` to be a child of itself.
