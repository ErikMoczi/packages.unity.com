#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedConfig : public WrappingBase<ArConfig>
{
public:
    void CreateDefault();

    ArLightEstimationMode GetLightEstimationMode() const;
    ArPlaneFindingMode GetPlaneFindingMode() const;
    ArUpdateMode GetUpdateMode() const;

    bool TrySetLightEstimationMode(ArLightEstimationMode mode);
    bool TrySetPlaneFindingMode(ArPlaneFindingMode mode);
    bool TrySetUpdateMode(ArUpdateMode mode);

    bool IsSupported() const;
};
