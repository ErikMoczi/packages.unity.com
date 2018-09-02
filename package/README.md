# ProGrids

The development repository is mirrored to the Package Manager staging repository, which will pick up any tagged commits
and create a new package uploaded to staging.

**Do not commit to the Package Manager mirror**.

## Development

ProGrids is distributed as a Package Manager package. See [UPM Template](https://gitlab.internal.unity3d.com/upm-packages/upm-package-template)
for more information on Package Manager.

The easiest way to work on packages is by setting up an empty Unity project, then adding ProGrids as a local dependency:

```
#!/bin/sh
git clone $(progrids repository url)
mkdir MyDevProject
mkdir MyDevProject/Assets
mkdir MyDevProject/Packages
echo "{
	\"dependencies\": {
		\"com.unity.progrids\":\"file:../../progrids\"
	},
	\"testables\": [
		\"com.unity.progrids\"
	]
}" > MyDevProject/Packages/manifest.json
```

## Deployment

### Publish to Staging

- [ ] Update `package.json`
- [ ] Update `CHANGELOG.md`
- [ ] Update `Editor/Version.cs`
- [ ] `git tag` commit with `-preview.{build}` ([semantic versioning](https://semver.org/))\*
- [ ] Complete a new [QA Report](https://drive.google.com/drive/u/0/folders/1jwQWHOMR2GPSaDPwLUp1aeTrbOmobkLy)

\* ProGrids specific tagging:

- Pre-release versions are marked appended with "preview" and an optional increment.
- Release versions are major, minor, and patch values only.

### Testing

- [ ] Create a new Unity project with the targeted Unity version (check in `package.json` for this value)
- [ ] Add `"registry":"http://staging-packages.unity.com"` to the `Packages/manifest.json`
- [ ] Install the staged ProGrids version (specified in `package.json`)
- [ ] Complete a new [QA Report]((https://drive.google.com/drive/u/0/folders/1jwQWHOMR2GPSaDPwLUp1aeTrbOmobkLy))

### Publish to Production

After a staging package is approved by QA:

- [ ] Update `package.json`, `changelog.md`, and `Version.cs` to omit `-preview` suffix.
- [ ] `git tag` with major, minor, and patch, omitting `-preview.x` suffix.
- [ ] Push to staging, and contact the release management team to move to production.
    - [Release Management Production Submission Form](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform)

