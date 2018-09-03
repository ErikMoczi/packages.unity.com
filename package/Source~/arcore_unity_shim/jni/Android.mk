LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)
LOCAL_MODULE           := arcore_sdk_lib
LOCAL_SRC_FILES        := lib/libarcore_sdk_c.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := arpresto_api
LOCAL_C_INCLUDES := $(LOCAL_PATH)/../include
LOCAL_SRC_FILES := arpresto_api.cc \
                   scoped_pthread_mutex.cc \
                   jni_manager.cc \
                   apk_manager.cc \
                   initialization_manager.cc \
                   context.cc \
                   session_manager.cc \
                   arpresto_config.cc
LOCAL_LDLIBS := -L$(SYSROOT)/usr/lib -llog
LOCAL_SHARED_LIBRARIES := arcore_sdk_lib
include $(BUILD_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := arcore_unity_api
LOCAL_C_INCLUDES := $(LOCAL_PATH)/../include \
                    $(LOCAL_PATH)/../../../../../glm/latest/glm
LOCAL_SRC_FILES := arcore_unity_api.cc
LOCAL_LDLIBS := -L$(SYSROOT)/usr/lib -llog
LOCAL_SHARED_LIBRARIES := arpresto_api arcore_sdk_lib
include $(BUILD_SHARED_LIBRARY)
