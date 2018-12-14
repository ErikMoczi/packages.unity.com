# Particles Module

Tiny supports a subset of the particle workflows available in Unity. This subset is referred to as the Particles module. This document provides common use cases, examples, and a reference of Particles components.

## Use Cases and Examples

This section demonstrates how to use the Particles module through common use cases and usage examples. If a use case or an example is similar to a topic in the Unity User Manual, a link is provided to the appropriate topic.

### Simple Particle System

1. Create a [Sprite2D entity](./manual-module-core2d.md#simple-sprite-rendering). This will be your proto-particle.
2. Create an entity.
    1. Add a [ParticleEmitter](#particleemitter) component.
    2. Set the `particle` property to the Sprite2D entity.
    3. Add a [EmitterBoxSource](#emitterboxsource) component.

Modify the [ParticleEmitter](#particleemitter) or the [EmitterBoxSource](#emitterboxsource) properties to affect the emission (e.g. emission rate).

### Spawn particles with initial modifiers

After adding a particle system, it is easy to control the state of newly created particles. From the [ParticleEmitter](#particleemitter) component, you can add a related component using the `+` button.

1. Add a [EmitterInitialScale](#emitterinitialscale) component to set the scale of each particle on emission.
2. Add a [EmitterInitialRotation](#emitterinitialrotation) component to set the rotation around the z-axis of each particle on emission.
3. Add a [EmitterInitialVelocity](#emitterinitialvelocity) component to set the velocity (direction and speed) of each particle on emission.

For each of these components, it is possible to define the upper and lower bounds for the value; the actual value varies randomly between those bounds.

### Spawn particles with changes over time

After adding a particle system, it is easy to specify the particle's changes over time. From the [ParticleEmitter](#particleemitter) component, you can add a related component using the `+` button. Using a change over time component will most likely override any initial value component you provided.

#### Using a gradient
1. Create an empty entity.
    1. Add a Gradient component.
    2. Set the color/alpha values on the gradient so that it represents the lifetime of the particle.
2. Add a [LifetimeColor](#lifetimecolor) component to the particle system entity in order to change the particle's color over time.

#### Using a curve
1. Create an empty entity.
    1. Add a Curve component.
    2. Set the response values (y-axis) on the graph so that it represents the lifetime of the particle.

A Curve entity will be used to specify the value over the particle's lifetime for one of these components:
1. Add a [LifetimeAlpha](#lifetimealpha) component to the particle system entity in order to change the particle's transparency over time. `Note:` This component is ignored when the entity also has a [LifetimeColor](#lifetimecolor) component with a defined gradient.
2. Add a [LifetimeRotation](#lifetimerotation) component to the particle system entity in order to change the particle's rotation over time.
3. Add a [LifetimeScale](#lifetimescale) component to the particle system entity in order to change the particle's scale over time.
4. Add a [LifetimeVelocity](#lifetimevelocity) component to the particle system entity in order to change the particle's velocity (direction and speed) over time.

Alternatively, you can check the `Modulate` property to specify if the particle's value is multiplied with its start value.

### Example: Fireworks

The `Fireworks` project found in `Assets/UTinySamples` covers the use of the Particles Module components.

> This table can be used to associate a Particles Component to a sample entity.

| Particles Components | Entity Name |
| ------------- | ------------- |
| [EmitterBoxSource](#emitterboxsource) | FireworksEmitter1, FireworksEmitter2, FireworksEmitter3 |
| [EmitterInitialRotation](#emitterinitialrotation) | FireworksEmitter3 |
| [EmitterInitialScale](#emitterinitialscale) | FireworksEmitter3 |
| [EmitterInitialVelocity](#emitterinitialvelocity) | FireworksEmitter1, FireworksEmitter2, FireworksEmitter3 |
| [LifetimeAlpha](#lifetimealpha) | FireworksEmitter1, AlphaCurve |
| [LifetimeColor](#lifetimecolor) | FireworksEmitter2, FireworksEmitter3, ColorGradient1, ColorGradient2 |
| [LifetimeRotation](#lifetimerotation) | FireworksEmitter3, RotationCurve |
| [LifetimeScale](#lifetimescale) | FireworksEmitter1, FireworksEmitter2, FireworksEmitter3, ScaleCurve |
| [LifetimeVelocity](#lifetimevelocity) | FireworksEmitter1, FireworksEmitter2, FireworksEmitter3, DirectionCurve, SpeedCurve1, SpeedCurve2 |
| [ParticleEmitter](#particleemitter) | FireworksEmitter1, FireworksEmitter2, FireworksEmitter3 |

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

> Use this table to associate familiar Unity concepts with Tiny.

| Unity Particle System Module | Particles Components                  |
| ---------------------------- | ------------------------------------- |
| Emission, Shape              | [EmitterBoxSource](#emitterboxsource) |
| Color Over Lifetime          | [LifetimeAlpha](#lifetimealpha)       |
| Color Over Lifetime          | [LifetimeColor](#lifetimecolor)       |
| Rotation Over Lifetime       | [LifetimeRotation](#lifetimerotation) |
| Size Over Lifetime           | [LifetimeScale](#lifetimescale)       |
| Velocity Over Lifetime       | [LifetimeVelocity](#lifetimevelocity) |
| Particle System Main module  | [ParticleEmitter](#particleemitter)   |

| Unity Particle System Property    | Particles Components                              |
| --------------------------------- | ------------------------------------------------- |
| 3D Start Rotation, Start Rotation | [EmitterInitialRotation](#emitterinitialrotation) |
| Start Size                        | [EmitterInitialScale](#emitterinitialscale)       |
| Start Speed                       | [EmitterInitialVelocity](#emitterinitialvelocity) |

### EmitterBoxSource

* Requires: [ParticleEmitter](#particleemitter)
* Unity References: [Emission module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysEmissionModule.html), [Shape module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysShapeModule.html)

Change the Shape of emission to a Box with defined position and scale. Particles will be emitted from a random spot inside the box. The `rect` will be centered around the emitter's position.

> Defaults the emission Rate over Time (number of particles emitted per unit of time) to 10.

|Property|Description|
|--------|-----------|
|rect|The region of the entity to use as the box emitter source.  The position is set with the x and y values. The scaling is set with the width and height. Defaults width and height to 50.|
|attachToEmitter|Sets whether the emitted particle's transform will be a child of this emitter. <br> If true, the emission position is set as the entity's local position, and the particle will be added as a transform child. <br> If false, the emitter's world position will be added to the emission position, and that result is set as the local position.|

### EmitterInitialRotation

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Particle System Main module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysMainModule.html)

Sets the initial angle of each particle on emission.

> Since the particle system is only in 2D coordinates as of right now, the angle is around the z-axis.

|Property|Description|
|--------|-----------|
|rotation|The initial rotation angle in **degrees** of each particle around the z-axis. If the start and end values are the same, the initial rotation is constant. Otherwise, the rotation is a random value between the start and end values.|

### EmitterInitialScale

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Particle System Main module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysMainModule.html)

Sets the initial scale of each particle on emission.

|Property|Description|
|--------|-----------|
|scale|The initial scale of each particle. Defaults to 1. If the start and end values are the same, the initial scale is constant. Otherwise, the scale is a random value between the start and end values.|

### EmitterInitialVelocity

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Particle System Main module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysMainModule.html)

Sets the initial velocity (direction and speed) of each particle on emission.

|Property|Description|
|--------|-----------|
|direction|The initial emission direction of each particle from the chosen emission position. The direction is represented by an angle in **radians** around the z-axis (positive counter clockwise). If the start and end values are the same, the initial direction is constant. Otherwise, the direction is a random value between the start and end values.|
|speed|The initial speed of each particle in the specified direction. If the start and end values are the same, the initial speed is constant. Otherwise, the speed is a random value between the start and end values.|

### LifetimeAlpha

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Color Over Lifetime module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysColorOverLifeModule.html)

Specifies how a particle's transparency changes over its lifetime. It modifies the [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer)'s alpha.

> This component is overrided by the [LifetimeColor](#lifetimecolor) component. The transparency value is between [0..1].

|Property|Description|
|--------|-----------|
|curve|A curve which defines the particle's transparency, between [0..1], over its lifetime. In this case, the curve is a line graph that shows the transparency over time.|
|modulate|Sets if the alpha is multiplied with the particle's start alpha. <br> If true, the alpha is multiplied. <br> If false, the alpha is set directly.|

### LifetimeColor

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Color Over Lifetime module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysColorOverLifeModule.html)

Specifies how a particle's color changes over its lifetime. It modifies the [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer)'s color.

|Property|Description|
|--------|-----------|
|gradient|A gradient which defines the particle's color over its lifetime. In this case, the gradient is a visual representation of a colour progression, which simply shows the main colours (which are called stops) and all the intermediate shades between them.|
|modulate|Sets if the color is multiplied with the particle's start color. <br> If true, the color is multiplied. <br> If false, the color is set directly.|

### LifetimeRotation

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Rotation Over Lifetime module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysRotOverLifeModule.html)

 Specifies how a particle's angle changes over its lifetime. It modifies the [Transform](./manual-module-core2d.md#transform)'s local rotation.

> This component uses angular velocity in the editor and explicit rotation values in the runtime. In Unity, the rotation over lifetime indicates an angular velocity in degrees per second whereas in Tiny, the LifetimeRotation indicates the actual rotation value used at time x.

|Property|Description|
|--------|-----------|
|curve|A curve which defines the particle's rotation in **degrees** around the z-axis over its lifetime. In this case, the curve is a line graph that sets the rotation over time.|
|modulate|Sets if the rotation is multiplied with the particle's start rotation. <br> If true, the rotation is multiplied. <br> If false, the rotation is set directly.|

### LifetimeScale

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Size Over Lifetime module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysSizeOverLifeModule.html)

Specifies how a particle's scale changes over its lifetime. It modifies the [Transform](./manual-module-core2d.md#transform)'s scale with a uniform scale (on x, y, z axis).

|Property|Description|
|--------|-----------|
|curve|A curve which defines how the particle's scale changes over its lifetime. In this case, the curve is a line graph that sets the scale over time.|
|modulate|Sets if the scale is multiplied with the particle's start scale. <br> If true, the scale is multiplied. <br> If false, the scale is set directly.|

### LifetimeVelocity

* Requires: [ParticleEmitter](#particleemitter)
* Unity Reference: [Velocity Over Lifetime module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysVelOverLifeModule.html)

Specifies how a particle's velocity (direction and speed) changes over its lifetime.

|Property|Description|
|--------|-----------|
|directionCurve|A curve which defines how the particle's direction changes over its lifetime. In this case, the curve is a line graph that sets the direction in **radians** around the z-axis over time.|
|speedCurve|A curve which defines how the particle's speed changes over its lifetime. In this case, the curve is a line graph that sets the speed over time.|
|modulateDirection|Sets if the direction is multiplied with the particle's start direction. <br> If true, the direction is multiplied. <br> If false, the direction is set directly.|
|modulateSpeed|Sets if the speed is multiplied with the particle's start speed. <br> If true, the speed is multiplied. <br> If false, the speed is set directly.|

### ParticleEmitter

* Requires: [UTiny.Core2D](./manual-module-core2d.md), UTiny.Math
* Unity References: [Graphics.Particle Systems](https://docs.unity3d.com/2018.2/Documentation/Manual/ParticleSystems.html), [Particle System Main module](https://docs.unity3d.com/2018.2/Documentation/Manual/PartSysMainModule.html)

**This is the core particle emitter component.** When added to an entity, it becomes an emitter with given characteristics. The system continuously emits particles (loop). It is linked to the source data, the initial values and the lifetime values for particles. It contains global properties that affect the whole system (e.g. `lifetime` property). Various particle modifiers after initial emit can be added as component (e.g. [EmitterInitialScale](#emitterinitialscale)).

> The Unity `Prewarm`'s property is enabled if the prewarmPercent is equal or more than 0.5.

|Property|Description|
|--------|-----------|
|particle|The UTiny entity attached to the particle system and rendered in run-time. This proto-particle is used as a template.|
|maxParticles|The maximum number of particles in the system at once. If the limit is reached, some particles are removed. Defaults to 1000.|
|emitRate|The number of particles emitted per **second**. Defaults to 10.|
|startDelay|Delay in **seconds** before the system starts emitting once enabled.|
|prewarmPercent|The number of particles to immediately generate at start. It's the multiplier of maxPaticles (0.0: none, 1.0: maxParticles).|
|lifetime|The lifetime of each particle in **seconds**. Defaults start (min) and end (max) to 5.|

## Systems

### ParticleSystem

* Updates Before: Shared.InputFence, Shared.RenderingFence

The main entry system for particles. Spawns particles with given initial characteristics and updates particles' properties ([scale](#lifetimescale), [color](#lifetimecolor), [alpha](#lifetimealpha), [rotation](#lifetimerotation)) over time.
