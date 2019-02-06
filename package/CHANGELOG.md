# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.8-preview.1] - 2018-01-25
### Added
- Add compatibility for Unity 2019.1

## [1.0.7-preview.1] - 2018-11-15
### Added
- Fix IK SceneView Gizmo for Unity 2018.3
- Bumped minimum Unity version for this package to 2018.3

## [1.0.6-preview.1] - 2018-11-14
### Added
- Fix effector-target name. Effector becomes the transform that moves and the Target becomes the transform to reach.

## [1.0.6-preview] - 2018-6-20
### Added
- Fix compatibility issues with Bone Manipulator
- Restore default pose was not surviving between scenes.
- Fix case where parenting effectors and applying undo would make unity crash.
- Fix constrained target rotation in editor to SetLocalEulerHints
- Fix restore default pose and FK2IK should not rotate target when constrain rotation is false.
- Fix undo on dragging a bone in a chain

## [0.1.0] - 2017-11-20
### Added
- Edit Sprite Bind Pose
- Edit Sprite Skin Weights
- Edit Sprite Mesh
- IK
- Runtime Deformation using JobSystem