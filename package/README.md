Animation C# Jobs Samples
=========================

The **Animation C# Jobs** allow to modify the animation stream directly inside
the data flow of the animation tree. This feature is powered by the
**Playables**.

This package gives some examples of how this feature could be used.

Prerequisites
-------------

In order to play with the animation C# jobs, knowledge of C# and the Playable
system is required.

How to use this package
-----------------------

Go to the `Packages/Animation C# Jobs Samples/Samples` directory and import `SampleScenes.unitypackage`.

Once imported, go to your `Assets` directory and open the Unity scene that you would like to test.

Samples
-------

### SimpleMixer

This sample shows how to mix two clips together on a generic character.

### WeightedMaskMixer

This sample is based on the SimpleMixer, but you can change the weights of some
joints (left arm, right arm, and head), so that the second clip only applies
based on the joint weights.

### LookAt

This sample adds a look-at behaviour on a Chomper, a quadruped creature from the
[3D Game Kit](https://assetstore.unity.com/packages/essentials/tutorial-projects/3d-game-kit-115747).

### TwoBoneIK

This sample adds a two-bone IK on a generic human character.

### FullBodyIK

This sample create a fullbody IK on a humanoid character.

See also
--------

The [GraphVisualizer](https://github.com/Unity-Technologies/graph-visualizer)
allows to dynamically see all the Playable graphs in the scene.
