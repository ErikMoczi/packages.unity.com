#include "Utility.h"
#include "WrappedAnchor.h"
#include "WrappedPose.h"

template<>
void WrappingBase<ArAnchor>::ReleaseImpl()
{
    ArAnchor_release(m_Ptr);
}

void WrappedAnchor::GetPose(WrappedPose& pose) const
{
    if (m_Ptr == nullptr)
    {
        pose.Release();
        return;
    }

    pose.CreateIdentity();
    ArAnchor_getPose(GetArSession(), m_Ptr, pose);
}

ArTrackingState WrappedAnchor::GetTrackingState() const
{
    ArTrackingState ret = AR_TRACKING_STATE_STOPPED;
    ArAnchor_getTrackingState(GetArSession(), m_Ptr, &ret);
    return ret;
}

void WrappedAnchor::RemoveFromSessionAndRelease()
{
    ArAnchor_detach(GetArSession(), m_Ptr);
    Release();
}

ArStatus WrappedAnchor::CreateFromPose(const ArPose* pose)
{
    ArStatus ars = ArSession_acquireNewAnchor(GetArSession(), pose, ReleaseAndGetAddressOf());
    if (ARSTATUS_FAILED(ars))
        m_Ptr = nullptr;

    InitRefCount();
    return ars;
}

ArStatus WrappedAnchor::CreateFromPose(const UnityXRPose& pose)
{
    WrappedPose arPose = pose;
    return CreateFromPose(arPose);
}
