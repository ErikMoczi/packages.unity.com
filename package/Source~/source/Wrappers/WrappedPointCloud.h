#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

struct UnityXRVector3;

class WrappedPointCloud : public WrappingBase<ArPointCloud>
{
public:
    WrappedPointCloud();
    WrappedPointCloud(eWrappedConstruction);

    void AcquireFromFrameAndGetData();

    int32_t GetNumPoints() const;
    void GetPositionAt(int32_t index, UnityXRVector3& position);
    float GetConfidenceAt(int32_t index);

private:
    static const int kNumFloatsPerPoint;
    static const int kOffsetToConfindenceWithinPoint;

    const float* m_Data;
};
