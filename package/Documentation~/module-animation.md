# Animation Module

The Animation module allows you to animate properties in components. You can animate single entities as well as an entire hierarchy of the entities. Animations can be blended between each other which allows you to make smooth transitions.

The animation data, which consists of the reference to curves and target properties, is stored in the **AnimationClip** component. Entities with the AnimationClip component are referenced in the **AnimationClipPlayer** component, which also stores other parameters related to animation, like animation time or speed. One entity with the AnimationClip component can be referenced in many AnimationClipPlayer components. 

The system that works on the **AnimationClipPlayer** evaluates values for all animated properties. Those values can be applied directly to the animated entity (by using the AnimationTarget component). Or the values can be stored internally upon blending. Animation blending can be done programmatically using **AnimationService** class.

<!-- To Do : exteremly needs more info -->