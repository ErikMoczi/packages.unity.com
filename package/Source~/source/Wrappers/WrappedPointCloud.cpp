#include "WrappedPointCloud.h"
#include "Utility.h"

WrappedPointCloud::WrappedPointCloud()
    : m_ArPointCloud(nullptr)
{
}

WrappedPointCloud::WrappedPointCloud(const ArPointCloud* arPointCloud)
    : m_ArPointCloud(arPointCloud)
{
}

WrappedPointCloud::operator const ArPointCloud*() const
{
    return m_ArPointCloud;
}

const ArPointCloud* WrappedPointCloud::Get() const
{
    return m_ArPointCloud;
}

int32_t WrappedPointCloud::NumPoints() const
{
    int32_t ret = 0;
    ArPointCloud_getNumberOfPoints(GetArSession(), m_ArPointCloud, &ret);
    return ret;
}

void WrappedPointCloud::GetData(const float*& pointCloudData) const
{
    ArPointCloud_getData(GetArSession(), m_ArPointCloud, &pointCloudData);
}

int64_t WrappedPointCloud::GetTimestamp() const
{
    int64_t ret = 0;
    ArPointCloud_getTimestamp(GetArSession(), m_ArPointCloud, &ret);
    return ret;
}

WrapppedPointCloudMutable::WrapppedPointCloudMutable()
{
}

WrapppedPointCloudMutable::WrapppedPointCloudMutable(ArPointCloud* pointCloud)
    : WrappedPointCloud(pointCloud)
{
}

WrapppedPointCloudMutable::operator ArPointCloud*()
{
    return GetArPointCloudMutable();
}

ArPointCloud* WrapppedPointCloudMutable::Get()
{
    return GetArPointCloudMutable();
}

ArPointCloud*& WrapppedPointCloudMutable::GetArPointCloudMutable()
{
    return *const_cast<ArPointCloud**>(&m_ArPointCloud);
}

WrappedPointCloudRaii::WrappedPointCloudRaii()
{
}

WrappedPointCloudRaii::~WrappedPointCloudRaii()
{
    Release();
}

ArStatus WrappedPointCloudRaii::TryAcquireFromFrame()
{
    Release();
    return ArFrame_acquirePointCloud(GetArSession(), GetArFrame(), &GetArPointCloudMutable());
}

void WrappedPointCloudRaii::Release()
{
    if (m_ArPointCloud == nullptr)
        return;

    ArPointCloud_release(GetArPointCloudMutable());
    m_ArPointCloud = nullptr;
}
