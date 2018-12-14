# Core2D Module

The Core2D module provides support for a subset of Unity's 2D features in Tiny Mode. This page provides common use cases, examples, and a reference of the Core2D components.

### Simple Sprite Rendering

Core2D provides a **Sprite2DRenderer** component which you can use to draw sprites on the screen.

You can quickly create an entity with a Sprite2DRenderer component attached, and a sprite assigned, by dragging a Sprite asset from the Project window to the Tiny Hierarchy window. This creates an entity with a properly setup [Sprite2DRenderer](#sprite2drenderer) component.

Alternatively you can perform these steps manually:
1. Create an Entity
2. Add the [Transform](#transform) component
3. Add the [Sprite2DRenderer](#sprite2drenderer) component
4. Set the **sprite** property to a Sprite asset

Sprites are rendered in local space at the pixels-per-unit scale defined in the [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) asset. You can change the scale by selecting the Texture in the Project window and modifying its **Pixels Per Unit** property in the Inspector window.

You can add the [Sprite2DRendererOptions](#sprite2drendereroptions) component for more control over Sprite rendering.

### 9-slicing Sprites

1. Follow the steps in the [9-slicing Sprites](https://docs.unity3d.com/Manual/9SliceSprites.html)  topic in the Unity User Manual to prepare a Sprite asset.
2. Set the **Mesh Type** of the Sprite to **Full Rect**.
3. Set the appropriate Sprite borders in the Sprite Editor window.
4. Once your Sprite asset is properly configured, add both the [Sprite2DRenderer](#sprite2drenderer) and the [Sprite2DRendererOptions](#sprite2drendereroptions) components to the entity.
5. Follow the steps in [Simple Sprite Rendering](#simple-sprite-rendering).

Depending on your desired visual outcome, you can configure the [Sprite2DRendererOptions](#sprite2drendereroptions) drawMode property to either ContinuousTiling or AdaptiveTiling. Set the drawMode property to **Stretch**, the default value, to remove the 9-slicing rendering effect. See [DrawMode](#drawmode) for more details about these modes.

### Sorting Sprites

By default, Tiny Mode orders sprites according to the Z axis. You can use the following components to change this behavior:

- Use the [LayerSorting](#layersorting) component to place sprites on the specified Unity sorting layer or in the specified order. The LayerSorting component is ofter used to sort background sprites with foreground sprites. This component enables you to have as many sorting layers as you want.
- Use the [SortingGroup](#sortinggroup) component to group sprites together within a hierarchy. This component prevents multi-sprite objects from conflicting their ordering with other multi-sprite objects.
- Use the [Camera2DAxisSort](#camera2daxissort) component to modify the default `(0, 0, 1)` sort axis of cameras. This component orders sprites along the Y axis to create a fake perspective effect where lower Y sprites appear on top of higher Y sprites.

Tiny Mode uses the same priority as Unity for these different sorting components:

1. Sort by [LayerSorting](#layersorting) layer. If it's a tie, then:
2. Sort by [LayerSorting](#layersorting) order. If it's a tie, then:
3. Sort by [Camera2DAxisSort](#camera2daxissort) or default Z axis. If it's a tie, then:
4. Sorting is undefined.

Note that [SortingGroup](#sortinggroup) always precedes other sorting components to ensure sprites are always grouped together. However, sprites can be freely re-ordered within a [SortingGroup](#sortinggroup) and follows the same behavior and priority as described above.

### Using Sprite Atlas

A [Sprite Atlas](https://docs.unity3d.com/Manual/class-SpriteAtlas.html) is a texture asset containing multiple sprite images. It is used to export optimized textures in runtime saving memory space and allowing easy theme customization for your Tiny project.

1. Create a [Sprite Atlas](https://docs.unity3d.com/Manual/class-SpriteAtlas.html).
2. Add an object (e.g. sprite) for packing.
3. Follow the steps in [Simple Sprite Rendering](#simple-sprite-rendering). Select a sprite from the Sprite Atlas.

Alternatively, you can add a folder to the **Objects for Packing** if you have multiple sprites to add to your Sprite Atlas. You can preview the Sprite Atlas using the **Pack Preview** button.

_Note: Some settings such as **Allow Rotation** and **Tight Packing** are not supported in Tiny Mode yet._

The Sprite Atlas should be exported with the project. An exported Sprite Atlas will generate an entity with a [SpriteAtlas](#spriteatlas) component, as well as a texture. Sprites part of a Sprite Atlas will be exported with path `assets/sprites/<Sprite Atlas name>/<Sprite name>`. It is possible to get those Sprites by searching through the World's entities: `world.getByName(<Sprite path>)`. Please refer to the `SkinSystem` in the [`Flappy`](#example-flappy) project for an example.

> A Sprite Atlas will not be exported with the project if none of its sprites is  referenced by a Tiny entity (e.g. in a [Sprite2DRenderer](#sprite2drenderer) component). In this case, you can add an explicit reference to one of its sprites in the [Assets tab](../tiny-editor.md#assets-tab). By doing so, the Sprite Atlas should be exported properly and its sprites should be accessible in runtime.

### Example: Flying Yolk Project

The **Flying Yolk** project found in `Assets/UTinySamples` covers the use of the Core2D Module components and of Sprite Atlas. In the project, you will find an example about creating a game scene with a camera, positioning entities, rendering sprites, animating sprites, and using multiple themes with Sprite Atlas.

> This table can be used to associate a Core2D Component to an entity and/or a system.

| Core2D Components | Entity Name | System Name |
| ------------- | ------------- | ------------- |
| [Camera2D](#camera2d) | Camera |  |
| LayerSorting | Backgrounds, Grounds, Bird |  |
| [SortingGroup](#sortinggroup) | Grounds |  |
| [SpriteAtlas](#spriteatlas) |  | SkinSystem |
| [Sprite2DRenderer](#sprite2drenderer) | Backgrounds, ground0, ground1, ground2, ground3, Bird, GetReady, Tutorial, Title, GameOver, Scoreboard, Character_2, Character_1, Character_0, Pipe_Top, Pipe_Bottom |  |
| [Sprite2DRendererOptions](#sprite2drendereroptions) | Backgrounds | SkinSystem |
| [Sprite2DSequence](#sprite2dsequence) | Bird | SkinSystem |
| [Sprite2DSequencePlayer](#sprite2dsequenceplayer) | Bird |  |
| [Transform](#transform) | WorldScale, Backgrounds, Grounds, ground0, ground1, ground2, ground3, Bird, Bounds, Camera, GetReady, Tutorial, Title, GameOver, Scoreboard, FinalScore, Character_2, Character_1, Character_0, HighScore, Pipes, Pipe_Top, Pipe_Bottom, Score | AutoDestroySystem, RepeatedBackgroundSystem, ScrollingObjectSystem |

This project contains three Sprite Atlases (`Game`, `Day`, `Night`). `Game` contains common assets whereas `Day` and `Night` contain skin sprites (background, bird, pipe). To switch between the `Day` and `Night` skin, we rename sprites for the same entity with the same name (e.g. `bg` sprite in the [Assets tab](../tiny-editor.md#assets-tab)).

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

Use this table to associate familiar Unity concepts with Tiny Mode.

| Unity Component | Core2D Components                                            |
| --------------- | ------------------------------------------------------------ |
| AnimationClip   | [Sprite2DSequence](#sprite2dsequence)                        |
| Animator        | [Sprite2DSequencePlayer](#sprite2dsequenceplayer)            |
| Camera          | [Camera2D](#camera2d)                                        |
| SortingGroup    | [SortingGroup](#sortinggroup)                                |
| Sprite          | [Sprite2D](#sprite2d), [Sprite2DBorder](#sprite2dborder), [Shape2D](#shape2d) |
| Sprite Atlas    | [SpriteAtlas](#spriteatlas)                                  |
| SpriteRenderer  | [Sprite2DRenderer](#sprite2drenderer), [Sprite2DRendererOptions](#sprite2drendereroptions), [LayerSorting](#layersorting) |
| Texture2D       | [Image2D](#image2d)                                          |
| Transform       | [Transform](#transform)                                      |

### Camera2D

* Requires: [Transform](#transform)
* Unity Reference: [UnityEngine.Camera](https://docs.unity3d.com/ScriptReference/Camera.html)

Represents an orthographic camera.

|Property|Description|
|--------|-----------|
|clearFlags|Specifies how to clear the camera viewport before rendering. See [CameraClearFlags](#cameraclearflags).|
|backgroundColor|When *clearFlags* is set to *SolidColor*, this property controls which color is used when clearing.|
|cullingMode|Determines how the cullingMask is interpreted. See [CameraCullingMode](#cameracullingmode) for details.|
|cullingMask|A component mask that indicates which components are considered when culling entities, based on cullingMode.|
|halfVerticalSize|Half of the viewport height, in world units.|
|rect|The viewport region in screen coordinates, where (0,0) represents the lower left corner.|
|depth|Controls the rendering order when multiple cameras are active. Lower numbers render first.|

### Camera2DAxisSort

* Requires: [Camera2D](#camera2d)

Sorting axis selector. The sorting value used is the camera space position dot product with this vector.
* The default sort axis is Z: `(0, 0, 1)`
* To sort along the Y axis (typical in some 2D fake perspective) set to `(0, 1, 0)`.
* For isometric perspective one can also sort by unusual combinations like `(1, 1, 0)`.

|Property|Description|
|--------|-----------|
|axis|Direction used when sorting elements rendered by this camera.|

### Image2D

* Unity Reference: [UnityEngine.Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html)

Entities with this component are **created automatically** for you when exporting components referencing image data. Adding this component explicitly through scripting or the Editor should only be considered by advanced users.

|Property|Description|
|--------|-----------|
|imageFile|Path of the image asset.|
|maskFile|Path of the mask asset. The red channel will be used as the mask; efficient compression can be used (e.g. a single channel PNG or paletted PNG8).|
|pixelsToWorldUnits|Conversion ratio of image pixels to world units. A value of 0.01 equals to 100 pixels per world unit. Exported value is 1.0 divided by [TextureImporter.spritePixelsPerUnit](https://docs.unity3d.com/ScriptReference/TextureImporter-spritePixelsPerUnit.html).|
|disableSmoothing|Disable image bilinear filtering. Exported value is bound to [TextureImporter.filterMode](https://docs.unity3d.com/ScriptReference/TextureImporter-filterMode.html): true when filterMode is Point, false otherwise.|
|imagePixelSize|Read-only. Image size in pixels. Available once the image is loaded.|
|hasAlpha|Read-only. Whether or not the image contains semi-transparent pixels. Available once the image is loaded.|
|status|Read-only. Current image status. See [ImageStatus](#imagestatus).|

### Image2DAlphaMask

Used by the HitBox2D module.

### LayerSorting

* Requires: [Sprite2DRenderer](#sprite2drenderer) or [SortingGroup](#sortinggroup)
* Unity References:
  - When used with [Sprite2DRenderer](#sprite2drenderer) component: [Renderer.sortingLayerID](https://docs.unity3d.com/ScriptReference/Renderer-sortingLayerID.html) and [Renderer.sortingOrder](https://docs.unity3d.com/ScriptReference/Renderer-sortingOrder.html)
  - When used with [SortingGroup](#sortinggroup) component: [SortingGroup.sortingLayerID](https://docs.unity3d.com/ScriptReference/Rendering.SortingGroup-sortingLayerID.html) and [SortingGroup.sortingOrder](https://docs.unity3d.com/ScriptReference/Rendering.SortingGroup-sortingOrder.html)

Available values for the `layer` property match the [sorting layers](https://docs.unity3d.com/Manual/class-TagManager.html) defined in the Unity project settings.

> This component **does not** propagate down the [Transform](#transform) hierarchy.

|Property|Description|
|--------|-----------|
|layer|Unique ID of the sorting layer.|
|order|Order within that sorting layer.|

### Shape2D

* Requires: [Sprite2DRenderer](#sprite2drenderer)

> This component is experimental, and has **no Editor bindings** yet. You can use it, but won't be able to preview its appearance in the Scene or Game windows.

Add this component on an Entity with a [Sprite2DRenderer](#sprite2drenderer), and make sure its `sprite` property is set to None. The convex shape will be drawn, filled with the [Sprite2DRenderer](#sprite2drenderer) `color`.

This component can be used to create temporary placeholders until textures are available.

|Property|Description|
|--------|-----------|
|vertices|The vertices of the shape. Shapes must be convex.|

### Sprite2D

* Unity Reference: [UnityEngine.Sprite](https://docs.unity3d.com/ScriptReference/Sprite.html)

> Entities with this component are **created automatically** for you when exporting components referencing sprite data. Adding this component explicitly through scripting or the Editor should only be considered by advanced users.

|Property|Description|
|--------|-----------|
|image|The Entity on which to look for a [Image2D](#image2d) component to use as the source image. If null, the [Image2D](#image2d) is looked for on the same entity as the [Sprite2D](#sprite2d).|
|imageRegion|The region of the source image to use as the sprite.  The image is treated as a unit rectangle; thus this rectangle should use values in the range of [0..1]. For example, to use the bottom left portion of the image, the rectangle should go from (0, 0) to (0.5, 0.5)
|pivot|The point in the sprite that is the sprite's center.  Relative to the bottom-left corner of the sprite, in unit rectangle coordinates.|

### Sprite2DBorder

* Requires: [Sprite2D](#sprite2d)
* Unity Reference: [Sprite.border](https://docs.unity3d.com/ScriptReference/Sprite-border.html)

> Entities with this component are **created automatically** for you when exporting components referencing a Sprite with non-zero borders. Use the [Sprite Editor](https://docs.unity3d.com/Manual/SpriteEditor.html) to edit a Sprite border.

|Property|Description|
|--------|-----------|
|bottomLeft|Bottom left slice inset point, normalized [0..1] to sprite (not image). Defaults to (0,0) for no border.|
|topRight|Top right slice inset point, normalized [0..1] to sprite (not image). Defaults to (1,1) for no border.|

### Sprite2DRenderer

* Requires: [Transform](#transform)
* Unity Reference: [UnityEngine.SpriteRenderer](https://docs.unity3d.com/ScriptReference/SpriteRenderer.html)

|Property|Description|
|--------|-----------|
|sprite|The Entity on which to look for a [Sprite2D](#sprite2d) or [Shape2D](#shape2d) component to describe the sprite or shape to render. If null, the [Sprite2D](#sprite2d) or [Shape2D](#shape2d) is looked for on the same entity as the [Sprite2DRenderer](#sprite2drenderer).|
|alignToPixels|After transforming, should we align this sprite to pixels?|
|color|A color tint to apply to the sprite image. Defaults to opaque white (1,1,1,1). When the alpha component is set to zero, the sprite is culled (not drawn) automatically.|
|blending|[BlendOp](#blendop) for rendering the sprite. Defaults to Alpha.|

### Sprite2DRendererOptions

This component requires the [Sprite2DRenderer](#sprite2drenderer) component.

|Property|Description|
|--------|-----------|
|size|Sprite size in world units. Use this property to override the computed natural sprite size. Without this component, the natural size is `Image2D.imagePixelSize * Image2D.pixelsToWorldUnits * Sprite2D.imageRegion.size`.|
|drawMode|See [DrawMode](#drawmode).|

### Sprite2DSequence

Describes an animated sprite sequence.

|Property|Description|
|--------|-----------|
|sprites|Sprite entity references, required to have [Sprite2D](#sprite2d) component.|
|frameRate|Base frame rate of the sequence, in frames per second.|

### Sprite2DSequencePlayer

This component requires the [Sprite2DRenderer](#sprite2drenderer) component.

|Property|Description|
|--------|-----------|
|sequence|Sequence entity reference, required to have the [Sprite2DSequence](#sprite2dsequence) component|
|paused|Set to true to pause the animation sequence. Set to false, the default value, to play the animation sequence.|
|loop|Sets the looping behavior of the animation sequence. Defaults to Loop. See [LoopMode](#loopmode).|
|speed|Speed multiplier for playback. Defaults to 1.0.|
|time|Current time for playback. Defaults to 0.0.|

### SpriteAtlas

* Unity Reference: [Sprite Atlas](https://docs.unity3d.com/Manual/SpriteAtlas.html)

This component contains a list of sprites.

> When a Tiny project is exported, each Sprite Atlas is exported as a Tiny entity containing this component. Please refer to [Using Sprite Atlas](#using-sprite-atlas).

|Property|Description|
|--------|-----------|
|sprites|An array of Tiny Sprite entities.|

### SortingGroup

* Requires: [Transform](#transform)
* Unity Reference: [UnityEngine.SortingGroup](https://docs.unity3d.com/ScriptReference/Rendering.SortingGroup.html)

Flags this [Transform](#transform) and its children as part of a sorting group. All members of a sorting group sort together, excluding members of other groups.

### Transform

* Unity Reference: [UnityEngine.Transform](https://docs.unity3d.com/ScriptReference/Transform.html)

Entities can use the [Transform](#transform) component to be included in the spatial representation of the World. Transforms have a single parent (or None), and may have zero or more children. The transformation information they hold (position, rotation, and scale) affects themselves and their children.

This component is optional in Tiny. Entities without a [Transform](#transform) cannot be included in the visual hierarchy (cannot act as a parent or child of other Entities with a [Transform](#transform) component).

|Property|Description|
|--------|-----------|
|localPosition|Position in local space.|
|localRotation|Rotation in local space.|
|localScale|Scale in local space.|

## Systems

### DisplayList

* Updates After: Shared.RenderingFence
* Updates Before: Shared.PlatformRenderingFence

Prepares a [Sprite2DRenderer](#sprite2drenderer) display list for the platform renderer system to consume. This system also creates an entity with a [Camera2D](#camera2d) component if none exists.

### SequencePlayerSystem

* Updates Before: Shared.InputFence

A system used to drive [Sprite2DSequencePlayer](#sprite2dsequenceplayer) components.

## Enums

### BlendOp

See [ShaderLab: Blending](https://docs.unity3d.com/Manual/SL-Blend.html) for a good reference on blend modes.

|Value|Description|
|-----|-----------|
|Alpha|Traditional transparency. Translates to `output = (source alpha * source fragment) + ((1 – source alpha) * destination fragment)`.|
|Add|Additive blending. Translates to `output = source fragment + destination fragment`.|
|Multiply|Multiplicative blending. Translates to `output = source fragment * destination fragment`.|

### CameraClearFlags

|Value|Description|
|-----|-----------|
|Nothing|Don't clear anything.|
|SolidColor|Clear with a background color.|

### CameraCullingMode

|Value|Description|
|-----|-----------|
|None|Disable culling and render every renderable entity.|
|All|Render only entities that have **all** of their components in cullingMask.|
|Any|Render only entities that have **at least one** of their components in cullingMask. This is similar to Unity's cullingMask.|

### DrawMode

|Value|Description|
|-----|-----------|
|ContinuousTiling|Tiles the sprite continuously if the area is larger than the source sprite, or cuts it off if it is smaller.|
|AdaptiveTiling|Adaptively tiles the sprite. When the target area is smaller, the sprite is scaled down, like in Stretch mode. If the area is larger a combination of scaling and tiling is used, that minimizes scaling but always renders complete tiles.|
|Stretch|Scales the sprite to fill the target area.|

### ImageStatus

|Value|Description|
|-----|-----------|
|Invalid||
|Loaded||
|Loading||
|LoadError||

### LoopMode

|Value|Description|
|-----|-----------|
|Loop|Play the sequence in a continuous loop.|
|Once|Play the sequence once, then pause and set time to 0.0.|
|PingPong|Play the sequence in a ping-pong continuous loop.|
|PingPongOnce|Play the sequence once forward then back to the start, then pause and set time to 0.0.|
|ClampForever|Plays back the animation once, then play the last frame continuously.|

## Structs

### Color

|Property|Description|
|--------|-----------|
|r|Red channel.|
|g|Green channel.|
|b|Blue channel.|
|a|Alpha channel.|
