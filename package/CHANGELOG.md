# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.13.4] - 2018-12-13
* Bug fixes and performance improvements 
* Enhancements to the bitmap fonts rendering on canvas
* Fix typescript Vector2/3/etc. aliases and declarations 
* Improve the WebGL rendering initializations and deinitialization 
* Fix Physics Velocity components to allow setting velocity properly

## [0.13.2] - 2018-12-04
* Tiny Mode is now in open beta!
* Must use Unity 2018.3.0b12 and above

### [Samples]
* Removed and renamed some samples
  
### [Hierarchy]
* Added search by component functionality

### [Editor]
* New LiveLink feature that allows for debugging a running exported project
* Tweaked some icons
* General bug fixes

### [Runtime]
* More Font fixes
* General bug fixes


## [0.12.0] - 2018-11-20
* Please use the latest 2018.3.0b10 and above

### [Prefabs]
* Added prefabs

### [Samples]
* Added a new match3 sample game called TinyArms

### [Tilemaps]
* Greatly improved performance while painting

### [Editor]
* Node tools are now moved to native executables for increased performance
* Fixed an issue with spaces being in the Unity installation mpath

### [Runtime]
* Added CircleCollider2D
* RectHitBox2D now works without a renderer
* Fixed tile maps drawn with a second palette
* Fixed sprite tinting bug
* Fixed a bug with Firefox causing the runtime to crash
* Fixed text texture issue while switching renderer
* Fixed a native font issue in Firefox


## [0.11.3-preview] - 2018-10-31
### Editor
* Added new icons for Tiny assets
* Fixed multiple rename component field bug
* Fixed migration bug for “blending” member
* Added an error when there is a space in the Mono Path

## [0.11.2-preview] - 2018-10-29
* Please use the latest Unity 2018.3.0b7 and above
  
### Samples
* Fixed Galaxy Raiders startup bug
* Removed fireworks sample

### Editor
* Changed colours of components in Component Families
* Selecting an entity now pings the entity in the Hierarchy window
* Updated icons in component inspector for clarity
* LifeTimeVelocity now uses three curves
* Component fields now auto-add required modules
* Fixed domain reload bug

## [0.11.1-preview] - 2018-10-26
* Fixed critical bugs causing errors on install

## [0.11.0-preview] - 2018-10-25
* Unity 2018.2.0b7 and above must be used
* Data Access Layer was removed. 
* Behaviours were slightly changed. This may break functionality. See below.

### Editor
* A components folder is now created when starting a new project
* New fields are now in a rename state on creation
* Removed and cleaned up public APIs to improve quality of life
* UTiny references are now renamed to Tiny
* Added support for angle fields in EmitterInitialVelocity component
* Added dialog to warn about Tiny package needing non-legacy sprite packing mode
* Added camera component family
* You can now drag and drop component assets into an Entity via the inspector window
* Made web socket server more stable on domain reload
* Errors are now shown during export and on creation of data types that are reserved words
* Cyclic type references are now not allowed
* An exception is now shown when a sprite is found in more than one SpriteAtlas
* Tiny assets can now be created in the respective folder.
* Fixed inconsistent Z-axis layer sorting between Game and Scene view
* Fixed component value reset bug
* Fixed quaternion bug

### Scripting
* Cleaned up BehaviourFilter and reworked template
* Renamed OnEnable to OnEntityEnable (applies to related methods)
  
### Samples
* Compressed files in Audio sample
* Fixed flappy sound bug

### Particles
* Fixed particle preview in Game and Scene windows

### Runtime
* Reduced runtime size by 17%
* Removed Data Access Layer but preserved the ease of data manipulation in certain scenarios
* Added AddTween function
* Added click, keyup, and keydown events to unlock audio
* Improved HitBox2D
* Improved stability and performance of Particles module
* Fixed memory leaks
* Fixed autoplay audio bug
* Fixed various WebGL bugs
* Fixed missing hasComponent check on 2DSequencePlayer component


## [0.10.2-preview] - 2018-10-10
* Touch input bug fix

## [0.10.1-preview] - 2018-10-05
* Bug fixes and minor UX improvements

## [0.10.0-preview] - 2018-09-27
* Due to the runtime changes, scripting has changed. Previous scripts may fail to compile. Check the scripting documents for more details.

### Known Issues
* On the Personal Editor skin the Component field bullet is misplaced
* Reseting to initial values is not available when showing Component Families
* Scripting reference link has been removed
* Some known TileMap bugs when undoing placed tiles

### UX
* Modules and configurations are now organized in inspected Project asset
* QR code and IP address have returned in the Project asset inspector under “Build Configuration” when a project is running
* Refactored component fields to simplify usage and remove information overload
* Documentation field has been moved into the component field menu
* Components in a component family are now easily distinguishable
* Added “Reset Initial Values" menu to component families

### Scripting
* Added DAL(Data Access Layer) to modify data
* Updated scripting documents
* World is now immutable (read-only) during iteration
* Audio API now uses AudioSourceStart and AudioSourceStop components to start/stop sounds
  
### Samples
* Flappy sample now uses its own gravity system and has configuration files
* All samples updated with DAL usage

### Runtime
* Completely refactored runtime to include archetype ECS changes
* Increased performance and quality of life Added Entity Command Buffer 


## [0.9.0-preview] - 2018-09-14

### Known Issues
* Known issue hard crash bug with Sprite Atlases (Do not have a sprite being referenced by multiple sprite atlases)
* Package now needs Unity 2018.3
* In the Project Editor Settings make sure the Sprite Packer is "Always Enabled"

### UX
* New Tiny Context window shows all assets, data types, and modules that are related to the project
* Project settings have been relocated to the Settings (Edit > Settings) window
* Asset settings have been relocated to the Settings window under “Included in build”
* Build Report has been relocated to the Settings window
* Project can now be built from the inspected project asset file
* Loaded project name is now displayed in the Hierarchy window
* Added Component groups to bundle similar components in the inspector 
* Selecting certain objects will ping and locate the corresponding asset in the Project window
* Removed the Tiny Editor window
* Removed Tiny Hierarchy and Inspector windows, you may now use normal Hierarchy and Inspector windows
* Removed layouts
* Changed documentation about Tiny Hierarchy/inspector windows

### Scripting
* Systems must now be defined through scripts
* Added ComponentBehaviours and EntityFilters
* Deprecation of Javascript support
* Removed the support for .utsystem files
* TSconfig overrides can now be defined
* Added scripting manual

### Data Types
* A project can be loaded by double clicking on a project asset
* Startup scene can now be defined through the EntityGroup asset
* Extra controls have been added to components
* Components can now by unlisted and not discoverable in the editor
* Component fields can now be hidden in the inspector
* Tilemaps are now supported
* Sprite Atlases are now supported
  
### Samples
* Added a new tile map sample, DungeonZ
* Edited all systems in Flappy to use a user defined system file

### Runtime
* Added runtime capabilities to handle UI controls
* Added runtime capabilities to handle the Animation module
* Added runtime capabilities to handle the Watchers module
* Added runtime capabilities to handle the Video playback module
* Added runtime capabilities to handle the new and improved Text module
* Added runtime capabilities to handle the new and improved Tweens module
* Added camera 2D clipping planes
* A 1x1 white sprite is now rendered when an image or sprite component is missing


### Fixed
* Text is now sorted properly in the Game view
* Fixed camera Z clip bug
* Fixed entity destruction leaking bug
* Fixed profiler
* Fixed multi-editing components bug

## [0.8.0-preview] - 2018-08-09

### Known Issues
> Warning: Transform component has now been split into multiple components. Your scripts will need to be refactored to support these new components.
* Profiler connection with Unity is not working. A fix is due in the next release.
* “Link To Source” option is being refactored for better UX experience. It currently does not work.

### Added
* New transform components TransformNode, TransformLocalPosition, TransformLocalRotation and TransformLocalScale have been added to replace the origin Transform component
* Added Entity Templates such as Sprite, Camera, Audio, and UI related entities
* Audio documentation and Audio sample is now included
* Systems and components are now separated from the project file and are defined as separate assets
* Added features compatibility (Unity vs Tiny Package) documentation
* Added particle component warnings
* Added HitBox2D component warnings

### Changed
* Improved performance on auto-complete
* All samples have been converted to support TypeScript
* Improved project load times
* Updated HitBox2D documentation and added warnings to the component
* deltaTime is now clamped between 0.00001 and 1.0 second
  
### Fixed
* Fixed undo on new project bug
* Fixed auto-complete not referring to the correct method name
* Optimized deltaTime()
* Fixed layer sorting on UI Panels


## [0.7.1-preview] - 2018-08-01

### Fixed
* Fixed space in path problems

## [0.7.0-preview] - 2018-07-26

### Known Issues
> Warning: This release changes how your projects are saved to disk. Make sure to backup your projects before upgrading to this release, and let us know if you experience any issue opening or saving your projects. 

> Attention: You must reimport all assets after this update. Right-click the project window and click "Reimport All"
* Auto-complete incorrectly converts the first character of field names to lowercase. A fix is due in the next release.
* Profiler connection with Unity is not working. A fix is due in the next release.
* TypeScript pipeline is not optimized. This affects project load time and script imports. A fix is due in the next release.

### Added
* **TypeScript is now supported** as the default scripting language. Definition files (`.d.ts`) files are available for the Tiny runtime API, and are maintained semi-automatically for user-created Tiny types (components, structs, enums, and systems).
* Development environments that support TypeScript will give you **auto-complete**. We tested this workflow using Visual Studio 2017, Rider, VS Code, Sublime Text, and Atom. Visual Studio 2015 is not supported. Make sure to get the latest popular compatible Typescript plugin for Atom/VSCode/Sublime.
* A new **UI Layout** module was added. This is the first release with in-Editor support for GUI, expect more GUI support in future releases. Refer to the module documentation page for common use cases and more details.
* Added documentation for the new UI Layout module
* A conversion dialogue window has been added for older projects.

### Changed
* A new project structure is created when you create a new Tiny project.
* Entity groups are now serialized in separate `.utdata` files (they were serialized in the monolithic `.utproject` file before). This is to facilitate collaboration on larger projects: expect new file types for Tiny objects in future releases.
* Systems now always assume the definition of an `update` function on the generated system object (e.g. one script in your project includes `game.MySystem.update = function(scheduler, world) { /* game logic goes here */ });`. This behaviour was previously supported by the *External* option in the Editor UI, and is now the only way to schedule a system. Existing projects are automatically migrated to this requirement.
* UI: Static Entities have been removed. Entities with no transforms appear as normal entities. Re-parenting an entity with no Transform component will automatically add a Transform component.

### Fixed
* Fixed undo stability problems


## [0.6.0-preview] - 2018-07-12
* Possible issue when switching to Tiny layout (may need to open the windows manually through the menu)

### Added
* Added Fireworks sample
* Added GalaxyRaiders sample
* Added Pong sample
* Added TinyPong sample. An example of Pong sample project optimized for minimum size.
* Added Physics2D sample
* Added Particles Module documentation
* Added HitBox2D Module documentation
* Added Physics2D Module documentation
* IP address is now shown in console after an export
* Exporting a project now shows the build time in console

### Changed
* Unity 2018.2 should be used
* Previous settings that used "Override for HTML5" for assets in the assets tab are reset

### Fixed
* Improved performance in Tiny editor
* Modules are now reading from the correct namespace
* Fixed an issue with getOwnPropertyDescriptors being not a function in ES5
* Fixed keyboard keys for codes higher or equal to to 256 (including arrow keys)
* Initial velocity for Particles is now supported
* Particle system will now use LayerSorting info from templated particle

## [0.5.0-preview] - 2018-06-15
### Added
* Unity Profiler is now integrated with Tiny builds and will open on export

### Changed
* Overall stability changes

### Fixed
* AutoSize Canvas field now retains settings
* Hitbox2D component now retains settings
* Fixed null ref when right-clicking an empty list
* Fixed opening projects/modules that doesn't have loaded entity groups or doesn't have any objects in them

## [0.4.0-preview] - 2018-06-01
### Added
* Added `Tiny / Help` menu items to open the API documentation, user manual, and user forums
* Added `SortingGroup`, `LayerSorting`, and `Camera2DAxisSort` components in the `Core2D` module
* Added hot reload for code files enabled by the new "Link to Source" project setting
* Added a new `/~project` HTTP server route to serve static project content
* "Fence" systems are now stored in the new `UTiny.Shared` built-in module
* Added `UserCodeStart` and `UserCodeEnd` fences
* Added 9-slice support via the "stretch" draw mode in `Sprite2DRendererOptions`

### Changed
* Automatically frame the "scene view" when opening a project
* `Sprite2DRendererTiling` is now called `Sprite2DRendererOptions`
* Systems are now included in the “UserCode” fence by default

### Fixed
* Entity references across Entity Groups is not allowed
* Multiple instances of Unity running Tiny can now coexist
* Fixed `setEnabled(...)` not being called recursively on disabled entities on export.


## [0.3.0-preview] - 2018-05-15