//-----------------------------------------------------------------------
// <copyright file="arpresto_config.cc" company="Google">
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

#include "arpresto_config.h"
#include <cstdlib>
#include <cstring>
#include "logging.h"

void ArPrestoConfig_ctor(ArPrestoConfig* config)
{
    memset(config, 0, sizeof(*config));
}

void ArPrestoConfig_dtor(ArPrestoConfig* config)
{
    std::free(config->augmented_image_database_bytes);
    config->augmented_image_database_bytes = nullptr;
}

void ArPrestoConfig_copy(const ArPrestoConfig& src, ArPrestoConfig* dest)
{
    dest->update_mode = src.update_mode;
    dest->plane_finding_mode = src.plane_finding_mode;
    dest->light_estimation_mode = src.light_estimation_mode;
    dest->cloud_anchor_mode = src.cloud_anchor_mode;

    dest->augmented_image_database_bytes = (uint8_t*)std::realloc(
            dest->augmented_image_database_bytes,
            src.augmented_image_database_size);
    std::memcpy(dest->augmented_image_database_bytes,
                src.augmented_image_database_bytes,
                src.augmented_image_database_size);

    dest->augmented_image_database_size = src.augmented_image_database_size;
}
