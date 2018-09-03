#include "WrappedHitResult.h"
#include "WrappedPose.h"
#include "WrappedTrackable.h"

template<>
void WrappingBase<ArHitResult>::CreateOrAcquireDefaultImpl()
{
    ArHitResult_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArHitResult>::ReleaseImpl()
{
    ArHitResult_destroy(m_Ptr);
}

WrappedHitResult::WrappedHitResult()
    : WrappingBase<ArHitResult>()
{
}

WrappedHitResult::WrappedHitResult(eWrappedConstruction)
    : WrappingBase<ArHitResult>()
{
    CreateOrAcquireDefault();
}

void WrappedHitResult::CreateDefault()
{
    CreateOrAcquireDefault();
}

float WrappedHitResult::GetDistance() const
{
    float ret = -1.0f;
    ArHitResult_getDistance(GetArSession(), m_Ptr, &ret);
    return ret;
}

void WrappedHitResult::AcquireTrackable(WrappedTrackable& trackable)
{
    ArHitResult_acquireTrackable(GetArSession(), m_Ptr, trackable.ReleaseAndGetAddressOf());
    trackable.InitRefCount();
}

void WrappedHitResult::GetPose(WrappedPose& pose)
{
    pose.Release();
    ArHitResult_getHitPose(GetArSession(), m_Ptr, pose);
}
