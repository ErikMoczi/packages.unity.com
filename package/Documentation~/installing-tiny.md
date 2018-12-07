# Installing the package


## Set the Latest Scripting Runtime Version

Tiny Editor requires your project to use the latest scripting runtime version. You can update this setting by opening **Edit / Project Settings / Player > Other Settings**, and setting **Scripting Runtime Version** to **.Net 4.x Equivalent**.


## Adding This Package

Change your project manifest.json file, located in the [Project Folder]/Packages/ directory, to the following:

```
{ \
  "dependencies": { \
      ... \
      <list of pre-populated packages> \
      ... \
  }, \
  "registry": "https://staging-packages.unity.com" \
}
```

Your directory structure should resemble the following:

The package in the directory structure **_must_** be named **com.unity.tiny.editor**.
```
├── Assets \
├── Packages \
│   ├── manifest.json \
│   └── com.unity.tiny.editor \
│       └── [package contents]
```

Once you have updated the **manifest.json** file, and you have downloaded and extracted the **"com.unity.tiny.editor"** package into the Packages folder, open or return to Unity so that it recognizes your changes.

