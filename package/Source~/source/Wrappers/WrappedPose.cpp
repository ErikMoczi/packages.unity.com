#include "MathConversion.h"
#include "WrappedPose.h"
#include <cstring>

#include "Unity/UnityXRTypes.h"

WrappedPose::WrappedPose()
    : m_ArPose(nullptr)
{
}

WrappedPose::WrappedPose(const ArPose* arPose)
    : m_ArPose(arPose)
{
}

WrappedPose::operator const ArPose*() const
{
    return m_ArPose;
}

const ArPose* WrappedPose::Get() const
{
    return m_ArPose;
}

void WrappedPose::GetPose(UnityXRPose& xrPose) const
{
    float googlePoseRaw[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_ArPose, googlePoseRaw);
    MathConversion::ToUnity(xrPose, googlePoseRaw);
}

void WrappedPose::GetPosition(UnityXRVector3& position) const
{
    float googlePoseRaw[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_ArPose, googlePoseRaw);
    MathConversion::ToUnity(position, googlePoseRaw + ToIndex(eGooglePose::PositionBegin));
}

void WrappedPose::GetRotation(UnityXRVector4& rotation) const
{
    float googlePoseRaw[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), m_ArPose, googlePoseRaw);
    MathConversion::ToUnity(rotation, googlePoseRaw + ToIndex(eGooglePose::RotationBegin));
}

WrappedPoseMutable::WrappedPoseMutable()
{
}

WrappedPoseMutable::WrappedPoseMutable(ArPose* arPose)
    : WrappedPose(arPose)
{
}

WrappedPoseMutable::operator ArPose*()
{
    return GetArPoseMutable();
}

ArPose* WrappedPoseMutable::Get()
{
    return GetArPoseMutable();
}

ArPose*& WrappedPoseMutable::GetArPoseMutable()
{
    return *const_cast<ArPose**>(&m_ArPose);
}

WrappedPoseRaii::WrappedPoseRaii()
{
    CopyFrom(GetIdentityPose());
}

WrappedPoseRaii::WrappedPoseRaii(const ArPose* arPose)
{
    CopyFrom(arPose);
}

WrappedPoseRaii::WrappedPoseRaii(const UnityXRPose& xrPose)
{
    CopyFrom(xrPose);
}

WrappedPoseRaii& WrappedPoseRaii::operator=(const ArPose* arPose)
{
    CopyFrom(arPose);
    return *this;
}

WrappedPoseRaii& WrappedPoseRaii::operator=(const UnityXRPose& xrPose)
{
    CopyFrom(xrPose);
    return *this;
}

WrappedPoseRaii::~WrappedPoseRaii()
{
    Destroy();
}

void WrappedPoseRaii::CopyFrom(const ArPose* arPose)
{
    Destroy();

    float googlePoseRaw[kGooglePoseArraySize];
    ArPose_getPoseRaw(GetArSession(), arPose, googlePoseRaw);
    ArPose_create(GetArSession(), googlePoseRaw, &GetArPoseMutable());
}

void WrappedPoseRaii::CopyFrom(const UnityXRPose& xrPose)
{
    Destroy();

    float googlePoseRaw[kGooglePoseArraySize];
    MathConversion::ToGoogle(googlePoseRaw, xrPose);
    ArPose_create(GetArSession(), googlePoseRaw, &GetArPoseMutable());
}

void WrappedPoseRaii::Destroy()
{
    if (m_ArPose == nullptr)
        return;

    ArPose_destroy(GetArPoseMutable());
    m_ArPose = nullptr;
}

static const UnityXRPose kIdentityPose =
{
    { 0.0f, 0.0f, 0.0f },
    { 0.0f, 0.0f, 0.0f, 1.0f }
};

const UnityXRPose& GetIdentityPose()
{
	return kIdentityPose;
}
