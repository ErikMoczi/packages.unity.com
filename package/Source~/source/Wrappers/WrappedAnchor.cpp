#include "Utility.h"
#include "WrappedAnchor.h"
#include "WrappedPose.h"

WrappedAnchor::WrappedAnchor()
    : m_ArAnchor(nullptr)
{
}

WrappedAnchor::WrappedAnchor(const ArAnchor* arAnchor)
    : m_ArAnchor(arAnchor)
{
}

WrappedAnchor::operator const ArAnchor*() const
{
    return m_ArAnchor;
}

const ArAnchor* WrappedAnchor::Get() const
{
    return m_ArAnchor;
}

void WrappedAnchor::GetPose(ArPose* arPose) const
{
    ArAnchor_getPose(GetArSession(), m_ArAnchor, arPose);
}

void WrappedAnchor::GetPose(UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose = GetIdentityPose();
    GetPose(wrappedPose);
    wrappedPose.GetPose(xrPose);
}

UnityXRTrackingState WrappedAnchor::GetTrackingState() const
{
    ArTrackingState arTrackingState = AR_TRACKING_STATE_STOPPED;
    ArAnchor_getTrackingState(GetArSession(), m_ArAnchor, &arTrackingState);
    return ConvertGoogleTrackingStateToUnity(arTrackingState);
}

WrappedAnchorMutable::WrappedAnchorMutable()
{
}

WrappedAnchorMutable::WrappedAnchorMutable(ArAnchor* arAnchor)
    : WrappedAnchor(arAnchor)
{
}

WrappedAnchorMutable::operator ArAnchor*()
{
    return GetArAnchorMutable();
}

ArAnchor* WrappedAnchorMutable::Get()
{
    return GetArAnchorMutable();
}

void WrappedAnchorMutable::Detach()
{
    ArAnchor_detach(GetArSessionMutable(), GetArAnchorMutable());
}

ArAnchor*& WrappedAnchorMutable::GetArAnchorMutable()
{
    return *const_cast<ArAnchor**>(&m_ArAnchor);
}

WrappedAnchorRaii::WrappedAnchorRaii()
{
}

WrappedAnchorRaii::~WrappedAnchorRaii()
{
    Release();
}

ArStatus WrappedAnchorRaii::TryAcquireAtPose(const ArPose* arPose)
{
    Release();
    return ArSession_acquireNewAnchor(GetArSessionMutable(), arPose, &GetArAnchorMutable());
}

ArStatus WrappedAnchorRaii::TryAcquireAtPose(const UnityXRPose& xrPose)
{
    WrappedPoseRaii wrappedPose = xrPose;
    return TryAcquireAtPose(wrappedPose);
}

ArStatus WrappedAnchorRaii::TryAcquireAtTrackable(ArTrackable* arTrackable, ArPose* arPose)
{
    Release();
	return ArTrackable_acquireNewAnchor(GetArSessionMutable(), arTrackable, arPose, &GetArAnchorMutable());
}

ArStatus WrappedAnchorRaii::TryAcquireAtTrackable(ArTrackable* arTrackable, const UnityXRPose& xrPose)
{
    WrappedPoseRaii wrappedPose = xrPose;
    return TryAcquireAtTrackable(arTrackable, wrappedPose);
}

void WrappedAnchorRaii::AcquireFromList(const ArAnchorList* arAnchorList, int32_t index)
{
    Release();
    ArAnchorList_acquireItem(GetArSession(), arAnchorList, index, &GetArAnchorMutable());
}

void WrappedAnchorRaii::Release()
{
    if (m_ArAnchor != nullptr)
        ArAnchor_release(GetArAnchorMutable());
    m_ArAnchor = nullptr;
}

void WrappedAnchorRaii::AssumeOwnership(ArAnchor*& arAnchor)
{
	Release();
	m_ArAnchor = arAnchor;
	arAnchor = nullptr;
}

ArAnchor* WrappedAnchorRaii::TransferOwnership()
{
	ArAnchor* ret = GetArAnchorMutable();
	m_ArAnchor = nullptr;
	return ret;
}
