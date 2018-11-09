#!/bin/bash

if [ -z "$ANDROID_NDK_ROOT" ]; then
    echo "ERROR: Path to Android NDK is not setup. Please make sure you set the ANDROID_NDK env variable to the location of your NDK installation!"
    exit -1
fi

PROJECT=UnityARCore

$ANDROID_NDK_ROOT/build/ndk-build

TARGET_FOLDER=../Runtime/Android

if [ ! -e "$TARGET_FOLDER" ]; then
    mkdir -p "$TARGET_FOLDER"
fi

rm -rf AndroidLibrary
mkdir AndroidLibrary
mkdir -p AndroidLibrary/jni/armeabi-v7a

cp -v libs/armeabi-v7a/lib$PROJECT.so AndroidLibrary/jni/armeabi-v7a
cat >> AndroidLibrary/AndroidManifest.xml << AXML
<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.unity3d.arcore">
    <uses-sdk android:minSdkVersion="14" android:targetSdkVersion="19" />
</manifest>
AXML

pushd AndroidLibrary
zip -r ../$PROJECT.aar *
popd
rm -r AndroidLibrary

mv $PROJECT.aar "$TARGET_FOLDER"
