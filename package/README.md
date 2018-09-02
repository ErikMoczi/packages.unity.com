# progrids

## development

Clone [dev repo](https://gitlab.internal.unity3d.com/world-building/progrids) into a Unity project

```
mkdir DevProject/Assets
cd DevProject/Assets
git clone git@gitlab.internal.unity3d.com:world-building/progrids.git
```

## deployment

Update `package.json`
Update `CHANGELOG.md`
Tag release (using semantic versioning)
    - Pre-release versions are marked appended with "preview" and an optional increment.
    - Release versions are major, minor, and patch values only.
Complete QAReport.md

The development repository is mirrored to the Package Manager staging repository, which will pick up any tagged commits and create a new package uploaded to staging.
