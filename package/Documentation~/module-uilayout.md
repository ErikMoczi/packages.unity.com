# UILayout Module

Tiny supports a subset of [Unity UI Basic Layout](https://docs.unity3d.com/2018.2/Documentation/Manual/UIBasicLayout.html) referred to as the UILayout module. This document provides common use cases, an example, and a reference of UILayout components.

## Use Cases and Examples

This section demonstrates how to use the UILayout module through common use cases and usage examples. If a use case or an example is similar to a topic in the Unity User Manual, a link is provided to the appropriate topic.

### Simple Responsive UILayout

1. Create an entity.
2. Add a [Transform](./manual-module-core2d.md#transform) component.
3. Add a [UICanvas](#uicanvas) component. A [RectTransform](#recttransform) is automatically added.
4. Set the [UICanvas](#uicanvas)'s `camera` property to the scene's [Camera2D](./manual-module-core2d.md#camera2d), the one used to render a view of your project.
5. Add a child entity. The child entity should already be populated with a [Transform](./manual-module-core2d.md#transform) and a [RectTransform](#recttransform) components.
6. Add a [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer) component to the child entity. A [Sprite2DRendererOptions](./manual-module-core2d.md#sprite2drendereroptions) is automatically added.
7. Set the `sprite` property to a Sprite asset.
8. Select an Anchor Preset by pressing the *Alt* key to set both the child entity's position and to anchor the child entity to its parent (e.g. anchored to the top right corner).

After exporting your project, your UILayout should adapt the Sprite2D's position if you adjust the screen orientation and size.

Please refer to the `Canvas`, `Background` and `Window` entities in the [`UILayoutDemo`](#example-uilayoutdemo) project for examples.

### Responsive Nested UILayout

Follow steps 1 to 5 of [Simple Responsive UILayout](#simple-responsive-uilayout).

1. Create another entity under the child entity of step 5.
2. Add a [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer) component. A [Sprite2DRendererOptions](./manual-module-core2d.md#sprite2drendereroptions) is automatically added.
3. Set the `sprite` property to a Sprite asset.
4. Select an Anchor Preset by pressing the *Alt* key to set both the entity's position and to anchor the entity to its parent (e.g. anchored and stretched to the middle).
5. Select the RectTool from the Unity Toolbar.

You should be able to move, resize and rotate the parent entity directly on the Unity Scene Window. The nested entity should adjust its position, size and rotation.

Please refer to the `Header`, `Dropdown` and `Button` entities in the [`UILayoutDemo`](#example-uilayoutdemo) project for examples.

### Example: UILayoutDemo

The `UILayoutDemo` project found in `Assets/UTinySamples` covers the use of the UILayout Module components. In the project, you will find an example about creating a responsive UI layout for mobiles (orientation) and web browsers (screen size).

> This table can be used to associate a UILayout Component to a sample entity and/or system.

| UILayout Components | Entity Name |
| ------------- | ------------- |
| [RectTransform](#recttransform) | Background, Window, Header, Dropdown, Button |
| [UICanvas](#uicanvas) | Canvas |

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

> This table can be used by Unity developers to associate familiar Unity concepts with Tiny.

| Unity Component       | UILayout Components             |
| --------------------- | ------------------------------- |
| Rect Transform        | [RectTransform](#recttransform) |
| Canvas, Canvas Scaler | [UICanvas](#uicanvas)           |

### RectTransform

* Requires: [ut.Core2D.Transform](./manual-module-core2d.md#transform), [UICanvas](#uicanvas) on the root entity
* Unity Reference: [Rect Transform](https://docs.unity3d.com/2018.2/Documentation/Manual/UIBasicLayout.html)

The RectTransform component is the 2D layout counterpart of the [ut.Core2D.Transform](./manual-module-core2d.md#transform) component. Where [ut.Core2D.Transform](./manual-module-core2d.md#transform) represents a single coordinate, RectTransform represents a rectangle with UI elements inside. RectTransform can be nested in order to specify how a child RectTransform should be positioned and sized relative to the parent rectangle. Please refer to the [Unity Basic Layout Documentation](https://docs.unity3d.com/2018.2/Documentation/Manual/UIBasicLayout.html) to learn how to use the Rect Tool, the Pivot and the Anchors.

> A [ut.Core2D.Sprite2DRendererOptions](./manual-module-core2d.md#sprite2drendereroptions) component is needed with a [Sprite2DRenderer](./manual-module-core2d.md#sprite2drenderer) component when using a RectTransform. Properties value from a RectTransform on a root entity will be driven by a UICanvas (RectTransform GUI is disable in this case).

|Property|Description|
|--------|-----------|
|anchorsMin|The anchor point for the **lower left corner** of the rectangle defined **as a fraction** of the size of the parent rectangle. <br> The normalized position (`X` and `Y` coordinates) 0,0 corresponds to anchoring to the lower left of the parent, while 1,1 corresponds to anchoring in the upper right corner of the parent.|
|anchorsMax|The anchor point for the **upper right corner** of the rectangle defined **as a fraction** of the size of the parent rectangle. <br> The normalized position (`X` and `Y` coordinates) 0,0 corresponds to anchoring to the lower left of the parent, while 1,1 corresponds to anchoring in the upper right corner of the parent.|
|left, top, right, bottom|Positions of the rectangle's edges relative to their anchors. This can be imagined as a padding inside the rectangle. Shown in place of `posX`, `posY`, `width`, `height` when the anchors are separated.|
|posX, posY, width, height|The rectangle's pivot position relative to the anchors is defined with `PosX` and `PoxY`. Size is defined with `Width` and `Height` properties. Shown in place of `left`, `top`, `right`, `bottom` when the anchors are grouped.|
|pivot|The normalized pivot point (`X` and `Y` coordinates), defined **between [0..1]**, represents the rectangle's origin. Rotation and scaling modifications occur around this point. <br> 0,0 corresponds to the lower left corner while 1,1 corresponds to the upper right corner. Defaults to 0.5 for `X` and `Y` coordinates.|
|blueprintMode|Enable **Blueprint mode** by clicking the `dotted squared` button in the Inspector. When **enable**, it allows editions on Rect Transforms as if there were no rotation and scaling. In this mode, you can snap the rectangle on sides and corners.|
|rawEditMode|Enable **Raw edit mode** by clicking the `R` button in the Inspector. When **enable**, manually editing pivot and anchor values will not counter-adjust the position and size of the rectangle in order to stay in place. Changing the pivot and anchor values by using the "anchor presets" button will counter-adjust as usual.|

### UICanvas

* Unity References: [Canvas](https://docs.unity3d.com/2018.2/Documentation/Manual/UICanvas.html), [Canvas Scaler](https://docs.unity3d.com/Manual/script-CanvasScaler.html)

The UICanvas is the area that all UI elements should be inside as child entities. It is an abstract space in which the UI is laid out and rendered. This component is also used to control the overall elements scale.

From a root entity containing a UICanvas, creating a new child entity automatically creates an entity with a [RectTransform](#recttransform) component.

> At the moment, UICanvas can only be a component of a root entity. Nested UICanvas is not supported. Creating multiple UICanvases, using separated entities, is possible. Note that the UI is rendered to the screen as if it was drawn on a placed object some distance in front of a given camera (see `Screen Space - Camera` [render mode](https://docs.unity3d.com/2018.2/Documentation/Manual/class-Canvas.html)).

|Property|Description|
|--------|-----------|
|camera|The camera to which the UI should be rendered.|
|[uiScaleMode](#uiscalemode)|Determines how UI elements in the UICanvas are scaled. Defaults to `ConstantPixelSize`. <br> Options are: **ConstantPixelSize**, **ScaleWithScreenSize**.|
|referenceResolution|When `uiScaleMode` is set to **ScaleWithScreenSize**, this property determines the resolution the UI layout is designed for on the `x` and `y` axis. If the screen resolution is larger, the UI will scaled up, and if it's smaller, the UI will scale down. Defaults to 1920 on the `x` axis and 1080 on the `y` axis.|
|matchWidthOrHeight|When `uiScaleMode` is set to **ScaleWithScreenSize**, it's possible to scale the canvas area with the width as reference, the height as reference, or something in between. <br> 0 corresponds to matching with the width whereas 1 corresponds to matching with the height.|

## Systems

### SetSprite2DSizeSystem

* Updates After: UILayout.UILayoutSystem
* Updates Before: Shared.RenderingFence

Sets the size of the Sprite2DRenderer sprite through the [Sprite2DRendererOptions](./manual-module-core2d.md#sprite2drendereroptions) component using the entity's [RectTransform](#recttransform) data (anchors and size) and the parent entity size.

### UICanvasSystem

* Updates After: Shared.UserCodeEnd, Shared.InputFence
* Updates Before: Shared.RenderingFence

Sets the entity's [Transform](./manual-module-core2d.md#transform) and [RectTransform](#recttransform) properties based on the [UICanvas](#uicanvas)'s property values such as the camera's scale and the scale mode.

### UILayoutSystem

* Updates After: UILayout.UICanvasSystem
* Updates Before: Shared.RenderingFence

Updates an entity's [Transform.localPosition](./manual-module-core2d.md#transform) based on the [RectTransform](#recttransform) component such as the anchors' positions and the pivot point.

## Enums

### UIScaleMode

See [Canvas Scaler](https://docs.unity3d.com/Manual/script-CanvasScaler.html) for a good reference on the scale modes.

|Value|Description|
|-----|-----------|
|ConstantPixelSize|Makes UI elements retain the same size in pixels regardless of screen size.|
|ScaleWithScreenSize|Makes UI elements proportionally bigger depending on the screen size.|
