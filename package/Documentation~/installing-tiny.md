# Installing the package

## Package Manager

Follow these steps:

1. From the menu, choose **Window > Package Manager** to open the package manager.
2. In the package manager, check the **Advanced** dropdown and make sure **Show Preview Packages** is enabled.
3. From the package list in the left-hand pane, choose **Tiny Mode**.
4. In the right-hand pane, click the **Install** button.

> We recommend to avoid using Tiny Mode with other *preview* packages.

## Prerequisites

Tiny mode works when installed in a new Unity 2018.3 project. If you install it in an existing project, you may have to manually change some project settings to ensure compatibility.

### .Net 4.x

Tiny Mode requires that your project use the latest version of the scripting runtime. You can update this setting by doing the following:

1. From the menu, choose **Edit > Project Settings...** to open the Project Settings window.
2. In the **Player** section, set the **Configuration > Scripting Runtime Version** to **.Net 4.x Equivalent**.
