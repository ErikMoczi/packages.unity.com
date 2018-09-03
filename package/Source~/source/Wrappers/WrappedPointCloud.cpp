#include "MathConversion.h"
#include "Utility.h"
#include "WrappedPointCloud.h"

const int WrappedPointCloud::kNumFloatsPerPoint = 4;
const int WrappedPointCloud::kOffsetToConfindenceWithinPoint = 3;

template<>
void WrappingBase<ArPointCloud>::ReleaseImpl()
{
    ArPointCloud_release(m_Ptr);
}

WrappedPointCloud::WrappedPointCloud()
    : WrappingBase<ArPointCloud>()
    , m_Data(nullptr)
{
}

WrappedPointCloud::WrappedPointCloud(eWrappedConstruction)
    : WrappedPointCloud()
{
    AcquireFromFrameAndGetData();
}

void WrappedPointCloud::AcquireFromFrameAndGetData()
{
    ArStatus ars = ArFrame_acquirePointCloud(GetArSession(), GetArFrame(), ReleaseAndGetAddressOf());
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_ERROR("Failed to acquire point cloud - this should never happen, given how we handle frames and the documented error codes for this in ARCore's API! Error: '%s'.", PrintArStatus(ars));
        m_Ptr = nullptr;
        return;
    }

    InitRefCount();
    ArPointCloud_getData(GetArSession(), m_Ptr, &m_Data);
}

int32_t WrappedPointCloud::GetNumPoints() const
{
    int32_t ret = 0;
    ArPointCloud_getNumberOfPoints(GetArSession(), m_Ptr, &ret);
    return ret;
}

void WrappedPointCloud::GetPositionAt(int32_t index, UnityXRVector3& position)
{
    MathConversion::ToUnity(position, m_Data + index * kNumFloatsPerPoint);
}

float WrappedPointCloud::GetConfidenceAt(int32_t index)
{
    return m_Data[kNumFloatsPerPoint * index + kOffsetToConfindenceWithinPoint];
}
