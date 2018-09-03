#include "MathConversion.h"
#include "WrappedPose.h"
#include <cstring>

template<>
void WrappingBase<ArPose>::CreateOrAcquireDefaultImpl()
{
    float rawIdentity[kGooglePoseArraySize];
    std::memset(rawIdentity, 0, sizeof(rawIdentity));
    rawIdentity[3] = 1.0f;

    ArPose_create(GetArSession(), rawIdentity, &m_Ptr);
}

template<>
void WrappingBase<ArPose>::ReleaseImpl()
{
    ArPose_destroy(m_Ptr);
}

WrappedPose::WrappedPose()
    : WrappingBase<ArPose>()
{
}

WrappedPose::WrappedPose(eWrappedConstruction)
    : WrappingBase<ArPose>()
{
    CreateOrAcquireDefault();
}

WrappedPose::WrappedPose(const UnityXRPose& xrPose)
    : WrappingBase<ArPose>()
{
    CreateOrAcquireDefault();
    CopyFromXrPose(xrPose);
}

WrappedPose& WrappedPose::operator=(const UnityXRPose& xrPose)
{
    CopyFromXrPose(xrPose);
    return *this;
}

void WrappedPose::CreateIdentity()
{
    CreateOrAcquireDefault();
}

void WrappedPose::GetXrPose(UnityXRPose& xrPose)
{
    float rawPose[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_Ptr, rawPose);
    MathConversion::ToUnity(xrPose, rawPose);
}

void WrappedPose::GetPosition(UnityXRVector3& position)
{
    float rawPose[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_Ptr, rawPose);
    MathConversion::ToUnity(position, rawPose + ToIndex(eGooglePose::PositionBegin));
}

void WrappedPose::GetRotation(UnityXRVector4& rotation)
{
    float rawPose[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_Ptr, rawPose);
    MathConversion::ToUnity(rotation, rawPose + ToIndex(eGooglePose::RotationBegin));
}

void WrappedPose::CopyFromXrPose(const UnityXRPose& xrPose)
{
    float rawPose[kGooglePoseArraySize];
    MathConversion::ToGoogle(rawPose, xrPose);

    Release();
    ArPose_create(GetArSession(), rawPose, &m_Ptr);
    InitRefCount();
}
