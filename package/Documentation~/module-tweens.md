# Tweens Module

The tweening module provides easy tweening functionality: It allows easy animating of individual component fields from code. 

Tweening provides a limited form of animation, in that it is not a full keyframe animation system. It allows linear interpolation between two values while modifying the interpolation time with one of several predefined function. 

Tweening is usually used to add little flourishes of animation to a game. For example tweening is useful for making a UI element bounce a little bit as a call for action. 

To start tweening, use the **TweenService.addTween** function. It takes a complete description of the value to tween: The entity to tween on, and then the component and field inside the component. In addition to that it takes the actual tweening curve: Start and end values, duration, a time offset, looping, and what interpolation mode to use. 

This description is then stored as a component on a newly created entity, which is returned by addTween. Under the hood, addTween is really just a short cut to:

*   Create tweening entity
*   Create a TweenComponent component, fill it in with value and add it to the tweening entity

Tweens are actually animated by the TweenSystem, which runs just like any other system. When it runs it iterates over all TweenComponent components and evaluates them. Because tweens are just regular components, it is possible to change or read them at any time like any other component data. This can be useful, for example to pause them, or to check if they have ended. 

Warning: Because tweening is driven by entities, it is easy to forget to destroy them when the tweening target is destroyed or the tween is over. You can either set the **destoryWhenDone** field to true so the TweenSystem can destroy them, or use any of the helper functions on TweenService like **removeAllTweensInWorld** between levels to clean up tweens. 

Note: Not all values can be tweened. Only **Color, Vector2, Vector3, float, and Quaternion** can be tweened. It is possible to tween individual values in sub-structs though. For example, it is possible to tween only the alpha channel of a color as a single float by passing for example **Sprite2DRenderer.color.a** into **addTween**. 

(See this module's API documentation for more information)