# Built-in Components

Tiny Mode's built-in components cover common app and game related features such as Camera rendering, collision, audio, particles and UI. You can add components to Entities in the inspector in order to create Entities that serve distinct useful purposes in your project.

For example, if you are making a game which has a player-controlled character, you might create an Entity that has a position component (so that it can move around), a sprite renderer component (so that it can display a graphic), and a physics component (so that it can detect collisions with other things).

Tiny Mode Components contain _collections of related data._ For example, a position component contains numeric data for the x, y & z values. A camera component contains data about the viewport size, background colour, etc. 

Some built-in component types have corresponding built-in systems which act on them. For example, the Hitbox2D component has a corresponding system which applies gravity and performs physics calculations.

You can find out more about the built-in components by reading the documentation for each of the [modules](modules.md).

_Note: You cannot add more than one component of the same type to an Entity._

