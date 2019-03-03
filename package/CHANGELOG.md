# Changelog
All notable changes to this package will be documented in this file.

<!--
## [0.2.2] - 2019-02-XX
### Patch Update of *Unity Package Animation Rigging*.
- Added dependency on Burst package to optimize all rigging constraint jobs.
-->

## [0.2.1] - 2019-02-28
### Patch Update of *Unity Package Animation Rigging*.
- Added Burst support to existing constraints.  The Animation Rigging package now depends on com.unity.burst.
- Upgraded weighted transform arrays in order for weights to be animatable.  The following constraints were modified and will require a manual update:
	- MultiAimConstraint
	- MultiParentConstraint
	- MultiPositionConstraint
	- MultiReferentialConstraint
	- TwistCorrection

## [0.2.0] - 2019-02-12

### Keyframing support for *Unity Package Animation Rigging*.
- Changed RigBuilder to build and update the PlayableGraph for Animation Window.
- Added attribute [NotKeyable] to properties that shouldn't be animated.
- Removed 'sync' property flag on transform fields for constraints. Syncing scene data to the animation stream is now performed by marking a constraint field with [SyncSceneToStream].
- Fixed issue where constraint parameters were evaluated one frame late when animated.
- Added attribute [DisallowMultipleComponent] to constraints to avoid use of multiple constraints per Game Object.
- Updated constraints to use new AnimationStream API to reduce engine to script conversion.
- Added IAnimatableProperty helpers for Bool/Int/Float/Vector2/Vector3/Vector4 properties. 
- Added ReadOnlyTransformHandle and ReadWriteTransformHandle.

## [0.1.4] - 2018-12-21

### Patch Update of *Unity Package Animation Rigging*.
- Fixed onSceneGUIDelegate deprecation warning in BoneRenderUtils

## [0.1.3] - 2018-12-21

### Patch Update of *Unity Package Animation Rigging*.
- Fixed stale bone rendering in prefab isolation view.
- Updated constraints to have a transform sync scene to stream toggle only on inputs.
- Fixed Twist Correction component to have twist nodes with weight varying between [-1, 1]
- Added Maintain Offset dropdown to TwoBoneIK, ChainIK, Blend and Multi-Parent constraints
- Added Rig Transform component in order to tag extra objects not specified by constraints to have an influence in the animation stream
- Updated documentation and samples to reflect component changes

## [0.1.2] - 2018-11-29

### Patch Update of *Unity Package Animation Rigging*.
- Added constraint examples to Sample folder (Samples/ConstraintExamples/AnimationRiggingExamples.unitypackage)
- Fixed links in documentation
- Updated package description

## [0.1.1] - 2018-11-26

### Patch Update of *Unity Package Animation Rigging*.
- Improved blend constraint UI layout
- Fixed jittering of DampedTransform when constraint weight was in between 0 and 1
- Made generic interface of Get/Set AnimationJobCache functions
- Added separate size parameters for bones and tripods in BoneRenderer.
- Fixed NullReferenceException when deleting skeleton hierarchy while it's still being drawn by BoneRenderer.
- Fixed Reset and Undo operations on BoneRenderer not updating skeleton rendering.
- Reworked multi rig playable graph setup to have one initial scene to stream sync layer followed by subsequent rigs
- Prune duplicate rig references prior to creating sync to stream job
- Added passthrough conditions in animation jobs for proper stream values to be passed downstream when job weights are zero. Fixes a few major issues when character did not have a controller.
- Fixed bug in ChainIK causing chain to not align to full extent when target is out of reach
- Fixed TwoBoneIK bend normal strategy when limbs are collinear
- Reworked AnimationJobData classes to be declared as structs in order for their serialized members to be keyable within the Animation Window. 
- Renamed component section and menu item "Runtime Rigging" to "Animation Rigging"
- Added check in SyncToStreamJob to make sure StreamHandle is still valid prior to reading it's values.
- Adding first draft of package documentation.

## [0.1.0] - 2018-11-01

### This is the first release of *Unity Package Animation Rigging*.
### Added
- RigBuilder component.
- Rig component.
- The following RuntimeRigConstraint components:
	- BlendConstraint
	- ChainIKConstraint
	- MultiAimConstraint
	- MultiParentConstraint
	- MultiPositionConstraint
	- MultiReferentialConstraint
	- OverrideTransform
	- TwistCorrection
