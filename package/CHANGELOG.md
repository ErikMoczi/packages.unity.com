# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- New installation steps: copy-paste the Unity scene files in your `Assets` folder.

### Changed

- Better CHANGELOG.md.
- Animation C# jobs are now in `Runtime/AnimationJobs/`.
- Sample scripts are now in `Samples/Scripts`.
- Sample scenes are now in `Samples/Scenes`.
- Fix warning about the unused `weight` variable.

### Removed

- The .unitypackagefile in favor of copy-pasting the Unity scene files directly.

## [0.5.0-preview] - 2018-05-30

### Changed

- First version into production.

## [0.4.0] - 2018-05-30

### Added

- A unitypackage file for the scenes.

### Fixed

- Some errors when disabling and re-enabling scripts.

## [0.3.0] - 2018-05-28

### Added

- New sample: LookAt.
- The Chomper model from [3D Game Kit](https://assetstore.unity.com/packages/essentials/tutorial-projects/3d-game-kit-115747).

### Changed

- Rename sample BoneWeightMask to WeightedMaskMixer.

## [0.2.0] - 2018-05-24

### Added

- New sample: SimpleMixer.
- A humanoid and a generic DefaultMale FBX files with Idle and ROM animations
  for both (see `Resources/DefaultMale/Models/`).

### Changed

- Rename sample CustomMixer to BoneWeightMask.
- Improve the TwoBoneIK script. 
- Factorize utility functions in `SampleUtility.cs`

## 0.1.0 - 2018-05-07

### Added

- New samples: CustomMixer, FullBodyIK, and TwoBoneIK.
- First release of *Unity Package Animation C# Jobs Samples*.

[Unreleased]: https://gitlab.internal.unity3d.com/upm-packages/animation/com.unity.animation.cs-jobs-samples/compare/v0.5.0-preview...HEAD
[0.5.0-preview]: https://gitlab.internal.unity3d.com/upm-packages/animation/com.unity.animation.cs-jobs-samples/v0.4.0...v0.5.0-preview
[0.4.0]: https://gitlab.internal.unity3d.com/upm-packages/animation/com.unity.animation.cs-jobs-samples/v0.3.0...v0.4.0
[0.3.0]: https://gitlab.internal.unity3d.com/upm-packages/animation/com.unity.animation.cs-jobs-samples/v0.2.0...v0.3.0
[0.2.0]: https://gitlab.internal.unity3d.com/upm-packages/animation/com.unity.animation.cs-jobs-samples/compare/v0.1.0...v0.2.0

