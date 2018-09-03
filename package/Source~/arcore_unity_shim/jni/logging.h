//-----------------------------------------------------------------------
// <copyright file="logging.h" company="Google">
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

#ifndef LOGGING_H
#define LOGGING_H

#include <android/log.h>

// Uncomment the line below to enable verbose logging.
// #define VERBOSE_LOGGING

#ifndef LOGI
#define LOGI(...) \
  __android_log_print(ANDROID_LOG_INFO, "ArPresto", __VA_ARGS__)
#endif  // LOGI

#ifndef LOGE
#define LOGE(...) \
  __android_log_print(ANDROID_LOG_ERROR, "ArPresto", __VA_ARGS__)
#endif  // LOGE

#ifndef LOG_FATAL
#define LOG_FATAL(...) \
  __android_log_print(ANDROID_LOG_FATAL, "ArPresto", __VA_ARGS__)
#endif  // LOG_FATAL

#ifdef VERBOSE_LOGGING
#define LOGI_VERBOSE(...) \
  __android_log_print(ANDROID_LOG_INFO, "ArPresto", __VA_ARGS__)
#else
#define LOGI_VERBOSE(...) \
  do {} while(false)
#endif // VERBOSE_LOGGING

#endif // LOGGING_H
