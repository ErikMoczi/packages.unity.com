LOCAL_PATH := $(call my-dir)/..

include $(CLEAR_VARS)
LOCAL_MODULE := arcore-sdk
LOCAL_SRC_FILES := arcore_unity_shim/jni/lib/libarcore_sdk_c.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := arpresto
LOCAL_SRC_FILES := arcore_unity_shim/jni/lib/libarpresto_api.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := arextensions
LOCAL_SRC_FILES := arextensions/lib/libARExtensions.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_CPPFLAGS := -std=c++11 -D_STLP_USE_NEWALLOC -mfpu=neon -O3 -Wall
LOCAL_MODULE := UnityARCore
LOCAL_LDLIBS := -L$(SYSROOT)/usr/lib -llog -lEGL -lGLESv2 -lmediandk
LOCAL_SRC_FILES := \
    source/DllMain.cpp \
    source/CameraImage/CameraImageAndroid.cpp \
    source/MathConversion.cpp \
    source/Utility.cpp \
    source/Providers/CameraProvider.cpp \
    source/Providers/DepthProvider.cpp \
    source/Providers/InputProvider.cpp \
    source/Providers/InputProvider_V1.cpp \
    source/Providers/InputProvider_V2.cpp \
    source/Providers/LifecycleProviderCamera.cpp \
    source/Providers/LifecycleProviderDepth.cpp \
    source/Providers/LifecycleProviderInput.cpp \
    source/Providers/LifecycleProviderInput_V1.cpp \
    source/Providers/LifecycleProviderInput_V2.cpp \
    source/Providers/LifecycleProviderPlane.cpp \
    source/Providers/LifecycleProviderRaycast.cpp \
    source/Providers/LifecycleProviderReferencePoint.cpp \
    source/Providers/LifecycleProviderSession.cpp \
    source/Providers/PlaneProvider.cpp \
    source/Providers/RaycastProvider.cpp \
    source/Providers/ReferencePointProvider.cpp \
    source/Providers/SessionProvider.cpp \
    source/Wrappers/WrappedAnchor.cpp \
    source/Wrappers/WrappedAnchorList.cpp \
    source/Wrappers/WrappedCamera.cpp \
    source/Wrappers/WrappedCameraIntrinsics.cpp \
    source/Wrappers/WrappedFrame.cpp \
    source/Wrappers/WrappedHitResult.cpp \
    source/Wrappers/WrappedHitResultList.cpp \
    source/Wrappers/WrappedLightEstimate.cpp \
    source/Wrappers/WrappedPlane.cpp \
    source/Wrappers/WrappedPose.cpp \
    source/Wrappers/WrappedPointCloud.cpp \
    source/Wrappers/WrappedSession.cpp \
    source/Wrappers/WrappedTrackable.cpp \
    source/Wrappers/WrappedTrackableList.cpp \
    source/Wrappers/PrestoConfig.cpp

LOCAL_C_INCLUDES := \
    source \
    External \
    External/Unity \
    arcore_unity_shim/include \
    arextensions/include

LOCAL_STATIC_LIBRARIES := arcore-sdk arpresto arextensions
include $(BUILD_SHARED_LIBRARY)
