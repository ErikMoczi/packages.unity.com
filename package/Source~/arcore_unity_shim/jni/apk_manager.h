//-----------------------------------------------------------------------
// <copyright file="apk_manager.h" company="Google">
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

#ifndef ARPRESTO_APK_MANAGER_H
#define ARPRESTO_APK_MANAGER_H

#include <vector>
#include "arpresto_api.h"
#include "jni_manager.h"

namespace ArPresto {

struct AvailabilityRequestRecord
{
    CheckApkAvailabilityResult_FP on_result;
    void *context;
};

struct InstallRequestRecord
{
    RequestApkInstallationResult_FP on_result;
    void *context;
};

class ApkManager
{
public:
    ApkManager(JniManager *jni_manager);
    void CheckAvailabilityAsync(CheckApkAvailabilityResult_FP on_result,
        void *context);
    void RequestInstallAsync(bool user_requested,
        RequestApkInstallationResult_FP on_result, void *context);
    void Update();

private:
    JniManager *jni_manager;
    std::vector<AvailabilityRequestRecord> availability_request_records;
    std::vector<InstallRequestRecord> install_request_records;
    bool user_requested_install;
};

} // namespace ArPresto

#endif // ARPRESTO_APK_MANAGER_H
