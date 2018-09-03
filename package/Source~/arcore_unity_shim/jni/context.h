//-----------------------------------------------------------------------
// <copyright file="context.h" company="Google">
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

#ifndef ARPRESTO_CONTEXT_H
#define ARPRESTO_CONTEXT_H

#include <unordered_map>
#include "arcore_c_api.h"
#include "arpresto_api.h"
#include "jni_manager.h"
#include "initialization_manager.h"
#include "session_manager.h"

namespace ArPresto {

class Context
{
public:

    Context(JavaVM *java_vm, jobject activity,
        CameraPermissionRequestProvider_FP request_camera_permission,
        OnBeforeSetConfigurationCallback_FP on_before_set_config);
    ~Context();
    
    void HandleActivityResume();
    void HandleActivityPause();
    void Update();
    void SetSessionConfiguration(const ArPrestoConfig* config);
    void SetSessionCameraTextureName(int texture_id);
    void SetSessionEnabled(bool is_enabled);
    void GetFrame(ArFrame **frame);
    void GetSession(ArSession **session);
    void GetStatus(ArPrestoStatus *status);
    ApkManager* GetApkManager();
    void Reset();

private:
    void CreateSessionIfNeeded();
    JniManager jni_manager;
    ApkManager apk_manager;
    InitializationManager initialization_manager;
    SessionManager *session_manager;
    ArPrestoConfig presto_config;
    int camera_texture_id;
    bool is_activity_paused;
    bool is_session_enabled;
    bool is_config_set;
    OnBeforeSetConfigurationCallback_FP on_before_set_config;
};

} // namespace ArPresto

#endif // ARPRESTO_CONTEXT_H
