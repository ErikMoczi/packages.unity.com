# PlayableAd Module Reference

Tiny supports creating MRAID compliant playable ads. This module is referred to as the PlayableAd module. This document provides an example and a reference of PlayableAd components.

## Useful information about playable ads

* Unity Reference: [Unity Ads playable guide](https://drive.google.com/file/d/0B36xgVXh_2EmYzJXdlpsRm5lc2RKaFM5U0NTX0hqQUFIR1Vn/view)

Please note that the above guide is about creating playable ads in general. When using Tiny and the PlayableAd module, parts of the creation process are automatic.

It is best to follow the example below to get an idea about the steps needed when creating playables with Tiny.

## Example

This section demonstrates how to use the PlayableAd module through an example.

1. Create an end card for the playable using UICanvas.
2. Create a UI element or button that accepts touch input from the user, then add it to the end card.
3. Set the end card to appear after 30 seconds from launch.
4. Open <your_projectname>.utproject in the Inspector and fill the store URLs in Configurations > PlayableAdInfo.
5. If the UI element is clicked during runtime, call ut.PlayableAd.Service.openStore();

Verify that the end card appears correctly after 30 seconds and that clicking the UI element opens the correct store URL.

In addition, we recommend adding Analytics to the playable so the playable can be optimized later on if needed. Basic analytics events are available from ut.PlayableAd.Analytics, additionally ut.PlayableAd.Service.sendAnalyticsEvent can be used.

## Configurations

### PlayableAdInfo

Specifies the store URLs that should be opened if a user interacts with the end card of the playable.

|Property|Description|
|--------|-----------|
|googlePlayStoreUrl|Determines the store URL of the advertised product on the Android platform.|
|appStoreUrl|Determines the store URL of the advertised product on the iOS platform.|

## Enums

### AdState

Describes the current state of the playable ad.

|Value|Description|
|-----|-----------|
|Hidden|The playable is hidden.|
|Default|The playable is in the default fixed position.|
|Loading|The playable is still initializing and not yet ready.|
|Expanded|The playable has been expanded to occupy a larger screen area.|

## Structs

### OrientationProperties

Specifies how the playable's orientation should be handled.

|Property|Description|
|--------|-----------|
|allowOrientationChange|Determines whether the playable's orientation should be allowed to change.|
|forceOrientation|Forces the playable to a specific orientation.|

