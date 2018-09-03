#pragma once

#include "arcore_c_api.h"

void SetLightEstimationMode(ArLightEstimationMode mode);

void SetPlaneFindingMode(ArPlaneFindingMode mode);

void SetUpdateMode(ArUpdateMode mode);

void SetCloudAnchorMode(ArCloudAnchorMode mode);

void UpdateConfigIfNeeded();
