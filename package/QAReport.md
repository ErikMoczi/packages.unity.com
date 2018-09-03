# SPRITESHAPE QA REPORT

## QA Owner:
Wes Wong, Ed Shih

## TEST STRATEGY

### Testing Plan:
Sprite Shape test document
* https://docs.google.com/a/unity3d.com/document/d/1_BnTrHbCK0F-Yi28vWBdk_Bl7DmQDMPRM-N6tNrLLG4/edit?usp=sharing

### Test Rail Cases:
Asset parameters
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/59058
Creating basic usable object (SpriteShapeProfile)
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/59059
Object parameters
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/59383
Creating basic usable object (SpriteShapeController)
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/59382
Polygon Collider
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/59045
Colliders
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/69249

### Use Cases:
Create a platform/terrain with auto-fill and borders that change based on shape
* http://qatestrail.hq.unity3d.com/index.php?/cases/view/69249

### Automated Tests:
n/a

### Scenario Testing:
During 2017.3 Scenario Test Week, 4 teams touched on 2D features.
Total of 73 bugs were reported, 0 bugs related to 2D in general, or SpriteShape in particular.

## PACKAGE STATUS

### Package stability:
Package is stable with no reported/unresolved crashes, and no major unresolved bugs.

### Known bugs, issues:
There are no critical bugs/issues that would impede package release.
Refer to the SpriteShape testing document for full list.

### Performance metrics:
There are no major performance regressions detected since beta 4 (SpriteShape code merged in).

Suite: Runtime
Sample group: TotalTime
Branch: 2017.3/staging
Platforms: Android NVidia Shield, iPad Mini 2, Windows Standalone, Mac Standalone

* http://performance.qa.hq.unity3d.com/?%7Bsuite%7D=Runtime&%7Bconfigurations%7D=%5BAndroid%20Nvidia%20Shield,iOS%20iPad%20Mini2,Mac%20Standalone%20Katana,Windows%20Standalone%20Katana%5D&%7BsampleGroup%7D=TotalTime&%7BbranchFrom%7D=2017.3/staging&%7BchangesetFrom%7D=106ea7f5be35&%7BbranchTo%7D=2017.3/staging&%7BchangesetTo%7D=5d4fc56d3656&%7BregressionsOnly%7D=false&%7BprogressionsOnly%7D=false&%7BstatisticalMethod%7D=Median&%7BtestSelection%7D=%5BDynamicBatchingSprites,SceneBased_AnimatedSpriteRendering_AnimatedSpriteRendering,SpriteRendering%5D

Suite: Runtime
Sample group: FrameTime
Branch: 2017.3/staging
Platforms: Android NVidia Shield, iPad Mini 2, Windows Standalone, Mac Standalone

* http://performance.qa.hq.unity3d.com/?%7Bsuite%7D=Runtime&%7Bconfigurations%7D=%5BAndroid%20Nvidia%20Shield,iOS%20iPad%20Mini2,Mac%20Standalone%20Katana,Windows%20Standalone%20Katana%5D&%7BsampleGroup%7D=FrameTime&%7BbranchFrom%7D=2017.3/staging&%7BchangesetFrom%7D=106ea7f5be35&%7BbranchTo%7D=2017.3/staging&%7BchangesetTo%7D=5d4fc56d3656&%7BregressionsOnly%7D=false&%7BprogressionsOnly%7D=false&%7BstatisticalMethod%7D=Median&%7BtestSelection%7D=%5BDynamicBatchingSprites,SceneBased_AnimatedSpriteRendering_AnimatedSpriteRendering,SpriteRendering%5D

