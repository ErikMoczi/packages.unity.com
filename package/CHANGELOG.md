# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.5] - 2018-03-26

### This is the first preview release of the ARCore package for multi-platform AR.

In this release we are shipping a working iteration of the ARCore package for 
Unity's native multi-platform AR support.
Included in the package are dynamic libraries, configuration files, binaries 
and project files needed to adapt ARCore to the Unity multi-platform AR API.

## [1.0.0-preview.8] - 2018-05-07

### Added 
-Created a Legacy XRInput interface to automate the switch between 2018.1 and 2018.2 XRInput versions.

### Changed
-Only report display and projection matrices if we actually get them from ARCore.

## [1.0.0-preview.9] - 2018-05-09
### Fixed
- Fixed crash when ARCore is not present or otherwise unable to initialize.
- Add support for availability check and apk install

## [1.0.0-preview.10] - 2018-05-23
- Change dependency to `ARExtensions` preview.2
