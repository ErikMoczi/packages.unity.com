# Animation Rigging package

Animation Rigging uses a set of predefined animation constraints to manually build a control rig hierarchy for a generic character. At runtime, rig constraints are converted to a list of IAnimationJobs which are appended as post-process operations to the controller playable graph.

## Rig Hierarchy

A character rig hierarchy can be defined by game objects and two types of components: rig and rig constraint components. This hierarchy should hold all necessary elements such as effectors/goal/manipulator objects that are required by certain constraint definitions. The rig hierarchy root lives at the same level as the skeleton root under the animator. In other words, the control hierarchy is not interlaced with the animation skeleton but rather lives beside it.

The Rig component is the main entry point to all the rig constraints. There should only be one Rig component per control hierarchy which is added to the root node. Its main purpose is to gather and collect all constraint components defined under its own local hierarchy to generate an ordered  list of IAnimationJobs which may then be appended to a playable graph. The order in which the jobs are evaluated is defined by the way the rig hierarchy is constructed since constraints are gathered using GetComponentsInChildren (which follows depth-first traversal).

## Rig Constraint Components

The constraint components live on game objects part of the rig hierarchy. They are essentially data containers for known constraint types. Exposed user fields in the property inspector are defined by IAnimationJobData and these components do not implement any Unity callbacks (Update, LateUpdate, etc.).

These are the constraints available in the Animation Rigging package:
- BlendConstraint
- ChainIKConstraint
- MultiAimConstraint
- MultiParentConstraint
- MultiPositionConstraint
- MultiReferentialConstraint
- OverrideTransform
- TwistCorrection

It's possible to create new constraint by inheriting from RuntimeRigConstraint.

## Rig Builder

The RigBuilder component lives alongside the Animator component and will create a Playable Graph that will be appended to the existing graph in the Animator.

