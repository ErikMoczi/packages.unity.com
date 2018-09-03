LOCAL_PATH := $(call my-dir)/..

include $(CLEAR_VARS)
LOCAL_MODULE := arcore-sdk
LOCAL_SRC_FILES := arcore_sdk/libs/$(APP_ABI)/libarcore_sdk.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_CFLAGS := -std=c++11 -D_STLP_USE_NEWALLOC
LOCAL_MODULE := UnityARCore
LOCAL_LDLIBS := -L$(SYSROOT)/usr/lib -llog -lEGL -lGLESv2
LOCAL_SRC_FILES := source/DllMain.cpp source/MathConversion.cpp source/Utility.cpp source/Providers/CameraProvider.cpp source/Providers/DepthProvider.cpp source/Providers/InputProvider.cpp source/Providers/LifecycleProviderCamera.cpp source/Providers/LifecycleProviderDepth.cpp source/Providers/LifecycleProviderInput.cpp source/Providers/LifecycleProviderPlane.cpp source/Providers/LifecycleProviderRaycast.cpp source/Providers/LifecycleProviderReferencePoint.cpp source/Providers/LifecycleProviderSession.cpp source/Providers/PlaneProvider.cpp source/Providers/RaycastProvider.cpp source/Providers/ReferencePointProvider.cpp source/Providers/SessionProvider.cpp source/Wrappers/WrappedAnchor.cpp source/Wrappers/WrappedAnchorList.cpp source/Wrappers/WrappedCamera.cpp source/Wrappers/WrappedConfig.cpp source/Wrappers/WrappedFrame.cpp source/Wrappers/WrappedHitResult.cpp source/Wrappers/WrappedHitResultList.cpp source/Wrappers/WrappedLightEstimate.cpp source/Wrappers/WrappedPlane.cpp source/Wrappers/WrappedPlaneList.cpp source/Wrappers/WrappedPointCloud.cpp source/Wrappers/WrappedPose.cpp source/Wrappers/WrappedSession.cpp source/Wrappers/WrappedTrackable.cpp source/Wrappers/WrappedTrackableList.cpp

LOCAL_C_INCLUDES := source external arcore_sdk/include ${ANDROID_NDK_ROOT}/sources/cxx-stl/gnu-libstdc++/4.9/include
LOCAL_STATIC_LIBRARIES := arcore-sdk
include $(BUILD_SHARED_LIBRARY)
