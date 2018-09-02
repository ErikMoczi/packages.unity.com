# Quality Report

Test strategy & current info for ProBuilder 2.x

## QA Owner: Gabriel Williams
## UX Owner: Gabriel Williams, Karl Henkel

## Package Status

* **Latest QA Test Results:** [ProBuilder 2.10.0 QA Sheet](https://docs.google.com/spreadsheets/d/1B4cszFPbVvRvUDm2hqoyWaf98MHkB_TLQhCYtM2E9Bs/edit#gid=1764739309)
* Known Bugs: [ProBuilder Issue Tracker on Git](https://github.com/procore3d/probuilder2/issues)
* Planning: [Favro - World Building Collection](https://favro.com/organization/c564ede4ed3337f7b17986b6/5458f34f10ce252532bf6d1e)

## Test Strategy

**Manual Testing**

For each compatible version of Unity:

### General Testing

1. Create a new Unity project, add ProBuilder to UnityPackageManager manifest (see readme for Package Manager install from a local source).
1. Test any items that are new or modified in this release (Tools/ProBuilder/About will list new features).
1. Open the [ProBuilder QA Sheet](https://docs.google.com/a/unity3d.com/spreadsheets/d/1B4cszFPbVvRvUDm2hqoyWaf98MHkB_TLQhCYtM2E9Bs/edit?usp=sharing), and duplicate.
1. Rename the new sheet to the version number, including the build letter (ex, 3.0.0f0).
1. Rename "Trunk" column to the Unity version tested against (or if testing a backport use of the existing columns).
1. Test each item in the "Test in a New Project" section, and mark results (see "Legend" notes in the QA Sheet).

### Upgrading an existing project

https://docs.google.com/document/d/1stVfKVL23bUr2Ep5o3iSbsSEzxn-HD56lFNHnizFeBA/edit

**When all QA testing is complete**, copy a link to the new QA Sheet and update the "Latest QA Test Results" above.
