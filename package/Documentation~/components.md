# Components

There are a number of built-in component types that are provided for you to use when building your Tiny Mode projects. These are [similar but different to regular Unity Components](intro-for-unity-developers.md).

In Tiny Mode, components adhere to the [Entity-Component-System](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) pattern, and as such they serve to _store data only_ for a particular aspect of an [Entity](entities.md). They do not provide functionality.

Many Tiny Mode modules such as [Physics2D](module-physics2d.md) and [Audio](module-audio.md) provide [built-in components](built-in-components.md) which allow you to make use of their features.

You can also create your own [custom components](creating-custom-components.md) which allows you to define your own sets of data that you can attach to Entities.