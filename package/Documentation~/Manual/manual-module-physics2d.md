# Physics2D Module Reference

Tiny supports a subset of Unity 2D Physics referred to as the Physics2D module. This document provides common use cases and an alphabetical reference of Physics2D components.

## Use Cases and Examples

This section demonstrates how to use the Physics2D module through common use cases and usage examples. If a use case or an example is similar to a topic in the Unity User Manual, a link is provided to the appropriate topic.

### Simple Static Collider Body

1. Create an entity.
2. Add a [RectCollider](#rectcollider) component.

A Static body does not move under simulation at all (gravity or collision).

### Simple Gravity System

1. Enable the `UTiny.Physics2D` module from the Modules tab.
2. Select the `UTiny.Physics2D` module to show the Configurations panel.
2. Modify the amount of gravity applied to all entities in the [Physics2DConfig](#physics2dconfig) settings, if needed.
3. Create an entity.
4. Add a [RectCollider](#rectcollider) component.
5. Set the [`bodyType`](#rectcollider-bodytype) property to `Dynamic` or `BulletDynamic`.

### Set Velocity to an Entity

1. Create an entity.
2. Add a [RectCollider](#rectcollider) component.
3. Set the [`bodyType`](#rectcollider-bodytype) property to `Dynamic`.
4. Add a [Velocity2D](#velocity2d) component.
5. Set the `velocity` property in the X and Y axis to get the desired speed and direction.

### Apply Impulse to an Entity

1. Create an entity.
2. Add a [RectCollider](#rectcollider) component.
3. Set the [`bodyType`](#rectcollider-bodytype) property to `Dynamic` or `BulletDynamic`.
4. Add an [Impulse2D](#impulse2d) component.
5. Set the `impulse` property to adjust the force in X and Y axis.

### Detect Collisions

1. Create two entities.
2. For each entity, add a [RectCollider](#rectcollider) component.
3. Set the [`bodyType`](#rectcollider-bodytype) property to `Dynamic` or `BulletDynamic` on the entity you want to detect collisions.
4. Create a user-based component. This will serve as an entities filter.
5. Create a system.
6. Add the newly created user-based component and the [ColliderContacts](#collidercontacts) component to the Component Types list.

You can use the information provided by the [ColliderContacts](#collidercontacts) to act upon collisions (e.g. print a collider entity's name).

### Example: PhysicsDemo

The `PhysicsDemo` project found in `Assets/UTinySamples` covers the use of the Physics2D Module components. In the project, you will find examples about how to give initial impulse to an entity, how to apply friction between entities, how to handle collisions, and how to control an entity (change its velocity and add impulse).

> This table can be used to associate a Physics2D Component to a sample entity and/or system.

| Physics2D Components | Entity Name | System Name |
| ------------- | ------------- | ------------- |
| [ColliderContacts](#collidercontacts) | Ball1 | CollisionSystem |
| [Impulse2D](#impulse2d) | Square2 | MoveSystem |
| [RectCollider](#rectcollider) | _All entities_ (except Camera) | |
| [Velocity2D](#velocity2d) | Square5 | MoveSystem |

## Module Configuration

### Physics2DConfig

* Requires: UTiny.Physics2D
* Unity Reference: [Physics 2D Settings](https://docs.unity3d.com/2018.2/Documentation/Manual/class-Physics2DManager.html)

Use Physics2DConfig to provide global settings for 2D physics. You can access Physics2DConfig via the project configurations panel by selecting a module from the Tiny Editor Modules tab.

|Property|Description|
|--------|-----------|
|gravity|The amount of gravity applied to all entities with a **Dynamic** or **BulletDynamic** [body type](#rectcollider-bodytype). Generally, gravity is only set for the negative direction of the y-axis. Defaults -10 to gravity's y-axis.|

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

> Use this table to associate familiar Unity concepts with Tiny.

| Unity Component                                    | Physics2D Components                  |
| -------------------------------------------------- | ------------------------------------- |
| Colliders, Physics2D.GetContacts                   | [ColliderContacts](#collidercontacts) |
| Rigidbody2D.AddForceAtPosition, ForceMode.Impulse  | [Impulse2D](#impulse2d)               |
| Box Collider 2D, Rigidbody 2D, Physics Material 2D | [RectCollider](#rectcollider)         |
| Rigidbody2D.velocity                               | [Velocity2D](#velocity2d)             |

### ColliderContacts

* Requires: [RectCollider](#rectcollider)
* Unity References: [Colliders](https://docs.unity3d.com/2018.2/Documentation/Manual/CollidersOverview.html), [Physics2D.GetContacts](https://docs.unity3d.com/ScriptReference/Physics2D.GetContacts.html)

Provides lists of tiny entities that are colliding with the actual entity.

> A contact will occur if both entities have a RectCollider. ColliderContacts provides information about an entity with a Dynamic/BulletDynamic [body type](#rectcollider-bodytype).

|Property|Description|
|--------|-----------|
|newContacts|An array of entities with all new collisions.|
|endedContacts|An array of entities with all collisions that ended.|
|contacts|An array of entities with all current collisions.|

### Impulse2D

* Requires: [RectCollider](#rectcollider)
* Unity References: [Rigidbody2D.AddForceAtPosition](https://docs.unity3d.com/ScriptReference/Rigidbody2D.AddForceAtPosition.html), [ForceMode.Impulse](https://docs.unity3d.com/ScriptReference/ForceMode.Impulse.html)

Applies a linear impulse force that acts straight through the entity's centre of mass producing only positional movement with no rotation. An entity can rotate if the force is applied at a certain point.

> In Unity, it is possible to add a force to a Rigidbody2D in script whereas it is possible to add an Impulse2D component to an entity in the editor and in a system in UTiny. If added to an entity as a component, the impulse will be applied to the entity at the beginning and then removed from it.

|Property|Description|
|--------|-----------|
|point|Position in world space to apply the impulse.|
|impulse|Components of the force in X and Y axes.|
|scaleImpulse|Chooses whether the impulse remains static or is multiplied by the scale of the entity. Set scaleImpulse to true to multiply the impulse by the scale of the entity. Set scaleImpulse to false keep the impulse to the same value regardless of the scale of the enity.|

### RectCollider

* Unity References: [Box Collider 2D](https://docs.unity3d.com/2018.2/Documentation/Manual/class-BoxCollider2D.html), [Rigidbody 2D](https://docs.unity3d.com/2018.2/Documentation/Manual/class-Rigidbody2D.html), [Physics Material 2D](https://docs.unity3d.com/Manual/class-PhysicsMaterial2D.html)

Entities can only move in the XY plane and can only rotate on an axis perpendicular to that plane. A RectCollider defines a rectangle collision area for the purposes of physical collisions. 

The area defined by the collider is invisible and does not need to be the same shape as the its sprite. A rough approximation is often more efficient and indistinguishable in gameplay.

The RectCollider component adds a Box Collider 2D with defined offset (`pivot`) and size (`width` and `height`) properties.

|Property|Description|
|--------|-----------|
|[bodyType](#rectcollider-bodytype)|Sets how the entity behaves under physics simulation. , how you can manipulate movement (position and rotation) behavior and collider interaction. Defaults to `Static`. <br> Options are: **Static**, **Kinematic**, **Dynamic**, **BulletDynamic**.|
|fixedRotation|Controls whether physics will change the rotation of the object. If enabled, the rotation in Z is not modified by the physics simulation.|
|friction|Coefficient of friction for this collider, **between [0..1]**. A value of 0 indicates a surface with no friction while a value of 1 indicates maximum friction.|
|restitution|The degree to which collisions rebound from the surface, **between [0..1]**. A value of 0 indicates no bounce (e.g. inelastic collision) while a value of 1 indicates perfect bounce with no loss of energy (e.g. elastic collision).|
|density|Change the density to change the mass calculations of the entity's associated body. If set to 0, its associated body ignores the RectCollider for all mass calculations, including centre of mass calculations.|
|width|Set the width of the collider rectangle in local space units. Defaults to the width of the attached 2D Sprite.|
|height|Set the height of the collider rectangle in local space units. Defaults to the height of the attached 2D Sprite.|
|pivot|Set the local offset of the collider geometry in a 2D coordinate (x, y). Defaults to 0.5 for x and y coordinates.|

### Velocity2D

* Requires: [RectCollider](#rectcollider)
* Unity References: [Rigidbody2D.velocity](https://docs.unity3d.com/ScriptReference/Rigidbody2D-velocity.html)

Defines the linear velocity of the entity.

> There is no Z direction in 2D physics. The velocity is not usually set directly but rather by using _forces_ such as the [Impulse2D](#impulse2d) component. In Unity, a Rigidbody2D's velocity is accessible in script whereas an entity's velocity is accessible in the editor and in a system in UTiny.

|Property|Description|
|--------|-----------|
|velocity|Velocity vector with components in the X and Y directions.|

## Systems

### Physics2DSystem

* Updates Before: Shared.InputFence

The main entry system for 2d physics. Manages data in the 2d physics simulation. Sets entities [collider](#rectcollider). Applies [velocities](#velocity2d) and [impulses](#impulse2d). Updates [collisions](#collidercontacts) information.

## Enums

### [RectCollider](#rectcollider) BodyType

|Value|Description|
|-----|-----------|
|Static|A Static body is designed to not move under simulation at all; if anything collides with it, a Static body behaves like an immovable object (as though it has infinite mass). That is the default body type, because it is the least resource-intensive body type to use. A Static body only collides with Dynamic bodies. Having two Static bodies 2Ds collide is not supported, since they are not designed to move.|
|Kinematic|A Kinematic body is designed to move under simulation, but only under very explicit user control. While a Dynamic body is affected by gravity and forces, a Kinematic body isn’t. For this reason, it is fast and has a lower demand on system resources than a Dynamic body. Use [ColliderContacts component](#collidercontacts) or [HitBox2D module](./manual-module-hitbox2d.md) to detect collisions. Use systems to decide where and how the body should move. A Kinematic body does not collide with other Kinematic bodies or with Static bodies; it only collides with Dynamic bodies. Similar to a Static body, a Kinematic body behaves like an immovable object (as if it has infinite mass) during collisions.|
|Dynamic|A Dynamic body is designed to move under simulation. It is affected by gravity and forces. A Dynamic body will collide with every other body type, and is the most interactive of body types. It’s also the most performance-expensive body type, because of its dynamic nature and interactivity with everything around it.|
|BulletDynamic|A BulletDynamic body is a dynamically managed physics object that is likely to be fast moving. The physics engine should track it in a special "continuous collision detection" way to avoid possible tunneling. It calculates exact impact position on collision.|
