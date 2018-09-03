#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedPose;

class WrappedAnchor : public WrappingBase<ArAnchor>
{
public:
    void GetPose(WrappedPose& pose) const;
    ArTrackingState GetTrackingState() const;

    void RemoveFromSessionAndRelease();

private:
    friend class ReferencePointProvider;
    friend class WrappedAnchorList;
    ArStatus CreateFromPose(const ArPose* pose);
    ArStatus CreateFromPose(const UnityXRPose& pose);
};
