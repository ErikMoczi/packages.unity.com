//-----------------------------------------------------------------------
// <copyright file="arpresto_api.cc" company="Google">
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
#include "context.h"
#include "scoped_pthread_mutex.h"

ArPresto::Context *g_context = NULL;
pthread_mutex_t mutex = PTHREAD_MUTEX_INITIALIZER;

extern "C" void ArPresto_initialize(JavaVM *java_vm, jobject activity,
    CameraPermissionRequestProvider_FP request_camera_permission,
    OnBeforeSetConfigurationCallback_FP on_before_set_config)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI("ArPresto_initialize");

    g_context = new ArPresto::Context(java_vm, activity,
        request_camera_permission, on_before_set_config);
}

extern "C" void ArPresto_handleActivityResume()
{
    ScopedPThreadMutex lock(&mutex);
    LOGI("ArPresto_handleActivityResume");
    if (g_context == NULL)
    {
        return;
    }

    g_context->HandleActivityResume();
}

extern "C" void ArPresto_handleActivityPause()
{
    ScopedPThreadMutex lock(&mutex);
    LOGI("ArPresto_handleActivityPause");
    if (g_context == NULL)
    {
        return;
    }

    g_context->HandleActivityPause();
}

extern "C" void ArPresto_update()
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_update");
    if (g_context == NULL)
    {
        return;
    }

    g_context->Update();
}

extern "C" void ArPresto_checkApkAvailability(
    CheckApkAvailabilityResult_FP on_result, void *context)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_checkApkAvailability");
    if (g_context == NULL)
    {
        LOGE("ArPresto_checkApkAvailability called before "
            "ArPresto_initialize.");
        return;
    }

    g_context->GetApkManager()->CheckAvailabilityAsync(on_result, context);
}

extern "C" void ArPresto_requestApkInstallation(bool user_requested,
    RequestApkInstallationResult_FP on_result, void *context)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_requestApkInstallation");
    if (g_context == NULL)
    {
        LOGE("ArPresto_requestApkInstallation called before "
            "ArPresto_initialize.");
        return;
    }

    g_context->GetApkManager()->
        RequestInstallAsync(user_requested, on_result, context);
}

extern "C" void ArPresto_setConfiguration(const ArPrestoConfig* config)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_setConfiguration");
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_setConfiguration before "
            "ArPresto_initialize.");
        return;
    }

    g_context->SetSessionConfiguration(config);
}

extern "C" void ArPresto_setCameraTextureName(int texture_id)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI("ArPresto_setCameraTextureName");
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_setCameraTextureName before "
            "ArPresto_initialize.");
        return;
    }

    g_context->SetSessionCameraTextureName(texture_id);
}

extern "C" void ArPresto_setEnabled(bool isEnabled)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI("ArPresto_setEnabled");
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_setEnabled before ArPresto_initialize.");
        return;
    }

    g_context->SetSessionEnabled(isEnabled);
}

extern "C" void ArPresto_getFrame(ArFrame **frame)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_getFrame");
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_getFrame before ArPresto_initialize.");
        return;
    }

    g_context->GetFrame(frame);
}

extern "C" void ArPresto_getSession(ArSession **session)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_getSession");
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_getSession before ArPresto_initialize.");
        return;
    }

    g_context->GetSession(session);
}

extern "C" void ArPresto_getStatus(ArPrestoStatus *status)
{
    ScopedPThreadMutex lock(&mutex);
    LOGI_VERBOSE("ArPresto_getStatus");
    if (g_context == NULL)
    {
        *status = ARPRESTO_STATUS_UNINITIALIZED;
        return;
    }

    g_context->GetStatus(status);
}

extern "C" void ArPresto_reset()
{
    if (g_context == NULL)
    {
        LOG_FATAL("Calling ArPresto_reset before ArPresto_initialize.");
        return;
    }

    g_context->Reset();
}
