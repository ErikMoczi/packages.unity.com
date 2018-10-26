//-----------------------------------------------------------------------
// <copyright file="arpresto_api.h" company="Google">
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

#ifndef ARPRESTO_API_H
#define ARPRESTO_API_H
#include "arcore_c_api.h"
#include <jni.h>

typedef enum {
    ARPRESTO_STATUS_UNINITIALIZED = 0,
    ARPRESTO_STATUS_REQUESTING_APK_INSTALL = 1,
    ARPRESTO_STATUS_REQUESTING_PERMISSION = 2,

    ARPRESTO_STATUS_RESUMED = 100,
    ARPRESTO_STATUS_RESUMED_NOT_TRACKING = 101,
    ARPRESTO_STATUS_PAUSED = 102,

    ARPRESTO_STATUS_ERROR_FATAL = 200,
    ARPRESTO_STATUS_ERROR_APK_NOT_AVAILABLE = 201,
    ARPRESTO_STATUS_ERROR_PERMISSION_NOT_GRANTED = 202,
    ARPRESTO_STATUS_ERROR_SESSION_CONFIGURATION_NOT_SUPPORTED = 203,
} ArPrestoStatus;

typedef enum {
    ARPRESTO_APK_INSTALL_UNINITIALIZED = 0,
    ARPRESTO_APK_INSTALL_REQUESTED = 1,

    ARPRESTO_APK_INSTALL_SUCCESS = 100,

    ARPRESTO_APK_INSTALL_ERROR = 200,
    ARPRESTO_APK_INSTALL_ERROR_DEVICE_NOT_COMPATIBLE = 201,
    ARPRESTO_APK_INSTALL_ERROR_USER_DECLINED = 203,
} ArPrestoApkInstallStatus;

// A configuration struct to allow configuration without the existence of
// and ArSession.  Should be kept in sync with ArConfig.
typedef struct {
    ArUpdateMode update_mode;
    ArPlaneFindingMode plane_finding_mode;
    ArLightEstimationMode light_estimation_mode;
    ArCloudAnchorMode cloud_anchor_mode;
    uint8_t* augmented_image_database_bytes;
    int64_t augmented_image_database_size;
} ArPrestoConfig;

// A callback to report the result of a camera permission request.
typedef void (*CameraPermissionsResultCallback_FP)(bool granted, void *context);

// A callback that implements a camera permission request.
typedef void (*CameraPermissionRequestProvider_FP)(
    CameraPermissionsResultCallback_FP on_complete, void *context);

// A callback to report the result of an ARCore APK availability check request.
typedef void (*CheckApkAvailabilityResult_FP)(ArAvailability status,
    void *context);

// A callback to report the result of an ARCore APK install request.
typedef void (*RequestApkInstallationResult_FP)(
    ArPrestoApkInstallStatus status, void *context);

// A callback that fires before a configuration is set.
typedef void (*OnBeforeSetConfigurationCallback_FP)(ArSession *session,
    ArConfig *config);

// A callback that fires before a session is resumed.
typedef void (*OnBeforeResumeSessionCallback_FP)(ArSession *session);

/// Initializes ArPresto, a library for that manages ArCore session
/// lifecycle and multiple ArCore sessions for presentation layer
/// applications.
///
/// @param[in]    java_vm            The Android Java virtual machine.
/// @param[in]    frame              The Android activity.
/// @param[in]    request_camera_permission
///                                  A callback fired to request the
///                                  camera permission.  If null,
///                                  permissions will not be requested
///                                  by ArPresto.
/// @param[in]    on_before_set_config
///                                  A callback fired every time before
///                                  a configuration is set.
/// @param[in]    on_before_resume_session
///                                  A callback fired every time just
///                                  before a session is resumed.
extern "C" void ArPresto_initialize(
    JavaVM *java_vm, jobject activity,
    CameraPermissionRequestProvider_FP request_camera_permission,
    OnBeforeSetConfigurationCallback_FP on_before_set_config,
    OnBeforeResumeSessionCallback_FP on_before_resume_session);

/// Handles Android activity resume and its effects on ArPresto/ArCore state.
extern "C" void ArPresto_handleActivityResume();

/// Handles Android activity pause and its effects on ArPresto/ArCore state.
extern "C" void ArPresto_handleActivityPause();

/// Called each frame to pump ArPresto.  If an ArCore session is enabled,
/// the frame returned by ArPresto_getFrame will be updated.
extern "C" void ArPresto_update();

/// Asynchronous checks the availability of the ARCore APK on the device.
///
/// @param[in]    on_result        A callback fired when the check is done.
/// @param[in]    context          An optional context to track the asynchronous
///                                call.  The pointer will be passed to the
///                                on_result callback.
extern "C" void ArPresto_checkApkAvailability(
    CheckApkAvailabilityResult_FP on_result, void *context);

/// Asynchronous requests installation of the ARCore APK on the device.
///
/// @param[in]    user_requested   Whether the user requested this installation
///                                by clicking a call-to-action.
/// @param[in]    on_result        A callback fired when the request is done.
/// @param[in]    context          An optional context to track the asynchronous
///                                call.  The pointer will be passed to the
///                                on_result callback.
extern "C" void ArPresto_requestApkInstallation(bool user_requested,
    RequestApkInstallationResult_FP on_result, void *context);

/// Sets the configuration used for the ArCore session.
///
/// @param[in]    session          The session configuration.
extern "C" void ArPresto_setConfiguration(const ArPrestoConfig* config);

/// Sets the camera texture used by ArCore for background rendering.
///
/// @param[in]    texture_id       The GL texture id.
extern "C" void ArPresto_setCameraTextureName(int texture_id);

/// Sets the enabled stat of the ArCore session.  An enabled session indicates
/// that the ArCore state desired by the application is a tracking ArCore
/// session.
///
/// @param[in]     is_enabled       Whether the session should be enabled.
extern "C" void ArPresto_setEnabled(bool is_enabled);

/// Gets the frame associated with a session's update.
///
/// @param[out]    frame             The last updated ArFrame or null if none
///                                  exists.
extern "C" void ArPresto_getFrame(ArFrame **frame);

/// Gets the ArCore session.
///
/// @param[out]   session          The current ARCore session or null if no
///                                session has been created; the session will
///                                exist when ArPresto_setEnabled(true)
///                                has been called and initialization has
///                                successfully completed.
extern "C" void ArPresto_getSession(ArSession **session);

/// Gets the current ArPrestoStatus status.
///
/// @param[out]    status            The ArPrestoStatus of the session.
extern "C" void ArPresto_getStatus(ArPrestoStatus *status);

/// Resets the ARPresto session and all tracking data.
extern "C" void ArPresto_reset();

#endif // ARPRESTO_API_H