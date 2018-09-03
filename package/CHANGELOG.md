# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.8] - 2018-05-23
- Change dependency to `ARExtension` 1.0.0-preview.2

## [1.0.0-preview.7] - 2018-05-23
- Remove "timeout" from AR Session
- Add availability and AR install support
- Significant rework to startup lifecycle & asynchronous availability check

## [1.0.0-preview.6] - 2018-04-25

### Rename ARUtilities to ARFoundation

- This package is now called `ARFoundation`.
- Removed `ARPlaceOnPlane`, `ARMakeAppearOnPlane`, `ARPlaneMeshVisualizer`, and `ARPointCloudParticleVisualizer` as these were deemed sample content.
- Removed setup wizard.
- Renamed `ARRig` to `ARSessionOrigin`.
- `ARSessionOrigin` no longer requires its `Camera` to be a child of itself.

## [1.0.0-preview.9] - 2018-06-06

- Rename `ARBackgroundRenderer` to `ARCameraBackground`
- Unify `ARSessionState` & `ARSystemState` enums

## [1.0.0-preview.10] - 2018-06-06

- Update documentation: ARSession image and written description.

## [1.0.0-preview.11] - 2018-06-08

- Improve lifecycle reporting: remove public members `ARSubsystemManager.availability` and `ARSubsystemManager.trackingState`. Combine with `ARSubsystemManager.systemState` and the public event `ARSubsystemManager.systemStateChanged`.
- Docs improvements
- Move `ParticleSystem` to the top of the `ARDebugPointCloudVisualizer`
