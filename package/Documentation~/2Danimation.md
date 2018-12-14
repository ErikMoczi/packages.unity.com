# Introduction

The aim of this manual is to guide you to quickly rig and animate a 2D character in Unity with the 2D Animation V2 package and tools. This package is only supported for Unity 2018.3.

## Adobe Photoshop PSB format

For character animation with the __2D Animation V2 package__, the __PSD Importer__ package is required. Currently the PSD Importer package only supports Adobe Photoshop PSB format, and does not support the Adobe PSD format. The PSB format has identical functions as PSD, with the additional support for much larger image sizes.

## Preparing and Importing artwork

When the art Asset is imported with the PSD Importer, Unity automatically arranges the individual characters parts (prepared as Layers) into a Sprite Sheet layout. The __PSD Importer__ imports the graphic data from each Photoshop layer and does the following:

1. Arrange/Mosiac the layers into a Sprite Sheet layout.
2. Generate a __Sprite__ from each layer’s graphic data.

When an artist designs a character for animation (see Example 1), they would usually manually separate and arrange the different parts of the character (see Example 2). The PSD Importer can generate a Prefab that reassembles the Sprites in their original positions as arranged in the PSB source file automatically (see Example 3), making it more convenient for the artist to begin animating the character.

![Example 1: Layered character artwork in Adobe Photoshop](images/2DAnimationV2_PSDLayers.png)

Example 1: Layered character artwork in Adobe Photoshop.

![Example 2: Manually created Sprite Sheet with different layer parts.](images/2DAnimationV2_split.png)

Example 2: Manually created Sprite Sheet with different layer parts.

![Example 3: The **Mosaic** layers and the generated Prefab of the character.](images/2DAnimationV2_Mosaic_Prefab.png)

Example 3: The layers mosaic into a Sprite Sheet, with a generated Prefab of the character in source pose.

## Import settings

Prepare your character by separating the character's limbs and parts into separate layers, and arrange them in a default pose. 

1. Save your artwork as a PSB file in Adobe Photoshop by selecting the **Large Document Format** under the **Save As** menu. You can convert an existing PSD file to PSB in the same way.
2. Import the PSB file into Unity as an Asset.
3. Select the Asset to bring up the __PSD Importer__ Inspector window.
4. In the Inspector window, ensure these settings are set (see the example below):
   - Set __Texture Type__ to 'Sprite(2D and UI)'.
   - Set **Sprite Mode** to 'Multiple'.
   - Check __Mosaic Layer__.
   - Check __Character Rig__.
   - Check __Use Folder Grouping__.

![Importer Window settings](images/ImporterWindow.png)

Click __Apply__ to apply the settings when ready. Refer to the __PSD Importer__ package documentation for more information about these settings.

# Skinning Editor module

1. Select the imported __Asset__ and select the __Sprite Editor__.
2. Select the **Skinning Editor** module from the dropdown menu in the **Sprite Editor** window. The following options and tools then appear.

![The Skinning Editor interface](images/2DAnimationV2_SkinnerUI_expanded.png)



| Tool                                                         | Default Shortcut | Function                                                     |
| :----------------------------------------------------------- | :--------------: | :----------------------------------------------------------- |
| ![icon_PreviewPose](images/icon_PreviewPose.png)Preview Pose |    Shift + Q     | Preview character poses after rigging.                       |
| ![icon_EditJoints](images/icon_EditJoints.png)Edit Joints    |    Shift + W     | Reposition the bones into a new positions. These changes are automatically saved as the default bind pose for the Restore Bind Pose tool.<br/>Sprite geometry does not deform with the bones in this mode, even if the bones are attached as influencers. |
| ![icon_CreateBone](images/icon_CreateBone.png)Create Bone    |    Shift + E     | Click and drag to create bones.                              |
| ![icon_SplitBone](images/icon_SplitBone.png)Split Bone       |    Shift + R     | Splits the selected bone.                                    |
| ![icon_ReparentBone](images/icon_ReparentBone.png)Reparent Bone |    Shift + T     | Reparents a child bone to a different parent bone, changing the bone hierarchy. |
| ![icon_GenGeo](images/icon_GenGeo.png)Auto Geometry      |    Shift + A     | Autogenerate meshes for Sprites.                             |
| ![icon_EditGeo](images/icon_EditGeo.png)Edit Geometry        |    Shift + S     | Edit generated meshes by repositioning vertices.             |
| ![icon_CreateVertex](images/icon_CreateVertex.png)Create Vertex |    Shift + D     | Create new vertices to create geometry.                      |
| ![icon_CreateEdge](images/icon_CreateEdge.png)Create Edge    |    Shift + G     | Create new edges to create geometry.                         |
| ![icon_SplitEdge](images/icon_SplitEdge.png)Split Edge       |    Shift + H     | Split an existing edge into two.                             |
| ![icon_GenWeights](images/icon_GenWeights.png)Auto Weights   |    Shift + Z     | Autogenerate weights for geometry.                           |
| ![icon_WeightSlider](images/icon_WeightSlider.png)Weight Slider |    Shift + X     | Adjust weights via slider control.                           |
| ![icon_WeightPaint](images/icon_WeightPaint.png)Weight Brush |    Shift + C     | Adjust weights by painting with a brush.                     |
| ![icon_BoneInfluence](images/icon_BoneInfluence.png)Bone Influence |    Shift + V     | Select which bones influence a Sprite.                       |
| ![icon_RestoreBind](images/icon_RestoreBind.png)Reset Pose |    Shift + 1     | Restore a character’s bones and joints to their original positions. |
| ![icon_ToggleView](images/icon_ToggleView.png)Toggle View Mode |    Shift + 2     | Switch between the Character and Sprite Sheet view           |
| ![icon_Copy](images/icon_Copy.png)Copy                       |     Ctrl + C     | Copy the data from the current selection.                    |
| ![icon_Paste](images/icon_Paste.png)Paste                    |     Ctrl + V     | Pastes the copied data.                                      |
| ![icon_Paste](images/icon_Paste.png)Paste                    |    Shift + B     | Show additional pasting options.                             |
| ![icon_Visibility](images/icon_Visibility.png)Visibility     |    Shift + P     | Toggle visibility of selected Sprites or bones.              |
| Toggle Tool Text     |    Shift + `     | Show or hide text on tool buttons              |

## Sprite selection

1. Double-click a Sprite to select it.
2. If there are multiple Sprites that overlay each other, double-click to cycle through all Sprites at the cursor location.
3. Double-click on a blank area to deselect all Sprites.

## Bone and mesh vertex selection

1. Click a bone or mesh vertex to select it.
2. Click and drag a selection box over multiple bones or vertices to select them at once.
3. Right click to deselect any selected bone or mesh vertices.


# Tool Preferences

The Unity 2D Animation preferences menu is found in the Preferences menu by going to  **Edit** > **Preferences** > **Unity 2D Animation**. It provides several options to customize aspects of the animation tool.

![Tool Preferences](images/2DAnimationV2_ToolPreferences.png)

| Setting                    | Function                                                     |
| -------------------------- | ------------------------------------------------------------ |
| **Hide Tool Text**        | Enable this option to hide tool text to have a compact view (see example below). |
| **Selected Outline Color** | Customize the outline color of selected Sprite and bone.            |
| **Sprite Outline Size**    | Use the slider to adjust the outline thickness of a selected Sprite. |
| **Bone Outline Size**      | Use the slider to adjust the outline thickness of a selected  bone. |

![Compact windows](images/2DAnimationV2_compacted.png)

# Character rigging

1. Select the![icon_small_CreateBone](images/icon_small_CreateBone.png)__Create Bone__ tool to begin creating the bones of the character skeleton.

2. With the tool selected, click in the **Sprite Editor **window to define the start-point of the bone. Move the cursor to where the bone should end, and click again to set the bone’s end-point.

3. To create a continuous chain of bones, select the ![icon_small_CreateBone](images/icon_small_CreateBone.png)__Create Bone__ tool and click the end-point of an existing bone. The new bone is started from the end-point, creating a chain.

4. Once all bones are created, generate the mesh geometry for the Sprites. It is recommended to use the ![icon_small_GenGeo](images/icon_small_GenGeo.png)__Auto Geometry__ tool to autogenerate the Sprites’s geometry mesh. Then refine the geometry with the ![icon_small_CreateVertex](images/icon_small_CreateVertex.png)__Create Vertex__ and ![icon_small_CreateEdge](images/icon_small_CreateEdge.png)__Create Edge__ tools.

5. To edit the bones that influence a Sprite, select the ![icon_small_BoneInfluence](images/icon_small_BoneInfluence.png)__Bone Influence__ tool and double-click a Sprite to select it. A list of bones currently influencing the Sprite appears. 

    * To remove any of the listed bones, select it in the list and click '-' to remove them.

    - To add a bone, select it in the **Sprite Window** and click + to add it to the list.

6. The weight attached to vertices affects the influence between bones and the Sprites' geometry. Select the  ![icon_small_GenWeight](images/icon_small_GenWeight.png)__Auto Weights__ tool to autogenerate the weight of a selected Sprites. To generate weights for all Sprites at once, deselect all Sprites before selecting the tool. Refine the weights of the vertices with the ![icon_small_WeightPainter](images/icon_small_WeightPainter.png)__Weight Brush__ and ![icon_small_WeightSlider](images/icon_small_WeightSlider.png)__Weight Slider__ tools.

7. Test the skeleton rig previewing poses with the ![icon_small_PreviewPose](images/icon_small_PreviewPose.png)__Preview Pose__ tool. Move and rotate the different bones to check their influence on the geometry mesh. Previewing poses can also be done while using the following tools: the ![icon_small_ReparentBone](images/icon_small_ReparentBone.png)__Reparent Bone__, ![icon_small_WeightPainter](images/icon_small_WeightPainter.png)__Weight Brush__, ![icon_small_WeightSlider](images/icon_small_WeightSlider.png)__Weight Slider__, ![icon_small_BoneInfluence](images/icon_small_BoneInfluence.png)__Bone Influence__, ![icon_small_GenWeight](images/icon_small_GenWeight-1543300126435.png)__Auto Weights__, and ![icon_small_Visibility](images/icon_small_Visibility.png)__Visibility__ tools.

    * To restore a skeleton rig to its original pose, select ![icon_small_RestoreBind](images/icon_small_RestoreBind.png)__Reset Pose__. The default pose can be edited with the ![icon_small_EditJoints](images/icon_small_EditJoints.png)__Edit Joints__ tool.

# Animating

To begin animating, drag the Asset into the Scene. This creates a GameObject that reassembles the imported character as it originally appeared in the PSB file. The GameObject contains multiple child GameObjects in the order that represents the bone hierarchy.

With the new GameObject in the scene, begin animating it with the usual [Unity animation workflow and tools](https://docs.unity3d.com/Manual/AnimationSection.html).
