//-----------------------------------------------------------------------
// <copyright file="jni_manager.h" company="Google">
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

#ifndef ARPRESTO_JNI_MANAGER_H
#define ARPRESTO_JNI_MANAGER_H

#include <jni.h>

// The version of JNI that should be used.
#define ARCORE_JNI_VERSION JNI_VERSION_1_6

namespace ArPresto {

class JniManager {
public:
    JniManager(JavaVM *java_vm, jobject activity);
    JavaVM* GetJavaVM();
    JNIEnv* GetEnv();
    jobject GetActivity();
    jobject GetActivityContext();
    jobject CallJavaMethod(jobject object, const char* name,
        const char* sig);

private:
    JavaVM* java_vm = NULL;
    JNIEnv* jni_env = NULL;
    jobject jni_activity;
    jobject jni_activity_context;
};

} // namespace ArPresto

#endif // ARPRESTO_JNI_MANAGER_H
