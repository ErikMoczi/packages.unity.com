# Magic Leap

This package provides support for developing Magic Leap applications using Unity. 
Currently, it provides necessary functionality to enable rendering and spatial mapping
capabilities.

## How to develop this package

### Prerequisites

- git clone the [`com.unity.xr.magicleap`](https://github.cds.internal.unity3d.com/xr/sdk/magicleap) project.
- use the latest 2019.1 installers, or check out the latest revision of `platform/lumin`.
- `cd magicleap; [mono] bee.exe` (on Windows, you can execute bee.exe directly; everywhere else requires you launch it via mono)
- open one of the projects under `./TestProjects/`
- If you have any C# compilation errors, something is wrong.

### How to build this package

We are using Bee to build. You must be in the repository root:

```
> [mono] ./bee
```

If you are a Unity employee, Bee will try to automatically download a compatible SDK for you. If Bee cannot find a suitable MLSDK (or if you need to explictly override the one it does find), You may set the `LUMINSDK_UNITY` environment variable to the MLDSK path you would like to use. Example:
```
D:\mlsdks\MLSDK_Mainline_MLTP6_0.15.0_06052018\internal_mlsdk_win64_full\mlsdk
```

This repository requires version 0.18.0 of the MLSDK. It will probably work with a newer version, but it's currently unsupported.

Ping `@cary` or `@stuartr` if you have questions.