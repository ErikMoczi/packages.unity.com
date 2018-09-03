//-----------------------------------------------------------------------
// <copyright file="jni_manager.cc" company="Google">
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
#include "jni_manager.h"

namespace ArPresto {

JniManager::JniManager(JavaVM *java_vm, jobject activity)
{

    if (java_vm == NULL)
    {
        LOG_FATAL("JniManager::Invalid java virtual machine.");
        return;
    }

    this->java_vm = java_vm;
    java_vm->GetEnv((void**)&jni_env, ARCORE_JNI_VERSION);

    if (jni_env == NULL)
    {
        LOGE("JniManager::Failed to construct a valid jni environment.");
        return;
    }

    jni_activity = jni_env->NewGlobalRef(activity);

    jni_activity_context = CallJavaMethod(activity, "getApplicationContext",
        "()Landroid/content/Context;");
    jni_activity_context = jni_env->NewGlobalRef(jni_activity_context);

    if (jni_activity_context == NULL)
    {
        LOGE("JniManager::Failed to access the activity context.");
        return;
    }
}

JavaVM* JniManager::GetJavaVM()
{
    return java_vm;
}

JNIEnv* JniManager::GetEnv()
{
    return jni_env;
}

jobject JniManager::GetActivity()
{
    return jni_activity;
}

jobject JniManager::GetActivityContext()
{
    return jni_activity_context;
}

jobject JniManager::CallJavaMethod(jobject object, const char* name,
    const char* sig)
{
    jclass clazz = jni_env->GetObjectClass(object);
    jmethodID method = jni_env->GetMethodID(clazz, name, sig);
    return jni_env->CallObjectMethod(object, method);
}

} // namespace ArPresto
