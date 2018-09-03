#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedPose;
class WrappedTrackable;

class WrappedHitResult : public WrappingBase<ArHitResult>
{
public:
    WrappedHitResult();
    WrappedHitResult(eWrappedConstruction);

    void CreateDefault();

    void AcquireTrackable(WrappedTrackable& trackable);
    void GetPose(WrappedPose& pose);

    float GetDistance() const;
};
