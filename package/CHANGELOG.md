# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.11] - 2018-26-01
### Bugfixes and minor enhancements.  Also some restructuring in prep for UPM
- Project restructure: Removed Base, Timeline, and PostFX folders from project root
- Added API to support seamless warping of target objects
- New OverlayImage extension to put screen-space overlays on vcams.  Great for storyboard placeholders.
- Added support for custom blend curves
- Lookahead: added Ignore Y Axis Movement option
- Added support for cascading blends (i.e. blending from mid-blend looks better)
- POV/Orbital/FreeLook axis: exposed Min, Max, and Wrap in the UI, for customized axis range
- FreeLook: added Y Axis recentering
- Path: Added Normalized Path units option: 0 is start of path, 1 is end.
- Path: added length display in inspector
- Timeline Clip Editor: vcam sections are now collapsible
- Bugfix: OrbitalTransposer works better with WorldUp overrides
- Bugfix: Remove StateDrivenCamera "not playing a controller" warning
- Bugfix: DepthOfField blending was wrong in PPv2
- Bugfix: Handle exceptions thrown by assemblies that don't want to be introspected
- Bugfix: vcams following physics objects snapped to origin after exiting play mode
- Bugfix: predictor now supports time pause
- Bugfix: Moved StartCoroutine in Brain to OnEnable()

## [2.1.10] - 2017-11-28
### This is the first UPM release of *Unity Package Cinemachine*.
- New Aim component: Same As Follow Target simply uses the same orientation as the Follow target
- Perlin Noise component: added inspector UI to clone or locate existing Noise profiles, and to create new ones
- Noise Presets were moved outside of the Examples folder
- Example Assets are now included as embedded package, not imported by default
- Bugfix: FreeLook with PositionDelta was not properly updating the heading
- Bugfix: Transitioning between FreeLooks simetimes caused a short camera freeze
- Bugfix: Added some null checks to FreeLook, to prevent error messages at build time

## [2.1.9] - 2017-11-17
### Initial version.
*Version 2.1.9 cloned from private development repository, corresponding to package released on the asset store*
