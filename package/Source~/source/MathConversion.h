#pragma once

#include "arcore_c_api.h"
#include "mathfu/matrix.h"
#include "mathfu/quaternion.h"
#include "mathfu/vector.h"
#include "Unity/UnityXRTypes.h"
#include "Utility.h"

class MathConversion
{
public:
    // simple type conversion, no coordinate system conversion
    static void ToUnity(UnityXRVector3& xrPosition, const mathfu::Vector<float, 3>& fuPosition);
    static void ToUnity(UnityXRVector4& xrRotation, const mathfu::Quaternion<float>& fuRotation);
    static void ToUnity(UnityXRMatrix4x4& xrMatrix, const mathfu::Matrix<float, 4, 4>& fuMatrix);

    // simple type conversion, no coordinate system conversion
    static void ToMathFu(mathfu::Vector<float, 3>& fuPosition, const UnityXRVector3& xrPosition);
    static void ToMathFu(mathfu::Quaternion<float>& fuRotation, const UnityXRVector4& xrRotation);
    static void ToMathFu(mathfu::Matrix<float, 4, 4>& fuMatrix, const UnityXRMatrix4x4& xrMatrix);

    // converts between coordinate systems
    static void ToUnity(UnityXRVector3& xrPosition, const float* googlePositionRaw);
    static void ToUnity(UnityXRVector4& xrRotation, const float* googleRotationRaw);
    static void ToUnity(UnityXRVector3& xrPosition, UnityXRVector4& xrRotation, const float* googlePoseRaw);
    static void ToUnity(UnityXRPose& xrPose, const float* googlePoseRaw);
    static void ToUnity(UnityXRMatrix4x4& xrMatrix, const float* googleMatrixRaw);
    static void ToUnityProjectionMatrix(UnityXRMatrix4x4& xrProjectionMatrix, const float* googleProjectionMatrixRaw);

    // converts between coordinate systems
    static void ToGoogle(float* googlePositionRaw, const UnityXRVector3& xrPosition);
    static void ToGoogle(float* googleRotationRaw, const UnityXRVector4& xrRotation);
    static void ToGoogle(float* googlePoseRaw, const UnityXRVector3& xrPosition, const UnityXRVector4& xrRotation);
    static void ToGoogle(float* googlePoseRaw, const UnityXRPose& xrPose);
    static void ToGoogle(float* googleMatrixRaw, const UnityXRMatrix4x4& xrMatrix);

    // shortcut methods using Unity types as an in-between
    static void ToMathFu(mathfu::Vector<float, 3>& fuPosition, const float* googlePositionRaw);
    static void ToMathFu(mathfu::Quaternion<float>& fuRotation, const float* googleRotationRaw);
    static void ToMathFu(mathfu::Matrix<float, 4, 4>& fuMatrix, const float* googleMatrixRaw);

    // shortcut methods using Unity types as an in-between
    static void ToGoogle(float* googlePositionRaw, const mathfu::Vector<float, 3>& fuPosition);
    static void ToGoogle(float* googleRotationRaw, const mathfu::Quaternion<float>& fuRotation);
    static void ToGoogle(float* googleMatrixRaw, const mathfu::Matrix<float, 4, 4>& fuMatrix);
};
