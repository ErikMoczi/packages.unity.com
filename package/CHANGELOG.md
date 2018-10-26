# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2018-09-09
- Add new public API to test all embedded packages.
- Validate that package dependencies won't cause major conflicts
- Validate that package has a minimum set of tests.
- Fix the fact that validation suite will pollute the project.
- Add project template support
- Hide npm pop-ups on Windows.
- Fix validation suite freeze issues when used offline
- Add validation to check repository information in `package.json`

## [0.3.0] - 2018-06-05
- Hide validation suite when packages are not available
- Accept versions with and without  pre-release tag in changelog
- Fix 'View Results' button to show up after validation
- Shorten assembly definition log by shortening the path
- Fix validation of Assembly Definition file to accept 'Editor' platform type.
- Fix npm launch in paths with spaces
- Fix validation suite UI to show up after new installation.
- Fix validation suite to support `documentation` folder containing the special characters `.` or `~`
- Fix validation suite display in built-in packages
- Add tests for SemVer rules defined in [Semantic Versioning in Packages](https://confluence.hq.unity3d.com/display/PAK/Semantic+Versioning+in+Packages)
- Add minimal documentation.
- Enable API Validation
- Clarify the log message created when the old binaries are not present on Artifactory
- Fix build on 2018.1

## [0.1.0] - 2017-12-20
### This is the first release of *Unity Package Validation Suite*.
