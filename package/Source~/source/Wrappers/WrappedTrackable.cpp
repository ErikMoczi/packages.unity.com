#include "Utility.h"
#include "WrappedTrackable.h"

static UnityXRTrackingState ToUnityTrackingState(ArTrackingState googleTrackingState)
{
    switch (googleTrackingState)
    {
        case AR_TRACKING_STATE_TRACKING:
            return kUnityXRTrackingStateTracking;
        case AR_TRACKING_STATE_PAUSED:
            return kUnityXRTrackingStateUnavailable;
        case AR_TRACKING_STATE_STOPPED:
            return kUnityXRTrackingStateUnknown;
    }

    return kUnityXRTrackingStateUnknown;
}

WrappedTrackable::WrappedTrackable()
    : m_ArTrackable(nullptr)
{
}

WrappedTrackable::WrappedTrackable(const ArTrackable* arTrackable)
    : m_ArTrackable(arTrackable)
{
}

WrappedTrackable::operator const ArTrackable*() const
{
    return m_ArTrackable;
}

const ArTrackable* WrappedTrackable::Get() const
{
    return m_ArTrackable;
}

ArTrackableType WrappedTrackable::GetType() const
{
    ArTrackableType ret = EnumCast<ArTrackableType>(-1);
    ArTrackable_getType(GetArSession(), m_ArTrackable, &ret);
    return ret;
}

UnityXRTrackingState WrappedTrackable::GetTrackingState() const
{
    ArTrackingState arTrackingState = EnumCast<ArTrackingState>(-1);
    ArTrackable_getTrackingState(GetArSession(), m_ArTrackable, &arTrackingState);
    return ToUnityTrackingState(arTrackingState);
}

WrappedTrackableMutable::WrappedTrackableMutable()
{
}

WrappedTrackableMutable::WrappedTrackableMutable(ArTrackable* arTrackable)
    : WrappedTrackable(arTrackable)
{
}

WrappedTrackableMutable::operator ArTrackable*()
{
    return GetArTrackableMutable();
}

ArTrackable* WrappedTrackableMutable::Get()
{
    return GetArTrackableMutable();
}

ArTrackable*& WrappedTrackableMutable::GetArTrackableMutable()
{
    return *const_cast<ArTrackable**>(&m_ArTrackable);
}

WrappedTrackableRaii::WrappedTrackableRaii()
{
}

WrappedTrackableRaii::~WrappedTrackableRaii()
{
    Release();
}

void WrappedTrackableRaii::AcquireFromHitResult(const ArHitResult* arHitResult)
{
    Release();
    ArHitResult_acquireTrackable(GetArSession(), arHitResult, &GetArTrackableMutable());
}

void WrappedTrackableRaii::AcquireFromList(const ArTrackableList* arTrackableList, int32_t index)
{
    Release();
    ArTrackableList_acquireItem(GetArSession(), arTrackableList, index, &GetArTrackableMutable());
}

void WrappedTrackableRaii::Release()
{
    if (m_ArTrackable != nullptr)
        ArTrackable_release(GetArTrackableMutable());
    m_ArTrackable = nullptr;
}
