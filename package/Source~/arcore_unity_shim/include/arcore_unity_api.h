//-----------------------------------------------------------------------
// <copyright file="arcore_unity_api.h" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

#ifndef ARCORE_UNITY_API_H_
#define ARCORE_UNITY_API_H_

#include <jni.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    // ARCore is supported.
    ARCORE_UNITY_SUPPORTED = 0,

    // The ARCore plugin is not present.  This happens when ARCore is not included in
    // the Unity project and the default shim compiled with Unity is used.
    ARCORE_UNITY_PLUGIN_NOT_PRESENT = 1,

    // The expected API level is not supported by ARCore.
    ARCORE_UNITY_API_LEVEL_UNAVAILABLE = 2,

    // The device is not able to run ARCore.
    ARCORE_UNITY_DEVICE_NOT_SUPPORTED = 3,
} ArCoreUnitySupport;

typedef enum {
    // Indicates valid data was available to fulfill the request.
    ARCORE_UNITY_DATA_AVAILABLE = 0,

    // Indicates no data was available to fulfill the request.
    ARCORE_UNITY_DATA_UNAVAILABLE = 1,
} ArCoreUnityDataStatus;

typedef enum {
    ARCORE_UNITY_SUCCESS = 0,
    ARCORE_UNITY_FAILURE = 1,
} ArCoreUnityStatus;

typedef void (*EarlyUpdateCallback_FP)();

void ArCoreUnity_getARCoreSupportStatus(
     uint32_t expected_api_level, ArCoreUnitySupport *arcore_support_status);
void ArCoreUnity_getPose(float position[3], float rotation[4],
    ArCoreUnityDataStatus *status);

void ArCoreUnity_onUnityPlayerInitialize(JavaVM* java_vm,
    jobject unity_activity);
void ArCoreUnity_onUnityPlayerPause();
void ArCoreUnity_onUnityPlayerResume();
void ArCoreUnity_onUnityEarlyUpdate();
void ArCoreUnity_onUnityBeforeRenderARBackground(uint32_t texture_id);

#ifdef __cplusplus
}
#endif

#endif  // ARCORE_UNITY_API_H_
