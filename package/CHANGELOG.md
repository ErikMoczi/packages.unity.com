# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.4] - 2018-12-03
### Added
 - Integrated the readme asset with the Tutorials
 - Ability to have more than a single Tutorial in a project
 - Propper flow for users to go into and out of a tutorial
 - Ability to add Images, Video to a tutorial
 - New Color type added to PropertyModificaitonCriterion
 - PropertyModificationCriterion has a new mode where it will complete if the user changes a property to a different value than initial
 - Added option to the masking system to prevent interactions to the unmasked area
 - New Criterions: FrameSelectedCriterion, MaterialPropertyChanged, ActiveToolCriterion, SceneCameraViewMovedCritertion
 - Ability for Tutorials to reference each other
 - "Home/Skip" button have 2 modes. Legacy will open the Hub, and CloseWindow will close the Tutorial window
 - Ability to choose the name of the Tutorial Window


### Fixed
 - Updated usages of obsolete APIs

### Changed
 - Initial version of the in editor tutorial framework as a package. 
 - Contained the use of internals to a single folder.

