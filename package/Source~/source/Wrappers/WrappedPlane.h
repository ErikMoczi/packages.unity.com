#include "arcore_c_api.h"

struct IUnityXRPlaneDataAllocator;
struct UnityXRPlane;
struct UnityXRPose;

class WrappedPlane
{
public:
    WrappedPlane();
    WrappedPlane(const ArPlane* arPlane);

    operator const ArPlane*() const;
    const ArPlane* Get() const;

    void ConvertToXRPlane(UnityXRPlane& xrPlane, IUnityXRPlaneDataAllocator& xrAllocator) const;

    bool IsPoseInPolygon(const ArPose* arPose) const;
    bool IsPoseInPolygon(const UnityXRPose& xrPose) const;

    bool IsPoseInExtents(const ArPose* arPose) const;
    bool IsPoseInExtents(const UnityXRPose& xrPose) const;

    void GetExtents(float& x, float& z) const;

    int32_t GetPolygonSize() const;
    void GetPolygon(float* boundaryVerts) const;

    void GetCenterPose(ArPose* arPose) const;
    void GetCenterPose(UnityXRPose& xrPose) const;

protected:
    const ArPlane* m_ArPlane;
};

class WrappedPlaneMutable : public WrappedPlane
{
public:
    WrappedPlaneMutable();
    WrappedPlaneMutable(ArPlane* arPlane);

    operator ArPlane*();
    ArPlane* Get();

protected:
    ArPlane*& GetArPlaneMutable();
};

class WrappedPlaneRaii : public WrappedPlaneMutable
{
public:
    WrappedPlaneRaii();
    ~WrappedPlaneRaii();

    bool TryAcquireSubsumedBy(const ArPlane* planeSubsumed);
    void Release();

private:
    WrappedPlaneRaii(const WrappedPlaneRaii&);
    WrappedPlaneRaii& operator=(const WrappedPlaneRaii&);
};
