## Develop your package
Package development works best within the Unity Editor.  Here's how to set that up:

1. ~~Clone the `Package Starter Kit` repository locally~~

1. ~~Create a new repository for your package and clone to you desktop~~

1. ~~Copy the contents of the Package Starter Kit folder to your new package.  Be careful not to copy the Package Starter Kit `.git` folder over.~~

1. ~~Start **Unity**, create a local empty project and import your package in the project~~

1. ~~In a console (or terminal) application, push the template files you copied in your new package repository to it's remote~~

1. ##### ~~Fill in your package information~~

	Update the following field in file **Tests/.tests.json**:
	- `"createSeparatePackage"`: If this is set to true, the CI will create a separate package for these tests.

1. ~~Restart Unity. For more information on embedded packages see [here](https://confluence.hq.unity3d.com/display/PAK/How+to+embed+a+package+in+your+project).~~

1. ~~If on 2018.1 - Enable package support in the editor (*Internal Feature*).  From the **Project** window's right hang menu, enable `DEVELOPER`->`Show Packages in Project Window` (*only available in developer builds*).  You should now see your package in the Project Window, along with all other available packages for your project.~~

1. ##### Update **README.md**

    The README.md file should contain all pertinent information for developers using your package, such as:
	* Prerequistes
	* External tools or development libraries
	* Required installed Software
	* Command line examples to build, test, and run your package.

1. ##### ~~Rename and update **your-package-name.md** documentation file.~~

1. ##### ~~Rename and update assembly definition files.~~

1. ##### ~~Document your package.~~

1. ##### ~~Add samples to your package (code & assets).~~

1. ##### Validate your package.

    **Validate your package using the Validation Suite**
    Before you publish your package, you need to make sure that it passes all the necessary validation checks by using the [Package Validation Suite extension](https://gitlab.internal.unity3d.com/upm-packages/package-ux/package-validation-suite) (required).
    Once the Validation Suite package is installed, it will add a `Validate` button in the Package Manager UI's details of your installed packages. To install the extension, follow these steps:

    1. Make sure you have `Package Manager UI v1.9.6` or above.
    1. Your project manifest will need to point to a staging registry for this, which you can do by adding this line to it: `"registry": "https://staging-packages.unity.com"`
    1. Install `Package Validation Suite v0.4.0-preview.2` or above from the `Package Manager UI` in the `All Packages` section. If you can't find it there, try turning on `Show preview packages` in the `Advanced` menu.
    1. After installation, you will see a `Validate` button show up in the Package Manager UI, which, when pressed, will run a series of tests and expose a `See Results` button for additional explanation.
        1. If it succeeds, you will see a green bar with a `Success` message.
        1. If it fails, you will see a red bar with a `Failed` message.

    The validation suite is still in preview, if you come across arguable results, please discuss them on #release-management.

1. ##### Design Guidelines.

    1. You should follow these design guideline when creating your package
      1. [Package design guidelines](https://confluence.hq.unity3d.com/display/UX/Packages)
      1. [Unity design checklist](https://unitytech.github.io/unityeditor-hig/topics/checklist.html)

    1. The namespace for code in the asmdef **must** match the asmdef name, except the initial `Unity`, which should be replaced with `UnityEngine` or `UnityEditor`:
      1. Runtime codeWe should only use the `Unity` namespace for code that has no dependency on anything in `UnityEngine` or `UnityEditor` and instead uses `ECS` and other `Unity`-namespace systems.

1. ##### ~~Add tests to your package.~~

1. ##### ~~Setup your package CI.~~

1. ##### ~~Update **CHANGELOG.md**.~~

## ~~Create a Pre-Release Package~~

## ~~Register your package~~

## ~~Make sure your package meets all legal requirements~~

##### ~~Update **Third Party Notices.md**~~

## Preparing your package for Staging

Before publishing your package to production, you must send your package on the Package Manager's **staging** repository.  The staging repository is monitored by QA and release management, and is where package validation will take place before it is accepted in production.
## *** IMPORTANT: The staging repository is publicly accessible, do not publish any packages with sensitive material you aren't ready to share with the public ***


1. Publishing your changes to the package manager's **staging** repository happens from Github.cds.  To do so, simply setup your project's Continuous integration, which will be triggered by "Tags" on your branches.  

For information please see [here](https://confluence.hq.unity3d.com/display/PAK/Setting+up+your+package+CI)


1. Test your package locally

    Now that your package is published on the package manager's **staging** repository, you can test your package in the editor by creating a new project, and editing the project's `manifest.json` file to point to your staging package, as such:
      ```
      dependencies: {
        "com.unity.[sub-group].[your-package-name]": "0.1.0"
      },
      "registry": "https://staging-packages.unity.com"
      ```
