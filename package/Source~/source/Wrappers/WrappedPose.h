#pragma once

#include "arcore_c_api.h"

struct UnityXRPose;
struct UnityXRVector3;
struct UnityXRVector4;

class WrappedPose
{
public:
    WrappedPose();
    WrappedPose(const ArPose* arPose);

    operator const ArPose*() const;
    const ArPose* Get() const;

    void GetPose(UnityXRPose& xrPose) const;
    void GetPosition(UnityXRVector3& position) const;
    void GetRotation(UnityXRVector4& rotation) const;

protected:
    const ArPose* m_ArPose;
};

class WrappedPoseMutable : public WrappedPose
{
public:
    WrappedPoseMutable();
    WrappedPoseMutable(ArPose* arPose);

    operator ArPose*();
    ArPose* Get();

protected:
    ArPose*& GetArPoseMutable();
};

class WrappedPoseRaii : public WrappedPoseMutable
{
public:
    WrappedPoseRaii();
    WrappedPoseRaii(const ArPose* arPose);
    WrappedPoseRaii(const UnityXRPose& xrPose);

	WrappedPoseRaii& operator=(const ArPose* arPose);
    WrappedPoseRaii& operator=(const UnityXRPose& xrPose);

    ~WrappedPoseRaii();

private:
    void CopyFrom(const ArPose* arPose);
    void CopyFrom(const UnityXRPose& xrPose);

    void Destroy();
};

const UnityXRPose& GetIdentityPose();
