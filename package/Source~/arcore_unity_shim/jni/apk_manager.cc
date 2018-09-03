//-----------------------------------------------------------------------
// <copyright file="apk_manager.cc" company="Google">
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
#include "apk_manager.h"

namespace ArPresto {

ApkManager::ApkManager(JniManager *jni_manager) :
    jni_manager(jni_manager),
    user_requested_install(false)
{
}

void ApkManager::CheckAvailabilityAsync(CheckApkAvailabilityResult_FP on_result,
    void *context)
{
    AvailabilityRequestRecord record;
    record.on_result = on_result;
    record.context = context;
    availability_request_records.push_back(record);
}

void ApkManager::RequestInstallAsync(bool user_requested,
    RequestApkInstallationResult_FP on_result, void *context)
{
    if (install_request_records.size() == 0)
    {
        user_requested_install = user_requested;
    }

    InstallRequestRecord record;
    record.on_result = on_result;
    record.context = context;
    install_request_records.push_back(record);
}

void ApkManager::Update()
{
    if (availability_request_records.size() > 0)
    {
        ArAvailability ar_availability;
        ArCoreApk_checkAvailability(jni_manager->GetEnv(),
            jni_manager->GetActivityContext(), &ar_availability);

        if (ar_availability != AR_AVAILABILITY_UNKNOWN_CHECKING)
        {
            for (AvailabilityRequestRecord record :
                availability_request_records)
            {
                record.on_result(ar_availability, record.context);
            }

            availability_request_records.clear();
        }
    }

    if (install_request_records.size() > 0)
    {
        ArInstallStatus ar_install_status;
        ArStatus ar_status = ArCoreApk_requestInstall(jni_manager->GetEnv(),
            jni_manager->GetActivity(), user_requested_install,
            &ar_install_status);
        user_requested_install = false;

        ArPrestoApkInstallStatus apk_status;

        if (ar_status == AR_SUCCESS &&
            ar_install_status == AR_INSTALL_STATUS_INSTALL_REQUESTED)
        {
            apk_status = ARPRESTO_APK_INSTALL_REQUESTED;
        }
        else if (ar_status == AR_SUCCESS &&
            ar_install_status == AR_INSTALL_STATUS_INSTALLED)
        {
            apk_status = ARPRESTO_APK_INSTALL_SUCCESS;
        }
        else if (ar_status == AR_UNAVAILABLE_DEVICE_NOT_COMPATIBLE)
        {
            apk_status = ARPRESTO_APK_INSTALL_ERROR_DEVICE_NOT_COMPATIBLE;
        }
        else if (ar_status == AR_UNAVAILABLE_USER_DECLINED_INSTALLATION)
        {
            apk_status = ARPRESTO_APK_INSTALL_ERROR_USER_DECLINED;
        }
        else
        {
            apk_status = ARPRESTO_APK_INSTALL_ERROR;
        }

        if (apk_status != ARPRESTO_APK_INSTALL_REQUESTED)
        {
            for (InstallRequestRecord record : install_request_records)
            {
                record.on_result(apk_status, record.context);
            }

            install_request_records.clear();
        }
    }
}

} // namespace ArPresto
