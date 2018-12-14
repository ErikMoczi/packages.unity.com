- [Engine Overview](#engine-overview)
- [Entity Component System Overview](#entity-component-system-overview)
  * [Entity](#entity)
  * [Component](#component)
  * [System](#system)
- [Schema Definition vs Data Definition](#schema-definition-vs-data-definition)

<!-- toc -->

## For Unity Developers

Unity developers are familiar with the `GameObject` and `Component` concepts. GameObjects are organised in a `Transform` hierarchy and are stored in assets - either Scenes or Prefabs - for later intantiation in game. Components are scripts attached to game objects. They hold both data and the logic executed each frame by the Unity runtime, usually by implementing `MonoBehaviour` *messages* pre-defined by Unity such as `Awake`, `Update`, and `OnDestroy`.

uTiny shares concepts with Unity. `Entities` are simplified game objects, without the mandatory Transform component. In fact, an entity in uTiny is a number, nothing more. You can attach data to entities by adding `Components` to them. The uTiny runtime is optimized for dynamic component allocation - which makes attaching components to entities a lightweight operation (think of it as *flipping a bit*).

Components in uTiny are plain old data (POD) objects, with no logic attached to them. To provide logic, you must write a `System`. A system is written in **JavaScript**, and is defined by the components it processes. You can think of systems as functions executed once per frame for each entity mathing their component signature.

To schedule systems for execution, you simply enable them in the uTiny editor, and define their *Execute Before* and *Execute After* constraints to control when they'll be executed within a frame.

Your challenges as a Unity developer learning uTiny are as follows:
1. Embrace a pure Entity-Component-System (ECS) development model
2. Switch from C# to JavaScript
3. Restrict yourself to what's supported by the uTiny runtime

To ease this transition, we try to keep the uTiny runtime API similar to the Unity API. We also provide an asset pipeline hosted in Unity.

## For Web Developers

**WIP**

The uTiny runtime is written in C++, and compiled to JavaScript via Emscripten

## Engine Overview

uTiny is a lightweight game runtime independent of the Unity runtime. The uTiny Editor Extension enables Unity developers to produce content for the uTiny runtime.

## Entity Component System Overview

This section is to give you a brief overview of how entity component systems work in order to help you produce content. ECS is built around the concept of data oriented design. 

### Entity

An entity conceptually represents a single object or actor in your game. It is defined by a single `id` and a bitmask of `components` that represent the data for the entity.

Entities can be created in one of two ways.

```javascript



```

### Component

Components represent blocks of data.

### System

Systems execute your logic and transform your data. A system operates on all entities that match a specific signature.

```javascript

function (shed, world) {

}

```

## Schema Definition vs Data Definition

In uTiny all types are dynamic. Theres no compilation. A type can be created and registered on the fly.