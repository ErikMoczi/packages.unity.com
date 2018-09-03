//-----------------------------------------------------------------------
// <copyright file="session_manager.cc" company="Google">
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

#include "session_manager.h"
#include "logging.h"

namespace ArPresto {

SessionManager::SessionManager(JniManager *jni_manager,
    OnBeforeSetConfigurationCallback_FP on_before_set_config)
{
    this->jni_manager = jni_manager;
    camera_texture_id = -1;
    is_session_enabled = false;
    is_activity_paused = false;
    config_status = AR_ERROR_INVALID_ARGUMENT;
    presto_status = ARPRESTO_STATUS_PAUSED;
    this->on_before_set_config = on_before_set_config;

    ArStatus ar_status = ArSession_create(jni_manager->GetEnv(),
        jni_manager->GetActivityContext(), &ar_session);

    if (ar_status == AR_SUCCESS)
    {
        LOGI("ArPresto::Session created.");
        ArFrame_create(ar_session, &ar_frame);
        ArConfig_create(ar_session, &ar_config);
    }
    else
    {
        LOGE("ArPresto::ArCore session creation failed.");
        presto_status = ARPRESTO_STATUS_ERROR_FATAL;
    }
}

SessionManager::~SessionManager()
{
    if (ar_frame != NULL)
    {
        ArFrame_destroy(ar_frame);
        ar_frame = NULL;
    }

    if (ar_config != NULL)
    {
        ArConfig_destroy(ar_config);
        ar_config = NULL;
    }

    if (ar_session != NULL)
    {
        ArSession_destroy(ar_session);
        ar_session = NULL;
    }
}

void SessionManager::HandleActivityResume()
{
    is_activity_paused = false;
    ApplyState();
}

void SessionManager::HandleActivityPause()
{
    is_activity_paused = true;
    ApplyState();
}

void SessionManager::Update()
{
    ApplyState();

    if (presto_status == ARPRESTO_STATUS_RESUMED ||
        presto_status == ARPRESTO_STATUS_RESUMED_NOT_TRACKING)
    {
        ArStatus status = ArSession_update(ar_session, ar_frame);
        if (status != AR_SUCCESS)
        {
            LOGE("ArPresto::ArSession_update failed with status %d.", status);
        }
    }
}

void SessionManager::SetConfiguration(ArPrestoConfig* config)
{
    ArConfig_setUpdateMode(ar_session, ar_config, config->update_mode);
    ArConfig_setPlaneFindingMode(ar_session, ar_config,
        config->plane_finding_mode);
    ArConfig_setLightEstimationMode(ar_session, ar_config,
        config->light_estimation_mode);
    ArConfig_setCloudAnchorMode(ar_session, ar_config,
        config->cloud_anchor_mode);

    ArAugmentedImageDatabase* db;
    ArStatus status = ArAugmentedImageDatabase_deserialize(
            ar_session,
            config->augmented_image_database_bytes,
            config->augmented_image_database_size,
            &db);
    if (status == AR_SUCCESS)
    {
        ArConfig_setAugmentedImageDatabase(ar_session, ar_config, db);
        ArAugmentedImageDatabase_destroy(db);
    }
    else
    {
        LOGE("ArPresto::SetConfiguration failed to deserialize Augmented "
             "Image database with status %d.", status);
    }

    if (on_before_set_config != NULL)
    {
        on_before_set_config(ar_session, ar_config);
    }

    config_status = ArSession_configure(ar_session, ar_config);
    ApplyState();
}

void SessionManager::SetCameraTextureName(int texture_id)
{
    camera_texture_id = texture_id;
    ArSession_setCameraTextureName(ar_session, texture_id);
    ApplyState();
}

void SessionManager::SetEnabled(bool is_enabled)
{
    is_session_enabled = is_enabled;
    ApplyState();
}

void SessionManager::GetFrame(ArFrame **frame)
{
    *frame = ar_frame;
}

void SessionManager::GetSession(ArSession **session)
{
    *session = ar_session;
}

void SessionManager::GetStatus(ArPrestoStatus *status)
{
    *status = presto_status;
}

void SessionManager::ApplyState()
{
    ArPrestoStatus starting_presto_status = presto_status;

    // States
    bool permission_not_granted_state =
        presto_status == ARPRESTO_STATUS_ERROR_PERMISSION_NOT_GRANTED;

    bool session_paused_state =
        presto_status == ARPRESTO_STATUS_PAUSED;

    bool session_resumed_state =
        presto_status == ARPRESTO_STATUS_RESUMED ||
        presto_status == ARPRESTO_STATUS_RESUMED_NOT_TRACKING;

    // Transition conditions
    bool is_bad_config =
        config_status != AR_SUCCESS;

    bool is_enabled_and_unpaused =
        is_session_enabled && !is_activity_paused;

    bool is_bad_config_blocking_resume =
        is_enabled_and_unpaused && is_bad_config;

    bool is_can_attempt_resume =
        is_enabled_and_unpaused && !is_bad_config &&
        camera_texture_id != -1;

    if (permission_not_granted_state && !is_session_enabled)
    {
        // Disabling a session with permissions error state resets it.
        presto_status = ARPRESTO_STATUS_UNINITIALIZED;
    }
    else if (session_paused_state && is_bad_config_blocking_resume)
    {
        presto_status =
            ARPRESTO_STATUS_ERROR_SESSION_CONFIGURATION_NOT_SUPPORTED;
    }
    else if (session_paused_state && is_can_attempt_resume)
    {
        ArStatus status = ArSession_resume(ar_session);

        switch (status)
        {
            case AR_SUCCESS:
                presto_status = ARPRESTO_STATUS_RESUMED;
                break;
            case AR_ERROR_CAMERA_PERMISSION_NOT_GRANTED:
                presto_status =
                    ARPRESTO_STATUS_ERROR_PERMISSION_NOT_GRANTED;
                break;
            default:
                presto_status = ARPRESTO_STATUS_ERROR_FATAL;
                if (status != AR_ERROR_FATAL)
                {
                    LOGE("ArPresto::ArCore resume failed ArStatus %d.", status);
                }

                break;
        }
    }
    else if (session_resumed_state && !is_enabled_and_unpaused)
    {
        ArStatus status = ArSession_pause(ar_session);
        if (status == AR_SUCCESS)
        {
            presto_status = ARPRESTO_STATUS_PAUSED;
        }
        else
        {
            LOGE("ArPresto::Pause session failed with ArStatus %d.", status);
            presto_status = ARPRESTO_STATUS_ERROR_FATAL;
        }
    }

    ArCamera *camera;
    ArTrackingState tracking_state;
    ArFrame_acquireCamera(ar_session, ar_frame, &camera);
    ArCamera_getTrackingState(ar_session, camera, &tracking_state);
    ArCamera_release(camera);

    if (presto_status == ARPRESTO_STATUS_RESUMED &&
        tracking_state != AR_TRACKING_STATE_TRACKING)
    {
        presto_status = ARPRESTO_STATUS_RESUMED_NOT_TRACKING;
    }
    else if (presto_status == ARPRESTO_STATUS_RESUMED_NOT_TRACKING &&
        tracking_state == AR_TRACKING_STATE_TRACKING)
    {
        presto_status = ARPRESTO_STATUS_RESUMED;
    }

    if (starting_presto_status != presto_status)
    {
        LOGI("ArPresto::Moving from ArPrestoStatus %d to %d.",
            starting_presto_status, presto_status);
    }
}

} // namespace ArPresto
