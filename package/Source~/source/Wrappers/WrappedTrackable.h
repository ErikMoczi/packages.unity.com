#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedTrackable : public WrappingBase<ArTrackable>
{
public:
    ArTrackableType GetType() const;

private:
    friend class WrappedHitResult;
};
