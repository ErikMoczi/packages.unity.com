//-----------------------------------------------------------------------
// <copyright file="initialization_manager.cc" company="Google">
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

#include <cstddef>

#include "logging.h"
#include "initialization_manager.h"

namespace ArPresto {

void InitializationManager_onRequestInstallResult(
    ArPrestoApkInstallStatus install_status, void *context)
{
    LOGI("ArPresto::Got install result.");
    reinterpret_cast<InitializationManager*>(context)->
        HandleRequestInstallResult(install_status);
}

void InitializationManager_onCameraPermissionsResult(bool granted,
    void *context)
{
    LOGI("ArPresto::Got permission result.");
    reinterpret_cast<InitializationManager*>(context)->
        HandleCameraPermissionResult(granted);
}

InitializationManager::InitializationManager(ApkManager *apk_manager,
    CameraPermissionRequestProvider_FP request_camera_permission)
{
    this->apk_manager = apk_manager;
    this->request_camera_permission = request_camera_permission;
    initialization_status = INITIALIZATION_STATUS_UNINITIALIZED;
}

void InitializationManager::Initialize()
{
    if (initialization_status == INITIALIZATION_STATUS_INITIALIZED)
    {
        LOGE("ArPresto::Mutiple calls to InitializationManager::Initialize");
        return;
    }

    initialization_status = INITIALIZATION_STATUS_REQUESTING_APK_INSTALL;
    apk_manager->RequestInstallAsync(true,
        InitializationManager_onRequestInstallResult, this);
}

void InitializationManager::HandleRequestInstallResult(
    ArPrestoApkInstallStatus install_status)
{
    if (initialization_status != INITIALIZATION_STATUS_REQUESTING_APK_INSTALL)
    {
        LOGE("ArPresto::Got unexpected installation request result during "
            "initialization.");
        return;
    }
    else if (install_status != ARPRESTO_APK_INSTALL_SUCCESS)
    {
        LOGE("ArPresto::Apk install failed with %d.", install_status);
        initialization_status = INITIALIZATION_STATUS_ERROR_APK_NOT_AVAILABLE;
        return;
    }

    initialization_status = INITIALIZATION_STATUS_REQUESTING_PERMISSION;
    request_camera_permission(
        InitializationManager_onCameraPermissionsResult, this);
}

void InitializationManager::HandleCameraPermissionResult(bool granted)
{
    if (initialization_status != INITIALIZATION_STATUS_REQUESTING_PERMISSION)
    {
        LOGE("ArPresto::Got unexpected permission result during "
            "initialization.");
        return;
    }
    else if (!granted)
    {
        initialization_status =
            INITIALIZATION_STATUS_ERROR_PERMISSION_NOT_GRANTED;
        return;
    }

    initialization_status = INITIALIZATION_STATUS_INITIALIZED;
}

void InitializationManager::GetPrestoStatus(ArPrestoStatus *presto_status)
{
    // If the initialization has succeeded, the ArPrestoStatus returned will
    // be 'ARPRESTO_STATUS_PAUSED' -- as this is the final state reached by
    // a successful initialization phase.
    switch (initialization_status)
    {
        case INITIALIZATION_STATUS_UNINITIALIZED:
            *presto_status = ARPRESTO_STATUS_UNINITIALIZED;
            break;
        case INITIALIZATION_STATUS_REQUESTING_APK_INSTALL:
            *presto_status = ARPRESTO_STATUS_REQUESTING_APK_INSTALL;
            break;
        case INITIALIZATION_STATUS_REQUESTING_PERMISSION:
            *presto_status = ARPRESTO_STATUS_REQUESTING_PERMISSION;
            break;
        case INITIALIZATION_STATUS_INITIALIZED:
            *presto_status = ARPRESTO_STATUS_PAUSED;
            break;
        case INITIALIZATION_STATUS_ERROR_APK_NOT_AVAILABLE:
            *presto_status = ARPRESTO_STATUS_ERROR_APK_NOT_AVAILABLE;
            break;
        case INITIALIZATION_STATUS_ERROR_PERMISSION_NOT_GRANTED:
            *presto_status =
                ARPRESTO_STATUS_ERROR_PERMISSION_NOT_GRANTED;
            break;
        default:
            LOG_FATAL("ArPresto::Invalid enum value for "
                "initialization_status.");
            break;
    }
}

bool InitializationManager::IsCompletedSuccessfully()
{
    return initialization_status == INITIALIZATION_STATUS_INITIALIZED;
}

}  // namespace ArPresto
