# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [5.7.2-preview] - 2019-03-09

## [5.7.1-preview] - 2019-03-08

## [5.7.0-preview] - 2019-03-07

## [5.6.0] - 2019-02-21

## [5.5.0-preview] - 2019-02-18
### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named.

## [5.4.0-preview] - 2019-02-11
### Added

### Fixed
- Incorrect toggle rectangle in VisualEffect inspector
- Shader compilation with SimpleLit and debug display

## [5.3.1-preview] - 2019-01-28

## [5.3.0-preview] - 2019-01-28
### Added
- Add spawnTime & spawnCount operator
- Add seed slot to constant random mode of Attribute from curve and map
- Add customizable function in VariantProvider to replace the default cartesian product
- Add Inverse Lerp node
- Expose light probes parameters in VisualEffect inspector

### Fixed
- Some fixes in noise library
- Some fixes in the Visual Effect inspector
- Visual Effects menu is now in the right place
- Remove some D3D11, metal and C# warnings
- Fix in sequential line to include the end point
- Fix a bug with attributes in Attribute from curve
- Fix source attributes not being taken into account for attribute storage
- Fix legacy render path shader compilation issues
- Small fixes in Parameter Binder editor
- Fix fog on decals
- Saturate alpha component in outputs
- Fixed scaleY in ConnectTarget

## [5.2.0-preview] - 2018-11-27
### Added
- Prewarm mechanism

### Fixed
- Handle data loss of overriden parameters better

### Optimized
- Improved iteration times by not compiling initial shader variant

## [4.3.0-preview] - 2018-11-23

Initial release
