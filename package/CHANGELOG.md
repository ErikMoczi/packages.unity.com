# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.8.8] - 2018-04-24
- Fixed reloading with all filter selected

## [1.8.6] - 2018-03-11
- Fix unit tests

## [1.8.5] - 2018-03-09
- Fix issue with fast remove of multiple packages

## [1.8.4] - 2018-03-08
- Fix concurrent adds and endless spinner issue

## [1.8.3] - 2018-03-07
- Added caching for faster ui response time

## [1.8.2] - 2018-03-02
- Modified Tags to reflect new package workflow (Preview -> Released(no tag) -> Verified)

## [1.8.1] - 2018-02-23
- Do not display Recommended tag if package version is alpha, beta or experimental

## [1.8.0] - 2018-02-16
- Hide built-in packages
- Fix packages sorting in All tab

## [1.7.3] - 2018-02-16
- Will no longer loop error report with an invalid manifest

## [1.7.2] - 2018-02-08
- Will no longer report errors infinitely when an exception is thrown during an operation
- Only show "View Changes" when there is an update button
- Fixes typos in dialog when updating package manager ui

## [1.7.0] - 2018-02-05
- Added 'View Documentation' link to package details
- Added 'View changes' link to package details

## [1.6.1] - 2018-01-30
### Fixes
- When updating from 1.5.1, ask user to confirm close of window prior to update

## [1.6.0] - 2018-01-28
### Fixes
- Make window dockable
- UI style rework
- Keyboard navigation

## [1.5.1] - 2018-01-18
### Fixes
- Replace VisualContainer by VisualElement in code and templates

## [1.5.0] - 2018-01-17
### Fixes
- Move "Project->Packages->Manage" menu item to "Window->Package Manager"
- Show the latest version and description in tab "Install" instead of the current version
- Display "Recommended" tag properly
- Display "Go back to" when latest version is less than current one
- Do not display "Update to" when current version is greater than lastest one. (case for embedded or local packages)

## [1.4.0] - 2018-01-12
### Fixes
- Surround specific Unity version 2018.2 code due to API break in UIElements
- Replace packages action button label:
	- "Install" instead of "Add" for packages
	- "Enable/Disable" instead of "Add/Remove" for built-in packages
- "alpha", "beta", "experimental" and "recommended" tags support
- Add loading progress while opening window
- UI polish
- Package description and display name update
- Extra messaging on package state
- Documentation update

## [1.3.0] - 2017-12-12
### Changes
- Add assembly definition files
- Force SemVer to use .NetStandard
- Fix ValidationSuiteTests tests
- Handle compatible versions returned in PackageInfo

## [1.2.0] - 2017-11-16
### Fixes
- Fix flickering test When_Default_FirstPackageUIElement_HasSelectedClass, use package only
- Fix documentation
- Ignore QAReport.md from publish

## [1.1.0] - 2017-11-16
### Changes
- Add Doxygen configuration file
- Removed unused fields in package.json
- Update QAReport.md
- Change 'Modules' for 'Built In Packages'
- Remove version display for Built In Packages

### Notes
- The following files cannot be removed from publish [NPM Developer Guide](https://docs.npmjs.com/misc/developers):
	- package.json
	- README (and its variants)
	- CHANGELOG (and its variants)
	- LICENSE / LICENCE

## [1.0.0] - 2017-11-10
### Changes
- Update QAReport.md
- Update documentation
- Order packages by package display name in all filters

## [0.1.9] - 2017-11-09
### Changes
- Check for null in PackageCollection
- Show warnings
- Briefly shows text when switching filter
- Shows package errors correctly
- Order packages by package display name
- Cancel pending operations when switching filter

## [0.1.8] - 2017-11-08
### Changes
- Adding mock for OriginType, State and Group
- Rename "TestOperation..." to "MockOperation..."
- The tab install will now list all available packages
- Move Captain in its own package.
- UpmPackageInfo was renamed to PackageInfo in Packman API

## [0.1.7] - 2017-11-01
### Added
- Remove outdated operation and use list information instead
- Preview tag are now displayed
### Fixes
- [UX] Prevent removal of this package (com.unity.package-manager-ui)

## [0.1.6] - 2017-10-30
### Added
- Use UIElements UXMLFactory
- Fixes remove modules
- Make sure package group ordering is consistent
- Button instead of label
- View documentation (hidden for now)
- Error handling (via Alert for the moment)
### Fixes
- [Bug] Add shows a cleared list afterward. Should show correct list
- [UX] no ui-feedback when click “Remove”… maybe “Removing” like we do for Add
- [UX] Resizing window quickly creates fuzzy layout
- [UX/Bug UI-Elements] when maximize/minimize window, weird layout with glitch. We should make the window non-resizable

## [0.1.5] - 2017-10-18
### Added
- Manifest Validation code + associated tests

## [0.1.4] - 2017-10-10
### This is the first release of *Unity Package Manager UI*.

*Short description of this release*
