Unity Package Manager user interface, where a project's packages can be managed and new packages can be discovered.

What is a package?
A package contains additional features to enhance various part of your project. This can include any aspect of Unity, from the editor itself to the runtime. Packages can also provide you with assets such as textures and objects. Simply click on a package to see its detailed information in this pane.

The "In Project" and "Install" filters allow you to show different lists of packages:
▪ In Project: Lists the packages that are currently available in your project.
▪ Install: Lists all discoverable packages and all previously downloaded packages. If a package is already installed in your project, it will show with a differently colored background.

What's new:
▪ Move "Project->Packages->Manage" menu item to "Window->Package Manager"
▪ Show the latest version and description in tab "Install" instead of the current version
▪ Display "Recommended" tag properly
▪ Display "Go back to" when latest version is less than current one
▪ Do not display "Update to" when current version is greater than lastest one. (case for embedded or local packages)
▪ Replace packages action button label: "Install" instead of "Add" for packages and "Enable/Disable" instead of "Add/Remove" for built-in packages
▪ "alpha", "beta", "experimental" and "recommended" tags support
▪ Add loading progress while opening window
▪ UI polish
▪ Package description and display name update
▪ Extra messaging on package state
▪ Documentation update

Unity Package Manager UI includes the following known limitation:
▪ Modifying the manifest.json by hand doesn't update the package list. You need to either re-open the window or change filter to force an update.
▪ Built-in packages can't be enable or disable right now.
