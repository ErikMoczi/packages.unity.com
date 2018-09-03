//-----------------------------------------------------------------------
// <copyright file="context.cc" company="Google">
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
#include "context.h"
#include "arpresto_config.h"

namespace ArPresto {

Context::Context(JavaVM *java_vm, jobject activity,
    CameraPermissionRequestProvider_FP request_camera_permission,
    OnBeforeSetConfigurationCallback_FP on_before_set_config) :
    jni_manager(java_vm, activity),
    apk_manager(&jni_manager),
    initialization_manager(&apk_manager, request_camera_permission),
    session_manager(NULL),
    camera_texture_id(-1),
    is_activity_paused(false),
    is_session_enabled(false),
    is_config_set(false),
    on_before_set_config(on_before_set_config)
{
    ArPrestoConfig_ctor(&presto_config);
}

Context::~Context()
{
    ArPrestoConfig_dtor(&presto_config);
}

void Context::HandleActivityResume()
{
    is_activity_paused = false;
    if (session_manager != NULL)
    {
        session_manager->HandleActivityResume();
    }
}

void Context::HandleActivityPause()
{
    is_activity_paused = true;
    if (session_manager != NULL)
    {
        session_manager->HandleActivityPause();
    }
}

void Context::Update()
{
    apk_manager.Update();
    CreateSessionIfNeeded();

    if (session_manager != NULL)
    {
        session_manager->Update();
    }
}

void Context::SetSessionConfiguration(const ArPrestoConfig* config)
{
    is_config_set = true;
    ArPrestoConfig_copy(*config, &presto_config);
    if (session_manager != NULL)
    {
        session_manager->SetConfiguration(&presto_config);
    }
}

void Context::SetSessionCameraTextureName(int texture_id)
{
    camera_texture_id = texture_id;
    if (session_manager != NULL)
    {
        session_manager->SetCameraTextureName(camera_texture_id);
    }
}

void Context::SetSessionEnabled(bool is_enabled)
{
    // Update context tracking of session enabled.
    is_session_enabled = is_enabled;

    // If enabling and needed, start initialization.
    ArPrestoStatus initialization_status;
    initialization_manager.GetPrestoStatus(&initialization_status);
    bool should_initialize =
        initialization_status == ARPRESTO_STATUS_UNINITIALIZED ||
        initialization_status == ARPRESTO_STATUS_ERROR_PERMISSION_NOT_GRANTED ||
        initialization_status == ARPRESTO_STATUS_ERROR_APK_NOT_AVAILABLE;
    if (is_enabled && should_initialize)
    {
        initialization_manager.Initialize();
    }

    // Communicate state to session manager if we have one.
    if (session_manager != NULL)
    {
        session_manager->SetEnabled(is_enabled);
    }
}

void Context::GetFrame(ArFrame **frame)
{
    if (session_manager == NULL)
    {
        *frame = NULL;
        return;
    }

    session_manager->GetFrame(frame);
}

void Context::GetSession(ArSession **session)
{
    if (session_manager == NULL)
    {
        *session = NULL;
        return;
    }

    session_manager->GetSession(session);
}

void Context::GetStatus(ArPrestoStatus *status)
{
    if (!initialization_manager.IsCompletedSuccessfully())
    {
        initialization_manager.GetPrestoStatus(status);
        return;
    }
    else if (session_manager == NULL)
    {
        *status = ARPRESTO_STATUS_PAUSED;
        return;
    }

    session_manager->GetStatus(status);
}

ApkManager* Context::GetApkManager()
{
    return &apk_manager;
}

void Context::CreateSessionIfNeeded()
{
    if (!is_session_enabled || session_manager != NULL ||
        !initialization_manager.IsCompletedSuccessfully())
    {
        return;
    }

    session_manager = new SessionManager(&jni_manager, on_before_set_config);

    if (is_activity_paused)
    {
        session_manager->HandleActivityPause();
    }

    if (is_config_set)
    {
        session_manager->SetConfiguration(&presto_config);
    }

    if (camera_texture_id != -1)
    {
        session_manager->SetCameraTextureName(camera_texture_id);
    }

    session_manager->SetEnabled(is_session_enabled);
}

void Context::Reset()
{
    if (session_manager != NULL)
    {
        delete session_manager;
        session_manager = NULL;
    }
}

} // namespace ArPresto
