# ProGrids 3.0.0-preview.7

## Features

- New About window.
- Now distributed as Package Manager module.
- Project now uses Assembly Definition files to reduce compilation overhead.
- Add a shortcut to reset the snap multiplier (alpha numeric 0 by default).

## Bug Fixes

- Temporary objects are now longer created in scene files.
- Fix grid rendering on top of UI elements.
- Remove duplicate snap on scale pref in Prefences pane.
- Remove duplicate preference fields from in-scene settings window.
- Fix single key shortcuts not working.
- Fix snapping multiple objects not undoing to original state.
- Fix multiple objects snapping to first selected transform instead of the active transform.

## Changes

- Change color of "close" button to light blue.
- Remove `pg_` suffix from class and file names.
- Remove automatic About Window popup on update.

## Changes from preview.6

- Reset grid size and offset shortcut are now the same, fixing a conflict.

# ProGrids 2.5.0-f.0

## Features

- Single key shortcuts now configurable via preferences.

## Bug Fixes

- Don't prevent compiling to Windows Store target.
- Single key shortcuts no long beep on Mac.
- Fix null reference error if GameObject has a null component.

# ProGrids 2.4.1-f.0

## Bug Fixes

- Prevent About Window from opening other tool changelogs.

# ProGrids 2.4.0-f.0

## Features

- Add `pg_IgnoreSnapAttribute` and `ProGridsConditionalSnapAttribute` to disable or conditionally disable snapping on objects.
- Increase accessible grid multiplier range.

## Bug Fixes

- Fix sRGB import settings on icons.
- Prevert overflow when increasing grid multiplier.

# ProGrids 2.3.0-f.0

## Features

- Add option to set major line increment.
- Automatically hide and show the Unity grid when opening / closing ProGrids.

## Bug Fixes

- Fix bug where ProGrids could fail to find icons when root folder is moved.
- Fix bug where ProGrids would not remember it's state between Unity sessions.

## Changes

- Slightly increase opacity of default grid colors.

# ProGrids 2.2.7-f.0

## Bug Fixes

- Fix cases where `Snap on Selected Axes` would sometimes be unset.

# ProGrids 2.2.6-f.0

## Bug Fixes

- Fix warnings in Unity 5.4 regarding API use during serialization.

# ProGrids 2.2.5-f.0

## Bug Fixes

- Fix an issue where ProGrids would not stay open across Unity restarts.

# ProGrids 2.2.4-f.0

## Bug Fixes

- Fix issue where adjusting grid offset would not repaint grid.
- Attempt to load GUI resources on deserialization, possibly addressing issues with menu icons not loading.

# ProGrids 2.2.3-f.0

## Bug Fixes

- If icons aren't found, search the project for matching images (allows user to rename or move ProGrids folder).
- Make menu usable even if icons aren't found in project.
- Fix bug where grid would fail to render on Mac.
- Improve performance of grid rendering and increase draw distance.

# ProGrids 2.2.2-f.0

## Bug Fixes

- Fix possible leak in pg_GridRenderer.
- Fix 10th line highlight being lost on script reload.
- Remember open/closed state between Unity loads.
- Fix bug where multiple ProGrids instances could potentially be instantiated.

# ProGrids 2.2.1-f.0

## Features

- New interface jettisons bulky Editor Window in favor of a minimal dropdown in the active sceneview.
- New "Predictive Grid" option will automatically change the grid plane to best match the current movement.
- Add option to snap all selected objects independently of on another (toggle off "Snap as Group").

## Bug Fixes

- Improve support for multiple open scene view windows.
- Respect local rotation when calculating snap value.

# ProGrids 2.1.7-f.0

## Features

- Add preference to enabled snapping scale values.

# ProGrids 2.1.6-p.2

## Features

- Unity 5 compatibility.
- Add documentation PDF.

## Bug Fixes

- Fix Upgradable API warning.
- Fix version marking in About.

# ProGrids 2.1.5-f.0

## Bug Fixes

- Fix crash on OSX in Unity 5.
- Remember grid position when closing and re-opening ProGrids.
- Grid lines no longer render on top of geometry in Deferred Rendering.
- Improve performance of Editor when rendering perspective grids.

# ProGrids 2.1.4-f.0

## Bug Fixes

- Remember On/Off state when closing window.
- ProBuilder now respects temporary snapping disable toggle.
- ProBuilder now respects temporary axis constraint toggles.
- Snap value resolution now retained when using -/+ keys to increase or decrease size.

## Changes

- Remove deprecated SixBySeven.dll.
- Remove unused font from Resources folder.

# ProGrids 2.1.3-f.0

## Bug Fixes

- Catch instance where GridRenderer would not detect Deferred Rendering path, causing grid to appear black and spotty.
- Remember grid show/hide preferences across Unity launches.

# ProGrids 2.1.2-f.0

## Bug Fixes

- Fix missing grid when using Deferred Rendering path.
- Fix conflicting shortcut for toggle axis constraints.

# ProGrids 2.1.1-f.0

## Features

- New perspective plane grids.
- New perspective 3d grid.
- Redesigned interface
- New `[` and `]` shortcuts decrease and increase grid resolution.
- New `-` and `+` shortcuts move 3d plane grid forwards and backwards along axis.
- New `\` shortcut key to toggle between orthographic axis and perspective modes.
- Improve orthographic grid rendering performance.
- Highlight every 10th line.
- New preference toggles use of Axis Constraints while dragging objects (use 'C' key to invert preference on the fly).
- Shiny new About window.

## Bug Fixes

- Update grid in real time while modifying preferences.
- Catch a rare divide by zero exception on Unity 3.

## Changes
- Move ProGrids from 6by7 folder to ProCore.
- Use new `ProCore.dll` library instead of `SixBySeven.dll`.
