# Scripting in Tiny

Tiny is a game engine based on the [Entity-Component-System](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) architectural pattern. While you can author entities and components in the Editor, Systems are authored in code.

Tiny supports a single programming language at the moment: [TypeScript](https://www.typescriptlang.org/).

> 2018-09: We're working on C# support, which will become the recommended programming language on Tiny. You'll be able to invoke JavaScript from C# on Web platforms.

## Setup

You don't need to install anything to get TypeScript working in Tiny, but you may want to setup a different IDE to open `.ts` files. You can configure your default TypeScript IDE by opening the *Preferences* settings window (`Unity / Preferences...` on Mac, `Edit / Preferences...` on Windows), go to the *Tiny Preferences* tab, and modify the *IDE Path* property to your preferred IDE.

We highly recommend [Visual Studio Code](https://code.visualstudio.com/), but other popular IDEs such as JetBrains Rider, Sublime Text, and Visual Studio also work just fine.

## Systems

In Tiny, a System is a function processing entity data.

![Entities, Components, and Systems](../images/ecs-example.png)

In the example above, the *Bullet* entity is processed by the *Render System* since it contains both the *Render* and *Position* components. It is also processed by the *Spawning System* since it contains the *Position* and *Spawning* components.

### Creating a System

First, make sure you have a Tiny project open. Then, click on the `Tiny / Create / TypeScript System` menu item to create a new script in your project's `Scripts` folder.

> The `Scripts` folder is created for you by default. You can rename or move it in the Project window. You can also set it using the `Tiny` section of the `Settings` window and edit the `Script Root Directory` property.

```typescript
// Scripts/NewSystem.ts

namespace game {

    /** New System */
    export class NewSystem extends ut.ComponentSystem {
        
        OnUpdate(): void {

        }
    }
}
```

Let's break this script down and explain every part.

`namespace game` is your Tiny project `Default Namespace` property. You can change it as needed.

`export class NewSystem` means you're exposing the `NewSystem` class in the `game` namespace. You can change the class name as needed, but you must add the `export` modifier when declaring a system, otherwise it won't be considered.

`extends ut.ComponentSystem` means you're extending the built-in `ut.ComponentSystem` class, which is the base class for all user-defined systems in Tiny. `ut` is the default namespace in Tiny, and exposes every class and module of the scripting API.

`OnUpdate(): void` is a function you need to implement when extending the `ut.ComponentSystem` class. It is called exactly once per frame. This method takes no parameters, and returns nothing.

That's it. Systems are functions, as simple as that.

### Processing Entity Data

Now that we know how to define a system, let's do something with it.

Systems can process data by iterating over entities with a specific component *specification*.

Components are plain old data objects defined in the Editor. Use the *Tiny / Create / Component* menu to create a new Component. **You cannot define a component in TypeScript.**

When defining a component in the Editor, you automatically and immediately get TypeScript support for it in your IDE: a component class named after your component and your project's default namespace, extending the base `ut.Component` class, is declared and available to your game code.

Here's a simple System example.

```typescript
// Scripts/LogPosition.ts

namespace game {

  /** Logs the 2D world position of every entity to the console. */
  @ut.executeAfter(ut.Shared.PlatformRenderingFence)
  export class LogPosition extends ut.ComponentSystem {

    static onlyOnce: boolean = false;

    OnUpdate(): void {
      if (LogPosition.onlyOnce)
        return;
      LogPosition.onlyOnce = true;
      
      this.world.forEach([ut.Entity, ut.Core2D.TransformNode], (entity, transformNode) => {
        let worldPos = ut.Core2D.TransformService.computeWorldPosition(this.world, entity);

        console.log(`${this.world.getEntityName(entity)} is at (${worldPos.x}, ${worldPos.y})`);
      });
    }
  }
}
```

`this.world` is how you access the Data Access Layer (DAL). This object gives you an interface to get, set, or iterate on entities within the current World.

`dal.forEach(filter, callback)` is how you iterate over each entity matching the given set of components in that world. Internally, we make sure that this process runs as fast as possible.

`[ut.Entity, ut.Core2D.TransformNode]` is a simple component type array filter that translates to *give me all entities with at least the `ut.Core2D.TransformNode` component*. Adding `ut.Entity` in a filter gives you back the entity handle.

`(entity, transformNode) => { ... }` is the function called once for each entity returned by the filter.

In this example, the `LogPosition` system uses the `TransformService` to get the world position of an entity and log it. You can see the output in the *Console* section of your browser's *Developer Tools*.

* [Chrome DevTools](https://developers.google.com/web/tools/chrome-devtools/)
* [Firefox Toolbox](https://developer.mozilla.org/en-US/docs/Tools/Tools_Toolbox)
* [Safari Developer](https://support.apple.com/en-ca/guide/safari-developer/welcome/mac)

Let's consider another example.

```
// Defined in the Editor

game.Thing:
	radius: Int32
	time: Float32
	speed: Float32
```

```typescript
// Scripts/CircleMovement.ts

namespace game {

  /** Moves things in circle */
  @ut.executeAfter(ut.Shared.UserCodeStart)
  @ut.executeBefore(ut.Shared.UserCodeEnd)
  export class CircleMovement extends ut.ComponentSystem {

    static readonly twoPi: number = 2 * Math.PI;

    OnUpdate(): void {
      let dt = this.scheduler.deltaTime();

      this.world.forEach([ut.Entity, game.Thing], (entity, thing) => {
        let r = thing.radius;
        let t = thing.time + dt;

        let angle = t * CircleMovement.twoPi * thing.speed;

        let x = r * Math.cos(angle);
        let y = r * Math.sin(angle);

        // the iteration doesn't include the `ut.Core2D.TransformLocalPosition` component,
        // you must get and set its component data explicitly, or use the `usingComponentData` helper method

        this.world.usingComponentData(entity, [ut.Core2D.TransformLocalPosition], (position) => {
          position.position = new Vector3(x, y, 0);
        });

        // `thing` is a component view - that is, you can write directly to it
        thing.time = t;
      });
    }
  }
}
```

The `CircleMovement` system operates on every entity with a `game.Thing` component. Within the `OnUpdate` method, it first reads the frame delta time from the *scheduler* (in seconds), and then sets the local position of every entity returned by the filter as a function of the `time`, `radius`, and `speed` properties.

The system itself also stores a constant, 2 times Pi, to model a full rotation per second (at a speed of 1.0). Systems shouldn't store entity data outside the `OnUpdate` function, but constants like this one are fine.

The animation state is stored in the `time` property, updated every frame.

### Scheduling Systems

The `CircleMovement` example above will run each frame, but it could run anytime *during* a frame. That is, it could be the first system to be executed, or the last...

If you need systems to execute before or after other systems, you need to instrument your code with *decorators*.

`@ut.executeAfter` and `@ut.executeBefore` are two decorators you can use to schedule your systems. They take any number of system types as parameters, and create an edge in the system execution graph.

`ut.Shared.UserCodeStart` is a special system, called a Fence.

## Fences

Fences are basically empty nodes in the system execution graph: they perform no logic, but you can use them to schedule systems without knowing every other system in your game. Currently, all fences live under `ut.Shared`.

Here's the current fence scheduling order:
1. InputFence
2. UserCodeStart
3. UserCodeEnd
4. RenderingFence
5. PlatformRenderingFence

## Entity Filters

You can extend `ut.EntityFilter` to model queries, or filters, against a given World.

Here's an example:

```
// Defined in the Editor

game.Movement:
	speed: Vector2

game.SpeedBoost:
	multiplier: Float32
```

```typescript
// Scripts/MovementFilter.ts

namespace game {
  
  export class MovementFilter extends ut.EntityFilter {
    node: ut.Core2D.TransformNode;
    position: ut.Core2D.TransformLocalPosition;
    movement: game.Movement;
    speedBoost?: game.SpeedBoost;
  }
}
```

On `ut.EntityFilter` class extensions, fields of type `ut.Component` are *automatically* assigned to filter instances during iteration. For example:

```typescript
// Scripts/MovementSystem.ts


namespace game {
  
  export class MovementSystem extends ut.ComponentSystem {
    filter: MovementFilter = new MovementFilter();
    OnUpdate(): void {
      let dt = this.scheduler.deltaTime();
      this.filter.ForEach(this.world, (entity) => {

        // node and movement are required: they're available
        // position and speedBoost are optional: you have to test their existence before using them
        let data = this.filter;

        var s = data.movement.speed;

        // speedBoost is optional: you need to check for its existence before using it
        if (data.speedBoost) {
          let m = data.speedBoost.multiplier;
          s.x *= m;
          s.y *= m;
        }

        var localPosition = data.position;
        let p = localPosition.position;
        localPosition.position = new Vector3(p.x + s.x * dt, p.y + s.y * dt, 0);
      });
    }
  }
}

```

## Behaviours

If you used Unity before, you may be familiar with the ubiquitous `MonoBehaviour` base class. Tiny attempts to replicate this pattern by providing the `ut.ComponentBehaviour` base class.

> We're not introducing Object-Oriented programming here - rather, we're using ECS to provide a `MonoBehaviour`-like scheduling pattern and event model.

Here's a quick example:

```
// Defined in the Editor

game.Counter:
	frame: Int32
```
```typescript
// Scripts/FrameCounter.ts

namespace game {

  export class FrameCounterFilter extends ut.EntityFilter {
    entity: ut.Entity;
    counter: game.Counter;
  }

  export class FrameCounter extends ut.ComponentBehaviour {

    entityData: FrameCounterFilter;

    OnEnable(): void {
      console.log("FrameCounter enabled");
      this.entityData.counter.frame = 0;
    }

    OnUpdate(): void {
      let frame = this.entityData.counter.frame + 1;
      this.entityData.counter.frame = frame;
      console.log(`frame ${frame}`);
      if (frame == 5) {
        this.Disable(this.world, this.entityData.entity);
      }
    }

    OnDisable(): void {
      console.log("FrameCounter disabled");
    }
  }
}
```
```
// console output:
FrameCounter enabled
frame 1
frame 2
frame 3
frame 4
frame 5
FrameCounter disabled
```

When extending `ut.ComponentBehaviour`, you must:
* Define **exactly one** `ut.EntityFilter` field. In the example above, this is the `entityData` field.
* Define **at least one** standard method
  * OnEnable: called once when an entity matching the filter is initialized or becomes enabled.
  * OnUpdate: called every frame when an entity matching the filter is enabled
  * OnDisable: called once when an entity matching the filter becomes disabled

You can also use the `ut.executeAfter` and `ut.executeBefore` decorators to control the execution order between behaviours. However, since behaviours come with implicit scheduling, **you can't schedule behaviours against individual systems or fences**.

The `OnEnable` event methods are executed before `OnUpdate`, which are executed before `OnDisable`. For example, if you have 2 classes of `ComponentBehaviour`: `A` and `B`, with `B` scheduled after `A`, and with behaviours defining all supported behaviour methods, the execution order would be:

1. A.OnEnable
1. B.OnEnable
1. ut.Shared.InputFence
1. ...
1. ut.Shared.UserCodeStart
1. ...
1. A.OnUpdate
1. B.OnUpdate
1. ...
1. ut.Shared.UserCodeEnd
1. ...
1. ut.Shared.PlatformRenderingFence
1. A.OnDisable
1. B.OnDisable

Extending `ut.ComponentBehaviour` gives you:
* Automatic behaviour state management per entity. No need to create extra components manually to track *initialized* or *enabled* states.
* Automatic scheduling of the supported event methods: OnEnable happens before OnUpdate, which happens before OnDisable.

At runtime, exactly one `ut.ComponentBehaviour` instance is created per behaviour type. You can access this instance by using the `Instance` property of any behaviour type.

Note that using `ut.ComponentBehaviour` is not free - it comes with a small  performance overhead. If state management is not required, or if you need to iterate over multiple sets of entities within a system, prefer `ut.ComponentSystem`.

## Advanced Topics

### Low-level: Entity Command Buffers
Using an `EntityCommandBuffer` object, you can create a list of changes (commands), and commit them to a `World` later. Command buffers allow you to create entities, destroy them, and add / modify / remove components on them.

You can store a list of commands in an `EntityCommandBuffer` object, and `commit` it when needed. For example, you can use command buffers to *queue* changes during iteration, and effectively *defer* structural changes until the iteration is over.

### tsconfig.json

Tiny will generate and maintain a `tsconfig.json` file at the root of your *Unity* project folder (not the Tiny project folder). You can ignore this file from source control, as it is regularly updated by the Editor - for example when you switch between projects, modify module dependencies, etc.

Should you need to modify this file - for example to control compiler options - you can add a `tsconfig.override.json` file at the root of your project `Scripts` folder. What you put in this file gets merged in the final `tsconfig.json` file when updated.

Here's an example you can use to disallow implicit *any* type references in your scripts:

```json
{
	"compilerOptions": {
		"noImplicitAny": true
	}
}
```

See the [tsconfig.json](https://www.typescriptlang.org/docs/handbook/tsconfig-json.html) page of the TypeScript handbook for more options.
