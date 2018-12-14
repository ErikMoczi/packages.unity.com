# HitBox2D Module

Tiny supports a lightweight intersection and overlap checking module that does not require physics. This module is referred to as HitBox2D. This document provides common use cases, examples, and a reference of HitBox2D components.

## Use Cases and Examples

This section demonstrates how to use the HitBox2D module through common use cases and usage examples. If a use case or an example is similar to a topic in the Unity User Manual, a link is provided to the appropriate topic.

### Simple HitBox2D

1. Create a [Sprite2D entity](./manual-module-core2d.md#simple-sprite-rendering).
2. Add a [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) component.

> The [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) component will adjust its hitbox to fit the Sprite2D's shape.

Alternatively, you can add a [RectHitBox2D](#recthitbox2d) component instead of a [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) if you want to specify the hitbox's position and scale.

Please refer to the `raider` or `spaceship` entity in the [`GalaxyRaiders`](#example-galaxy-raiders) project for examples.

### Detect Collisions

1. Create two [Sprite2D entities](./manual-module-core2d.md#simple-sprite-rendering).
2. Add a hitbox on both entities using a [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) or a [RectHitBox2D](#recthitbox2d) component.
3. Create a system.
4. Add the [HitBoxOverlapResults](#hitboxoverlapresults) component to the Component Types list.

You can use the `overlaps` member of the [HitBoxOverlapResults](#hitboxoverlapresults) component to check if both entities are overlapping each other.

> Both entities have to be under the same camera.

Please refer to the `DamageSystem` in the [`GalaxyRaiders`](#example-galaxy-raiders) project for an example.

### Example: Galaxy Raiders

The `GalaxyRaiders` project found in `Assets/UTinySamples` covers the use of the HitBox2D Module components. In the project, you will find examples about adding a hitbox on an entity and implementing game logic when hitboxes overlap.

> This table can be used to associate a HitBox2D Component to a sample entity and/or system.

| HitBox2D Components | Entity Name | System Name |
| ------------- | ------------- | ------------- |
| [HitBoxOverlapResults](#hitboxoverlapresults) |  | DamageSystem |
| [RectHitBox2D](#recthitbox2d) | raider, border, defensePoint, groundBorder  |  |
| [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) | raiderLaser, spaceship, spaceshipLaser |  |

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

> Use this table to associate familiar Unity concepts with Tiny.

| Unity Component            | HitBox2D Components                                   |
| -------------------------- | ----------------------------------------------------- |
| Collider2D.OverlapCollider | [HitBoxOverlapResults](#hitboxoverlapresults)         |
| Box Collider 2D            | [RectHitBox2D](#recthitbox2d)                         |
| Polygon Collider 2D        | [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) |

### HitBoxOverlapResults

* Requires: [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) or [RectHitBox2D](#recthitbox2d)
* Unity Reference: [Collider2D.OverlapCollider](https://docs.unity3d.com/ScriptReference/Collider2D.OverlapCollider.html)

Provides a list of [HitBoxOverlaps](#hitboxoverlap). This list contains the other entities that are overlapping with the current entity's hitbox.

> Entities must be under the same camera.

### RectHitBox2D

* Unity Reference: [Box Collider 2D](https://docs.unity3d.com/Manual/class-BoxCollider2D.html)

A component describing a 2D hitbox for simple collision detection.

> Hitboxes can be separate from sprites rendering, but should have a [Transform](./manual-module-core2d.md#transform) component. A [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer) component (with an alpha `color` of more than 0) is needed with a RectHitBox2D.

|Property|Description|
|--------|-----------|
|box|Rectangle of hit area. Used in picking and non physics collision check. Pivot is at (0,0). The position is set with the `x` and `y` values. The scaling is set with the `width` and `height`. Defaults with the shape of the [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer).|

### Sprite2DRendererHitBox2D

* Unity Reference: [Polygon Collider 2D](https://docs.unity3d.com/Manual/class-PolygonCollider2D.html)

A component describing a 2D hitbox for simple collision detection. This component acts the same as a [RectHitBox2D](#recthitbox2d), but takes its size from a [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer).

> For custom rectangle shape, use [RectHitBox2D](#recthitbox2d). A [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer) component (with an alpha `color` of more than 0) is needed with a Sprite2DRendererHitBox2D.

|Property|Description|
|--------|-----------|
|pixelAccurate|Use pixel accurate hit testing from the Sprite. Defaults to false. In order to work, the Image2D component that is used for testing also needs to have the [Image2DAlphaMask](./manual-module-core2d.md#image2dalphamask) component placed next to it before image loading. Pixel accurate picking will ignore overlaps or hits where the sprite alpha is zero, but **can be very expensive** to compute. Please use this feature sparingly.|

## Systems

### HitBox2DSystem

* Updates Before: Shared.InputFence, Shared.RenderingFence

The main entry system for 2D intersection and overlap check. It will perform Overlap tests between all components under all cameras in the world. It will add or remove [HitBoxOverlapResults](#hitboxoverlapresults) components to any objects that have overlaps.

> It does not take into account motion of objects (use real physics in that case). Only objects that are overlapping under the **same camera** will have overlaps.

## Structs

### HitBoxOverlap

A struct describing a 2D hitbox overlap.

|Property|Description|
|--------|-----------|
|camera|An entity that represents the camera that sees the overlap.|
|otherEntity|The other entity, will have a [Sprite2DRendererHitBox2D](#sprite2drendererhitbox2d) or [RectHitBox2D](#recthitbox2d) component.|

### HitTestResult

|Property|Description|
|--------|-----------|
|entityHit|An entity that was hit or NONE if there was no hit.|
|uv|Normalized [0..1] coordinates inside the sprite of the hit point. Lower left origin, the same coordinate system as pivot points for [Sprite2D](./manual-module-core2d.md#sprite2d) components.|

### RayCastResult

|Property|Description|
|--------|-----------|
|entityHit|An entity that was hit or NONE if there was no hit.|
|t|Normalized [0..1] distance on ray. The hit point is `hit = rayStart + (rayEnd - rayStart) * t`. If the ray cast started from inside a hitbox, the `t` value will be the exit value.|

### DetailedHitBoxOverlap

|Property|Description|
|--------|-----------|
|vertices|A 2D convex polygon outline of the intersection of two hitboxes. Can be rendered as a [Shape2D](./manual-module-core2d.md#shape2d).|
