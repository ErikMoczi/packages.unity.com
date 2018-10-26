#pragma once

#include "arcore_c_api.h"

class WrappedPointCloud
{
public:
    WrappedPointCloud();
    WrappedPointCloud(const ArPointCloud* arPointCloud);

    operator const ArPointCloud*() const;
    const ArPointCloud* Get() const;

    int32_t NumPoints() const;
    void GetData(const float*& pointCloudData) const;
    int64_t GetTimestamp() const;

protected:
    const ArPointCloud* m_ArPointCloud;
};

class WrapppedPointCloudMutable : public WrappedPointCloud
{
public:
    WrapppedPointCloudMutable();
    WrapppedPointCloudMutable(ArPointCloud* pointCloud);

    operator ArPointCloud*();
    ArPointCloud* Get();

protected:
    ArPointCloud*& GetArPointCloudMutable();
};

class WrappedPointCloudRaii : public WrapppedPointCloudMutable
{
public:
    WrappedPointCloudRaii();
    ~WrappedPointCloudRaii();

    ArStatus TryAcquireFromFrame();
    void Release();

private:
    WrappedPointCloudRaii(const WrappedPointCloudRaii&);
    WrappedPointCloudRaii& operator=(const WrappedPointCloudRaii&);
};
