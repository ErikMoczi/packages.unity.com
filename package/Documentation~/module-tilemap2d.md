# Tilemap2D Module

Tiny supports a subset of [Unity Tilemap](https://docs.unity3d.com/Manual/Tilemap.html) referred to as the Tilemap2D module. This document provides common use cases, an example, and a reference of Tilemap2D components.

## Use Cases and Examples

This section demonstrates how to use the Tilemap2D module through common use cases and usage examples. If a use case or an example is similar to a topic in the Unity User Manual, a link is provided to the appropriate topic.

### Creating a 2D Tilemap

1. Add a camera to your scene (an entity with a TransformNode and a [Camera2D](./manual-module-core2d.md#camera2d)).
2. Create Tiles using the [same Unity workflow](https://docs.unity3d.com/Manual/Tilemap-CreatingTiles.html).
3. Create an entity.
4. Add a TransformNode component.
5. Add a [Tilemap](#tilemap) component.
6. Add a [TilemapRenderer](#tilemaprenderer) component.
7. Edit the [Tilemap](#tilemap) directly on the Unity Scene view with the Tilemap Palette using the [same Unity workflow](https://docs.unity3d.com/Manual/Tilemap-Palette.html).

Please refer to the `DungeonZ` entity in the [`DungeonZ`](#example-dungeonz) project for an example.

> If you change the TilemapRenderer `color` property or a Tile `color` property, you must set the rendering mode to `WebGL`.

### Example: DungeonZ

The `DungeonZ` project found in `Assets/UTinySamples` covers the use of the Tilemap2D Module components. In the project, you will find examples about how to use Tiles and how to create a Tilemap using the Tile Palette.

> This table can be used to associate a Tilemap2D Component to an entity or an asset.

| Tilemap2D Components | Entity Name | Asset Name |
| ------------- | ------------- | ------------- |
| [Tile](#tile) |  | CB-Temple-A_0 to CB-Temple-A_455 |
| [Tilemap](#tilemap) | DungeonZ |  |
| [TilemapRenderer](#tilemaprenderer) | DungeonZ |  |

## Components

Tiny is built around small, lightweight components. This way you only pay for what you need. This design usually creates more component types to represent data held in a single Unity component, which can be confusing at first for some developers.

> This table can be used by Unity developers to associate familiar Unity concepts with Tiny.

| Unity Tilemap Component | Tilemap2D Components |
| ------------- | ------------- |
| Tile Assets | [Tile](#tile) |
| Tilemap, Grid | [Tilemap](#tilemap) |
| TilemapRenderer | [TilemapRenderer](#tilemaprenderer) |

### Tile

* Unity Reference: [Tile Assets](https://docs.unity3d.com/Manual/Tilemap-TileAsset.html)

Describes properties of a tile used by tilemaps. This component maps to a Unity Tile asset. It references a sprite and it can be arranged on a [Tilemap](#tilemap).

> A Tilemap will contain an array of Tiles. Note that the [Physics2D module](./manual-module-physics2d.md) does not interact with the Tile collider yet.

|Property|Description|
|--------|-----------|
|color|Used to tint the material. Requires: Rendering mode to `WebGL`.|
|sprite|The sprite that is used by this tile asset to be rendered.|
|[colliderType](#tilecollidertype)|Tile's collider shape. Defaults to None. Options are: **None**, **Sprite**, **Grid**.|

### Tilemap

* Requires: [TilemapRenderer](#tilemaprenderer)
* Unity Reference: [Grid](https://docs.unity3d.com/ScriptReference/Grid.html), [Tilemap](https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.html)

The Tilemap allows you to create 2D levels using tiles and a grid overlay. It also stores the [Tiles](#tile) as a list of [TileData](#tiledata). This component determines the tilemap cells layout (e.g. properties of the grid to be draw onto).

> To paint on the Tilemap, you can follow the same Unity workflow with the [Tile Palette](https://docs.unity3d.com/Manual/Tilemap-Palette.html). Note that the Tilemap references tiles as opposed to sprites directly. **Cell swizzling is not supported yet**.

|Property|Description|
|--------|-----------|
|anchor|Anchor point of each tile within its grid cell. The normalized position (`X` and `Y` coordinates) 0,0 corresponds to anchoring to the lower left of the cell, while 1,1 corresponds to anchoring in the upper right corner of the cell. Defaults to 0.5 for the x and y axis.|
|position|Position of each tile within its grid cell relative to anchor.|
|rotation|Rotation of each tile within its grid cell relative to anchor.|
|scale|Scale of each tile within its grid cell relative to anchor.|
|cellSize|Size in pixels of each cell in the Tilemap grid.|
|cellGap|Size in pixels of the gap between each cell in the Tilemap grid.|

### TilemapRenderer

* Requires: [Tilemap](#tilemap)
* Unity Reference: [TilemapRenderer](https://docs.unity3d.com/ScriptReference/Tilemaps.TilemapRenderer.html)

Defines how a [Tilemap](#tilemap) is rendered.

> In order for the `color` property to work, the rendering mode must be set to `WebGL`.

|Property|Description|
|--------|-----------|
|color|Color of the [Tilemap](#tilemap) layer. Requires: Rendering mode to `WebGL`.|

## Systems

### TilemapChunkingSystem

* Updates Before: Shared.RenderingFence

Verifies if [Tilemap](#tilemap) is valid to render (valid sprites and [tiles](#tile)). Creates chunks of tiles so that sprites are grouped and rendered as a whole.

## Enums

### TileColliderType

Describes the different collider type for [tiles](#tile). See [Tilemaps and Physics2D](https://docs.unity3d.com/Manual/Tilemap-Physics2D.html) for a good reference on the interaction between tile's collider and Physics2D.

> This feature is not supported yet.

|Value|Description|
|-----|-----------|
|None|No collider shape is generated for the Tile.|
|Sprite|The Sprite outline is used as the collider shape for the Tile.|
|Grid|The grid layout boundary outline is used as the collider shape for the Tile.|

## Structs

### TileData

Contains information about a [Tile](#tile) instance and its position in a [Tilemap](#tilemap). Data required for rendering a Tile.

|Property|Description|
|--------|-----------|
|position|Position of the tile in the Tilemap.|
|tile|Reference to an entity with a Tile component. Tile to display.|
