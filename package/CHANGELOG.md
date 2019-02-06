# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).
## [0.1.0-preview.5] - 2019-01-29
### Added
 - Added progress bar displays for actions like opening/importing snapshots.
 - Restored binary compatibility with the 2017.4 memory snapshot format.
 - Added handling for duplicate GC handles present in the snapshot.

### Changed
 - Lowered the number of GC allocations when crawling a snapshot.
 - Upgraded dependency on EditorCoroutines package to 0.0.2-preview.1.
 - Changed the initialization of the managed memory sections to not overallocate. 
 - Fixed an issue where selecting a region on the memory map would expand the table underneath onto the whole window.
 - Fixed an issue where closing either snapshot (with two snapshots loaded) would close the other one instead.
 - Resolved UI issues with upstream UI elements versions.

### Removed
 - Removed links in columns: Native Object Name, Native Size. For the Native Objects table. 

## [0.1.0-preview.4] - 2019-01-02
### Added
 - Added on demand computing for tables with the purpose of speeding up the snapshot opening process.
 - Added better handling for corrupted snapshots, in order to avoid having the UI become non-responsive.

### Changed
 - Changed the managed data crawler to use a stack based approach in order to avoid stack overflows when processing large amounts of managed object references.
 - Fixed an issue where attempting to rename a snapshot with two snapshots open would cause an I/O sharing violation, due to the other snapshot being closed instead.
 - Changed capture sequence to first output to a temporary (.tmpsnap) file, to avoid having the Workbench's refresh functionality(triggered during application focus) try to access a snapshot that currently being streamed from a remote target.

## [0.1.0-preview.3] - 2018-12-17
### Added
 - Added enable callback for the capture button, to support cases where the compilation guards get triggered by building the Player.
 - Added missing deregister step for the compilation callbacks to the OnDisable method.

## [0.1.0-preview.2] - 2018-12-12
### Added 
 - Added documentation for the package.
 - Added a table display underneath the "TreeMap" in order to display data about the currently selected object.
 - Added metadata injection functionality, to allow users to specify their metadata collection in a simple way.
 - Added "Diff" functionality for the "MemoryMap".
 - Added import functionality for old snapshot formats that were exported via the "Bitbucket Memory Profiler".
 - Added platform icons for snapshots whose metadata contains the platform from which they were taken.
 - Added basic file management functionality (rename, delete) for the "Workbench". It can be found under the option cogwheel of the snapshots.
 - Added the "Open Snapshots View" to the "Workbench", where users can Diff the last two open snapshots.

### Changed
 - Reworked the "MemoryMap" to display memory allocations in a more intuitive way, allowing better understanding of the captured memory layout.
 - Reworked the "Workbench" to manage the snapshot directory and display all snaphot files contained in it. The "Workbench" default directory resides in "[ProjectRoot]/MemoryCaptures".
 - General UX improvements.

### Removed
 - Removed "Diff" button from the snapshot entries inside the "Workbench".
 - Removed "Delete" button from the snapshot entries inside the "Workbench". Delete can instead be found in the menu under the options cogwheel of the snapshot.

## [0.1.0-preview.1] - 2018-11-15
### Added
 - Added memory snapshot crawler.
 - Added data caching for the crawled snapshot.
 - Added database and tables for displaying processed object data.
 - Added "Diff" functionality to tables, in order to allow the user to compare two different snapshots.
 - Migrated the "TreeMap" view from the bitbucket memory profiler.
 - Added XML syntax for defining tables, with default tables being defined inside "memview.xml".
 - Added the concept of a "Workbench" to allow the user to save a list of known snapshots.
 - Added the "MemoryMap" view to allow users to see the allocated memory space for their application.

### This is the first release of *Unity Package Memory Profier*.
 Source code release of the Memory Profiler package, with no added documentation.