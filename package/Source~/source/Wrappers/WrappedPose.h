#pragma once

#include "arcore_c_api.h"
#include "Utility.h"
#include "WrappingBase.h"

struct UnityXRPose;

class WrappedPose : public WrappingBase<ArPose>
{
public:
    WrappedPose();
    WrappedPose(eWrappedConstruction);
    WrappedPose(const UnityXRPose& xrPose);
    WrappedPose& operator=(const UnityXRPose& xrPose);

    void CreateIdentity();

    void GetXrPose(UnityXRPose& xrPose);
    void GetPosition(UnityXRVector3& position);
    void GetRotation(UnityXRVector4& rotation);

private:
    void CopyFromXrPose(const UnityXRPose& xrPose);
};
