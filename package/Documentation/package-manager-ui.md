# Unity Package Manager
A package is a container that holds any combination of Assets, Shaders, Textures, plug-ins, icons, and scripts that enhance various parts of your project. Unity packages are newer, more tightly integrated versions of Asset Store packages, able to deliver a wide range of enhancements to Unity. 

Use the Unity Package Manager window (in Unity’s top menu: __Window__ > __Package Manager__) to view which packages are available for installation or already installed in your project. In addition, you can use this window to [install](#PackManInstall), [remove](#PackManRemove), or [update](#PackManUpdate) packages for each project.

![Package Manger window](Images/PackageManagerUI-Main.png)

The Package Manager window displays a [list view](#PackManLists) on the left and a [detail view](#PackManDetails) on the right.

<a name="PackManLists"></a>
## Finding packages
By default, the Package Manager window displays the list of packages in the **In Project** mode, so that only the packages already installed in your project appear in the list.

To expand the list to include all available packages, click the **All** button. The list now displays everything registered in the [package registry](#PackManRegistry), regardless of whether they are already installed in the project.

![In Project and All modes](Images/PackageManagerUI-Modes.png)


<a name="PackManDetails"></a>
## Viewing package details
The pane on the right side of the Package Manager window displays details about the selected package.

![Details pane](Images/PackageManagerUI-DetailsPane.png)

These details include the following information:
 - (A) The display name
 - (B) The version number (and tag, if available)
 - \(C\) The link to open the package documentation page
 - (D) The official package name from the registry starting with `com.unity.`
 - (E) The author
 - (F) The installation or update status
 - (G) A brief description
 - (H) Buttons to install, remove, or update the package
 - (I) The link to open the package change log

### Version tags
Some packages display tags next to the version number. These tags convey information about that version of the package. 

![Tagged version information](Images/PackageManagerUI-Tags.png)

The Package Manager uses the following values:

| **Tag** | **Meaning** |
|--|--|
| `verified` | Unity's Quality Assurance team has officially verified that this package works with a specific version of the Editor. |
| `preview` | This package is at an early stage of the release cycle and may not have been documented and fully validated by either the development team or Unity's Quality Assurance team. |

A Package without a tag is considered production quality.

### Finding more information
You can find out more about a package by viewing its documentation. 

To access it, click the View Documentation link in the Details pane:

![View Documentation link](Images/PackageManagerUI-ViewDocs.png)

The package documentation page opens in your default browser.

<a name="PackManOpen"></a>
## Accessing the Package Manager window
You can perform a variety of tasks through the Package Manager window:

 - [Install a new package](#PackManInstall)
 - [Remove an installed package](#PackManRemove)
 - [Update an installed package](#PackManUpdate)

 To open the Package Manager window, navigate to Unity's main menu and go to **Window > Package Manager**. 
 
![Window > Package Manager](Images/PackageManagerUI-Access.png)

<a name="PackManInstall"></a>
### Installing a new package
![Install button](Images/PackageManagerUI-InstallButton.png)

To install a new package:
 1. Open the Project Manager window and click the **All** button.
 2. Select the package you want to install from the **Packages** list. The package information appears in the Details pane.
 3. Click the **Install X.X.X** button. When the progress bar finishes, the new package is ready to use.

<a name="PackManRemove"></a>
### Removing an installed package
![Remove button](Images/PackageManagerUI-RemoveButton.png)

To remove an installed package:
 1. Open the Project Manager window.
 2. Click the **In Project** button if you are in **All** mode.
 3. Select the package you want to remove from the **Packages** list. The package information appears in the Details pane.
 4. Click the **Remove X.X.X** button. When the progress bar finishes, the package disappears from the list.

**Notes:** 
 - You can only remove packages which are not required by another package. 
 - When you remove a package, any Editor or run-time functionality which it implemented is no longer available.

<a name="PackManUpdate"></a>
### Updating an installed package
![Update button](Images/PackageManagerUI-UpdateButton.png)

You can update a package while in either the **In Project** or **All** mode:
 1. Open the Project Manager window. An arrow indicator appears next to any packages that have updates available.
 2. Select the package you want to update from the **Packages** list. The package information appears in the Details pane. 
 3. Click the **Update to X.X.X** button. When the progress bar finishes, the new package version information appears in the Details pane, and any new functionality is immediately available. 

## Advanced package topics
This section provides more advanced information about the package manifest file. You don't need to know anything about these topics to install, remove, and update packages.

In addition to its contents (Assets, Shaders, Textures, plug-ins, icons and scripts), a Unity package contains a [package manifest file](#PackManManifestsPackage). The package manifest tells Unity how to display its information page in the Package Manager window, and how to install the package in the project.

![Folder structure of a package](Images/PackageManagerUI-PackageStructure.png)

In addition, it contains several files that help manage the package deployment, including the tests, the samples, the license, the changelog, and the documentation.

<a name="PackManManifests"></a>
### Manifests
There are two types of manifest files: [project](#PackManManifestsProject) manifests (`manifest.json`), and [package](#PackManManifestsPackage) manifests (`package.json`). Both files use JSON (JavaScript Object Notation) syntax to communicate with the Package Manager, by describing which packages are available for each project, and what each package contains. 

<a name="PackManManifestsProject"></a>
#### Project manifests
Project manifests (`manifest.json`) tell the Package Manager which packages and versions are available to the project.

Project manifests support the following value:

| Key | JSON Type | Description |
|--|--|--|
| `dependencies` | Object |List of packages for the Package Manager to load. These are usually packages officially registered with Unity. |


Example of a `manifest.json` file:

	{
		"dependencies": {
			"com.unity.package-1": "1.0.0",
			"com.unity.package-2": "2.0.0",
			"com.unity.package-3": "3.0.0"
		}
	}

Unity stores each project manifest in the `[your_project_root_folder]/Packages/manifest.json` file.

<a name="PackManManifestsPackage"></a>
#### Package manifests
The Package Manager uses package manifests (`package.json`) to determine which version of the package to load, and what information to display in the Package Manager window.

Package manifests support the following values:

| Key | JSON Type | Description |
|--|--|--|
| `name` | String |The officially registered package name, following this naming convention: `"com.unity.[your package name]"`. For example, `"com.unity.resourcemanager"` |
| `displayName` | String |The package name as it appears in the Package Manager window. For example, `"Resource Manager"` |
| `version` | String |The package version `"MAJOR.MINOR.PATCH"`. Unity packages follow the [Semantic Versioning](https://semver.org) system. For example, `"1.3.0"`. |
| `unity` | String |The Unity version that supports this package. For example, `"2018.1"` indicates compatibility starting with Unity version 2018.1. |
| `description` | String |A brief description of the package. This is the text that appears on the Details pane of the Package Manager window. Some special formatting character codes are supported, such as line breaks (`\n`) and bullets (`\u25AA`). |
| `keywords` | Array of Strings |Keywords used for searching in the Package Manager window, specified as a JSON array of strings. For example, `["Physics", "RigidBody", "Component"]`. |
| `category` | String |The category this packages is in. For example, `"Forces"`. |
| `dependencies` | Object |A list of packages that this package depends on, expressed as a JSON dictionary where the key is the package name and the value is the version number. Unity downloads all dependencies and loads them into the project alongside this package. |

Example of a `package.json` file:

	{
		"name": "com.unity.package-4",
		"displayName": "Package Number 4",
		"version": "2.5.1",
		"unity": "2018.1",
		"description": "This package provides X, Y, and Z. \n\nTo find out more, click the \"View Documentation\" link.",
		"keywords": ["key X", "key Y", "key Z"],
		"category": "Controllers",
		"dependencies": {
			"com.unity.package-1": "1.0.0",
			"com.unity.package-2": "2.0.0",
			"com.unity.package-3": "3.0.0"
		}
	}

Unity stores each package manifest in the `[your_package_root_folder]/package.json` file.

<a name="PackManRegistry"></a>
### The Package Registry
Unity maintains a central registry of official packages that are available for distribution. When Unity loads, the Package Manager communicates with the registry, checks the project manifest file, and displays the status of each available package in the Package Manager window.

When you remove a package from the project, the Package Manager updates the project manifest to exclude that package from the list in **In Project** mode but it is still available in **All** mode because it is still on the registry.

When you install or update a package, the Package Manager downloads the package from the registry.


# Technical details

## Requirements

This version of Unity Package Manager is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

## Known limitations

The Package Manager includes the following known limitations:

* If you manually edit the `manifest.json` file, the Package Manager window doesn't refresh the list of packages. You need to either [re-open the window](#PackManOpen) or [toggle between In Project and All modes](#PackManLists) to force an update.

## Documentation revision history
|Date|Reason|
|---|---|
|Mar 13, 2018|Documentation updated (final edits). Matches package version 1.8.2.|
|Mar 2, 2018|Documentation updated. Matches package version 1.8.2.|
|Feb 19, 2018|Documentation updated. Matches package version 1.8.0.|
|Feb 8, 2018|Documentation updated. Matches package version 1.7.0.|
|Feb 6, 2018|Documentation cleaned up. Matches package version 1.6.1.|
|Jan 31, 2018|Documentation updated (developmental review)|
|Jan 29, 2018|Document updated. Matches package version 1.6.0.|
|Jan 18, 2018|Document updated. Matches package version 1.5.1.|
|Jan 17, 2018|Document updated. Matches package version 1.5.0.|
|Jan 12, 2018|Document updated. Matches package version 1.4.0.|
|Nov 7, 2017|Document created. Matches package version 1.0.0.|