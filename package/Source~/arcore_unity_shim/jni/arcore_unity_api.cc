//-----------------------------------------------------------------------
// <copyright file="arcore_unity_api.cc" company="Google">
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

#include "logging.h"
#include "arpresto_api.h"
#include "arcore_unity_api.h"
#include "jni_manager.h"
#include "glm.hpp"
#include "gtc/matrix_transform.hpp"
#include "gtc/type_ptr.hpp"

// The API level this shim implements: changes to extern function prototypes
// requires level bump.
#define ARCORE_UNITY_API_LEVEL 1

// Indicates whether ArPresto is initialized.
bool g_arpresto_initialized = false;

// An callback fired on early update when set.
EarlyUpdateCallback_FP g_on_early_update = NULL;

// The background texture id created by Unity.
uint32_t g_background_texture_id = -1;

// The background texture id set on ArCore.
uint32_t g_set_background_texture_id = -1;

// The jni manager.
ArPresto::JniManager *g_jni_manager = NULL;

void convertPoseToUnityWorldSpace(const ArSession *session, const ArPose *pose,
    float out_position[3], float out_rotation[4])
{
    const glm::mat4 unity_T_gl(
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, -1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f);

    glm::mat4 gl_world_T_gl_camera;
    ArPose_getMatrix(session, pose, glm::value_ptr(gl_world_T_gl_camera));
    const glm::mat4 unity_world_T_unity_camera =
        unity_T_gl * gl_world_T_gl_camera * glm::inverse(unity_T_gl);

    const glm::quat unity_rotation =
        glm::quat_cast(unity_world_T_unity_camera);
        std::memcpy(out_rotation, glm::value_ptr(unity_rotation),
        sizeof(float) * 4);

    std::memcpy(out_position, &unity_world_T_unity_camera[3][0],
        sizeof(float) * 3);
}

extern "C" void ArCoreUnity_getARCoreSupportStatus(
    uint32_t expected_api_level, ArCoreUnitySupport *arcore_support_status)
{
    LOGI("ArCoreUnity_getARCoreSupportStatus, expected_api_level=%d, "
        "supported_api_level=%d", expected_api_level, ARCORE_UNITY_API_LEVEL);

    if (expected_api_level != ARCORE_UNITY_API_LEVEL)
    {
        *arcore_support_status = ARCORE_UNITY_API_LEVEL_UNAVAILABLE;
        return;
    }

    *arcore_support_status = ARCORE_UNITY_SUPPORTED;
}

extern "C" void ArCoreUnity_getPose(float position[3], float rotation[4],
    ArCoreUnityDataStatus *status)
{
    ArSession *session;
    ArPresto_getSession(&session);
    if (session == NULL)
    {
        *status = ARCORE_UNITY_DATA_UNAVAILABLE;
        return;
    }

    ArFrame *frame;
    ArPresto_getFrame(&frame);

    ArPose *pose;
    ArPose_create(session, NULL, &pose);

    ArCamera *ar_camera;
    ArFrame_acquireCamera(session, frame, &ar_camera);
    ArCamera_getDisplayOrientedPose(session, ar_camera, pose);
    ArCamera_release(ar_camera);

    convertPoseToUnityWorldSpace(session, pose, position, rotation);

    ArPose_destroy(pose);
    *status = ARCORE_UNITY_DATA_AVAILABLE;
}

extern "C" uint32_t ArCoreUnity_getBackgroundTextureId()
{
    return g_background_texture_id;
}

extern "C" uint32_t ArCoreUnity_getJniInfo(JavaVM **java_vm,
    jobject *unity_activity)
{
    LOGI("ArCoreUnity_getJniInfo");
    if (g_jni_manager != NULL)
    {
        *java_vm = g_jni_manager->GetJavaVM();
        *unity_activity = g_jni_manager->GetActivity();
    }
}

extern "C" void ArCoreUnity_setArPrestoInitialized(
    EarlyUpdateCallback_FP on_early_update)
{
    LOGI("ArCoreUnity_setArPrestoInitialized");
    g_arpresto_initialized = true;
    g_on_early_update = on_early_update;
}

extern "C" void ArCoreUnity_onUnityPlayerInitialize(JavaVM *java_vm,
    jobject unity_activity)
{
    LOGI("ArCoreUnity_onUnityPlayerInitialize");
    if (g_jni_manager != NULL)
    {
        delete g_jni_manager;
    }

    g_jni_manager = new ArPresto::JniManager(java_vm, unity_activity);
}

extern "C" void ArCoreUnity_onUnityPlayerResume()
{
    LOGI("ArCoreUnity_onUnityPlayerResume");
    ArPresto_handleActivityResume();
}

extern "C" void ArCoreUnity_onUnityPlayerPause()
{
    LOGI("ArCoreUnity_onUnityPlayerPause");
    ArPresto_handleActivityPause();
}

extern "C" void ArCoreUnity_onUnityBeforeRenderARBackground(
    uint32_t background_texture_id)
{
    LOGI_VERBOSE("ArCoreUnity_onUnityBeforeRenderARBackground(%d)",
        background_texture_id);

    if (g_arpresto_initialized &&
        background_texture_id != g_background_texture_id)
    {
        g_background_texture_id = background_texture_id;
        ArPresto_setCameraTextureName(g_background_texture_id);
    }
}

extern "C" void ArCoreUnity_onUnityEarlyUpdate()
{
    LOGI_VERBOSE("ArCoreUnity_onUnityEarlyUpdate");
    if (g_on_early_update != NULL)
    {
        g_on_early_update();
    }
}
