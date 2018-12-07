# Tiny Editor
This package contains all you need to start using Tiny Unity.

Tiny Unity allows you to create HTML5 games using a brand new lightweight JavaScript runtime while using the Unity Editor to author content.

**WARNING**: This feature is experimental, and as such, we offer no backward compatibility guarantee.

## Prerequisites
### Unity 2018.3
This package requires Unity 2018.3 or later. It does not support previous versions. Get it [here](https://unity3d.com/get-unity/update).

### Internet Connection
When building Tiny Unity projects, you'll need an Internet connection to fetch external dependencies. Dependencies are fetched once per runtime update.

## Getting Started
### Open Unity
Open Unity and either create a new project or open an existing project.

Tiny Unity works *within* the context of a Unity project, so you can reuse assets between a Unity project and multiple Tiny Unity projects.

### Set the Latest Scripting Runtime Version
Tiny Editor requires your project to use the latest scripting runtime version. You can update this setting by opening `Edit / Project Settings / Player > Other Settings`, and setting *Scripting Runtime Version* to *.Net 4.x Equivalent*.

### Adding This Package
The process is a bit rough at the moment, so read carefully.

Change your project `manifest.json` file, located in the `[Project Folder]/Packages/` directory, to the following:
```
{
  "dependencies": {
      ...
      <list of pre-populated packages>
      ...
  },
  "registry": "https://staging-packages.unity.com"
}

```

Then, clone or download [this repository](https://github.com/Unity-Technologies/com.unity.tiny) to your `Packages` folder. To do this, go to the [releases](https://github.com/Unity-Technologies/com.unity.tiny/releases) page, download the latest release, and unzip it in the `Packages` folder.

Alternatively, if you're familiar with Git, you can use the command line:
```
cd [Project Folder]/Packages
git clone git@github.com:Unity-Technologies/com.unity.tiny.git
```

Your directory structure should resemble the following: 
> The package in the directory structure *must* be named `com.unity.tiny`.
```
├── Assets
├── Packages
│   ├── manifest.json
│   └── com.unity.tiny
│       └── [package contents]

```

Once the `manifest.json` file is updated, and the `com.unity.tiny` package has been manually downloaded and extracted in the `Packages` folder, open or return to Unity so that it recognizes your changes. 

### Setting up the Tiny workspace
Select `Tiny / Window / Editor`. You should see the Tiny Editor window at the bottom. While a Tiny project is opened, some Unity windows are no longer accessible. Unity will ask you to switch modes to be able to use Tiny Editor. Switching modes closes the current project.

### Importing Sample Projects
Once the package is available, you should see a new `Tiny` menu item in Unity.

Select `Import samples projects` in the Tiny Editor window to launch the interactive package importer. Sample projects are a great way to learn Tiny Unity.

### Open a Sample Project
To open sample projects, select  `Project / Load...` in the Tiny Editor window. Or, double-clicking a utproject file in the Project window.

Once a project is loaded, you can built it by clicking the `Export` button in the Tiny Editor window, or the Unity play mode button. Your default browser should open a web page at this location: `http://localhost:19050/`.

### ECS Introduction
Unlike Unity, Tiny Unity **requires** you to author *Entities*, *Components*, and *Systems*.

A good introduction to this concept is available in this GDC 2018 talk: https://youtu.be/EWVU6cFdmr0

### Documentation
The [user manual](./Documentation~/tiny-editor.md) contains more information about the Tiny Editor.

When writing code, the runtime API documentation is a useful reference. It is available in `Tiny / Help / Scripting Reference`. Additional help can be found in the forums located in `Tiny / Help / Forums`. 