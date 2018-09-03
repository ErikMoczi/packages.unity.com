//-----------------------------------------------------------------------
// <copyright file="session_manager.h" company="Google">
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

#ifndef ARPRESTO_SESSION_MANAGER_H
#define ARPRESTO_SESSION_MANAGER_H

#include "arpresto_api.h"
#include "jni_manager.h"

namespace ArPresto {

class SessionManager
{
public:
    SessionManager(JniManager *jni_manager,
        OnBeforeSetConfigurationCallback_FP on_before_set_config);
    ~SessionManager();
    void HandleActivityResume();
    void HandleActivityPause();
    void Update();
    void SetConfiguration(ArPrestoConfig* config);
    void SetCameraTextureName(int texture_id);
    void SetEnabled(bool is_enabled);
    void GetFrame(ArFrame **frame);
    void GetSession(ArSession **session);
    void GetStatus(ArPrestoStatus *status);

private:
    void ApplyState();
    JniManager *jni_manager;
    ArSession *ar_session;
    ArFrame *ar_frame;
    ArConfig *ar_config;
    int camera_texture_id;
    bool is_session_enabled;
    bool is_activity_paused;
    ArStatus config_status;
    ArPrestoStatus presto_status;
    OnBeforeSetConfigurationCallback_FP on_before_set_config;
};

} // namespace ArPresto

#endif // ARPRESTO_SESSION_MANAGER_H
