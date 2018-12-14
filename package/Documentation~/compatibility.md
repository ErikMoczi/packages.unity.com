# Compatibility Cheat Sheet

Tiny supports a subset of the workflows available in Unity.

> This table can be used by Unity developers to associate familiar Unity concepts with Tiny. Empty field means that this feature is not currently supported. Some features are specific to Tiny.

| Unity Features | Tiny Features |
| ------------- |------------|
|  | [Modules](../tiny-editor.md#modules-tab) |
|  | [Systems](../tiny-editor.md#systems-tab) |
| [Animation](https://docs.unity3d.com/Manual/AnimationSection.html) | <ul><li>ut.Animation (WIP)</li><li>[ut.Core2D.Sprite2DSequence](./manual-module-core2d.md#sprite2dsequence)</li><li>[ut.Core2D.Sprite2DSequencePlayer](./manual-module-core2d.md#sprite2dsequenceplayer)</li><li>ut.Tween (WIP)</li></ul> |
| [AssetBundles](https://docs.unity3d.com/Manual/AssetBundlesIntro.html) |  |
| [Assets](https://docs.unity3d.com/Manual/AssetTypes.html)  <ul><li>Image</li><li>FBX and Model</li><li>Meshes and animations</li><li>Audio</li><li>Standard Assets</li></ul> | Assets  <ul><li>Image</li><li>Audio</li><li>Unity Standard Assets (e.g. ParticleSystems)</li><li>Tiny Assets (e.g. EntityGroup)</li></ul> |
| [Audio](https://docs.unity3d.com/Manual/Audio.html) | ut.Audio |
| [Cameras](https://docs.unity3d.com/Manual/CamerasOverview.html) | [ut.Core2D.Camera2D](./manual-module-core2d.md#camera2d) |
| [Collider 2D](https://docs.unity3d.com/Manual/Collider2D.html) | [ut.Physics2D.RectCollider](./manual-module-physics2d.md#rectcollider) |
| [Component](https://docs.unity3d.com/Manual/UsingComponents.html) | [Component](../tiny-editor.md#components-tab) |
| [Constant Force 2D](https://docs.unity3d.com/Manual/class-ConstantForce2D.html) | [ut.Physics2D.Impulse2D](./manual-module-physics2d.md#impulse2d) |
| [Constraints](https://docs.unity3d.com/Manual/Constraints.html) |  |
| [Customize Workspace](https://docs.unity3d.com/Manual/CustomizingYourWorkspace.html) |  |
| [C# Job System](https://docs.unity3d.com/Manual/JobSystem.html) |  |
| [Editor Analytics](https://docs.unity3d.com/Manual/EditorAnalytics.html) |  |
| [Effectors 2D](https://docs.unity3d.com/Manual/Effectors2D.html) |  |
| [Event System](https://docs.unity3d.com/Manual/EventSystem.html) | <ul><li>ut.Runtime.Input</li> <li>ut.Core2D.Input</li><li>ut.Core2D.Touch</li><li>[ut.HitBox2D.HitBoxOverlapResults](./manual-module-hitbox2d.md#hitboxoverlapresults)</li><li>[ut.HitBox2D Structs](./manual-module-hitbox2d.md#structs)</li></ul> |
| [Game Input](https://docs.unity3d.com/Manual/ConventionalGameInput.html) | ut.Runtime.Input |
| [Game View](https://docs.unity3d.com/Manual/GameView.html) | Unity Game View |
| [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html) | Entity |
| [Hierarchy Window](https://docs.unity3d.com/Manual/Hierarchy.html) | [Tiny Hierarchy Window](../tiny-editor.md#tiny-hierarchy-window) |
| [Hotkeys](https://docs.unity3d.com/Manual/UnityHotkeys.html) |  |
| [Importing](https://docs.unity3d.com/Manual/ImportingAssets.html) | Unity Importing |
| [Inspector Window](https://docs.unity3d.com/Manual/UsingTheInspector.html) | [Tiny Inspector Window](../tiny-editor.md#tiny-inspector-window) |
| [Lightning](https://docs.unity3d.com/Manual/LightingOverview.html) |  |
| [Materials, Shaders & Textures](https://docs.unity3d.com/Manual/Shaders.html) | [ut.Core2D.Image2D](./manual-module-core2d.md#image2d) |
| [Mesh Components](https://docs.unity3d.com/Manual/comp-MeshGroup.html) |  |
| [Mobile Input](https://docs.unity3d.com/Manual/ConventionalGameInput.html) | <ul><li>ut.Core2D.Input</li><li>ut.Core2D.Touch</li></ul> |
| [Multiplayer and Networking](https://docs.unity3d.com/Manual/UNet.html) |  |
| [Navigation and Pathfinding](https://docs.unity3d.com/Manual/Navigation.html) |  |
| [Other Windows](https://docs.unity3d.com/Manual/OtherViews.html)  <ul><li>Console</li><li>Animation</li><li>Profiler</li><li>Lightning</li><li>Occlusion Culling</li></ul> | Other Windows <ul><li>Console</li><li>Animation (supports only linear animation curve)</li><li>Profiler (supports only CPU Usage)</li></ul> |
| [Particle Systems](https://docs.unity3d.com/Manual/ParticleSystems.html) | [ut.Particles](./manual-module-particles.md) |
| [Physics Material 2D](https://docs.unity3d.com/Manual/class-PhysicsMaterial2D.html) | [ut.Physics2D.RectCollider](./manual-module-physics2d.md#rectcollider) |
| [Platform-specific](https://docs.unity3d.com/Manual/PlatformSpecific.html) |  |
| [Plugins](https://docs.unity3d.com/Manual/Plugins.html) |  |
| [Post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html) |  |
| [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html) | [EntityGroup](../tiny-editor.md#entities-tab) |
| [Preferences](https://docs.unity3d.com/Manual/Preferences.html) | Unity Preferences (Tiny Unity tab) |
| [Presets](https://docs.unity3d.com/Manual/Presets.html) |  |
| [Profiler Window](https://docs.unity3d.com/Manual/ProfilerWindow.html) | Unity Profiler Window (supports only CPU Usage track) |
| [Project Settings](https://docs.unity3d.com/Manual/comp-ManagerGroup.html) | [Tiny Project Settings](../tiny-editor.md#settings-tab) |
| [Project Templates](https://docs.unity3d.com/Manual/ProjectTemplates.html) |  |
| [Project Window](https://docs.unity3d.com/Manual/ProjectView.html) | Unity Project Window, [Tiny Editor Window](../tiny-editor.md#tiny-editor-window) |
| [Publishing Builds](https://docs.unity3d.com/Manual/PublishingBuilds.html) | <ul><li>[Tiny Settings](../tiny-editor.md#settings-tab)</li><li>[Build Reports](../tiny-editor.md#build-report-tab)</li></ul> |
| [Random Numbers](https://docs.unity3d.com/Manual/RandomNumbers.html) |  |
| [RenderDoc Integration](https://docs.unity3d.com/Manual/RenderDocIntegration.html) |  |
| [Rendering Components](https://docs.unity3d.com/Manual/comp-RenderingGroup.html) |  |
| [RigidBody 2D](https://docs.unity3d.com/Manual/class-Rigidbody2D.html) | <ul><li>[ut.Physics2D.RectCollider](./manual-module-physics2d.md#rectcollider)</li><li>[RectCollider.BodyType](./manual-module-physics2d.md#rectcollider-bodytype)</li></ul> |
| [Scene View](https://docs.unity3d.com/Manual/UsingTheSceneView.html) | Unity Scene View |
| [SceneManagement](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html) | ut.Flow |
| [Scenes](https://docs.unity3d.com/Manual/CreatingScenes.html) | [EntityGroup](../tiny-editor.md#entities-tab) |
| [Scripting](https://docs.unity3d.com/Manual/CreatingComponents.html) |  |
| [Searching](https://docs.unity3d.com/Manual/Searching.html) | Unity Searching |
| [Social API](https://docs.unity3d.com/Manual/net-SocialAPI.html) |  |
| [Sprites](https://docs.unity3d.com/Manual/Sprites.html) <ul><li>Sprite Creator</li><li>Sprite Editor</li><li>Sprite Packer</li><li>Sorting Group</li><li>9-slicing Sprites</li><li>Sprite Masks</li><li>Sprite Atlas</li></ul> | Sprites <ul><li>Sprite Creator</li><li>Sprite Editor</li><li>Sprite Packer</li><li>[Sorting Sprites](./manual-module-core2d.md#sorting-sprites)</li><li>[9-slicing Sprites](./manual-module-core2d.md#9-slicing-sprites)</li><li>[ut.Core2D.SpriteAtlas](./manual-module-core2d.md#spriteatlas)</li><li>[ut.Core2D.Sprite2D](./manual-module-core2d.md#sprite2d)</li><li>[ut.Core2D.Sprite2DBorder](./manual-module-core2d.md#sprite2dborder)</li><li>[ut.Core2D.Shape2D](./manual-module-core2d.md#shape2d)</li></ul> |
| [Static GameObject](https://docs.unity3d.com/Manual/StaticObjects.html) | Static Entity |
| [Tag](https://docs.unity3d.com/Manual/Tags.html) |  |
| [Terrain Engine](https://docs.unity3d.com/Manual/script-Terrain.html) |  |
| [Tilemap](https://docs.unity3d.com/Manual/Tilemap.html) | [Tilemap2D](./manual-module-tilemap2d.md) <ul><li>ut.Tilemap2D.Grid</li><li>ut.Tilemap2D.Tile</li><li>ut.Tilemap2D.Tilemap</li><li>ut.Tilemap2D.TilemapRenderer</li><li>ut.Tilemap2D.TileData</li></ul> |
| [Timeline](https://docs.unity3d.com/Manual/TimelineSection.html) |  |
| [Toolbar](https://docs.unity3d.com/Manual/Toolbar.html) | <ul><li>Unity Toolbar</li><li>[Tiny Toolbar](../tiny-editor.md#toolbar)</li></ul> |
| [Transforms](https://docs.unity3d.com/Manual/Transforms.html) | [ut.Core2D.Transform](./manual-module-core2d.md#transform) |
| [Tree Editor](https://docs.unity3d.com/Manual/class-Tree.html) |  |
| [Tutorials](https://docs.unity3d.com/Manual/learn-tab.html) | <ul><li>Flappy</li><li>Project Samples</li></ul> |
| [UI Canvas Components](https://docs.unity3d.com/Manual/comp-CanvasComponents.html) | ut.UILayout.UICanvas |
| [UI IMGUI](https://docs.unity3d.com/Manual/GUIScriptingGuide.html) |  |
| [UI Interaction Components](https://docs.unity3d.com/Manual/comp-UIInteraction.html) |  |
| [UI Rect Transform](https://docs.unity3d.com/Manual/class-RectTransform.html) | ut.UILayout.RectTransform |
| [UI Visual Components](https://docs.unity3d.com/Manual/comp-UIVisual.html) | <ul><li>ut.Text</li><li>[ut.Core2D.Image2D](./manual-module-core2d.md#image2d)</li></ul> |
| [UIElements](https://docs.unity3d.com/Manual/UIElements.html) |  |
| [Unity Ads](https://docs.unity3d.com/Manual/UnityAds.html) | ut.AdSupport |
| [Unity Analytics](https://docs.unity3d.com/Manual/UnityAnalytics.html) |  |
| [Unity Cloud Build](https://docs.unity3d.com/Manual/UnityCloudBuild.html) |  |
| [Unity IAP](https://docs.unity3d.com/Manual/UnityIAP.html) |  |
| [Unity Collaborate](https://docs.unity3d.com/Manual/UnityCollaborate.html) |  |
| [Unity Performance Reporting](https://docs.unity3d.com/Manual/UnityPerformanceReporting.html) |  |
| [Unity Integrations](https://docs.unity3d.com/Manual/UnityIntegrations.html) |  |
| [Version Check](https://docs.unity3d.com/Manual/VersionCheck.html) |  |
| [Version Control](https://docs.unity3d.com/Manual/VersionControl.html) |  |
| [Video](https://docs.unity3d.com/Manual/VideoPlayer.html) |  |
| [Visual Effects](https://docs.unity3d.com/Manual/comp-Effects.html) |  |
| [Visual Studio Integration](https://docs.unity3d.com/Manual/VisualStudioIntegration.html) |  |
| [XR](https://docs.unity3d.com/Manual/XR.html) |  |
| [2D Joints](https://docs.unity3d.com/Manual/Joints2D.html) |  |
| [3D Physics](https://docs.unity3d.com/Manual/Physics3DReference.html) |  |
