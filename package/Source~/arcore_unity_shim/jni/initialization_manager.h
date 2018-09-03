//-----------------------------------------------------------------------
// <copyright file="initialization_manager.h" company="Google">
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

#ifndef ARPRESTO_INITIALIZATION_MANAGER_H
#define ARPRESTO_INITIALIZATION_MANAGER_H

#include "arpresto_api.h"
#include "jni_manager.h"
#include "apk_manager.h"

namespace ArPresto {

enum InitializationStatus {
    INITIALIZATION_STATUS_UNINITIALIZED = 0,
    INITIALIZATION_STATUS_REQUESTING_APK_INSTALL = 1,
    INITIALIZATION_STATUS_REQUESTING_PERMISSION = 2,

    INITIALIZATION_STATUS_INITIALIZED = 100,

    INITIALIZATION_STATUS_ERROR_APK_NOT_AVAILABLE = 200,
    INITIALIZATION_STATUS_ERROR_PERMISSION_NOT_GRANTED = 201,
};

class InitializationManager
{
public:
    InitializationManager(ApkManager *apk_manager,
        CameraPermissionRequestProvider_FP request_camera_permission);
    void Initialize();
    void HandleRequestInstallResult(ArPrestoApkInstallStatus install_status);
    void HandleCameraPermissionResult(bool granted);
    void GetPrestoStatus(ArPrestoStatus *presto_status);
    bool IsCompletedSuccessfully();

private:
    ApkManager *apk_manager;
    CameraPermissionRequestProvider_FP request_camera_permission;
    InitializationStatus initialization_status;
};

} // namespace ArPresto

#endif // ARPRESTO_INITIALIZATION_MANAGER_H
