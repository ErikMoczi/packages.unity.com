# Workflow: How to import a memory snapshot

There are two ways to open a memory snapshot that is not present in your current project.

### Import via Project folder

If you created the snapshot files that you want to import with the Memory Profiler (.snap), then do the following:

* Inside your Project folder, find the folder named _MemoryCaptures_ (create it if it does not exist).
* Copy/move the snapshot files to this folder.
* When you return to the [Memory Profiler window](memory-profiler-window.md), you will see the added snapshot in the [Workbench](workbench.md) panel.

### Import via Workbench

If you created the snapshot files that you want to import with the Memory Profiler (.snap) or the [Bitbucket Memory Profiler](https://bitbucket.org/Unity-Technologies/memoryprofiler) (.memsnap, .memsnap2, or .memsnap3), then do the following:

* In the Memory Profiler, click the __Import__ button. This action opens a file dialog.
* Locate and open the memory snapshot you want to import. If you choose to import a .snap file, it will copy the file to your _MemoryCaptures_ folder. If you decide to import a Bitbucket Memory Profiler snapshot file (.memsnap, .memsnap2, or .memsnap3), this action will generate a converted version (.snap) of this file in your Project.

> **Note**: We don’t recommend using the [Bitbucket Memory Profiler](https://bitbucket.org/Unity-Technologies/memoryprofiler) on Unity 2018.3 or later versions.



[Back to manual](manual.md)