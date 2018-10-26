#include "WrappedHitResult.h"
#include "WrappedPose.h"
#include "WrappedTrackable.h"
#include "Utility.h"

#include "Unity/UnityXRTypes.h"

WrappedHitResult::WrappedHitResult()
    : m_ArHitResult(nullptr)
{
}

WrappedHitResult::WrappedHitResult(const ArHitResult* arHitResult)
    : m_ArHitResult(arHitResult)
{
}

WrappedHitResult::operator const ArHitResult*() const
{
    return m_ArHitResult;
}

const ArHitResult* WrappedHitResult::Get() const
{
    return m_ArHitResult;
}

void WrappedHitResult::GetPose(ArPose* arPose) const
{
    ArHitResult_getHitPose(GetArSession(), m_ArHitResult, arPose);
}

void WrappedHitResult::GetPose(UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose = GetIdentityPose();
    GetPose(wrappedPose);
    wrappedPose.GetPose(xrPose);
}

float WrappedHitResult::GetDistance() const
{
    float ret = -1.0f;
    ArHitResult_getDistance(GetArSession(), m_ArHitResult, &ret);
    return ret;
}

WrappedHitResultMutable::WrappedHitResultMutable()
{
}

WrappedHitResultMutable::WrappedHitResultMutable(ArHitResult* arHitResult)
    : WrappedHitResult(arHitResult)
{
}

WrappedHitResultMutable::operator ArHitResult*()
{
    return GetArHitResultMutable();
}

ArHitResult* WrappedHitResultMutable::Get()
{
    return GetArHitResultMutable();
}

void WrappedHitResultMutable::GetFromList(const ArHitResultList* hitResultList, int32_t index)
{
    ArHitResultList_getItem(GetArSession(), hitResultList, index, GetArHitResultMutable());
}

ArHitResult*& WrappedHitResultMutable::GetArHitResultMutable()
{
    return *const_cast<ArHitResult**>(&m_ArHitResult);
}

WrappedHitResultRaii::WrappedHitResultRaii()
{
    ArHitResult_create(GetArSession(), &GetArHitResultMutable());
}

WrappedHitResultRaii::~WrappedHitResultRaii()
{
    if (m_ArHitResult != nullptr)
        ArHitResult_destroy(GetArHitResultMutable());
}
