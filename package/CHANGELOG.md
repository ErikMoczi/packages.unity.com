# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.6-preview] - 2018-03-13
- API rework
- bug fixes
- removed content catalog settings
- improved error checking/logging
- bug fixes

## [1.6.0] - 2018-01-24
- updating description in docs.
- Refactored async init.
- Improved robustness within providors
- code cleanup
- internal op simplification.
- changed params named "address" be a more generic "key" to avoid naming confusion with addressables system.

## [1.5.0] - 2018-01-10
- added UWP support
- removing tests from package so that all Unity projects don't have to have the bloat of these test assets.

## [1.4.0] - 2017-12-21
- removed a test that couldn't function in a package
- removed Samples as the asmdef's weren't playing nice with Tests. (intend to re-add later)
- fixed multi-load-call race condition
- adjusted API exposure
- improved navigation in profiler
- additional bug fixes

## [1.2.0] - 2017-12-3
- added asmdef files to keep ResourceManager contained in its own assembly

## [1.1.0] - 2017-11-12
- moved profiler graph to a more efficient rendering system.
- fix for macOS local file loading
- minor code cleanup/formatting

## [1.0.1] - 2017-11-12
### Removed
- All tests were moved out of package and into Unity's core engine test suite.  

## [1.0.0] - 2017-11-12
- Initial submission for package distribution

